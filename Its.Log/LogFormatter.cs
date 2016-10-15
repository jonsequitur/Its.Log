// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Its.Log.Instrumentation.Extensions;

namespace Its.Log.Instrumentation
{
    /// <summary>
    /// Provides methods for formatting objects into log strings.
    /// </summary>
    public static class LogFormatter
    {
        private static Func<Type, bool> autoGenerateForType = t => false;
        private static int defaultListExpansionLimit;
        private static int recursionLimit;
        internal static readonly RecursionCounter RecursionCounter = new RecursionCounter();

        private static readonly ConcurrentDictionary<Type, Action<object, TextWriter>> genericFormatters = new ConcurrentDictionary<Type, Action<object, TextWriter>>();

        /// <summary>
        /// Initializes the <see cref="LogFormatter"/> class.
        /// </summary>
        static LogFormatter()
        {
            ResetToDefault();
        }

        /// <summary>
        /// A factory function called to get a TextWriter for writing out log-formatted objects.
        /// </summary>
        public static Func<TextWriter> CreateWriter = () => new StringWriter(CultureInfo.InvariantCulture);

        internal static ILogTextFormatter TextFormatter = new SingleLineTextFormatter();

        internal static Action<object, TextWriter> Default { get; set; }

        /// <summary>
        /// Gets or sets the limit to the number of items that will be written out in detail from an IEnumerable sequence.
        /// </summary>
        /// <value>
        /// The list expansion limit.
        /// </value>
        public static int ListExpansionLimit
        {
            get
            {
                return defaultListExpansionLimit;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("ListExpansionLimit must be at least 0.");
                }
                defaultListExpansionLimit = value;
            }
        }

        /// <summary>
        /// Gets or sets the string that will be written out for null items.
        /// </summary>
        /// <value>
        /// The null string.
        /// </value>
        public static string NullString;

        /// <summary>
        /// Gets or sets the limit to how many levels the formatter will recurse into an object graph.
        /// </summary>
        /// <value>
        /// The recursion limit.
        /// </value>
        public static int RecursionLimit
        {
            get
            {
                return recursionLimit;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("RecursionLimit must be at least 0.");
                }
                recursionLimit = value;
            }
        }

        internal static event EventHandler Clearing;

        /// <summary>
        /// Resets all formatters and formatter settings to their default values.
        /// </summary>
        public static void ResetToDefault()
        {
            Clearing?.Invoke(null, EventArgs.Empty);

            AutoGenerateForType = t => false;
            ListExpansionLimit = 10;
            RecursionLimit = 6;
            NullString = "[null]";

            RegisterDefaults();
            Default = null;
        }

        /// <summary>
        /// Gets or sets a delegate that is checked when a type is being formatted that not previously been formatted and has no custom formatting rules set. If this delegate returns true, then <see cref="LogFormatter{T}.RegisterForAllMembers" /> is called for that type.
        /// </summary>
        /// <value>
        /// The type being formatted.
        /// </value>
        /// <exception cref="System.ArgumentNullException">value</exception>
        public static Func<Type, bool> AutoGenerateForType
        {
            get
            {
                return autoGenerateForType;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                autoGenerateForType = value;
            }
        }

        /// <summary>
        ///   Formats the specified object using a registered formatter function if available.
        /// </summary>
        /// <param name = "obj">The object to be formatted.</param>
        /// <returns>A string representation of the object.</returns>
        public static string Format(object obj)
        {
            var writer = CreateWriter();
            FormatTo(obj, writer);
            return writer.ToString();
        }

        /// <summary>
        /// Writes a formatted representation of the object to the specified writer.
        /// </summary>
        /// <typeparam name="T">The type of the object being written.</typeparam>
        /// <param name="obj">The object to write.</param>
        /// <param name="writer">The writer.</param>
        public static void FormatTo<T>(this T obj, TextWriter writer)
        {
            var custom = Default;
            if (custom != null)
            {
                custom(obj, writer);
                return;
            }

            if (obj != null)
            {
                var actualType = obj.GetType();
                if (typeof (T) != actualType)
                {
                    // in some cases the generic parameter is Object but the object is of a more specific type, in which case get or add a cached accessor to the more specific Formatter<T>.Format method
                    var genericFormatter =
                        genericFormatters.GetOrAdd(actualType,
                                                   GetGenericFormatterMethod);
                    genericFormatter(obj, writer);
                    return;
                }
            }

            LogFormatter<T>.Format(obj, writer);
        }

        internal static Action<object, TextWriter> GetGenericFormatterMethod(this Type type)
        {
            var methodInfo = typeof (LogFormatter<>)
                .MakeGenericType(type)
                .GetMethod("Format", new[] { type, typeof (TextWriter) });

            var targetParam = Expression.Parameter(typeof (object), "target");
            var writerParam = Expression.Parameter(typeof (TextWriter), "target");

            var methodCallExpr = Expression.Call(null, methodInfo,
                                                 Expression.Convert(targetParam, type),
                                                 writerParam);

            return Expression.Lambda<Action<object, TextWriter>>(methodCallExpr, targetParam, writerParam).Compile();
        }

        // TODO: (Formatter) make Join methods public and expose an override for iteration limit

        internal static void Join(
            IEnumerable list,
            TextWriter writer,
            int? listExpansionLimit = null) =>
                Join(list.Cast<object>(), writer, listExpansionLimit);
        

        internal static void Join<T>(IEnumerable<T> list,
                                     TextWriter writer,
                                     int? listExpansionLimit = null)
        {
            if (list == null)
            {
                writer.Write(NullString);
                return;
            } 

            var i = 0;

            TextFormatter.WriteStartSequence(writer);

            listExpansionLimit = listExpansionLimit ?? LogFormatter<T>.ListExpansionLimit;

            using (var enumerator = list.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (i < listExpansionLimit)
                    {
                        // write out another item in the list
                        if (i > 0)
                        {
                            TextFormatter.WriteSequenceDelimiter(writer);
                        }

                        i++;

                        TextFormatter.WriteStartSequenceItem(writer);

                        enumerator.Current.FormatTo(writer);
                    }
                    else
                    {
                        // write out just a count of the remaining items in the list
                        var difference = list.Count() - i;
                        if (difference > 0)
                        {
                            writer.Write(" ... (");
                            writer.Write(difference);
                            writer.Write(" more)");
                        }
                        break;
                    }
                }
            }

            TextFormatter.WriteEndSequence(writer);
        }

        /// <summary>
        ///   Registers a formatter to be used when formatting instances of a specified type.
        /// </summary>
        public static void Register(Type type, Action<object, TextWriter> formatter)
        {
            var genericRegisterMethod = typeof (LogFormatter<>)
                .MakeGenericType(type)
                .GetMethod("Register", new[] { typeof (Action<,>).MakeGenericType(type, typeof (TextWriter)) });

            genericRegisterMethod.Invoke(null, new object[] { formatter });
        }

        /// <summary>
        ///   Registers a formatter to be used when formatting instances of a specified type.
        /// </summary>
        public static void RegisterForAllMembers(Type type, bool includeInternals = false)
        {
            var genericRegisterMethod = typeof (LogFormatter<>)
                .MakeGenericType(type)
                .GetMethod("RegisterForAllMembers");

            genericRegisterMethod.Invoke(null, new object[] { includeInternals });
        }

        private static void RegisterDefaults()
        {
            RegisterDefaultLogEntryFormatters();

            // common primitive types
            LogFormatter<bool>.Default = (value, writer) => writer.Write(value);
            LogFormatter<byte>.Default = (value, writer) => writer.Write(value);
            LogFormatter<Int16>.Default = (value, writer) => writer.Write(value);
            LogFormatter<Int32>.Default = (value, writer) => writer.Write(value);
            LogFormatter<Int64>.Default = (value, writer) => writer.Write(value);
            LogFormatter<Guid>.Default = (value, writer) => writer.Write(value);
            LogFormatter<Decimal>.Default = (value, writer) => writer.Write(value);
            LogFormatter<Single>.Default = (value, writer) => writer.Write(value);
            LogFormatter<Double>.Default = (value, writer) => writer.Write(value);

            LogFormatter<DateTime>.Default = (value, writer) => writer.Write(value.ToString("u"));
            LogFormatter<DateTimeOffset>.Default = (value, writer) => writer.Write(value.ToString("u"));

            // common complex types
            LogFormatter<KeyValuePair<string, object>>.Default = (pair, writer) =>
            {
                writer.Write(pair.Key);
                TextFormatter.WriteNameValueDelimiter(writer);
                pair.Value.FormatTo(writer);
            };

            LogFormatter<DictionaryEntry>.Default = (pair, writer) =>
            {
                writer.Write(pair.Key);
                TextFormatter.WriteNameValueDelimiter(writer);
                pair.Value.FormatTo(writer);
            };

            LogFormatter<ExpandoObject>.Default = (expando, writer) =>
            {
                TextFormatter.WriteStartObject(writer);
                KeyValuePair<string, object>[] pairs = expando.ToArray();
                int length = pairs.Length;
                for (var i = 0; i < length; i++)
                {
                    KeyValuePair<string, object> pair = pairs[i];
                    writer.Write(pair.Key);
                    TextFormatter.WriteNameValueDelimiter(writer);
                    pair.Value.FormatTo(writer);
                    if (i < length - 1)
                    {
                        TextFormatter.WritePropertyDelimiter(writer);
                    }
                }
                TextFormatter.WriteEndObject(writer);
            };

            LogFormatter<Type>.Default = (type, writer) =>
            {
                var typeName = type.Name;
                if (typeName.Contains("`") && !type.IsAnonymous())
                {
                    writer.Write(typeName.Remove(typeName.IndexOf('`')));
                    writer.Write("<");
                    var genericArguments = type.GetGenericArguments();

                    for (var i = 0; i < genericArguments.Length; i++)
                    {
                        LogFormatter<Type>.Default(genericArguments[i], writer);
                        if (i < genericArguments.Length - 1)
                        {
                            writer.Write(",");
                        }
                    }
                    writer.Write(">");
                }
                else
                {
                    writer.Write(typeName);
                }
            };

            // an additional formatter is needed since typeof(Type) == System.RuntimeType, which is not public
            // ReSharper disable once PossibleMistakenCallToGetType.2
            Register(typeof (Type).GetType(),
                     (obj, writer) => LogFormatter<Type>.Default((Type) obj, writer));

            // supply a formatter for String so that it will not be iterated
            LogFormatter<string>.Default = (s, writer) => writer.Write(s);

            // extensions
            LogFormatter<Counter>.Default = LogFormatter<Counter>.GenerateForAllMembers();
            LogFormatter<Tracing>.Default = LogFormatter<Tracing>.GenerateForAllMembers();

            // Newtonsoft.Json types -- these implement IEnumerable and their default output is not useful, so use their default ToString
            TryRegisterDefault("Newtonsoft.Json.Linq.JArray, Newtonsoft.Json", (obj, writer) => writer.Write(obj));
            TryRegisterDefault("Newtonsoft.Json.Linq.JObject, Newtonsoft.Json", (obj, writer) => writer.Write(obj));
        }

        private static void TryRegisterDefault(string typeName, Action<object, TextWriter> write)
        {
            try
            {
                var type = Type.GetType(typeName);
                if (type != null)
                {
                    Register(type, write);
                }
            }
            catch (Exception exception)
            {
                if (exception.ShouldThrow())
                {
                    throw;
                }
                Log.Write(() => exception, $"An exception occurred while trying to register a formatter for type '{typeName}'.");
            }
        }

        internal static void RegisterDefaultLogEntryFormatters() =>
            LogFormatter<LogEntry>.RegisterForMembers(
                e => e.CallingType,
                e => e.CallingMethod,
                e => e.ElapsedMilliseconds,
                e => e.Category,
                e => e.ExceptionId,
                e => e.Message,
                e => e.Subject,
                e => e.TimeStamp,
                e => e.Params);
    }
}
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Its.Log.Instrumentation.Extensions;

namespace Its.Log.Instrumentation
{
    /// <summary>
    /// Provides formatting functionality for a specific type.
    /// </summary>
    /// <typeparam name="T">The type for which formatting is provided.</typeparam>
    public static class Formatter<T>
    {
        private static Action<T, TextWriter> @default = WriteDefault;
        private static readonly bool isAnonymous = typeof (T).IsAnonymous();
        private static Action<T, TextWriter> Custom;
        private static readonly bool isException = typeof (Exception).IsAssignableFrom(typeof (T));
        private static readonly bool writeHeader = !isAnonymous && typeof (T).BaseType != typeof (Params);
        private static readonly bool isLogEntry = typeof (LogEntry).IsAssignableFrom(typeof (T));
        private static int? listExpansionLimit = null;

        /// <summary>
        /// Initializes the <see cref="Formatter&lt;T&gt;"/> class.
        /// </summary>
        static Formatter()
        {
            Formatter.Clearing += (o, e) => Custom = null;
        }

        public static Action<T, TextWriter> Default
        {
            get
            {
                return @default;
            }
            set
            {
                @default = value;
            }
        }

        /// <summary>
        /// Generates a formatter action that will write out all properties and fields from instances of type <typeparamref name="T" />.
        /// </summary>
        /// <param name="includeInternals">if set to <c>true</c> include internal and private members.</param>
        public static Action<T, TextWriter> GenerateForAllMembers(bool includeInternals = false)
        {
            return CreateCustom(typeof (T).GetAllMembers(includeInternals).ToArray());
        }

        /// <summary>
        /// Generates a formatter action that will write out all properties and fields from instances of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="members">Expressions specifying the members to include in formatting.</param>
        /// <returns></returns>
        public static Action<T, TextWriter> GenerateForMembers(params Expression<Func<T, object>>[] members)
        {
            return CreateCustom(typeof (T).GetMembers(members).ToArray());
        }

        /// <summary>
        /// Registers a formatter to be used when formatting instances of type <typeparamref name="T" />.
        /// </summary>
        public static void Register(Action<T, TextWriter> formatter)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException("formatter");
            }

            if (typeof (T) == typeof (Type))
            {
                // special treatment is needed since typeof(Type) == System.RuntimeType, which is not public
                Formatter.Register(typeof (Type).GetType(), (o, writer) => formatter((T) o, writer));
            }

            Custom = formatter;
        }

        /// <summary>
        /// Registers a formatter to be used when formatting instances of type <typeparamref name="T" />.
        /// </summary>
        public static void Register(Func<T, string> formatter)
        {
            Register((obj, writer) => writer.Write(formatter(obj)));
        }

        /// <summary>
        /// Registers a formatter to be used when formatting instances of type <typeparamref name="T" />.
        /// </summary>
        public static void RegisterForAllMembers(bool includeInternals = false)
        {
            Register(GenerateForAllMembers(includeInternals));
        }

        /// <summary>
        /// Registers a formatter to be used when formatting instances of type <typeparamref name="T" />.
        /// </summary>
        public static void RegisterForMembers(params Expression<Func<T, object>>[] members)
        {
            if (members == null || !members.Any())
            {
                Register(GenerateForAllMembers());
            }
            else
            {
                Register(GenerateForMembers(members));
            }
        }

        internal static string Format(T obj)
        {
            var writer = Formatter.CreateWriter();
            Format(obj, writer);
            return writer.ToString();
        }

        /// <summary>
        /// Formats an object and writes it to a writer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="writer">The writer.</param>
        public static void Format(T obj, TextWriter writer)
        {
            // TODO: (Format) is it worth caching the result of this for LogEntry objects? it could be done directly in the default formatter.
            if (obj == null)
            {
                writer.Write(Formatter.NullString);
                return;
            }

            // find a formatter for the object type, and possibly register one on the fly
            using (Formatter.RecursionCounter.Enter())
            {
                if (Formatter.RecursionCounter.Depth <= Formatter.RecursionLimit)
                {
                    if (Custom == null)
                    {
                        if (isAnonymous || isException)
                        {
                            Custom = GenerateForAllMembers();
                        }
                        else if (isLogEntry)
                        {
                            // TODO: (Format) this shouldn't happen, but it occurs after formatter have been cleared. find a better way to do this. this really only occurs in testing, usually, so not highest priority.
                            typeof (T).GetMethod("RegisterFormatters", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
                        }
                        else if (Default == WriteDefault)
                        {
                            if (Formatter.AutoGenerateForType(typeof (T)))
                            {
                                Custom = GenerateForAllMembers();
                            }
                            else
                            {
                                // short circuit for future checks 
                                Custom = (o, w) => Default(o, w);
                            }
                        }
                    }
                    (Custom ?? @default)(obj, writer);
                }
                else
                {
                    @default(obj, writer);
                }
            }
        }

        /// <summary>
        /// Creates a custom formatter for the specified members.
        /// </summary>
        private static Action<T, TextWriter> CreateCustom(MemberInfo[] forMembers)
        {
            var accessors = forMembers.GetMemberAccessors<T>();

            if (isException)
            {
                // filter out internal values from the Data dictionary, since they're intended to be surfaced in other ways
                var dataAccessor = accessors.SingleOrDefault(a => a.MemberName == "Data");
                if (dataAccessor != null)
                {
                    var originalGetData = dataAccessor.GetValue;
                    dataAccessor.GetValue = e => ((IDictionary) originalGetData(e))
                                                     .Cast<DictionaryEntry>()
                                                     .Where(de => !de.Key.ToString().StartsWith(ExceptionExtensions.ExceptionDataPrefix))
                                                     .ToDictionary(de => de.Key, de => de.Value);
                }

                // replace the default stack trace with the full stack trace when present
                var stackTraceAccessor = accessors.SingleOrDefault(a => a.MemberName == "StackTrace");
                if (stackTraceAccessor != null)
                {
                    stackTraceAccessor.GetValue = e =>
                    {
                        var ex = e as Exception;
                        if (ex.Data.Contains(ExceptionExtensions.FullStackTraceKey))
                        {
                            return ex.Data[ExceptionExtensions.FullStackTraceKey];
                        }
                        return ex.StackTrace;
                    };
                }
            }

            return (target, writer) =>
            {
                Formatter.TextFormatter.WriteStartObject(writer);

                if (writeHeader)
                {
                    var entry = target as LogEntry;

                    if (entry != null)
                    {
                        Formatter.TextFormatter.WriteLogEntryHeader(entry, writer);
                    }
                    else
                    {
                        Formatter<Type>.Format(typeof (T), writer);
                    }

                    Formatter.TextFormatter.WriteEndHeader(writer);
                }

                for (var i = 0; i < accessors.Length; i++)
                {
                    try
                    {
                        var accessor = accessors[i];

                        object value = accessor.GetValue(target);

                        if (accessor.SkipOnNull && value == null)
                        {
                            continue;
                        }

                        Formatter.TextFormatter.WriteStartProperty(writer);
                        writer.Write(accessor.MemberName);
                        Formatter.TextFormatter.WriteNameValueDelimiter(writer);
                        value.FormatTo(writer);
                        Formatter.TextFormatter.WriteEndProperty(writer);

                        if (i < accessors.Length - 1)
                        {
                            Formatter.TextFormatter.WritePropertyDelimiter(writer);
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.RaiseErrorEvent();
                        if (ex.ShouldThrow())
                        {
                            throw;
                        }
                    }
                }

                Formatter.TextFormatter.WriteEndObject(writer);
            };
        }

        /// <summary>
        ///   Gets or sets the limit to the number of items that will be written out in detail from an IEnumerable sequence of <typeparamref name="T" />.
        /// </summary>
        /// <value> The list expansion limit.</value>
        internal static int ListExpansionLimit
        {
            get
            {
                return listExpansionLimit ?? Formatter.ListExpansionLimit;
            }
            set
            {
                listExpansionLimit = value;
            }
        }

        internal static bool IsCustom
        {
            get
            {
                return Custom != null || @default != WriteDefault;
            }
        }

        private static void WriteDefault(T obj, TextWriter writer)
        {
            if (obj is string)
            {
                writer.Write(obj);
                return;
            }

            var enumerable = obj as IEnumerable;
            if (enumerable != null)
            {
                Formatter.Join(enumerable, writer);
            }
            else
            {
                writer.Write(obj.ToString());
            }
        }
    }
}
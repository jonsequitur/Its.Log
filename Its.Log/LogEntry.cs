// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Its.Log.Instrumentation.Extensions;
using Its.Recipes;

namespace Its.Log.Instrumentation
{
    /// <summary>
    /// A log entry and associated information.
    /// </summary>
    [Serializable]
    public class LogEntry
    {
        /// <summary>
        /// The key used to reference the exception ID within <see cref="Exception.Data" />
        /// </summary>
        internal const string ExceptionIdKey = ExceptionExtensions.ExceptionDataPrefix + "ExceptionID__";

        /// <summary>
        /// The creation time of the instance
        /// </summary>
        private DateTime timeStamp = DateTime.UtcNow;

        private string callingMethod = string.Empty;
        private Type callingType;
        private TraceEventType? eventType;
        private AnonymousMethodInfo anonymousMethodInfo;
        internal Dictionary<Type, object> extensions;
        internal List<KeyValuePair<string, object>> info;

        /// <summary>
        /// The message
        /// </summary>
        private string message;

        private object subject;

        /// <summary>
        /// Indicates whether the LogEntry has been posted via Log.Write. This is used to prevent re-posts.
        /// </summary>
        internal bool HasBeenPosted;

        private IEnumerable<object> confirmations ;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogEntry"/> class.
        /// </summary>
        /// <param name="subject">An object of interest to be evaluated for logging.</param>
        public LogEntry(object subject)
        {
            if (subject is LogEntry)
            {
                throw new ArgumentException("The subject of a LogEntry cannot be another LogEntry");
            }

            Subject = subject;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogEntry"/> class.
        /// </summary>
        /// <param name="message">A message to be written to the log.</param>
        public LogEntry(string message) : this((object)message)
        {
            Message = message;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogEntry"/> class.
        /// </summary>
        /// <param name="comment">A message to be written to the log.</param>
        /// <param name="subject">An object of interest to be evaluated for logging.</param>
        public LogEntry(string comment, object subject) : this(subject)
        {
            Message = comment;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogEntry"/> class.
        /// </summary>
        /// <param name="anonymousMethodInfo">A <see cref="AnonymousMethodInfo" /> to be used to extract context information for the <see cref="LogEntry" />.</param>
        internal LogEntry(AnonymousMethodInfo anonymousMethodInfo) : this((object)null)
        {
            AnonymousMethodInfo = anonymousMethodInfo;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogEntry"/> class.
        /// </summary>
        /// <param name="subject">The subject.</param>
        /// <param name="anonymousMethodInfo">A <see cref="AnonymousMethodInfo" /> to be used to extract context information for the <see cref="LogEntry" />.</param>
        internal LogEntry(object subject, AnonymousMethodInfo anonymousMethodInfo) : this(subject)
        {
            AnonymousMethodInfo = anonymousMethodInfo;
        }

        /// <summary>
        /// Gets or sets the <see cref="Type" /> in whose code the log entry was initiated.
        /// </summary>
        /// <value>The calling <see cref="Type" />.</value>
        public virtual Type CallingType
        {
            get
            {
                if (callingType == null && anonymousMethodInfo != null)
                {
                    callingType = anonymousMethodInfo.EnclosingType;
                }
                return callingType;
            }
            private set
            {
                callingType = value;
            }
        }

        /// <summary>
        /// Gets or sets the method from which <see cref="Log.Write" /> was called.
        /// </summary>
        public string CallingMethod
        {
            get
            {
                return callingMethod;
            }
            set
            {
                callingMethod = value;
            }
        }

        /// <summary>
        /// Gets or sets the category.
        /// </summary>
        /// <value>The category.</value>
        [FormatterSkipsOnNull]
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets the closure info.
        /// </summary>
        /// <value>The closure info.</value>
        internal AnonymousMethodInfo AnonymousMethodInfo
        {
            get
            {
                return anonymousMethodInfo;
            }
            set
            {
                if (anonymousMethodInfo != value)
                {
                    anonymousMethodInfo = value;
                    CallingType = anonymousMethodInfo.EnclosingType;
                    CallingMethod = value.EnclosingMethodName;
                }
            }
        }

        /// <summary>
        /// Gets or sets the type of the event.
        /// </summary>
        /// <value>The type of the event.</value>
        public TraceEventType EventType
        {
            get
            {
                return eventType ?? TraceEventType.Information;
            }
            set
            {
                eventType = value;
            }
        }

        /// <summary>
        /// Gets the exception id.
        /// </summary>
        /// <value>The exception id.</value>
        /// <remarks>The exception id is unique per <see cref="Exception" /> instance</remarks>
        [FormatterSkipsOnNull]
        public Guid? ExceptionId { get; private set; }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        /// <value>The message.</value>
        /// <remarks>IfTypeIs not explicitly set, the <see cref="Message"/> will be generated from the <see cref="Subject" />.</remarks>
        [FormatterSkipsOnNull]
        public virtual string Message
        {
            get
            {
                return message = message ??
                                 Subject as string ??
                                 Subject.IfTypeIs<Func<string>>()
                                        .Then(f => f())
                                        .ElseDefault();
            }

            set
            {
                message = value;
            }
        }

        /// <summary>
        /// Gets or sets the object with which the log entry operation is concerned.
        /// </summary>
        [FormatterSkipsOnNull]
        public virtual object Subject
        {
            get
            {
                return subject;
            }
            set
            {
                subject = value;

                var exception = subject as Exception;
                if (exception != null)
                {
                    EnsureExceptionId();

                    if (eventType == null)
                    {
                        if (exception.HasBeenHandled())
                        {
                            eventType = TraceEventType.Warning;
                        }
                        else if (exception.IsFatal())
                        {
                            eventType = TraceEventType.Critical;
                        }
                        else
                        {
                            EventType = TraceEventType.Error;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the time stamp.
        /// </summary>
        /// <value>The time stamp.</value>
        public DateTime TimeStamp
        {
            get
            {
                return timeStamp;
            }
            set
            {
                timeStamp = value;
            }
        }

        /// <summary>
        /// Gets the subject, strongly typed.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type" /> of the subject</typeparam>
        /// <returns>The <see cref="Subject" />, strongly typed</returns>
        public virtual T GetSubject<T>()
        {
            if (Subject is T)
            {
                return (T)Subject;
            }
            return default(T);
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </returns>
        public override string ToString()
        {
            return Formatter<LogEntry>.Format(this);
        }

        /// <summary> 
        /// Adds info to the <see cref="LogEntry"/>.
        /// </summary>
        /// <param name="label">The label.</param>
        /// <param name="value">The value.</param>
        public LogEntry AddInfo(string label, object value)
        {
            if (info == null)
            {
                info = new List<KeyValuePair<string, object>>();
            }

            info.Add(new KeyValuePair<string, object>(label, value));

            return this;
        }

        /// <summary>
        /// Gets an extension of the specified type if it has been applied to this <see cref="LogEntry" />.
        /// </summary>
        /// <typeparam name="TExtension">The <see cref="Type" /> of the extension.</typeparam>
        /// <returns>An instance of <typeparamref name="TExtension" /> if it has been applied to the <see cref="LogEntry" />; otherwise, the default value of <typeparamref name="TExtension" />.</returns>
        public TExtension GetExtension<TExtension>()
        {
            if (extensions == null)
            {
                return default(TExtension);
            }
            object value;
            if (!extensions.TryGetValue(typeof (TExtension), out value))
            {
                return default(TExtension);
            }
            return (TExtension) value;
        }

        /// <summary>
        /// Gets information added via extensions to the log entry.
        /// </summary>
        public IEnumerable<object> Params
        {
            get
            {
                if (extensions != null)
                {
                    foreach (var extension in extensions.Values.OfType<Params>())
                    {
                        yield return extension;
                    }
                }

                if (info != null)
                {
                    foreach (var pair in info)
                    {
                        yield return pair;
                    }
                }
            }
        }

        /// <summary>
        /// If a log entry is part of a log activity, gets the objects passed to Confirm.
        /// </summary>
        [FormatterIgnores]
        public IEnumerable<object> Confirmations
        {
            get
            {
                return confirmations ?? (confirmations = Enumerable.Empty<object>());
            }
            internal set
            {
                confirmations = value;
            }
        }

        [FormatterSkipsOnNull]
        internal IEnumerable<object> Extensions
        {
            get
            {
                if (extensions == null)
                {
                    return null;
                }

                var es = extensions.Values.Except(Params).ToArray();

                if (es.Length == 0)
                {
                    return null;
                }

                return es;
            }
        }

        /// <summary>
        /// If a log entry is part of a log activity, gets the number of milliseconds since the activity started.
        /// </summary>
        [FormatterSkipsOnNull]
        public long? ElapsedMilliseconds
        {
            get
            {
                if (EventType == TraceEventType.Stop || EventType == TraceEventType.Verbose)
                {
                    var stopwatch = GetExtension<Stopwatch>();
                    if (stopwatch != null)
                    {
                        return stopwatch.ElapsedMilliseconds;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Determines whether this instance has an extension of the specific <see cref="Type" /> <typeparamref name="TExtension" />.
        /// </summary>
        /// <typeparam name="TExtension">The <see cref="Type" /> of the extension</typeparam>
        /// <returns>
        /// 	<c>true</c> if this instance has the extension; otherwise, <c>false</c>.
        /// </returns>
        public bool HasExtension<TExtension>() 
            where TExtension : new()
        {
            if (extensions == null)
            {
                return false;
            }
            return extensions.ContainsKey(typeof(TExtension));
        }

        /// <summary>
        /// Gets or adds an extension of the specified type.
        /// </summary>
        /// <typeparam name="TExtension">The type of the extension.</typeparam>
        /// <returns>The extension of type <typeparamref name="TExtension" /> if it has already been added to the log entry; otherwise, a new instsance, which is immediately added to the log entry.</returns>
        public TExtension WithExtension<TExtension>() 
            where TExtension : new()
        {
            if (HasExtension<TExtension>())
            {
                return GetExtension<TExtension>();
            }
            return AddExtension<TExtension>();
        }

        internal TExtension Extend<TExtension>()
            where TExtension : new()
        {
            object extensionObj;
            var type = typeof (TExtension);
            if ((extensions ?? (extensions = new Dictionary<Type, object>())).TryGetValue(type, out extensionObj))
            {
                return (TExtension) extensionObj;
            }

            var extension = new TExtension();
            extensions[type] = extension;
            return extension;
        }

        /// <summary>
        /// Adds the specified extension to the log entry.
        /// </summary>
        /// <typeparam name="TExtension">The type of the extension.</typeparam>
        /// <param name="ext">The extension to be added.</param>
        /// <returns>The same extension instance.</returns>
        public TExtension SetExtension<TExtension>(TExtension ext)
        {
            if (extensions == null)
            {
                extensions = new Dictionary<Type, object>();
            }
            extensions[ext.GetType()] = ext;
            return ext;
        }

        internal TExtension AddExtension<TExtension>() 
            where TExtension : new()
        {
            var ext = new TExtension();
            return SetExtension(ext);
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns>A <see cref="LogEntry" /> which is a clone of this instance.</returns>
        internal virtual LogEntry Clone(bool deep)
        {
            var clone = new LogEntry(Subject)
            {
                info = info,
                Message = Message,
                AnonymousMethodInfo = anonymousMethodInfo,
                extensions = deep || extensions == null
                                 ? extensions
                                 : new Dictionary<Type, object>(extensions)
            };
            return clone;
        }

        /// <summary>
        /// Ensures that the exception id is initialized.
        /// </summary>
        private void EnsureExceptionId()
        {
            if (ExceptionId == null)
            {
                var ex = Subject as Exception;
                while (ex != null)
                {
                    if (ex.Data.Contains(ExceptionIdKey))
                    {
                        ExceptionId = (Guid) ex.Data[ExceptionIdKey];
                    }

                    ex = ex.InnerException;
                }

                if (ExceptionId == null)
                {
                    ExceptionId = Guid.NewGuid();

                    // propagate the exception id back to the exception
                    GetSubject<Exception>().Data[ExceptionIdKey] = ExceptionId;
                }
            }
        }
    }
}
// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Its.Log.Instrumentation
{
    /// <summary>
    /// A log entry and associated information.
    /// </summary>
    public class LogEntry<TSubject> : LogEntry
    {
        private static readonly bool isAnonymous = typeof (TSubject).IsAnonymous();
        private readonly Func<TSubject> subjectAccessor;

        static LogEntry()
        {
            Formatter.Clearing += (o, e) => RegisterFormatters();
            RegisterFormatters();
        }

        internal static void RegisterFormatters()
        {
            if (isAnonymous)
            {
                Formatter<TSubject>.RegisterForMembers();
            }

            // register the default formatter for this LogEntry type, which should be the non-generic LogEntry formatter
            Formatter<LogEntry<TSubject>>.Register((e, writer) => Formatter<LogEntry>.Format(e, writer));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogEntry&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="subject">The subject.</param>
        public LogEntry(TSubject subject) : base(subject)
        {
        }
    
        /// <summary>
        /// Initializes a new instance of the <see cref="LogEntry&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="subjectAccessor">The params accessor.</param>
        public LogEntry(Func<TSubject> subjectAccessor) : base(subjectAccessor.GetAnonymousMethodInfo())
        {
            if (subjectAccessor != null)
            {
                Subject = subjectAccessor();
                if (typeof (TSubject) == typeof (string))
                {
                    Message = subjectAccessor() as string;
                }
            }

            this.subjectAccessor = subjectAccessor;
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </returns>
        public override string ToString()
        {
            return Formatter<LogEntry<TSubject>>.Format(this);
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns>A <see cref="LogEntry" /> which is a clone of this instance.</returns>
        internal override LogEntry Clone(bool deep)
        {
            var clone = new LogEntry<TSubject>(subjectAccessor)
            {
                info = info,
                Message = Message,
                extensions = deep || extensions == null
                                 ? extensions
                                 : new Dictionary<Type, object>(extensions)
            };
            return clone;
        }
    }
}
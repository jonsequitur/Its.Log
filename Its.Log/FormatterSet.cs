// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;

namespace Its.Log.Instrumentation
{
    /// <summary>
    /// Defines a set of conventions per type for formatting objects for log output.
    /// </summary>
    [Obsolete("Please use Formatter instead. FormatterSet will be deprecated in a future release.")]
    public class FormatterSet
    {
        /// <summary>
        /// Gets or sets the limit to the number of items that will be written out in detail from an IEnumerable sequence.
        /// </summary>
        /// <value>
        /// The list expansion limit.
        /// </value>
        public int ListExpansionLimit
        {
            get
            {
                return Formatter.ListExpansionLimit;
            }
            set
            {
                Formatter.ListExpansionLimit = value;
            }
        }

        /// <summary>
        /// Gets or sets the string that will be written out for null items.
        /// </summary>
        /// <value>
        /// The null string.
        /// </value>
        public string NullString
        {
            get
            {
                return Formatter.NullString;
            }
            set
            {
                Formatter.NullString = value ?? "";
            }
        }

        /// <summary>
        /// Gets or sets the limit to how many levels the formatter will recurse into an object graph.
        /// </summary>
        /// <value>
        /// The recursion limit.
        /// </value>
        public int RecursionLimit
        {
            get
            {
                return Formatter.RecursionLimit;
            }
            set
            {
                Formatter.RecursionLimit = value;
            }
        }

        /// <summary>
        ///   Clears all formatters in the <see cref = "FormatterSet" />.
        /// </summary>
        public void Clear() => Formatter.ResetToDefault();

        /// <summary>
        /// Dynamically generates a formatter function that will output the specified properties of objects of type <typeparamref name="T"/> as a string.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> targeted by the formatter function.</typeparam>
        /// <param name="members">An array of MemberExpressions specifying the members to include in formatting.</param>
        /// <returns>
        /// A formatter function.
        /// </returns>
        public Func<T, string> CreateFormatterFor<T>(params Expression<Func<T, object>>[] members)
        {
            var write = Formatter<T>.GenerateForMembers(members);
            return t =>
            {
                var writer = Formatter.CreateWriter();
                write(t, writer);
                return writer.ToString();
            };
        }

        /// <summary>
        ///   Dynamically generates a formatter function that will output the public properties of objects of type <typeparamref name = "T" /> as a string.
        /// </summary>
        /// <typeparam name = "T">The <see cref = "Type" /> targeted by the formatter function.</typeparam>
        /// <returns>A formatter function.</returns>
        public Func<T, string> CreatePropertiesFormatter<T>(bool includeInternals = false)
        {
            var write = Formatter<T>.GenerateForAllMembers(includeInternals);
            return t =>
            {
                var writer = Formatter.CreateWriter();
                write(t, writer);
                return writer.ToString();
            };
        }

        /// <summary>
        ///   Formats the specified object using a registered formatter function if available.
        /// </summary>
        /// <param name = "obj">The object to be formatted.</param>
        /// <returns>A string representation of the object.</returns>
        public string Format<T>(T obj) => obj.ToLogString();

        /// <summary>
        ///   Registers a formatter function for the specified <see cref = "Type" /> <typeparamref name = "T" />.
        /// </summary>
        /// <typeparam name = "T">The <see cref = "Type" /> targeted by the formatter function.</typeparam>
        /// <param name = "format">A function that returs a string representation of instances of <typeparamref name="T" />.</param>
        public FormatterSet RegisterFormatter<T>(Func<T, string> format)
        {
            Formatter<T>.Register(format);
            return this;
        }

        /// <summary>
        /// Registers an auto-generated formatter function for the specified <see cref="Type"/>
        /// 	<typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> targeted by the formatter function.</typeparam>
        /// <param name="members">An array of MemberExpressions specifying the members to include in formatting.</param>
        /// <returns></returns>
        public FormatterSet RegisterPropertiesFormatter<T>(params Expression<Func<T, object>>[] members) =>
            RegisterFormatter(CreateFormatterFor(members));

        /// <summary>
        ///   Registers an auto-generated formatter function for the specified <see cref = "Type" /> <typeparamref name = "T" />.
        /// </summary>
        /// <typeparam name = "T">The <see cref = "Type" /> targeted by the formatter function.</typeparam>
        public FormatterSet RegisterPropertiesFormatter<T>(bool includeInternals = false)
        {
            if (typeof (T) == typeof (string))
            {
                // no one wants to see the string length instead of the string value
                return this;
            }
            return RegisterFormatter(CreatePropertiesFormatter<T>(includeInternals));
        }
    }
}
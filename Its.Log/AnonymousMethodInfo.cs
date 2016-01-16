// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;

namespace Its.Log.Instrumentation
{
    /// <summary>
    /// Gets information relating to an anonymous method.
    /// </summary>
    internal class AnonymousMethodInfo
    {
        private readonly MethodInfo anonymousMethod;

        /// <summary>
        ///   Initializes a new instance of the <see cref = "AnonymousMethodInfo" /> class.
        /// </summary>
        /// <param name = "anonymousMethod">An anonymous method.</param>
        public AnonymousMethodInfo(MethodInfo anonymousMethod)
        {
            if (anonymousMethod == null)
            {
                throw new ArgumentNullException(nameof(anonymousMethod));
            }
            this.anonymousMethod = anonymousMethod;

            var declaringType = this.anonymousMethod.DeclaringType;

            var methodName = this.anonymousMethod.Name;
            var indexOfGt = methodName.IndexOf(">", StringComparison.OrdinalIgnoreCase);

            if (indexOfGt < 0)
            {
                EnclosingType = declaringType;
                EnclosingMethodName = this.anonymousMethod.Name;
                return;
            }

            EnclosingMethodName = methodName.Substring(0, indexOfGt).TrimStart('<');

            if (declaringType.IsCompilerGenerated() &&
                (declaringType
                          ?.DeclaringType
                          ?.IsGenericTypeDefinition ?? false))
            {
                EnclosingType = declaringType
                    .DeclaringType
                    ?.MakeGenericType(declaringType.GenericTypeArguments);
            }
            else
            {
                EnclosingType = declaringType;
            }

            while (EnclosingType?.DeclaringType != null &&
                   EnclosingType.IsCompilerGenerated())
            {
                EnclosingType = EnclosingType.DeclaringType;
            }
        }

        public string MethodName => anonymousMethod.Name;

        /// <summary>
        ///   Gets the name of the enclosing method.
        /// </summary>
        /// <value>The name of the method in which the anonymous method is declared.</value>
        public string EnclosingMethodName { get; }

        /// <summary>
        ///   Gets the type of the enclosing.
        /// </summary>
        /// <value>The name of the class within which the anonymous method is declared.</value>
        public Type EnclosingType { get; }
    }
}
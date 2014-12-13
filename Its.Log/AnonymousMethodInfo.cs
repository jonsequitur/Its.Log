// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
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
        private readonly string enclosingMethodName;
        private readonly Type enclosingType;
        private readonly string enclosingTypeName;

        /// <summary>
        ///   Initializes a new instance of the <see cref = "AnonymousMethodInfo" /> class.
        /// </summary>
        /// <param name = "anonymousMethod">An anonymous method.</param>
        public AnonymousMethodInfo(MethodInfo anonymousMethod)
        {
            if (anonymousMethod == null)
            {
                throw new ArgumentNullException("anonymousMethod");
            }
            this.anonymousMethod = anonymousMethod;

            // TODO: (AnonymousMethodInfo) differentiate overload method signatures
            // TODO: (EnsureMethodAndTypeInfo) this doesn't work with Expressions
            var methodName = this.anonymousMethod.Name;
            var indexOfGt = methodName.IndexOf(">", StringComparison.OrdinalIgnoreCase);

            if (indexOfGt < 0)
            {
                enclosingType = this.anonymousMethod.DeclaringType;
                enclosingMethodName = this.anonymousMethod.Name;
                return;
            }
            
            // it's an anonymous type
            methodName = methodName.Substring(0, indexOfGt).TrimStart('<');
            enclosingType = this.anonymousMethod.DeclaringType;

            enclosingMethodName = methodName;

            var typeName = enclosingType.FullName;
            if (typeName.Contains("+<"))
            {
                typeName = typeName.Substring(0, typeName.IndexOf("+<", StringComparison.OrdinalIgnoreCase));
            }

            while (enclosingType.DeclaringType != null && enclosingType.IsAnonymousMethod())
            {
                enclosingType = enclosingType.DeclaringType;
            }

            enclosingTypeName = typeName;
        }

        public string MethodName
        {
            get
            {
                return anonymousMethod.Name;
            }
        }

        /// <summary>
        ///   Gets the name of the enclosing method.
        /// </summary>
        /// <value>The name of the method in which the anonymous method is declared.</value>
        public string EnclosingMethodName
        {
            get
            {
                return enclosingMethodName;
            }
        }

        /// <summary>
        ///   Gets the type of the enclosing.
        /// </summary>
        /// <value>The name of the class within which the anonymous method is declared.</value>
        public Type EnclosingType
        {
            get
            {
                return enclosingType;
            }
        }

        public string EnclosingTypeName
        {
            get
            {
                return enclosingTypeName;
            }
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}", EnclosingTypeName, EnclosingMethodName);
        }
    }
}
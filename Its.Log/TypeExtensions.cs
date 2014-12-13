// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Its.Log.Instrumentation
{
    [DebuggerStepThrough]
    internal static class TypeExtensions
    {
        public static bool IsAnonymous(this Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            return Attribute.IsDefined(type, typeof (CompilerGeneratedAttribute), false) &&
                   type.IsGenericType && type.Name.Contains("AnonymousType") &&
                   (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$")) &&
                   (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }

        public static bool IsAnonymousMethod(this Type type)
        {
            return Attribute.IsDefined(type, typeof (CompilerGeneratedAttribute), false) &&
                   type.Name.Contains("DisplayClass") &&
                   (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$")) &&
                   (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }

        public static bool IsAsync(this Type type)
        {
            return type == typeof (Task) ||
                   (type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Task<>));
        }
    }
}
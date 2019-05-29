﻿// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

#pragma warning disable 618

namespace Its.Log.Instrumentation
{
    /// <summary>
    /// Provides methods of discovering types within the application.
    /// </summary>
    internal static class Discover
    {
        /// <summary>
        /// Gets concrete types derived from from specified type.
        /// </summary>
        public static IEnumerable<Type> ConcreteTypesDerivedFrom(Type type) =>
            AppDomainTypes()
                .Concrete()
                .DerivedFrom(type);

        /// <summary>
        /// Gets the types derived from the specified type.
        /// </summary>
        public static IEnumerable<Type> DerivedFrom(this IEnumerable<Type> types, Type type) =>
            types.Where(type.IsAssignableFrom);

        /// <summary>
        /// Gets concrete types based on the specified generic type definition.
        /// </summary>
        public static IEnumerable<Type> ConcreteTypesOfGenericInterfaces(params Type[] types) =>
            AppDomainTypes()
                .Concrete()
                .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && types.Contains(i.GetGenericTypeDefinition())));

        /// <summary>
        /// Gets concrete types whose full name matches the specified type name.
        /// </summary>
        /// <remarks>The comparison is case insensitive.</remarks>
        public static IEnumerable<Type> ConcreteTypesNamed(string typeName) =>
            AppDomainTypes()
                .Concrete()
                .Where(t => t.FullName.Equals(typeName, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Gets concrete types, e.g. types that can be instantiated, not interfaces, abstract types, or generic type definitions.
        /// </summary>
        public static IEnumerable<Type> Concrete(this IEnumerable<Type> types) =>
            types
                .Where(t => !t.IsAbstract)
                .Where(t => !t.IsInterface)
                .Where(t => !t.IsGenericTypeDefinition);

        /// <summary>
        /// Returns a sequence of types within the <see cref="AppDomain" />.
        /// </summary>
        public static IEnumerable<Type> AppDomainTypes(bool includeNonPublic = false) =>
            AppDomainAssemblies()
                .Where(a => !a.IsDynamic)
                .Where(a => !a.GlobalAssemblyCache)
                .SelectMany(a =>
                {
                    try
                    {
                        return includeNonPublic
                                   ? a.GetTypes()
                                   : a.GetExportedTypes();
                    }
                    catch (TypeLoadException)
                    {
                    }
                    catch (ReflectionTypeLoadException)
                    {
                    }
                    catch (FileNotFoundException)
                    {
                    }
                    catch (FileLoadException)
                    {
                    }
                    return Enumerable.Empty<Type>();
                });

        internal static Assembly[] AppDomainAssemblies() =>
            AppDomain.CurrentDomain.GetAssemblies();
    }
}
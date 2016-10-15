// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// THIS FILE IS NOT INTENDED TO BE EDITED. 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Its.Recipes
{
#if !RecipesProject
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal static class Discover
    {
        public static IEnumerable<Type> DerivedFrom(this IEnumerable<Type> types, Type type)
        {
            return types.Where(type.IsAssignableFrom);
        }

        public static IEnumerable<Type> ConcreteTypes()
        {
            return AppDomain.CurrentDomain
                            .GetAssemblies()
                            .Where(a => !a.IsDynamic)
                            .Where(a => !a.GlobalAssemblyCache)
                            .SelectMany(a =>
                            {
                                try
                                {
                                    return a.GetExportedTypes();
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
                            })
                            .Where(t => !t.IsAbstract)
                            .Where(t => !t.IsInterface)
                            .Where(t => !t.IsGenericTypeDefinition);
        }
    }
}
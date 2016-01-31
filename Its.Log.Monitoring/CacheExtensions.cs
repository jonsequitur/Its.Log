// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Web.Caching;
using Its.Recipes;

namespace Its.Log.Monitoring
{
    internal static class CacheExtensions
    {
        public static T GetOrAdd<T>(this Cache cache, string key, Func<T> create) =>
            cache.Get(key)
                 .IfTypeIs<T>()
                 .Else(() =>
                 {
                     var value = create();
                     cache.Insert(key,
                                  value,
                                  null,
                                  DateTime.UtcNow.AddMinutes(15),
                                  Cache.NoSlidingExpiration);
                     return value;
                 });
    }
}
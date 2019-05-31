// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Its.Recipes;
using System.Linq;
using System.Reflection;
using System.Web;

#if NETSTANDARD20
using Microsoft.Extensions.Caching.Memory;
#endif

namespace Its.Log.Instrumentation
{
    internal static class DefaultDiagnosticSensors
    {
        private static readonly string appDomainStartTime = DateTimeOffset.Now.ToString("o");
        private static readonly string processStartTime = new DateTimeOffset(Process.GetCurrentProcess().StartTime).ToString("o");
        private static readonly int processId = Process.GetCurrentProcess().Id;

        private static readonly string location = Assembly.GetExecutingAssembly()
                                                          .CodeBase
                                                          .IfNotNull()
                                                          .Then(c => c.Remove(c.LastIndexOf("/", StringComparison.InvariantCultureIgnoreCase)))
                                                          .ElseDefault();
#if NETSTANDARD20
        private static readonly MemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());
#endif

        /// <summary>
        ///   Returns diagnostic information related to the deployed application.
        /// </summary>
        [DiagnosticSensor]
        public static IDictionary<string, object> Application() =>
            new Dictionary<string, object>
            {
                { "Location", location },
                { "Process ID", processId },
                { "AppDomain start time", appDomainStartTime },
                { "Process start time", processStartTime },
            };

        /// <summary>
        ///   Returns diagnostic information related to the runtime environment.
        /// </summary>
        [DiagnosticSensor]
        public static IDictionary<string, object> Environment() =>
            new Dictionary<string, object>
            {
                { ".NET Version", System.Environment.Version.ToString() },
                { "Server", System.Environment.MachineName }
            };

#if NETSTANDARD20
        /// <summary>
        ///   Returns diagnostic information related to the <see cref="MemoryCache" />.
        /// </summary>
        [DiagnosticSensor]
        public static IDictionary<string, object> Cache() =>
            new Dictionary<string, object>
            {
                { "Count", memoryCache.Count },
            };
#else
        /// <summary>
        ///   Returns diagnostic information related to the <see cref="HttpRuntime.Cache" />.
        /// </summary>
        [DiagnosticSensor]
        public static IDictionary<string, object> Cache() =>
            new Dictionary<string, object>
            {
                { "Count", HttpRuntime.Cache.Count },
                { "EffectivePercentagePhysicalMemoryLimit", HttpRuntime.Cache.EffectivePercentagePhysicalMemoryLimit },
                { "EffectivePrivateBytesLimit", HttpRuntime.Cache.EffectivePrivateBytesLimit },
                { "Keys", HttpRuntime.Cache.Cast<DictionaryEntry>().Select(item => item.Key).OrderBy(key => key) },
            };
#endif
    }
}
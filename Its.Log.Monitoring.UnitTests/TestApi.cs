// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Web.Http;

namespace Its.Log.Monitoring.UnitTests
{
    public class TestApi : HttpClient
    {
        public TestApi(Action<TestTargetRegistry> configureTargets = null,
                       params Type[] testTypes) : base(Configure(configureTargets, testTypes))
        {
        }

        private static HttpServer Configure(
            Action<TestTargetRegistry> configureTargets = null,
            params Type[] testTypes)
        {
            if (!testTypes.Any())
            {
                testTypes = null;
            }

            var configuration = new HttpConfiguration();
            configuration.MapTestRoutes(configureTargets, testTypes: testTypes);
            configuration.EnsureInitialized();
            return new HttpServer(configuration);
        }
    }
}
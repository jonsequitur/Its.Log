// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using Its.Recipes;

namespace Its.Log.Monitoring
{
    public class TestTargetRegistry : IEnumerable<TestTarget>
    {
        private readonly HttpConfiguration configuration;
        private readonly IDictionary<string, TestTarget> targets = new Dictionary<string, TestTarget>();

        internal TestTargetRegistry(HttpConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }
            this.configuration = configuration;
        }

        public TestTargetRegistry Add(string environment,
                                      string application,
                                      Uri baseAddress,
                                      Action<TestDependencyRegistry> testDependencies = null)
        {
            if (environment == null)
            {
                throw new ArgumentNullException("environment");
            }
            if (application == null)
            {
                throw new ArgumentNullException("application");
            }
            if (baseAddress == null)
            {
                throw new ArgumentNullException("baseAddress");
            }
            if (!baseAddress.IsAbsoluteUri)
            {
                throw new ArgumentException("Base address must be an absolute URI.");
            }

            var testTarget = new TestTarget
            {
                Application = application,
                Environment = environment,
                BaseAddress = baseAddress
            };

            var container = new PocketContainer()
                .IncludeDependencyResolver(configuration.DependencyResolver)
                .Register(c => new HttpClient())
                .Register(c => testTarget);

            if (testDependencies != null)
            {
                testDependencies(new TestDependencyRegistry((t, func) => container.Register(t, c => func())));
            }

            container.AfterResolve<HttpClient>((c, client) =>
            {
                if (client.BaseAddress == null)
                {
                    client.BaseAddress = testTarget.BaseAddress;
                }
                return client;
            });

            testTarget.ResolveDependency = container.Resolve;

            targets.Add(environment + ":" + application, testTarget);

            return this;
        }

        internal TestTarget Get(string environment, string application)
        {
            var testTarget = TryGet(environment, application);
            if (testTarget != null)
            {
                return testTarget;
            }

            if (!targets.Any(t => t.Value.Environment.Equals(environment, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException(string.Format("Environment '{0}' has not been defined.", environment));
            }

            throw new ArgumentException(string.Format("Environment '{0}' has no application named '{1}' defined.", environment, application));
        }

        internal TestTarget TryGet(string environment, string application)
        {
            TestTarget target;
            if (targets.TryGetValue(environment + ":" + application, out target))
            {
                return target;
            }
            return null;
        }

        public IEnumerator<TestTarget> GetEnumerator()
        {
            return targets.Select(c => c.Value).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
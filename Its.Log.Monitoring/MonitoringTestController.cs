// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Routing;

namespace Its.Log.Monitoring
{
    [TestUiHtmlConfiguration]
    public class MonitoringTestController : ApiController
    {
        [HttpGet]
        public dynamic Tests(string environment = null, string application = null)
        {
            IHttpRoute testRoute;

            if (!Configuration.Routes.TryGetValue(HttpConfigurationExtensions.TestRootRouteName, out testRoute))
            {
                //TODO: add test for this and return not found
                return NotFound();
            }

            var targets = Configuration.TestTargets();

            if (environment != null && !targets.Any(tt => tt.Environment.Equals(environment, StringComparison.OrdinalIgnoreCase)))
            {
                return NotFound();
            }

            if (application != null && !targets.Any(tt => tt.Application.Equals(application, StringComparison.OrdinalIgnoreCase)))
            {
                return NotFound();
            }

            var testDefinitions = Configuration.TestDefinitions()
                                               .Select(t => t.Value);

            var environments = targets
                .Where(tt => environment == null ||
                             tt.Environment.Equals(environment, StringComparison.OrdinalIgnoreCase))
                .Where(tt => application == null ||
                             tt.Application.Equals(application, StringComparison.OrdinalIgnoreCase))
                .Select(tt => new
                {
                    tt.Application,
                    tt.Environment
                })
                .ToArray();

            return new
            {
                Tests = testDefinitions
                    .SelectMany(t =>
                                environments.Select(ea =>
                                                    new Test
                                                    {
                                                        Environment = ea.Environment,
                                                        Application = ea.Application,
                                                        Url = Url.Link(t.RouteName,
                                                                       new
                                                                       {
                                                                           ea.Application, 
                                                                           ea.Environment
                                                                       }),
                                                        Tags = t.Tags
                                                    })
                                            .Where(l => l.Url != null))
            };
        }

        [HttpGet]
        [HttpPost]
        [TracingFilter]
        public async Task<dynamic> Run(string environment, string application, string testName)
        {
            var target = Configuration.TestTargets()
                                      .Get(environment, application);

            var result = Configuration.TestDefinition(testName).Run(ActionContext, target.ResolveDependency);

            if (result is Task)
            {
                return await result;
            }

            return result;
        }
    }
}

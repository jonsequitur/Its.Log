using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Routing;
using Its.Recipes;

namespace Its.Log.Monitoring
{
    internal abstract class TestConstraint : IHttpRouteConstraint
    {
        public TestDefinition TestDefinition { get; set; }

        protected abstract bool Match(TestTarget target, HttpRequestMessage request);

        public bool Match(HttpRequestMessage request,
                          IHttpRoute route,
                          string parameterName,
                          IDictionary<string, object> values,
                          HttpRouteDirection routeDirection)
        {
            var application = values.IfContains("application").And().IfTypeIs<string>().ElseDefault();
            var environment = values.IfContains("environment").And().IfTypeIs<string>().ElseDefault();

            var target = request.Properties
                                .IfContains("MS_RequestContext")
                                .And()
                                .IfTypeIs<HttpRequestContext>()
                                .Then(config => config.Configuration.TestTargets().TryGet(environment, application))
                                .ElseDefault();

            if (target == null)
            {
                return false;
            }

            return Match(target, request);
        }
    }
}
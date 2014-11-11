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
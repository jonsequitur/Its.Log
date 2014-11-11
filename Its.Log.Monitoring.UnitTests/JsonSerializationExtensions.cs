using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace Its.Log.Monitoring.UnitTests
{
    public static class JsonSerializationExtensions
    {
        public static dynamic JsonContent(this HttpResponseMessage response)
        {
            var json = response.Content.ReadAsStringAsync().Result;
            return JToken.Parse(json);
        }
    }
}
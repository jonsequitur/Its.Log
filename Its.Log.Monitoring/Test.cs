using System;
using Newtonsoft.Json;

namespace Its.Log.Monitoring
{
    internal class Test
    {
        public string Application { get; set; }

        public string Environment { get; set; }

        public string Url { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string[] Tags { get; set; }
    }
}
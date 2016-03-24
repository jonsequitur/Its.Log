// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TestParameter[] QueryParameters { get; set; }
    }
}
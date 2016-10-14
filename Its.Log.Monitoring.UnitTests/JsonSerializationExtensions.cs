// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using System.Web.Http;
using Its.Log.Instrumentation;
using Its.Recipes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Its.Log.Monitoring
{
    /// <summary>
    /// Exposes sensors discovered in all loaded assemblies via HTTP endpoints.
    /// </summary>
    [AuthorizeSensors]
    public class SensorController : ApiController
    {
        private static readonly JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        private readonly MediaTypeFormatter formatter = new JsonMediaTypeFormatter
        {
            SerializerSettings = jsonSerializerSettings
        };

        /// <summary>
        /// Reads all sensors.
        /// </summary>
        /// <returns>The values returned by all sensors.</returns>
        public async Task<HttpResponseMessage> Get()
        {
            var readings = DiagnosticSensor.KnownSensors().ToDictionary(s => s.Name, s => s.Read());

            var asyncSensors = readings.Where(pair => pair.Value is Task).ToArray();

            if (asyncSensors.Any())
            {
                await Task.WhenAll(asyncSensors.Select(pair => pair.Value).OfType<Task>());
            }

            foreach (var pair in asyncSensors)
            {
                readings[pair.Key] = ((dynamic) pair.Value).Result;
            }

            // add a self link
            var localPath = ControllerContext.Request.RequestUri.LocalPath;
            var links = new Dictionary<string, string>
            {
                { "self", localPath }
            };

            foreach (var sensorName in readings.Keys)
            {
                links.Add(sensorName, localPath.AppendSegment(sensorName.ToLowerInvariant()));
            }

            readings["_links"] = links;

            return ControllerContext.Request.CreateResponse(HttpStatusCode.OK, readings, formatter);
        }

        /// <summary>
        /// Reads the specified sensor.
        /// </summary>
        /// <param name="name">The name of the sensor to read.</param>
        /// <returns>The value returned by the sensor.</returns>
        public async Task<HttpResponseMessage> Get(string name)
        {
            var sensor = DiagnosticSensor
                .KnownSensors()
                .SingleOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));

            if (sensor == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            var reading = sensor.Read();

            var readingTask = reading as Task;
            if (readingTask != null)
            {
                await readingTask;
                if (readingTask.GetType().GetGenericArguments().First().IsVisible)
                {
                    reading = ((dynamic) readingTask).Result;
                }
                else
                {
                    // this is required to work around the fact that internal types cause dynamic calls to Result to fail. JSON.NET however will happily serialize them, at which point we can retrieve the Result property.
                    var serialized = JsonConvert.SerializeObject(reading, jsonSerializerSettings);
                    reading = JsonConvert.DeserializeObject<dynamic>(serialized).Result;
                }
            }

            var responseCode = reading is Exception
                                   ? HttpStatusCode.InternalServerError
                                   : HttpStatusCode.OK;

            // add a self link
            var localPath = ControllerContext.Request.RequestUri.LocalPath;
            reading
                .IfTypeIs<IDictionary<string, object>>()
                .ThenDo(d => d["_links"] = new { self = localPath })
                .ElseDo(() =>
                {
                    var json = JsonConvert.SerializeObject(reading, jsonSerializerSettings);

                    if (!json.Contains("{"))
                    {
                        json = @"{""value"":" + json + @"}";
                    }

                    var jtoken = JsonConvert.DeserializeObject<JToken>(json);

                    jtoken.IfTypeIs<JObject>()
                          .ThenDo(o => o.Add("_links", new JObject(new JProperty("self", localPath))));

                    reading = jtoken;
                });
            return ControllerContext.Request.CreateResponse(responseCode, reading, formatter);
        }
    }
}
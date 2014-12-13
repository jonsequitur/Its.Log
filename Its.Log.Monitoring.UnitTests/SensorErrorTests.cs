// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using FluentAssertions;
using Its.Log.Instrumentation;
using Its.Recipes;
using NUnit.Framework;

namespace Its.Log.Monitoring.UnitTests
{
    [TestFixture]
    public class SensorErrorTests
    {
        private static HttpClient apiClient;
        private string sensorName;

        public SensorErrorTests()
        {
            var configuration1 = new HttpConfiguration();
            configuration1.MapSensorRoutes(ctx => true);
            var server = new HttpServer(configuration1);
            apiClient = new HttpClient(server);
        }

        [SetUp]
        public void SetUp()
        {
            sensorName = Any.AlphanumericString(10, 20);
        }

        [TearDown]
        public void TearDown()
        {
            DiagnosticSensor.Remove(sensorName);
            TestSensor.GetSensorValue = null;
        }

        [Test]
        public void when_a_specific_sensor_is_requested_and_it_throws_then_it_returns_500_and_the_exception_text_is_in_the_response_body()
        {
            DiagnosticSensor.Register<string>(() => { throw new Exception("oops!"); }, sensorName);

            var result = apiClient.GetAsync("http://blammo.com/sensors/" + sensorName).Result;

            var body = result.Content.ReadAsStringAsync().Result;

            result.StatusCode
                  .Should().Be(HttpStatusCode.InternalServerError);
            body.Should().Contain("oops!");
        }

        [Test]
        public void when_all_sensors_are_requested_and_one_throws_then_it_returns_200_and_the_exception_text_is_in_the_response_body()
        {
            DiagnosticSensor.Register<string>(() => { throw new Exception("oops!"); }, sensorName);

            var result = apiClient.GetAsync("http://blammo.com/sensors/").Result;

            var body = result.Content.ReadAsStringAsync().Result;

            result.StatusCode
                  .Should().Be(HttpStatusCode.OK);
            body.Should().Contain("oops!");
        }

        [Test]
        public void Cyclical_references_in_object_graphs_do_not_cause_serialization_errors()
        {
            DiagnosticSensor.Register(() =>
            {
                var parent = new Node();
                parent.ChildNodes.Add(new Node
                {
                    ChildNodes = new List<Node> { parent }
                });
                return parent;
            }, sensorName);

            apiClient.GetAsync("http://blammo.com/sensors/").Result
                     .StatusCode
                     .Should()
                     .Be(HttpStatusCode.OK);
            apiClient.GetAsync("http://blammo.com/sensors/" + sensorName).Result
                     .StatusCode
                     .Should()
                     .Be(HttpStatusCode.OK);
        }

        public class Node
        {
            public List<Node> ChildNodes = new List<Node>();
        }
    }
}
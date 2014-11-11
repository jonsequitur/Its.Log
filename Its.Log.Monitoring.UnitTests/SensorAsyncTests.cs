using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using FluentAssertions;
using Its.Log.Instrumentation;
using Its.Recipes;
using NUnit.Framework;
using Newtonsoft.Json.Linq;

namespace Its.Log.Monitoring.UnitTests
{
    [TestFixture]
    public class SensorAsyncTests
    {
        private static HttpClient apiClient;
        private string sensorName;

        public SensorAsyncTests()
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
        public void When_a_sensor_returning_a_Task_of_anonymous_type_is_requested_specifically_then_the_Result_is_returned()
        {
            var words = Any.Paragraph(5);
            var sensorResult = new
            {
                words
            };
            DiagnosticSensor.Register(() => Task.Run(() => sensorResult), sensorName);

            dynamic result = JObject.Parse(apiClient.GetStringAsync("http://blammo.com/sensors/" + sensorName).Result);

            ((string) result.words)
                .Should()
                .Be(words);
        }

        [Test]
        public void When_a_sensor_returning_a_Task_is_requested_specifically_then_the_Result_is_returned()
        {
            var words = Any.Paragraph(5);
            DiagnosticSensor.Register(() => Task.Run(() => words), sensorName);

            dynamic result = JObject.Parse(apiClient.GetStringAsync("http://blammo.com/sensors/" + sensorName).Result);

            ((string) result.value)
                .Should()
                .Be(words);
        }

        [Test]
        public void When_all_sensors_are_requested_then_the_Result_values_are_returned_for_those_that_return_Tasks()
        {
            var words = Any.Paragraph(5);
            TestSensor.GetSensorValue = () => Task.Run(() => words);

            var result = apiClient.GetStringAsync("http://blammo.com/sensors/").Result;

            Console.WriteLine(result);

            result.Should()
                  .Contain(string.Format("\"SensorMethod\":\"{0}\"", words));
        }

        [Test]
        public void When_all_sensors_are_requested_and_some_are_slow_async_they_are_combined_with_synchronous_sensors()
        {
            var testSensor = Any.Paragraph(5);
            var dynamicSensor = Any.Paragraph(7);

            TestSensor.GetSensorValue = () => Task.Run(() =>
            {
                Thread.Sleep(Any.Int(3000, 5000));
                return testSensor;
            });

            DiagnosticSensor.Register(() => Task.Run(() =>
            {
                Thread.Sleep(Any.Int(3000, 5000));
                return dynamicSensor;
            }), sensorName);

            var result = apiClient.GetStringAsync("http://blammo.com/sensors/").Result;

            Console.WriteLine(result);

            result.Should()
                  .Contain(string.Format("\"SensorMethod\":\"{0}\"", testSensor))
                  .And
                  .Contain(string.Format("\"{0}\":\"{1}\"", sensorName, dynamicSensor));
        }
    }
}
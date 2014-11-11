using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AssemblyWithMissingDependency;
using FluentAssertions;
using Its.Recipes;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Its.Log.Instrumentation.UnitTests
{
    [TestFixture]
    public class DiagnosticSensorTests
    {
        [TearDown]
        public void TearDown()
        {
            if (StaticTestSensors.Barrier != null)
            {
                StaticTestSensors.Barrier.RemoveParticipants(StaticTestSensors.Barrier.ParticipantCount);
                StaticTestSensors.Barrier = null;
            }
        }

        [Test]
        public void Sensors_can_be_queried_based_on_sensor_name()
        {
            var sensors = DiagnosticSensor.KnownSensors().Where(s => s.Name == "DictionarySensor");

            Assert.That(sensors.Count(), Is.EqualTo(1));
        }

        [Test]
        public void Sensor_methods_can_be_internal()
        {
            var sensors = DiagnosticSensor.KnownSensors().Where(s => s.Name == "InternalSensor");

            Assert.That(sensors.Count(), Is.EqualTo(1));
        }

        [Test]
        public void Sensor_methods_can_be_private()
        {
            var sensors = DiagnosticSensor.KnownSensors().Where(s => s.Name == "PrivateSensor");

            Assert.That(sensors.Count(), Is.EqualTo(1));
        }

        [Test]
        public void Sensor_methods_can_be_static_methods()
        {
            var sensors = DiagnosticSensor.KnownSensors().Where(s => s.DeclaringType == typeof (StaticTestSensors));

            Assert.That(sensors.Count(), Is.AtLeast(1));
        }

        [Test]
        public void Sensor_methods_can_be_instance_methods()
        {
            var sensors = DiagnosticSensor.KnownSensors().Where(s => s.DeclaringType == typeof (TestSensors));

            Assert.That(sensors.Count(), Is.AtLeast(1));
        }

        [Test]
        public void Sensor_info_can_be_queried_based_on_sensor_declaring_type()
        {
            var sensors = DiagnosticSensor.KnownSensors().Where(s => s.DeclaringType == typeof (TestSensors));

            Assert.That(sensors.Count(), Is.AtLeast(2));
        }

        [Test]
        public void Sensors_can_be_queried_based_on_return_type()
        {
            var sensors = DiagnosticSensor.KnownSensors().Where(s => s.ReturnType == typeof (FileInfo));

            Assert.That(sensors.Count(), Is.EqualTo(1));
        }

        [Test]
        public void Sensor_names_can_be_specified_using_DisplayNameAttribute()
        {
            DiagnosticSensor.KnownSensors()
                            .Count(s => s.Name == "custom-name")
                            .Should().Be(1);
        }

        [Test]
        public void Sensor_methods_are_invoked_per_Read_call()
        {
            var first = DiagnosticSensor.KnownSensors().Single(s => s.Name == "CounterSensor").Read();
            var second = DiagnosticSensor.KnownSensors().Single(s => s.Name == "CounterSensor").Read();

            Assert.That(first, Is.Not.EqualTo(second));
        }

        [Test]
        public void When_sensor_throws_an_exception_then_the_exception_is_returned()
        {
            var result = DiagnosticSensor.KnownSensors().Single(s => s.Name == "ExceptionSensor").Read();

            Assert.That(result, Is.InstanceOf<Exception>());
        }

        [Test]
        public void Sensors_can_be_registered_at_runtime()
        {
            var sensorName = MethodBase.GetCurrentMethod().Name;
            DiagnosticSensor.Register(() => "hello", sensorName);

            var sensor = DiagnosticSensor.KnownSensors().Single(s => s.Name == sensorName);

            Assert.That(sensor.DeclaringType, Is.EqualTo(GetType()));
            Assert.That(sensor.ReturnType, Is.EqualTo(typeof (string)));
            Assert.That(sensor.Read(), Is.EqualTo("hello"));
        }

        [Test]
        public void When_registered_sensor_is_anonymous_then_default_name_is_derived_from_method_doing_registration()
        {
            var newGuid = Guid.NewGuid();
            DiagnosticSensor.Register(() => newGuid);

            var sensor = DiagnosticSensor.KnownSensors().Single(s => s.Read().Equals(newGuid));

            Assert.That(sensor.Name, Is.StringContaining(MethodBase.GetCurrentMethod().Name));
        }

        [Test]
        public void When_registered_sensor_is_a_method_then_name_is_derived_from_its_name()
        {
            DiagnosticSensor.Register(SensorNameTester);

            Assert.That(
                DiagnosticSensor.KnownSensors().Count(s => s.Name == "SensorNameTester"),
                Is.EqualTo(1));
        }

        [Test]
        public void When_registered_sensor_is_anonymous_then_DeclaringType_is_the_containing_type()
        {
            var newGuid = Guid.NewGuid();
            DiagnosticSensor.Register(() => newGuid);

            var sensor = DiagnosticSensor.KnownSensors().Single(s => s.Read().Equals(newGuid));

            Assert.That(sensor.DeclaringType, Is.EqualTo(GetType()));
        }

        [Test]
        public void When_registered_sensor_is_a_method_then_DeclaringType_is_the_containing_type()
        {
            DiagnosticSensor.Register(StaticTestSensors.ExceptionSensor);

            var sensor = DiagnosticSensor.KnownSensors().Single(s => s.Name == "ExceptionSensor");

            Assert.That(
                sensor.DeclaringType,
                Is.EqualTo(typeof (StaticTestSensors)));
        }

        [Test]
        public void Registered_sensors_can_be_removed_at_runtime()
        {
            DiagnosticSensor.Register(SensorNameTester);

            Assert.That(
                DiagnosticSensor.KnownSensors().Count(s => s.Name == "SensorNameTester"),
                Is.EqualTo(1));

            DiagnosticSensor.Remove("SensorNameTester");

            Assert.That(
                DiagnosticSensor.KnownSensors().Count(s => s.Name == "SensorNameTester"),
                Is.EqualTo(0));
        }

        [Test]
        public void Discovered_sensors_can_be_removed_at_runtime()
        {
            Assert.That(
                DiagnosticSensor.KnownSensors().Count(s => s.Name == "Sensor_for_Discovered_sensors_can_be_removed_at_runtime"),
                Is.EqualTo(1));

            DiagnosticSensor.Remove("Sensor_for_Discovered_sensors_can_be_removed_at_runtime");

            Assert.That(
                DiagnosticSensor.KnownSensors().Count(s => s.Name == "Sensor_for_Discovered_sensors_can_be_removed_at_runtime"),
                Is.EqualTo(0));
        }

        [Test]
        public void KnownSensors_is_safe_to_modify_while_being_enumerated()
        {
            StaticTestSensors.Barrier = new Barrier(2);
            DiagnosticSensor.Register(() => "hello", MethodBase.GetCurrentMethod().Name);
            new Thread(() =>
            {
                DiagnosticSensor.KnownSensors().Select(s =>
                {
                    Console.WriteLine(s.Name);
                    return s.Read();
                }).ToArray();
            }).Start();

            StaticTestSensors.Barrier.SignalAndWait();

            DiagnosticSensor.Register(SensorNameTester);
            DiagnosticSensor.Remove(MethodBase.GetCurrentMethod().Name);
        }

        [Test]
        public void When_a_sensor_returns_a_Task_then_its_IsAsync_Property_returns_true()
        {
            var sensorName = Any.Paragraph(5);
            DiagnosticSensor.Register(AsyncTask, sensorName);

            var sensor = DiagnosticSensor.KnownSensors().Single(s => s.Name == sensorName);

            sensor.IsAsync.Should().BeTrue();
        }

        [Test]
        public void When_a_sensor_does_not_return_a_Task_then_its_IsAsync_Property_returns_false()
        {
            var sensorName = Any.Paragraph(5);
            DiagnosticSensor.Register(() => "hi!", sensorName);

            var sensor = DiagnosticSensor.KnownSensors().Single(s => s.Name == sensorName);

            sensor.IsAsync.Should().BeFalse();
        }

        [Test]
        public void Missing_assemblies_do_not_cause_sensor_discovery_to_fail()
        {
            // force loading of the problem assembly. this assembly has a dependency which is deliberately not included in the project so that unprotected MEF usage will trigger a TypeLoadException. sensor discovery is intended to ignore this issue and swallow the exception. 
            var o = new ClassWithoutReferenceToMissingAssembly();

            DiagnosticSensor.Discover().Count().Should().BeGreaterThan(0);
        }

        [Test]
        public void ReflectionTypeLoadException_due_to_missing_assembly_is_signalled_by_sensor_output()
        {
            // force loading of the problem assembly. this assembly has a dependency which is deliberately not included in the project so that unprotected MEF usage will trigger a TypeLoadException. sensor discovery is intended to ignore this issue and swallow the exception. 
            var o = new ClassWithoutReferenceToMissingAssembly();

            dynamic reading = DiagnosticSensor.Discover()
                                              .Values
                                              .Select(s => s.Read())
                                              .OfType<ReflectionTypeLoadException>()
                                              .First();

            var loaderException = ((IEnumerable<Exception>) reading.LoaderExceptions).Single();

            loaderException.Message.Should().Contain("Could not load file or assembly 'MissingDependency, Version=1.0.0.0, Culture=neutral, PublicKeyToken=e9ed0c9abcf549e2' or one of its dependencies. The system cannot find the file specified.");
        }

        public static async Task<dynamic> AsyncTask()
        {
            var One = Task.Run(() => "one");

            var Two = Task.Run(() => 2);

            return new
            {
                One = await One,
                Two = await Two
            };
        }

        private object SensorNameTester()
        {
            return new object();
        }
    }

    public class TestSensors
    {
        [DiagnosticSensor]
        public IDictionary<string, object> DictionarySensor()
        {
            return new Dictionary<string, object>
            {
                { "an int", 42 }
            };
        }

        [DiagnosticSensor]
        public FileInfo FileInfoSensor()
        {
            return new FileInfo(@"c:\somefile.txt");
        }
    }

    public static class StaticTestSensors
    {
        private static int callCount = 0;

        public static Barrier Barrier;

        [Export("DiagnosticSensor")]
        internal static object InternalSensor()
        {
            return new object();
        }

        [DiagnosticSensor]
        private static object PrivateSensor()
        {
            return new object();
        }

        [DiagnosticSensor]
        internal static object CounterSensor()
        {
            return callCount++;
        }

        [DiagnosticSensor]
        [DisplayName("custom-name")]
        public static object CustomNamedSensor()
        {
            return new object();
        }

        [DiagnosticSensor]
        public static object ExceptionSensor()
        {
            throw new InvalidOperationException();
        }

        [DiagnosticSensor]
        private static object Sensor_for_Discovered_sensors_can_be_removed_at_runtime()
        {
            return new object();
        }

        [DiagnosticSensor]
        private static object ConcurrencySensor()
        {
            Barrier.SignalAndWait();
            return new object();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FluentAssertions;
using Its.Log.Instrumentation.Extensions;
using Its.Recipes;
using NUnit.Framework;
using Newtonsoft.Json;
using Assert = NUnit.Framework.Assert;

namespace Its.Log.Instrumentation.UnitTests
{
    [TestFixture, Ignore]
    public class JsonSerializationTests
    {
        [TearDown]
        public void TearDown()
        {
            Formatter.ResetToDefault();
        }

        [NUnit.Framework.Ignore("Scenario under development")]
        [Test]
        public void All_Log_entries_can_be_formatted_to_JSON_by_registering_a_formatter_for_LogEntry()
        {
            var serializer = new JsonSerializer
            {
                TypeNameHandling = TypeNameHandling.All,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            var log = "";
            var thereWereErrors = false;

            Formatter<LogEntry>.Register((entry, writer) =>
            {
                serializer.Serialize(writer, entry);
                writer.WriteLine();
            });

            using (Log.Events().Subscribe(e => log += e.ToLogString()))
            using (TestHelper.InternalErrors().Subscribe(e => thereWereErrors = true))
            {
                WriteSomeLogs(log);
            }

            Console.WriteLine(log);

            Assert.That(!thereWereErrors);
            Assert.Fail("Finish this test");
        }

        [NUnit.Framework.Ignore("Scenario under development")]
        [Test]
        public void All_Log_entries_can_be_formatted_to_JSON_by_setting_Formatter_Default()
        {
            var serializer = new JsonSerializer
            {
                TypeNameHandling = TypeNameHandling.All,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            var log = "";
            var thereWereErrors = false;

            Formatter.Default = (obj, writer) =>
            {
                serializer.Serialize(writer, obj);
                writer.WriteLine();
            };

            using (Log.Events().Subscribe(e => log += e.ToLogString()))
            using (TestHelper.InternalErrors().Subscribe(e => thereWereErrors = true))
            {
                WriteSomeLogs(log);
            }

            Console.WriteLine(log);

            Assert.That(!thereWereErrors);
            Assert.Fail("Finish this test");
        }

        [NUnit.Framework.Ignore("Scenario under development")]
        [Test]
        public void Log_entries_can_be_deserialized_from_JSON_as_dynamic_types()
        {
            // TODO (Log_entries_can_be_deserialized_from_JSON_as_dynamic_types) write test
            Assert.Fail("Test not written yet.");
        }

        [Test]
        public void ToLogString_can_be_made_to_output_JSON()
        {
            var serializer = new JsonSerializer
            {
                TypeNameHandling = TypeNameHandling.All,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            Formatter.Default = (o, writer) => serializer.Serialize(writer, o);

            Assert.That(new FileInfo(@"c:\temp\1.log").ToLogString(),
                        Is.EqualTo("{\"$type\":\"System.IO.FileInfo, mscorlib\",\"OriginalPath\":\"c:\\\\temp\\\\1.log\",\"FullPath\":\"c:\\\\temp\\\\1.log\"}"));
        }

        [NUnit.Framework.Ignore("Perf test")]
        [Test]
        public void Perf_benchmarks_for_JSON_versus_formatter()
        {
            // warm up the formatter...
            Log.Write(() => new { i = 9 });

            // native
            Timer.TimeOperation(i => Log.Write(() => new { i }), 1000000, "native");

            // JSON
            var serializer = new JsonSerializer
            {
                TypeNameHandling = TypeNameHandling.All,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            Formatter<LogEntry>.Register((entry, writer) =>
            {
                serializer.Serialize(writer, entry);
                writer.WriteLine();
            });
            Timer.TimeOperation(i => Log.Write(() => new { i }), 1000000, "using JSON.NET");

            // TODO (Perf_benchmarks_for_JSON_versus_formatter) write test
            Assert.Fail("Test not written yet.");
        }

        [Test]
        public void ActivityId_is_a_JSON_property_when_WithParams_Tracing_params_is_called()
        {
            var entries = new List<LogEntry>();
            var activityId = Any.Guid();
            Trace.CorrelationManager.ActivityId = activityId;

            using (Log.Events().Subscribe(entries.Add))
            using (Log.WithParams(() => new Tracing()).Enter(() => { }))
            {
            }

            var json = JsonConvert.SerializeObject(entries);
            Console.WriteLine(json);
            json.Should().Contain("\"ActivityId\":\"" + activityId + "\"");
        }

        [Test]
        public void ActivityId_is_a_JSON_property_when_added_using_WithExtension_Tracing_is_called()
        {
            var activityId = Any.Guid();
            Trace.CorrelationManager.ActivityId = activityId;

            var entry = new LogEntry("hello");
            entry.WithExtension<Tracing>();

            var json = JsonConvert.SerializeObject(entry);

            Console.WriteLine(json);
            json.Should().Contain("\"ActivityId\":\"" + activityId + "\"");
        }

        [Test]
        public void CallingType_is_serialized_as_the_full_type_name()
        {
            // FIX (CallingType_is_serialized_as_the_full_type_name) write test
            Assert.Fail("Test not written yet.");
        }

        [Test]
        public void EventType_can_be_serialized_as_a_string()
        {
            // FIX (EventType_can_be_serialized_as_a_string) write test
            Assert.Fail("Test not written yet.");
        }

        private static void WriteSomeLogs(string log)
        {
            using (Log.Events().Subscribe(e => log += e.ToLogString()))
            {
                var widget = new Widget<string>
                {
                    Parts = Enumerable.Range(1, 3).Select(i => new Part { PartNumber = i.ToString() }).ToList()
                };

                using (var activity = Log.Enter(() => new { widget }))
                {
                    widget.DoStuff();
                    activity.Trace("Done doing stuff");
                    try
                    {
                        widget.DoStuffThatThrows();
                    }
                    catch (Exception ex)
                    {
                        Log.Write(() => ex);
                    }
                }
            }
        }
    }
}
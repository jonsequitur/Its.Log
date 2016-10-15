// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using FluentAssertions;
using System.Linq;
using Its.Log.Instrumentation.Extensions;
using Its.Recipes;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Its.Log.Instrumentation.UnitTests
{
    [TestFixture]
    public class JsonSerializationTests
    {
        [TearDown]
        public void TearDown()
        {
            LogFormatter.ResetToDefault();
        }

        [Ignore("Scenario under development")]
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

            LogFormatter<LogEntry>.Register((entry, writer) =>
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

        [Ignore("Scenario under development")]
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

            LogFormatter.Default = (obj, writer) =>
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

        [Ignore("Scenario under development")]
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

            LogFormatter.Default = (o, writer) => serializer.Serialize(writer, o);

            Assert.That(new FileInfo(@"c:\temp\1.log").ToLogString(),
                        Is.EqualTo("{\"$type\":\"System.IO.FileInfo, mscorlib\",\"OriginalPath\":\"c:\\\\temp\\\\1.log\",\"FullPath\":\"c:\\\\temp\\\\1.log\"}"));
        }

        [Test]
        public void Confirmation_timings_appear_in_JSON()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            using (var activity = Log.Enter(() => { }))
            {
                activity.Confirm(() => "hello");
            }

            var json = JsonConvert.SerializeObject(log.Last());

            json.Should().Match("*hello (@*ms)*");
        }

        [Ignore("Perf test")]
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
            LogFormatter<LogEntry>.Register((entry, writer) =>
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

        [Ignore]
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

        [Ignore]
        [Test]
        public void CallingType_is_serialized_as_the_full_type_name()
        {
            // FIX (CallingType_is_serialized_as_the_full_type_name) write test
            Assert.Fail("Test not written yet.");
        }

        [Ignore]
        [Test]
        public void EventType_can_be_serialized_as_a_string()
        {
            // FIX (EventType_can_be_serialized_as_a_string) write test
            Assert.Fail("Test not written yet.");
        }

        [Test]
        public void Serializer_does_not_emit_private_state_of_Confirmation_objects()
        {
            var log = "";

            using (Log.Events().Subscribe(e => log += JsonConvert.SerializeObject(e)))
            using (var a = Log.Enter(() => { }))
            {
                var response = new SomeObject
                {
                    yes = "this should be in the log output",
                    no = "this should not be in the log output"
                };

                a.Confirm(() => new
                {
                    response.yes
                });
            }
            Console.WriteLine(log);
            log.Should().NotContain("this should not be in the log output");
        }

        public class SomeObject
        {
            public string yes;
            public string no;
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
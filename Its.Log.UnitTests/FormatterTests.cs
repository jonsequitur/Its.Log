// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using FluentAssertions;
using Its.Log.Instrumentation.Extensions;
using Its.Recipes;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Assert = NUnit.Framework.Assert;
using StringAssert = NUnit.Framework.StringAssert;

namespace Its.Log.Instrumentation.UnitTests
{
    [TestFixture]
    public class FormatterTests
    {
        [SetUp]
        public void SetUp()
        {
            Formatter.ResetToDefault();
            Extension.EnableAll();
        }

        [Test]
        public virtual void Generate_creates_a_function_that_emits_the_property_names_and_values_for_a_specific_type()
        {
            var write = Formatter<Widget>.GenerateForAllMembers();

            var writer = new StringWriter();
            write(new Widget { Name = "Bob" }, writer);

            var s = writer.ToString();
            Assert.That(s,
                        Contains.Substring("Name = Bob"));
        }

        [Test]
        public virtual void GenerateFor_creates_a_function_that_emits_the_specified_property_names_and_values_for_a_specific_type()
        {
            Formatter<SomethingWithLotsOfProperties>.RegisterForMembers(
                o => o.DateProperty,
                o => o.StringProperty);

            var s = new SomethingWithLotsOfProperties
            {
                DateProperty = DateTime.MinValue,
                StringProperty = "howdy"
            }.ToLogString();

            Assert.That(s, Contains.Substring("DateProperty = 0001-01-01 00:00:00Z"));
            Assert.That(s, Contains.Substring("StringProperty = howdy"));
            Assert.That(s, !Contains.Substring("IntProperty"));
            Assert.That(s, !Contains.Substring("BoolProperty"));
            Assert.That(s, !Contains.Substring("UriProperty"));
        }

        [Test]
        public void GenerateForMembers_throws_when_an_expression_is_not_a_MemberExpression()
        {
            var ex = Assert.Throws<ArgumentException>(() => Formatter<SomethingWithLotsOfProperties>.GenerateForMembers(
                o => o.DateProperty.ToShortDateString(),
                o => o.StringProperty));

            Assert.That(ex.Message,
                        Contains.Substring("o => o.DateProperty.ToShortDateString()"));
        }

        [Ignore("Scenario under development")]
        [Test]
        public void GenerateForMembers_compiles_expressions_that_are_not_MemberExpressions()
        {
            var node = new Node { Id = "1", Nodes = new[] { new Node { Id = "1.1" }, new Node { Id = "1.2" } } };

            Formatter<Node>.RegisterForAllMembers();

            Console.WriteLine(node.ToLogString());

            Formatter<Node>.GenerateForMembers(
                di => di.Id,
                di => di.Nodes.Select(n => n.Id).ToLogString());

            Assert.That(
                node.ToLogString(),
                Does.Contain("Nodes = {  }"));

            // TODO (GenerateForMembers_compiles_expressions_that_are_not_MemberExpressions) write test
            Assert.Fail("Test not written yet.");
        }

        [Test]
        public void Recursive_formatter_calls_do_not_cause_exceptions()
        {
            var widget = new Widget();
            widget.Parts = new List<Part> { new Part { Widget = widget } };

            Formatter<Widget>.RegisterForMembers();
            Formatter<Part>.RegisterForMembers();

            // this should not throw
            var s = widget.ToLogString();
            Console.WriteLine(s);
        }

        [Test]
        public void Formatter_expands_IEnumerable()
        {
            var list = new List<string> { "this", "that", "the other thing" };

            var formatted = list.ToLogString();

            Assert.That(formatted, Is.EqualTo("{ this, that, the other thing }"));
        }

        [Test]
        public void Formatter_expands_properties_of_ExpandoObjects()
        {
            dynamic expando = new ExpandoObject();
            expando.Name = "socks";
            expando.Parts = null;
            Formatter<Widget>.RegisterForMembers();

            dynamic expandoString = Log.ToLogString(expando);

            Assert.That(expandoString, Is.EqualTo("{ Name = socks | Parts = [null] }"));
        }

        [Test]
        public void Formatter_does_not_expand_string()
        {
            // set up a recursive call, so that the custom formatter will not be used once we go far enough in
            var widget = new Widget();
            widget.Parts = new List<Part> { new Part { Widget = widget } };

            Formatter<Widget>.RegisterForMembers();
            Formatter<Part>.RegisterForMembers();

            // this should not throw
            var s = widget.ToLogString();

            Assert.That(s, !Contains.Substring("Name = { D },{ e }"));
        }

        [Test]
        public void Default_formatter_for_Type_displays_only_the_name()
        {
            Assert.That(GetType().ToLogString(),
                        Is.EqualTo(GetType().Name));
            Assert.That(typeof (FormatterTests).ToLogString(),
                        Is.EqualTo(typeof (FormatterTests).Name));
        }

        [Test]
        public void Default_formatter_for_Type_displays_generic_parameter_name_for_single_parameter_generic_type()
        {
            Assert.That(typeof (List<string>).ToLogString(),
                        Is.EqualTo("List<String>"));
            Assert.That(new List<string>().GetType().ToLogString(),
                        Is.EqualTo("List<String>"));
        }

        [Test]
        public void Default_formatter_for_Type_displays_generic_parameter_name_for_single_parameter_generic_type_when_it_is_LogEntry_CallingType()
        {
            string log = "";
            using (Log.Events().Subscribe(e => log += e.ToLogString()))
            {
                new Widget<string>().DoStuff();
            }

            Assert.That(log, Contains.Substring("CallingType = Widget<String>"));
        }

        [Test]
        public void Default_formatter_for_Type_displays_generic_parameter_name_for_multiple_parameter_generic_type()
        {
            Assert.That(typeof (Dictionary<string, IEnumerable<int>>).ToLogString(),
                        Is.EqualTo("Dictionary<String,IEnumerable<Int32>>"));
        }

        [Test]
        public void Default_formatter_for_Type_displays_generic_parameter_names_for_open_generic_types()
        {
            Assert.That(typeof (IList<>).ToLogString(),
                        Is.EqualTo("IList<T>"));
            Assert.That(typeof (IDictionary<,>).ToLogString(),
                        Is.EqualTo("IDictionary<TKey,TValue>"));
        }

        [Test]
        public void Custom_formatter_for_Type_can_be_registered()
        {
            Formatter<Type>.Register(t => t.GUID.ToString());

            Assert.That(GetType().ToLogString(),
                        Is.EqualTo(GetType().GUID.ToString()));
        }

        [Test]
        public void Default_formatter_for_null_Nullable_indicates_null()
        {
            int? nullable = null;

            var output = nullable.ToLogString();

            Assert.That(output, Is.EqualTo(Log.ToLogString<object>(null)));
        }

        [Test]
        public virtual void Formatter_recursively_formats_types_within_IEnumerable()
        {
            var list = new List<Widget>
            {
                new Widget { Name = "widget x" },
                new Widget { Name = "widget y" },
                new Widget { Name = "widget z" }
            };

            Formatter<Widget>.Register(
                w => w.Name + ", Parts: " +
                     (w.Parts == null ? "0" : w.Parts.Count.ToString()));
            var formatted = list.ToLogString();
            Console.WriteLine(formatted);

            Assert.That(formatted,
                        Is.EqualTo("{ widget x, Parts: 0, widget y, Parts: 0, widget z, Parts: 0 }"));
        }

        [Test]
        public virtual void Formatter_truncates_expansion_of_long_IEnumerable()
        {
            var list = new List<string>();
            for (var i = 1; i < 11; i++)
            {
                list.Add("number " + i);
            }
            Formatter.ListExpansionLimit = 4;

            var formatted = list.ToLogString();

            Assert.IsTrue(formatted.Contains("number 1"));
            Assert.IsTrue(formatted.Contains("number 4"));
            Assert.IsFalse(formatted.Contains("number 5"));
            Assert.IsTrue(formatted.Contains("6 more"));
        }

        [Test]
        public virtual void Formatter_iterates_IEnumerable_property_when_its_actual_type_is_an_array_of_structs()
        {
            Assert.That(
                new[] { 1, 2, 3, 4, 5 }.ToLogString(),
                Is.EqualTo("{ 1, 2, 3, 4, 5 }"));
        }

        [Test]
        public virtual void Formatter_iterates_IEnumerable_property_when_its_actual_type_is_an_array_of_objects()
        {
            Formatter<Node>.RegisterForMembers();

            var node = new Node
            {
                Id = "1",
                Nodes =
                    new[]
                    {
                        new Node { Id = "1.1" },
                        new Node { Id = "1.2" },
                        new Node { Id = "1.3" },
                    }
            };

            var output = node.ToLogString();

            StringAssert.Contains("1.1", output);
            StringAssert.Contains("1.2", output);
            StringAssert.Contains("1.3", output);
        }

        [Test]
        public virtual void Formatter_iterates_IEnumerable_property_when_its_reflected_type_is_array()
        {
            Formatter<Node>.RegisterForMembers();

            var node = new Node
            {
                Id = "1",
                NodesArray =
                    new[]
                    {
                        new Node { Id = "1.1" },
                        new Node { Id = "1.2" },
                        new Node { Id = "1.3" },
                    }
            };

            var output = node.ToLogString();

            StringAssert.Contains("1.1", output);
            StringAssert.Contains("1.2", output);
            StringAssert.Contains("1.3", output);
        }

        [Test]
        public virtual void Default_LogEntry_formatter_can_be_set()
        {
            var log = "";
            Formatter<LogEntry>.Register(e => string.Format("Here's a log entry!"));

            using (Log.Events().Subscribe(e => log += e.ToLogString()))
            {
                Log.Write("hello");
            }

            Assert.That(log, Contains.Substring("Here's a log entry!"));
        }

        [Test]
        public virtual void When_default_LogEntry_formatter_is_changed_it_controls_formatting_of_newly_registered_LogEntry_generic_types()
        {
            var log = "";
            Formatter<LogEntry>.Register(e => "Here's a log entry!");

            using (Log.Events().Subscribe(e => log += e.ToLogString()))
            {
                Log.Write(() => new { hello = "hello" });
            }

            Assert.That(log, Contains.Substring("Here's a log entry!"));
            Assert.That(log, !Contains.Substring("hello"));
        }

        [Test]
        public void Previously_registered_formatters_for_generic_LogEntry_types_use_updated_non_generic_LogEntry_formatter()
        {
            var log = "";

            using (Log.Events().Subscribe(e => log += e.ToLogString()))
            {
                Log.Write(() => new { msg = "one" });
                Formatter<LogEntry>.Register(e => "Here's a log entry!");
                Log.Write(() => new { msg = "two" });
            }

            Assert.That(log, Contains.Substring("one"));
            Assert.That(log, Contains.Substring("Here's a log entry!"));
            Assert.That(log, !Contains.Substring("two"));
        }

        [Test]
        public virtual void GenerateForAllMembers_expands_properties_of_structs()
        {
            var write = Formatter<EntityId>.GenerateForAllMembers();
            var id = new EntityId("the typename", "the id");
            var writer = new StringWriter();

            write(id, writer);

            string value = writer.ToString();
            Assert.That(value,
                        Contains.Substring("TypeName = the typename"));
            Assert.That(value,
                        Contains.Substring("Id = the id"));
        }

        [Test]
        public void Static_fields_are_not_written()
        {
            Formatter<BoundaryTests.NestedWidget>.RegisterForMembers();

            Assert.That(new Widget().ToLogString(),
                        !Contains.Substring("StaticField"));
        }

        [Test]
        public void Static_properties_are_not_written()
        {
            Formatter<BoundaryTests.NestedWidget>.RegisterForMembers();

            Assert.That(new Widget().ToLogString(),
                        !Contains.Substring("StaticProperty"));
        }

        [Test]
        public virtual void GenerateForAllMembers_expands_fields_of_objects()
        {
            var write = Formatter<SomeStruct>.GenerateForAllMembers();
            var today = DateTime.Today;
            var tomorrow = DateTime.Today.AddDays(1);
            var id = new SomeStruct
            {
                DateField = today,
                DateProperty = tomorrow
            };
            var writer = new StringWriter();

            write(id, writer);
            var value = writer.ToString();

            Assert.That(value,
                        Contains.Substring("DateField = "));
            Assert.That(value,
                        Contains.Substring("DateProperty = "));
        }

        [Test]
        public void Exceptions_always_get_properties_formatters()
        {
            var exception = new ReflectionTypeLoadException(
                new[]
                {
                    typeof (FileStyleUriParser),
                    typeof (AssemblyKeyFileAttribute)
                },
                new Exception[]
                {
                    new EventLogInvalidDataException()
                });

            var message = exception.ToLogString();

            Assert.That(message,
                        Contains.Substring("ReflectionTypeLoadException"));
            Assert.That(message,
                        Contains.Substring("FileStyleUriParser"));
            Assert.That(message,
                        Contains.Substring("AssemblyKeyFileAttribute"));
            Assert.That(message,
                        Contains.Substring("EventLogInvalidDataException"));
        }

        [Test]
        public void Exception_Data_is_included_by_default()
        {
            var ex = new InvalidOperationException("oh noes!", new NullReferenceException());
            var key = "a very important int";
            ex.Data[key] = 123456;

            var msg = ex.ToLogString();

            Assert.That(msg, Contains.Substring(key));
            Assert.That(msg, Contains.Substring("123456"));
        }

        [Test]
        public void Exception_StackTrace_is_included_by_default()
        {
            string msg;
            var ex = new InvalidOperationException("oh noes!", new NullReferenceException());

            try
            {
                throw ex;
            }
            catch (Exception thrownException)
            {
                msg = thrownException.ToLogString();
            }

            Assert.That(msg,
                        Contains.Substring(string.Format("StackTrace =    at {0}.{1}", GetType().FullName, MethodInfo.GetCurrentMethod().Name)));
        }

        [Test]
        public void Exception_Type_is_included_by_default()
        {
            var ex = new InvalidOperationException("oh noes!", new NullReferenceException());

            var msg = ex.ToLogString();

            Assert.That(msg,
                        Contains.Substring("InvalidOperationException"));
        }

        [Test]
        public void Exception_Message_is_included_by_default()
        {
            var ex = new InvalidOperationException("oh noes!", new NullReferenceException());

            var msg = ex.ToLogString();

            Assert.That(msg,
                        Contains.Substring("oh noes!"));
        }

        [Test]
        public void Exception_InnerExceptions_are_included_by_default()
        {
            var ex = new InvalidOperationException("oh noes!", new NullReferenceException("oh my.", new DataException("oops!")));

            Assert.That(ex.ToLogString(),
                        Contains.Substring("NullReferenceException"));
            Assert.That(ex.ToLogString(),
                        Contains.Substring("DataException"));
        }

        [Test]
        public void When_a_property_throws_it_does_not_prevent_other_properties_from_being_written()
        {
            var log = new StringBuilder();
            Formatter<SomePropertyThrows>.RegisterForMembers();

            using (Log.Events().Subscribe(e => log.AppendLine(e.ToLogString())))
            using (TestHelper.LogToConsole())
            {
                Log.Write(() => new SomePropertyThrows());
            }

            Assert.That(log.ToString(), Contains.Substring("Ok ="));
            Assert.That(log.ToString(), Contains.Substring("Fine ="));
            Assert.That(log.ToString(), Contains.Substring("PerfectlyFine ="));
        }

        [Test]
        public void Clearing_formatters_reregisters_Params_formatters()
        {
            var log = "";
            using (Log.Events().Subscribe(e => log += e.ToLogString()))
            {
                Log.WithParams(() => new { ints = new[] { 1, 2, 3 } }).Write("1");
                Formatter.ResetToDefault();
                Log.WithParams(() => new { ints = new[] { 4, 5, 6 } }).Write("1");
            }

            Assert.That(log, Contains.Substring("ints = { 1, 2, 3 }"));
            Assert.That(log, Contains.Substring("ints = { 4, 5, 6 }"));
        }

        [Test]
        public void Clearing_formatters_reregisters_LogEntry_formatters()
        {
            var log = "";
            using (Log.Events().Subscribe(e => log += e.ToLogString()))
            {
                Log.Write(() => new { ints = new[] { 1, 2, 3 } });
                Formatter.ResetToDefault();
                Log.Write(() => new { ints = new[] { 4, 5, 6 } });
            }

            Assert.That(log, Contains.Substring("ints = { 1, 2, 3 }"));
            Assert.That(log, Contains.Substring("ints = { 4, 5, 6 }"));
        }

        [Test]
        public void GenerateForAllMembers_can_include_internal_fields()
        {
            var write = Formatter<Node>.GenerateForAllMembers(true);
            var writer = new StringWriter();

            write(new Node { Id = "5" }, writer);

            Assert.That(writer.ToString(), Contains.Substring("_id = 5"));
        }

        [Test]
        public void GenerateForAllMembers_does_not_include_autoproperty_backing_fields()
        {
            var formatter = Formatter<Node>.GenerateForAllMembers(true);
            var writer = new StringWriter();

            formatter(new Node(), writer);

            var output = writer.ToString();
            Assert.That(output, !Contains.Substring("<Nodes>k__BackingField"));
            Assert.That(output, !Contains.Substring("<NodesArray>k__BackingField"));
        }

        [Test]
        public void GenerateForAllMembers_can_include_internal_properties()
        {
            var formatter = Formatter<Node>.GenerateForAllMembers(true);
            var writer = new StringWriter();

            formatter(new Node { Id = "6" }, writer);

            Assert.That(writer.ToString(), Contains.Substring("InternalId = 6"));
        }

        [Test]
        public void When_ResetToDefault_is_called_then_default_formatters_are_immediately_reregistered()
        {
            var logEntry = new LogEntry("hola!");

            var before = logEntry.ToLogString();

            Formatter<LogEntry>.Register(e => "hello!");

            Assert.That(logEntry.ToLogString(),
                        Is.Not.EqualTo(before));

            Formatter.ResetToDefault();

            Assert.That(logEntry.ToLogString(),
                        Is.EqualTo(before));
        }

        [Test]
        public void Anonymous_types_are_automatically_fully_formatted()
        {
            var ints = new[] { 3, 2, 1 };

            var output = new { ints, count = ints.Count() }.ToLogString();

            Assert.That(output, Is.EqualTo("{ ints = { 3, 2, 1 } | count = 3 }"));
        }

        [Test]
        public void ToLogString_uses_actual_type_formatter_and_not_compiled_type()
        {
            Widget widget = new InheritedWidget();
            bool widgetFormatterCalled = false;
            bool inheritedWidgetFormatterCalled = false;

            Formatter<Widget>.Register(w =>
            {
                widgetFormatterCalled = true;
                return "";
            });
            Formatter<InheritedWidget>.Register(w =>
            {
                inheritedWidgetFormatterCalled = true;
                return "";
            });

            widget.ToLogString();

            Assert.That(!widgetFormatterCalled);
            Assert.That(inheritedWidgetFormatterCalled);
        }

        [NUnit.Framework.Ignore("Perf test")]
        [Test]
        public void Perf_experiment_comparing_methods_of_tracking_recursion_depth()
        {
            var threadStaticCounter = new RecursionCounter();

            int iterations = 100000;

            Timer.TimeOperation(i =>
            {
                using (threadStaticCounter.Enter())
                using (threadStaticCounter.Enter())
                using (threadStaticCounter.Enter())
                {
                    Assert.That(threadStaticCounter.Depth, Is.EqualTo(3));
                }
            }, iterations, "simple counter");
        }

        [NUnit.Framework.Ignore("Perf test")]
        [Test]
        public void Perf_experiment_comparing_methods_of_tracking_recursion_depth_across_multiple_threads()
        {
            var simpleCounter = new RecursionCounter();

            int iterations = 1000000;

            Timer.TimeOperation(i =>
            {
                using (simpleCounter.Enter())
                using (simpleCounter.Enter())
                using (simpleCounter.Enter())
                {
                    Assert.That(simpleCounter.Depth, Is.EqualTo(3));
                }
            }, iterations, "simple counter", true);
        }

        [Test]
        public void RecursionCounter_does_not_share_state_across_threads()
        {
            var threads = 15;
            var barrier = new Barrier(threads);
            var counter = new RecursionCounter();

            Enumerable.Range(1, threads).ForEach(i => new Thread(() =>
            {
                Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
                barrier.SignalAndWait(10000);

                Assert.That(counter.Depth, Is.EqualTo(0));
                using (counter.Enter())
                {
                    Assert.That(counter.Depth, Is.EqualTo(1));
                    using (counter.Enter())
                    {
                        Assert.That(counter.Depth, Is.EqualTo(2));
                    }
                }
                Assert.That(counter.Depth, Is.EqualTo(0));
            }).Start());
        }

        [Ignore("Nice to have but currently not working")]
        [Test]
        public void Params_formatters_are_not_subject_recursion_limits()
        {
            Formatter.RecursionLimit = 2;

            using (Formatter.RecursionCounter.Enter())
            using (Formatter.RecursionCounter.Enter())
            using (Formatter.RecursionCounter.Enter())
            {
                var p = new Params<Widget>();
                p.SetAccessor(() => new Widget());
                var writer = new StringWriter();
                Formatter<Params<Widget>>.Format(p, writer);
                Assert.That(writer.ToString(), !Contains.Substring(typeof (Widget).ToString()));
            }
        }

        [Test]
        public void Custom_formatters_can_be_registered_for_types_not_known_until_runtime()
        {
            Formatter.Register(
                type: typeof (FileInfo),
                formatter: (filInfo, writer) => writer.Write("hello"));

            Assert.That(new FileInfo(@"c:\temp\foo.txt").ToLogString(),
                        Is.EqualTo("hello"));
        }

        [Test]
        public void Generated_formatters_can_be_registered_for_types_not_known_until_runtime()
        {
            var obj = new SomethingWithLotsOfProperties
            {
                BoolProperty = true,
                DateProperty = DateTime.Now,
                IntProperty = 42,
                StringProperty = "oh hai",
                UriProperty = new Uri("http://blammo.com")
            };
            var reference = Formatter<SomethingWithLotsOfProperties>.GenerateForAllMembers();
            var writer = new StringWriter();
            reference(obj, writer);

            Formatter.RegisterForAllMembers(typeof (SomethingWithLotsOfProperties));
            
            Assert.That(obj.ToLogString(),
                        Is.EqualTo(writer.ToString()));
        }

        [Test]
        public void When_JObject_is_formatted_it_outputs_its_string_representation()
        {
            JObject jObject = JObject.Parse( JsonConvert.SerializeObject(new
            {
                SomeString = Any.String(10, 100),
                SomeInt = Any.Int()
            }));

            var output = jObject.ToLogString();

            output.Should().Be(jObject.ToString());
        }

        [Test]
        public void When_JArray_is_formatted_it_outputs_its_string_representation()
        {
            JArray jArray = JArray.Parse(JsonConvert.SerializeObject(Enumerable.Range(1, Any.PositiveInt(20)).Select(
                i => new
                {
                    SomeString = Any.String(10, 100),
                    SomeInt = Any.Int()
                }).ToArray()));

            var output = jArray.ToLogString();

            output.Should().Be(jArray.ToString());
        }

        [Test]
        public void ListExpansionLimit_can_be_specified_per_type()
        {
            Formatter<Dictionary<string, int>>.ListExpansionLimit = 1000;
            Formatter.ListExpansionLimit = 4;
            var dictionary = new Dictionary<string, int>
            {
                { "zero", 0 },
                { "two", 2 },
                { "three", 3 },
                { "four", 4 },
                { "five", 5 },
                { "six", 6 },
                { "seven", 7 },
                { "eight", 8 },
                { "nine", 9 },
                { "ninety-nine", 99 }
            };

            var output = dictionary.ToLogString();

            Assert.That(output, Does.Contain("zero"));
            Assert.That(output, Does.Contain("0"));
            Assert.That(output, Does.Contain("ninety-nine"));
            Assert.That(output, Does.Contain("99"));
        }

        [Test]
        public void FormatAllTypes_allows_formatters_to_be_registered_on_fly_for_all_types()
        {
            Formatter.AutoGenerateForType = t => true;

            Assert.That(new FileInfo(@"c:\temp\foo.txt").ToLogString(),
                        Does.Contain(@"DirectoryName = c:\temp"));
            Assert.That(new FileInfo(@"c:\temp\foo.txt").ToLogString(),
                        Does.Contain("Parent = "));
            Assert.That(new FileInfo(@"c:\temp\foo.txt").ToLogString(),
                        Does.Contain("Root = "));
            Assert.That(new FileInfo(@"c:\temp\foo.txt").ToLogString(),
                        Does.Contain("Exists = "));
        }

        [Test]
        public void FormatAllTypes_does_not_reregister_formatters_for_types_having_special_default_formatters()
        {
            var log = "";
            Formatter.AutoGenerateForType = t => true;
            using (Log.Events().Subscribe(e => log += e.ToLogString()))
            {
                Log.Write(() => "hello");
            }

            Assert.That(log, Does.Contain("hello"));
            Assert.That(log, Is.Not.StringContaining("Length"));
        }
    }
}
// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using Its.Log.Instrumentation.Extensions;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using StringAssert = NUnit.Framework.StringAssert;

namespace Its.Log.Instrumentation.UnitTests
{
    [TestFixture]
    public class FormatterSetTests
    {
        [SetUp]
        public void SetUp()
        {
            Log.Formatters.Clear();
            Extension.EnableAll();
        }

        [TearDown]
        public void TearDown()
        {
            Log.Formatters.Clear();
        }

        [Test]
        public void CreatePropertiesFormatter_creates_a_function_that_emits_the_property_names_and_values_for_a_specific_type()
        {
            var func = Log.Formatters.CreatePropertiesFormatter<Widget>();

            var s = func(new Widget { Name = "Bob" });

            Assert.That(s,
                        Contains.Substring("Name = Bob"));
        }

        [Test]
        public void CreateFormatter_creates_a_function_that_emits_the_specified_property_names_and_values_for_a_specific_type()
        {
            var func = Log.Formatters.CreateFormatterFor<SomethingWithLotsOfProperties>(
                o => o.DateProperty,
                o => o.StringProperty);

            var s = func(new SomethingWithLotsOfProperties
            {
                DateProperty = DateTime.MinValue,
                StringProperty = "howdy"
            });
           
            Assert.That(s, Contains.Substring("DateProperty = 0001-01-01 00:00:00Z"));
            Assert.That(s, Contains.Substring("StringProperty = howdy"));
            Assert.That(s, !Contains.Substring("IntProperty"));
            Assert.That(s, !Contains.Substring("BoolProperty"));
            Assert.That(s, !Contains.Substring("UriProperty"));
        }

        [Test]
        public void CreateFormatter_throws_when_an_expression_is_not_a_MemberExpression()
        {
            var formatter = new FormatterSet();

            var ex = Assert.Throws<ArgumentException>(() => formatter.CreateFormatterFor<SomethingWithLotsOfProperties>(
                o => o.DateProperty.ToShortDateString(),
                o => o.StringProperty));

            Assert.That(ex.Message,
                        Contains.Substring("o => o.DateProperty.ToShortDateString()"));
        }

        [Test]
        public void RegisterFormatter_creates_a_function_that_emits_the_specified_property_names_and_values_for_a_specific_type()
        {
            Log.Formatters.RegisterPropertiesFormatter<SomethingWithLotsOfProperties>(
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
        public void RegisterFormatter_throws_when_an_expression_is_not_a_MemberExpression()
        {
            var formatter = new FormatterSet();

            var ex = Assert.Throws<ArgumentException>(() => formatter.RegisterPropertiesFormatter<SomethingWithLotsOfProperties>(
                o => o.DateProperty.ToShortDateString(),
                o => o.StringProperty));

            Assert.That(ex.Message,
                        Contains.Substring("o => o.DateProperty.ToShortDateString()"));
        }

        [Test]
        public void RegisterPropertiesFormatter_for_string_does_nothing()
        {
            var formatter = new FormatterSet();
            formatter.RegisterPropertiesFormatter<string>();

            Assert.That(formatter.Format("hello"),
                        Is.EqualTo("hello"));
        }

        [Test]
        public void CreatePropertiesFormatter_LogEntry()
        {
            var func = Log.Formatters.CreatePropertiesFormatter<LogEntry>();
            var s = func(new LogEntry("message") { Category = "category" });
            Console.WriteLine(s);
            Assert.IsTrue(s.Contains("TimeStamp"));
            Assert.IsTrue(s.Contains("Category"));
            Assert.IsTrue(s.Contains("Message"));
        }

        [Test]
        public void Recursive_formatter_calls_do_not_cause_exceptions()
        {
            var widget = new Widget();
            widget.Parts = new List<Part> { new Part { Widget = widget } };

            Log.Formatters.RegisterPropertiesFormatter<Widget>();
            Log.Formatters.RegisterPropertiesFormatter<Part>();

            // this should not throw
            var s = widget.ToLogString();
            Console.WriteLine(s);
        }

        [Test]
        public void FormatterSet_expands_IEnumerable()
        {
            var list = new List<string> { "this", "that", "the other thing" };

            var formatted = new FormatterSet().Format(list);

            Assert.That(formatted, Is.EqualTo("{ this, that, the other thing }"));
        }

        [Test]
        public void FormatterSet_expands_properties_of_ExpandoObjects()
        {
            dynamic expando = new ExpandoObject();
            expando.Name = "socks";
            expando.Parts = null;
            Log.Formatters.RegisterPropertiesFormatter<Widget>();

            dynamic expandoString = Log.ToLogString(expando);

            Assert.That(expandoString, Is.EqualTo("{ Name = socks | Parts = [null] }"));
        }

        [Test]
        public void FormatterSet_does_not_expand_string()
        {
            var widget = new Widget();
            widget.Parts = new List<Part> { new Part { Widget = widget } };

            Log.Formatters.RegisterPropertiesFormatter<Widget>();
            Log.Formatters.RegisterPropertiesFormatter<Part>();

            // this should not throw
            var s = widget.ToLogString();
            Console.WriteLine(s);

            Assert.IsFalse(s.Contains("Name = { D },{ e }"));
        }

        [Test]
        public void Default_formatter_for_Type_displays_only_the_name()
        {
            var formatter = new FormatterSet();

            Assert.That(formatter.Format(GetType()),
                        Is.EqualTo(GetType().Name));
            Assert.That(formatter.Format(typeof (FormatterSetTests)),
                        Is.EqualTo(typeof (FormatterSetTests).Name));
        }

        [Test]
        public void Custom_formatter_for_Type_can_be_registered()
        {
            var formatter = new FormatterSet();

            formatter.RegisterFormatter<Type>(t => t.GUID.ToString());

            Assert.That(formatter.Format(GetType()),
                        Is.EqualTo(GetType().GUID.ToString()));
        }

        [Test]
        public void Default_formatter_for_null_Nullable_indicates_null()
        {
            var formatter = new FormatterSet();
            int? nullable = null;

            var output = formatter.Format(nullable);

            Assert.That(output, Is.EqualTo(formatter.Format<object>(null)));
        }

        [Test]
        public void FormatterSet_recursively_formats_types_within_IEnumerable()
        {
            var list = new List<Widget>
            {
                new Widget { Name = "widget x" },
                new Widget { Name = "widget y" },
                new Widget { Name = "widget z" }
            };

            var formatted = new FormatterSet()
                .RegisterFormatter<Widget>(
                    w => w.Name + ", Parts: " +
                         (w.Parts == null ? "0" : w.Parts.Count.ToString()))
                .Format(list);
            Console.WriteLine(formatted);

            Assert.That(formatted,
                        Is.EqualTo("{ widget x, Parts: 0, widget y, Parts: 0, widget z, Parts: 0 }"));
        }

        [Test]
        public void FormatterSet_truncates_expansion_of_long_IEnumerable()
        {
            var list = new List<string>();
            for (var i = 1; i < 11; i++)
            {
                list.Add("number " + i);
            }

            var formatterSet = new FormatterSet
            {
                ListExpansionLimit = 4
            };
            var formatted = formatterSet.Format(list);
            Console.WriteLine(formatted);

            Assert.IsTrue(formatted.Contains("number 1"));
            Assert.IsTrue(formatted.Contains("number 4"));
            Assert.IsFalse(formatted.Contains("number 5"));
            Assert.IsTrue(formatted.Contains("6 more"));
        }

        [Test]
        public void Formatter_iterates_IEnumerable_property_when_its_actual_type_is_an_array_of_structs()
        {
            Assert.That(
                new[] { 1, 2, 3, 4, 5 }.ToLogString(),
                Is.EqualTo("{ 1, 2, 3, 4, 5 }"));
        }

        [Test]
        public void Formatter_iterates_IEnumerable_property_when_its_actual_type_is_an_array_of_objects()
        {
            Log.Formatters.RegisterPropertiesFormatter<Node>();

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
        public void Formatter_iterates_IEnumerable_property_when_its_reflected_type_is_array()
        {
            Log.Formatters.RegisterPropertiesFormatter<Node>();

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
        public void Default_LogEntry_formatter_can_be_set()
        {
            var log = "";
            Log.Formatters.RegisterFormatter<LogEntry>(e => string.Format("Here's a log entry!"));

            using (Log.Events().Subscribe(e => log += e.ToLogString()))
            {
                Log.Write("hello");
            }

            Assert.That(log, Contains.Substring("Here's a log entry!"));
        }

        [Test]
        public void When_default_LogEntry_formatter_is_changed_it_controls_formatting_of_newly_registered_LogEntry_generic_types()
        {
            var log = "";
            Log.Formatters.RegisterFormatter<LogEntry>(e => "Here's a log entry!");

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
                Log.Formatters.RegisterFormatter<LogEntry>(e => "Here's a log entry!");
                Log.Write(() => new { msg = "two" });
            }

            Assert.That(log, Contains.Substring("one"));
            Assert.That(log, Contains.Substring("Here's a log entry!"));
            Assert.That(log, !Contains.Substring("two"));
        }

        [Test]
        public void CreatePropertiesFormatter_expands_properties_of_structs()
        {
            var func = new FormatterSet().CreatePropertiesFormatter<EntityId>();

            var id = new EntityId("the typename", "the id");
            var value = func(id);

            Console.WriteLine(value);

            Assert.That(value,
                        Contains.Substring("TypeName = the typename"));
            Assert.That(value,
                        Contains.Substring("Id = the id"));
        }

        [Test]
        public void Static_fields_are_not_written()
        {
            var formatter = new FormatterSet();
            formatter.RegisterPropertiesFormatter<BoundaryTests.NestedWidget>();

            Assert.That(formatter.Format(new Widget()),
                        !Contains.Substring("StaticField"));
        }

        [Test]
        public void Static_properties_are_not_written()
        {
            var formatter = new FormatterSet();
            formatter.RegisterPropertiesFormatter<BoundaryTests.NestedWidget>();

            Assert.That(formatter.Format(new Widget()),
                        !Contains.Substring("StaticProperty"));
        }

        [Test]
        public void CreatePropertiesFormatter_expands_fields_of_objects()
        {
            var func = new FormatterSet().CreatePropertiesFormatter<SomeStruct>();
            var today = DateTime.Today;
            var tomorrow = DateTime.Today.AddDays(1);
            var id = new SomeStruct
            {
                DateField = today,
                DateProperty = tomorrow
            };

            var value = func(id);

            Console.WriteLine(value);

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
            var formatter = new FormatterSet();

            var message = formatter.Format(exception);

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
                        Contains.Substring("StackTrace =    at Its.Log.Instrumentation.UnitTests.FormatterSetTests.Exception_StackTrace_is_included_by_default() "));
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
            Log.Formatters.RegisterPropertiesFormatter<SomePropertyThrows>();

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
                Log.Formatters.Clear();
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
                Log.Formatters.Clear();
                Log.Write(() => new { ints = new[] { 4, 5, 6 } });
            }

            Assert.That(log, Contains.Substring("ints = { 1, 2, 3 }"));
            Assert.That(log, Contains.Substring("ints = { 4, 5, 6 }"));
        }

        [Test]
        public void CreatePropertiesFormatter_can_include_internal_fields()
        {
            var formatter = new FormatterSet().CreatePropertiesFormatter<Node>(true);

            var output = formatter(new Node { Id = "5" });

            Assert.That(output, Contains.Substring("_id = 5"));
        }

        [Test]
        public void CreatePropertiesFormatter_does_not_include_autoproperty_backing_fields()
        {
            var formatter = new FormatterSet().CreatePropertiesFormatter<Node>(true);

            var output = formatter(new Node());

            Assert.That(output, !Contains.Substring("<Nodes>k__BackingField"));
            Assert.That(output, !Contains.Substring("<NodesArray>k__BackingField"));
        }

        [Test]
        public void CreatePropertiesFormatter_can_include_internal_properties()
        {
            var formatter = new FormatterSet().CreatePropertiesFormatter<Node>(true);

            var output = formatter(new Node { Id = "6" });

            Assert.That(output, Contains.Substring("InternalId = 6"));
        }

        [Test]
        public void When_Clear_is_called_then_default_formatters_are_immediately_reregistered()
        {
            var formatterSet = new FormatterSet();
            var logEntry = new LogEntry("hola!");

            var before = formatterSet.Format(logEntry);

            formatterSet.RegisterFormatter<LogEntry>(e => "hello!");

            Assert.That(formatterSet.Format(logEntry),
                        Is.Not.EqualTo(before));

            formatterSet.Clear();

            Assert.That(formatterSet.Format(logEntry),
                        Is.EqualTo(before));
        }

        [Test]
        public void Anonymous_types_are_automatically_fully_formatted()
        {
            var formatter = new FormatterSet();
            var ints = new[] { 3, 2, 1 };
            var output = formatter.Format(new { ints, count = ints.Count() });

            Assert.That(output, Is.EqualTo("{ ints = { 3, 2, 1 } | count = 3 }"));
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
    }
}
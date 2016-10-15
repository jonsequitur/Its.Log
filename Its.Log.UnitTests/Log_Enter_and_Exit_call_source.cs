// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Its.Log.Instrumentation.Extensions;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Its.Log.Instrumentation.UnitTests
{
    [TestFixture]
    public class Log_Enter_and_Exit_call_source
    {
        [SetUp]
        public void TestInitialize()
        {
            Extension.EnableAll();
            Counter.ResetAll();
            Trace.CorrelationManager.ActivityId = Guid.Empty;
        }

        [Test]
        public void from_instance_method_in_derived_class_show_correct_CallingMethod()
        {
            var entries = new List<LogEntry>();

            using (Log.Events().Subscribe(entries.Add))
            {
                var widget = new InheritedWidget();
                widget.DoStuff();
            }

            Assert.That(entries.Single(e => e.EventType == TraceEventType.Start).CallingMethod,
                        Is.EqualTo("DoStuff"));

            Assert.That(entries.Single(e => e.EventType == TraceEventType.Stop).CallingMethod,
                        Is.EqualTo("DoStuff"));
        }

        [Test]
        public void from_instance_method_in_derived_class_show_correct_CallingType()
        {
            var entries = new List<LogEntry>();

            using (Log.Events().Subscribe(entries.Add))
            {
                var widget = new InheritedWidget();
                widget.DoStuff();
            }

            Assert.That(entries.Single(e => e.EventType == TraceEventType.Start).CallingType,
                        Is.EqualTo(typeof (InheritedWidget)));
            Assert.That(entries.Single(e => e.EventType == TraceEventType.Stop).CallingType,
                        Is.EqualTo(typeof (InheritedWidget)));
        }

        [Test]
        public void from_instance_method_in_nested_class_show_correct_CallingMethod()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                var widget = new BoundaryTests.NestedWidget();
                widget.DoStuff();
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingMethod,
                        Is.EqualTo("DoStuff"));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingMethod,
                        Is.EqualTo("DoStuff"));
        }

        [Test]
        public void from_instance_method_in_nested_class_show_correct_CallingType()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                var widget = new BoundaryTests.NestedWidget();
                widget.DoStuff();
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingType,
                        Is.EqualTo(typeof (BoundaryTests.NestedWidget)));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingType,
                        Is.EqualTo(typeof (BoundaryTests.NestedWidget)));
        }

        [Test]
        public void from_instance_method_show_correct_CallingMethod()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                var widget = new Widget();
                widget.DoStuff("hello");
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingMethod,
                        Is.EqualTo("DoStuff"));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingMethod,
                        Is.EqualTo("DoStuff"));
        }

        [Test]
        public void from_instance_method_show_correct_CallingType()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                var widget = new Widget();
                widget.DoStuff("hello");
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingType,
                        Is.EqualTo(typeof (Widget)));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingType,
                        Is.EqualTo(typeof (Widget)));
        }

        [Test]
        public void from_static_method_in_nested_class_show_correct_CallingMethod()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                BoundaryTests.NestedWidget.DoStuffStatically();
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingMethod,
                        Is.EqualTo("DoStuffStatically"));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingMethod,
                        Is.EqualTo("DoStuffStatically"));
        }

        [Test]
        public void from_static_method_in_nested_class_show_correct_CallingType()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                BoundaryTests.NestedWidget.DoStuffStatically();
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingType,
                        Is.EqualTo(typeof (BoundaryTests.NestedWidget)));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingType,
                        Is.EqualTo(typeof (BoundaryTests.NestedWidget)));
        }

        [Test]
        public void from_static_method_show_correct_CallingMethod()
        {
            string param = Guid.NewGuid().ToString();
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                Widget.DoStuffStatically(param);
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingMethod,
                        Is.EqualTo("DoStuffStatically"));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingMethod,
                        Is.EqualTo("DoStuffStatically"));
        }

        [Test]
        public void from_static_method_show_correct_CallingType()
        {
            string param = Guid.NewGuid().ToString();
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                Widget.DoStuffStatically(param);
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingType,
                        Is.EqualTo(typeof (Widget)));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingType,
                        Is.EqualTo(typeof (Widget)));
        }

        [Test]
        public void from_static_method_in_derived_class_show_correct_CallingMethod()
        {
            string param = Guid.NewGuid().ToString();
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                InheritedWidget.DoStuffStatically(param);
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingMethod,
                        Is.EqualTo("DoStuffStatically"));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingMethod,
                        Is.EqualTo("DoStuffStatically"));
        }

        [Test]
        public void from_static_method_in_derived_class_show_correct_CallingType()
        {
            string param = Guid.NewGuid().ToString();
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                InheritedWidget.DoStuffStatically(param);
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingType,
                        Is.EqualTo(typeof (Widget)));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingType,
                        Is.EqualTo(typeof (Widget)));
        }

        [Test]
        public void from_local_anonymous_delegate_in_non_static_class_show_correct_CallingMethod()
        {
            var log = new List<LogEntry>();
            var action = new Action(() =>
            {
                using (Log.Enter(() => { }))
                {
                }
            });

            using (Log.Events().Subscribe(log.Add))
            {
                action();
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingMethod,
                        Is.EqualTo(MethodBase.GetCurrentMethod().Name));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingMethod,
                        Is.EqualTo(MethodBase.GetCurrentMethod().Name));
        }

        [Test]
        public void from_local_anonymous_delegate_in_non_static_class_show_correct_CallingType()
        {
            var log = new List<LogEntry>();
            var action = new Action(() =>
            {
                using (Log.Enter(() => { }))
                {
                }
            });

            using (Log.Events().Subscribe(log.Add))
            {
                action();
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingType,
                        Is.EqualTo(typeof (Log_Enter_and_Exit_call_source)));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingType,
                        Is.EqualTo(typeof (Log_Enter_and_Exit_call_source)));
        }

        [Test]
        public void from_local_anonymous_delegate_in_static_class_show_correct_CallingMethod()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                new Widget().ExtensionWithLoggingInLocallyScopedLambda();
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingMethod,
                        Is.EqualTo("ExtensionWithLoggingInLocallyScopedLambda"));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingMethod,
                        Is.EqualTo("ExtensionWithLoggingInLocallyScopedLambda"));
        }

        [Test]
        public void from_local_anonymous_delegate_in_static_class_show_correct_CallingType()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                new Widget().ExtensionWithLoggingInLocallyScopedLambda();
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingType,
                        Is.EqualTo(typeof (WidgetExtensions)));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingType,
                        Is.EqualTo(typeof (WidgetExtensions)));
        }
        
        [Test]
        public void from_static_anonymous_delegate_in_static_class_show_ctor_as_CallingMethod()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                new Widget().ExtensionWithLoggingInStaticallyScopedLambda();
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingMethod,
                        Is.EqualTo(".cctor"));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingMethod,
                        Is.EqualTo(".cctor"));
        }

        [Test]
        public void from_static_anonymous_delegate_in_static_class_show_correct_CallingType()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                new Widget().ExtensionWithLoggingInLocallyScopedLambda();
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingType,
                        Is.EqualTo(typeof (WidgetExtensions)));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingType,
                        Is.EqualTo(typeof (WidgetExtensions)));
        }

        [Test]
        public void from_class_nested_within_a_generic_class_shows_correct_CallingType()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                Console.WriteLine(new Generic<int>.Nested().Value);
            }

            log.Should().OnlyContain(e => e.CallingType == typeof (Generic<int>.Nested));
        }

        [Test]
        public void from_class_nested_within_a_generic_class_shows_correct_CallingMethod()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                Console.WriteLine(new Generic<int>.Nested().Value);
            }

            log.Should().OnlyContain(e => e.CallingMethod == "get_Value");
        }

        public class Generic<T>
        {
            public class Nested
            {
                private int value;

                public int Value
                {
                    get
                    {
                        using (Log.Enter(() => {}))
                        {
                            return value;
                        }
                    }
                }
            }
        }
    }
}
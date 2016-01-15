// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Its.Log.Instrumentation.Extensions;
using NUnit.Framework;

namespace Its.Log.Instrumentation.UnitTests
{
    [TestFixture]
    public class Log_Enter_and_Exit_call_source_async
    {
        [SetUp]
        public void TestInitialize()
        {
            Extension.EnableAll();
            Counter.ResetAll();
            Trace.CorrelationManager.ActivityId = Guid.Empty;
        }

        [Test]
        public async Task from_instance_method_in_derived_class_show_correct_CallingMethod()
        {
            var entries = new List<LogEntry>();

            using (Log.Events().Subscribe(entries.Add))
            {
                var widget = new InheritedWidget();
                await widget.DoStuffAsync();
            }

            Assert.That(entries.Single(e => e.EventType == TraceEventType.Start).CallingMethod,
                        Is.EqualTo("DoStuffAsync"));

            Assert.That(entries.Single(e => e.EventType == TraceEventType.Stop).CallingMethod,
                        Is.EqualTo("DoStuffAsync"));
        }

        [Test]
        public async Task from_instance_method_in_derived_class_show_correct_CallingType()
        {
            var entries = new List<LogEntry>();

            using (Log.Events().Subscribe(entries.Add))
            {
                var widget = new InheritedWidget();
                await widget.DoStuffAsync();
            }

            Assert.That(entries.Single(e => e.EventType == TraceEventType.Start).CallingType,
                        Is.EqualTo(typeof (InheritedWidget)));

            Assert.That(entries.Single(e => e.EventType == TraceEventType.Stop).CallingType,
                        Is.EqualTo(typeof (InheritedWidget)));
        }

        [Test]
        public async Task from_instance_method_in_nested_class_show_correct_CallingMethod()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                var widget = new BoundaryTests.NestedWidget();
                await widget.DoStuffAsync();
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingMethod,
                        Is.EqualTo("DoStuffAsync"));

            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingMethod,
                        Is.EqualTo("DoStuffAsync"));
        }

        [Test]
        public async Task from_instance_method_in_nested_class_show_correct_CallingType()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                var widget = new BoundaryTests.NestedWidget();
                await widget.DoStuffAsync();
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingType,
                        Is.EqualTo(typeof (BoundaryTests.NestedWidget)));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingType,
                        Is.EqualTo(typeof (BoundaryTests.NestedWidget)));
        }

        [Test]
        public async Task from_instance_method_show_correct_CallingMethod()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                var widget = new Widget();
                await widget.DoStuffAsync("hello");
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingMethod,
                        Is.EqualTo("DoStuffAsync"));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingMethod,
                        Is.EqualTo("DoStuffAsync"));
        }

        [Test]
        public async Task from_instance_method_show_correct_CallingType()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                var widget = new Widget();
                await widget.DoStuffAsync("hello");
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingType,
                        Is.EqualTo(typeof (Widget)));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingType,
                        Is.EqualTo(typeof (Widget)));
        }

        [Test]
        public async Task from_static_method_in_nested_class_show_correct_CallingMethod()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                await BoundaryTests.NestedWidget.DoStuffStaticallyAsync();
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingMethod,
                        Is.EqualTo("DoStuffStaticallyAsync"));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingMethod,
                        Is.EqualTo("DoStuffStaticallyAsync"));
        }

        [Test]
        public async Task from_static_method_in_nested_class_show_correct_CallingType()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                await BoundaryTests.NestedWidget.DoStuffStaticallyAsync();
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingType,
                        Is.EqualTo(typeof (BoundaryTests.NestedWidget)));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingType,
                        Is.EqualTo(typeof (BoundaryTests.NestedWidget)));
        }

        [Test]
        public async Task from_static_method_show_correct_CallingMethod()
        {
            string param = Guid.NewGuid().ToString();
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                await Widget.DoStuffStaticallyAsync(param);
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingMethod,
                        Is.EqualTo("DoStuffStaticallyAsync"));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingMethod,
                        Is.EqualTo("DoStuffStaticallyAsync"));
        }

        [Test]
        public async Task from_static_method_show_correct_CallingType()
        {
            string param = Guid.NewGuid().ToString();
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                await Widget.DoStuffStaticallyAsync(param);
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingType,
                        Is.EqualTo(typeof (Widget)));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingType,
                        Is.EqualTo(typeof (Widget)));
        }

        [Test]
        public async Task from_static_method_in_derived_class_show_correct_CallingMethod()
        {
            string param = Guid.NewGuid().ToString();
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                await InheritedWidget.DoStuffStaticallyAsync(param);
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingMethod,
                        Is.EqualTo("DoStuffStaticallyAsync"));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingMethod,
                        Is.EqualTo("DoStuffStaticallyAsync"));
        }

        [Test]
        public async Task from_static_method_in_derived_class_show_correct_CallingType()
        {
            string param = Guid.NewGuid().ToString();
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                await InheritedWidget.DoStuffStaticallyAsync(param);
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingType,
                        Is.EqualTo(typeof (Widget)));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingType,
                        Is.EqualTo(typeof (Widget)));
        }

        [Test]
        public async Task from_local_anonymous_delegate_in_non_static_class_show_correct_CallingMethod()
        {
            var log = new List<LogEntry>();
            var action = new Func<Task>(async () =>
            {
                using (Log.Enter(() => { }))
                {
                    await Task.Run(() => Console.WriteLine("hello"));
                }
            });

            using (Log.Events().Subscribe(log.Add))
            {
                await action();
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingMethod,
                        Is.EqualTo("from_local_anonymous_delegate_in_non_static_class_show_correct_CallingMethod"));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingMethod,
                        Is.EqualTo("from_local_anonymous_delegate_in_non_static_class_show_correct_CallingMethod"));
        }

        [Test]
        public async Task from_local_anonymous_delegate_in_non_static_class_show_correct_CallingType()
        {
            var log = new List<LogEntry>();
            var action = new Func<Task>(async () =>
            {
                using (Log.Enter(() => { }))
                {
                    await Task.Run(() => Console.WriteLine("hello"));
                }
            });

            using (Log.Events().Subscribe(log.Add))
            {
                await action();
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingType,
                        Is.EqualTo(typeof (Log_Enter_and_Exit_call_source_async)));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingType,
                        Is.EqualTo(typeof (Log_Enter_and_Exit_call_source_async)));
        }

        [Test]
        public async Task from_local_anonymous_delegate_in_static_class_show_correct_CallingMethod()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                await new Widget().ExtensionWithLoggingInLocallyScopedAsyncLambdaAsync();
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingMethod,
                        Is.EqualTo("ExtensionWithLoggingInLocallyScopedAsyncLambdaAsync"));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingMethod,
                        Is.EqualTo("ExtensionWithLoggingInLocallyScopedAsyncLambdaAsync"));
        }

        [Test]
        public async Task from_local_anonymous_delegate_in_static_class_show_correct_CallingType()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                await new Widget().ExtensionWithLoggingInLocallyScopedAsyncLambdaAsync();
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingType,
                        Is.EqualTo(typeof (WidgetExtensions)));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingType,
                        Is.EqualTo(typeof (WidgetExtensions)));
        }

        [Test]
        public async Task from_static_anonymous_delegate_in_static_class_show_ctor_as_CallingMethod()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                await new Widget().ExtensionWithLoggingInStaticallyScopedAsyncLambdaAsync();
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingMethod,
                        Is.EqualTo(".cctor"));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingMethod,
                        Is.EqualTo(".cctor"));
        }

        [Test]
        public async Task from_static_anonymous_delegate_in_static_class_show_correct_CallingType()
        {
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            {
                await new Widget().ExtensionWithLoggingInLocallyScopedAsyncLambdaAsync();
            }

            Assert.That(log.Single(e => e.EventType == TraceEventType.Start).CallingType,
                        Is.EqualTo(typeof (WidgetExtensions)));
            Assert.That(log.Single(e => e.EventType == TraceEventType.Stop).CallingType,
                        Is.EqualTo(typeof (WidgetExtensions)));
        }
    }
}
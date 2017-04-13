// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using FluentAssertions;
using Its.Log.Instrumentation.Extensions;
using NUnit.Framework;

namespace Its.Log.Instrumentation.UnitTests
{
    [TestFixture]
    public class BoundaryTests
    {
        [SetUp]
        public void TestInitialize()
        {
            Log.Formatters.Clear();
            Extension.EnableAll();
            Counter.ResetAll();
            Trace.CorrelationManager.ActivityId = Guid.Empty;
        }

        [Test]
        public void Boundary_logging_can_be_disabled_globally_when_calling_with_no_params()
        {
            Extension<Boundaries>.Disable();
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            using (Log.Enter(() => { }))
            {
            }

            log.Should().BeEmpty();
        }

        [Test]
        public void Boundary_logging_can_be_disabled_globally_when_calling_with_params()
        {
            Extension<Boundaries>.Disable();
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            using (Log.Enter(() => new { hello = "hello" }))
            {
            }

            log.Should().BeEmpty();
        }

        [Test]
        public void Boundary_logging_can_be_disabled_per_class_when_calling_with_no_params()
        {
            Extension<Boundaries>.DisableFor<BoundaryTests>();
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            using (Log.Enter(() => { }))
            {
            }

            log.Should().BeEmpty();
        }

        [Test]
        public void Boundary_logging_can_be_disabled_per_class_with_params()
        {
            Extension<Boundaries>.DisableFor<BoundaryTests>();
            var log = new List<LogEntry>();

            using (Log.Events().Subscribe(log.Add))
            using (Log.Enter(() => new { hello = "hello" }))
            {
            }

            log.Should().BeEmpty();
        }

        [Test]
        public void Parameter_logging_can_be_disabled_per_class()
        {
            var log = new StringWriter();

            Extension<Boundaries>.DisableFor<InheritedWidget>();
            using (Log.Events().Subscribe(e => log.Write(e.ToLogString())))
            {
                var w = new Widget();
                var iw = new InheritedWidget();
                w.DoStuff("should be in log");
                iw.DoStuff("should not be in log");
            }

            StringAssert.Contains("should be in log", log.ToString());
            StringAssert.DoesNotContain("should not be in log", log.ToString());
        }

        [Test]
        public void Log_Enter_with_no_params_does_not_throw_exceptions_thrown_from_observer()
        {
            var observer = Observer.Create<LogEntry>(e =>
            {
                throw new Exception();
            });

            using (Log.Events().Subscribe(observer))
            using (Log.Enter(() => { }))
            {
            }

            // no exception. success!
        }

        [Test]
        public void Log_Enter_with_params_does_not_throw_exceptions_thrown_from_writer()
        {
            var observer = Observer.Create<LogEntry>(e =>
            {
                throw new Exception();
            });

            string someParam = "whee";
            using (Log.Events().Subscribe(observer))
            using (Log.Enter(() => new { someParam }))
            {
            }

            // no exception. success!
        }

        [Test]
        public void When_method_boundaries_are_logged_timing_is_included_in_default_log_output()
        {
            var log = "";

            using (Log.Events().Subscribe(e => log += e.ToLogString()))
            using (Log.Events().LogToConsole())
            {
                using (Log.Enter(() => { }))
                {
                }
            }

            Assert.That(log,
                        Contains.Substring("ElapsedMilliseconds = "));
        }

        public class NestedWidget
        {
            public static void DoStuffStatically()
            {
                using (Log.Enter(() => { }))
                {
                    Log.Write("Doing some stuff...");
                }
            }

            public static void DoStuffStatically(string nestedWidgetStaticParam)
            {
                using (Log.Enter(() => new { nestedWidgetStaticParam }))
                {
                    Log.Write("Doing some stuff...");
                }
            }

            public static async Task DoStuffStaticallyAsync()
            {
                using (Log.Enter(() => { }))
                {
                    await Task.Run(() => { Log.Write("Doing some stuff..."); });
                }
            }

            public static async Task DoStuffStaticallyAsync(string nestedWidgetParam)
            {
                await Task.Run(() =>
                {
                    var activity = Log.Enter(() => new { nestedWidgetParam });

                    Log.Write("Doing some stuff...");

                    Log.Exit(activity);
                });
            }

            public void DoStuff()
            {
                using (Log.Enter(() => { }))
                {
                    Log.Write("Doing some stuff...");
                }
            }

            public void DoStuff(string nestedWidgetParam)
            {
                ILogActivity activity = Log.Enter(() => new { nestedWidgetParam });

                Log.Write("Doing some stuff...");

                Log.Exit(activity);
            }

            public async Task DoStuffAsync()
            {
                using (Log.Enter(() => { }))
                {
                    await Task.Run(() => { Log.Write("Doing some stuff..."); });
                }
            }

            public async Task DoStuffAsync(string nestedWidgetParam)
            {
                await Task.Run(() =>
                {
                    var activity = Log.Enter(() => new { nestedWidgetParam });

                    Log.Write("Doing some stuff...");

                    Log.Exit(activity);
                });
            }

            public static string StaticProperty { get; set; }

            public static string StaticField;
        }
    }
}
// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using Its.Log.Instrumentation.Extensions;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using StringAssert = NUnit.Framework.StringAssert;

namespace Its.Log.Instrumentation.UnitTests
{
    [TestFixture]
    public class ParameterLoggingTests
    {
        [TearDown]
        [SetUp]
        public void SetUpAndTearDown()
        {
            Extension.EnableAll();
        }

        [Test]
        public void Log_WithParams_supplies_enclosing_method()
        {
            const double one = 2d;
            const double two = 2d;
            var currentMethod = MethodBase.GetCurrentMethod().Name;
            var wasCalled = false;

            using (Log.Events().Where(e => e.CallingMethod.Contains(currentMethod)).Subscribe(
                    e => wasCalled = true))
            {
                Log.WithParams(() => new { one, two }).Write("message!");
            }

            Assert.IsTrue(wasCalled);
        }

        [Test]
        public void When_WithParams_is_passed_a_null_Func_it_still_logs_the_subject()
        {
            var log = "";
            Func<object> nullFunc = null;

            using (Log.Events().Subscribe(e=>log+=e.ToLogString()))
            {
                Log.WithParams(nullFunc).Write("hello!");
            }

            Assert.That(log, Does.Contain("hello!"));
        }

        [NUnit.Framework.Ignore("Perf timing test")]
        [Test]
        public void Perf_experiment_for_explicit_int_formatter_registration()
        {
            var iterations = 1000000;
            Timer.TimeOperation(i => i.ToLogString(), iterations, "without");
            Log.Formatters.RegisterFormatter<int>(i => i.ToString());
            Timer.TimeOperation(i => i.ToLogString(), iterations, "with");
        }

        [NUnit.Framework.Ignore("Perf timing test")]
        [Test]
        public void Perf_timing_for_boundary_logging_with_parameters()
        {
            Log.EntryPosted += (o, e) => e.ToLogString();
            Log.WithParams(() => new { one = 1, two = "2" }).Write("hello");
            Timer.TimeOperation(i => Widget.DoStuffStatically(i.ToString()), 1000000, "upgraded");
        }

        [Test]
        public virtual void Log_with_params_can_be_disabled_globally()
        {
            Extension<Params>.Disable();
            var writer = new StringWriter();

            var someGuid = Guid.NewGuid();

            using (Log.Events().Subscribe(e => writer.Write(e.ToLogString())))
            {
                Log.WithParams(() => new { someGuid }).Write("a message");
            }
            
            var output = writer.ToString();

            StringAssert.DoesNotContain("someGuid", output);
            StringAssert.DoesNotContain(someGuid.ToLogString(), output);
        }

        [Test]
        public virtual void Log_WithParams_can_be_disabled_per_class()
        {
            var writer = new StringWriter();
            var someGuid = Guid.NewGuid();

            Extension<Params>.DisableFor<ParameterLoggingTests>();
            using (Log.Events().Subscribe(e => writer.Write(e.ToLogString())))
            {
                Log.WithParams(() => new { someGuid }).Write("a message");
            }

            var output = writer.ToString();

            Assert.That(output, !Contains.Substring("someGuid"));
            Assert.That(output, !Contains.Substring(someGuid.ToLogString()));
        }
        
        [Test]
        public virtual void When_Log_WithParams_is_disabled_for_one_class_it_is_not_disabled_for_others()
        {
            var writer = new StringWriter();
            var someGuid = Guid.NewGuid();

            Extension<Params>.DisableFor<ParameterLoggingTests>();
            using (Log.Events().Subscribe(e => writer.Write(e.ToLogString())))
            {
                Log.WithParams(() => new { someGuid }).Write("a message");
                new Widget().DoStuff("widget param");
            }

            var output = writer.ToString();

            Assert.That(output, Contains.Substring("widget param"));
        }

        [Test]
        public virtual void Log_WithParams_comment_appears_in_output()
        {
            var writer = new StringWriter();
            var fortyone = 41;

            using (Log.Events().Subscribe(e => writer.Write(e.ToLogString())))
            {
                Log.WithParams(() => new { fortyone }).Write("a message");
            }

            StringAssert.Contains("a message", writer.ToString());
        }

        [Test]
        public virtual void Log_WithParams_automatically_generates_custom_formatter_for_array()
        {
            var writer = new StringWriter();
            var myInts = new[] { 2222, 3333, 4444 };

            using (Log.Events().Subscribe(e => writer.Write(e.ToLogString())))
            {
                Log.WithParams(() => new { myInts }).Write("a message");
            }

            var output = writer.ToString();
            StringAssert.Contains("2222", output);
            StringAssert.Contains("3333", output);
            StringAssert.Contains("4444", output);
            StringAssert.DoesNotContain(myInts.ToString(), output);
        }

        [Test]
        public virtual void Log_WithParams_automatically_generates_custom_formatter_for_reference_type()
        {
            var writer = new StringWriter();
            Log.Formatters.RegisterPropertiesFormatter<FileInfo>();
            const int anInt = 23;
            var fileInfo = new FileInfo(@"c:\temp\testfile.txt");

            using (Log.Events().Subscribe(e => writer.Write(e.ToLogString())))
            using (TestHelper.LogToConsole())
            {
                Log.WithParams(() => new { anInt, fileInfo }).Write("a message");
            }

            StringAssert.Contains(@"Directory = c:\temp", writer.ToString());
        }

        [Test]
        public virtual void Log_WithParams_supplies_enclosing_type()
        {
            var wasCalled = false;
            var one = 1f;
            var two = 2d;

            using (Log.Events().Where(e => e.CallingType == GetType()).Subscribe(_ => wasCalled = true))
            {
                Log.WithParams(() => new { one, two })
                    .Write("message!");
            }

            Assert.IsTrue(wasCalled);
        }

        [Test]
        public virtual void Log_WithParams_correctly_outputs_value_types()
        {
            var writer = new StringWriter();
            var forty = "40";
            var fortyone = 41;

            using (Log.Events().Subscribe(e => writer.Write(e.ToLogString())))
            {
                Log.WithParams(() => new { forty, fortyone }).Write("a message");
            }

            var output = writer.ToString();
            StringAssert.Contains("forty", output);
            StringAssert.Contains("40", output);
            StringAssert.Contains("fortyone", output);
            Assert.IsTrue(output.Contains("41"));
        }

        [Test]
        public void Log_WithParams_does_not_throw_when_params_func_is_null()
        {
            Func<string> paramAccessor = null;
            
            using (Log.Events().LogToConsole())
            using (TestHelper.InternalErrors().LogToConsole()) // this displays the resulting exception, which is swallowed
            {
                Log.WithParams(paramAccessor).Write("hello");
            }
        }
    }
}
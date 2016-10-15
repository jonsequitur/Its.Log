// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Its.Log.Instrumentation.Extensions;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Its.Log.Instrumentation.UnitTests
{
    [TestFixture]
    public class CounterTests
    {
        [SetUp]
        public void Initialize()
        {
            Extension<Counter>.Enable();
            Counter.ResetAll();
        }

        [Test]
        public void Log_Enter_without_params_increments_counter()
        {
            var widget = new Widget();

            widget.DoStuff("with a param");

            Assert.AreEqual(1, Counter.For<Widget>("DoStuff").Count);
        }

        [Test]
        public void Log_Enter_with_params_increments_counter()
        {
            var one = 1;
            using (Log.Enter(() => new { one }))
            {
            }

            Assert.AreEqual(1, Counter.For(GetType(), "Log_Enter_with_params_increments_counter").Count);
        }

        [Test]
        public void For_returns_same_Counter_each_time()
        {
            Assert.AreSame(Counter.For<Widget>(w => w.DoStuff()),
                           Counter.For<Widget>(w => w.DoStuff()));
        }
        
        [Test]
        public void Counter_For_method_overloads_return_same_counter()
        {
            Assert.AreSame(Counter.For<Widget>("DoStuff"),
                           Counter.For<Widget>(w => w.DoStuff()));
        }
    }
}
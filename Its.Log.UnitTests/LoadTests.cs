// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Its.Log.Instrumentation.UnitTests.SampleApp;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Its.Log.Instrumentation.UnitTests
{
    [TestFixture]
    public class LoadTests
    {
        private static IEnumerable<Type> loggableTypes;

        public static IEnumerable<Type> LoggableTypes
        {
            get
            {
                if (loggableTypes == null)
                {
                    loggableTypes = typeof (ILoggable<>)
                        .Assembly
                        .GetTypes()
                        .Where(t => t.GetInterfaces().Any(i => i.Name.Contains("ILoggable")));
                    if (!loggableTypes.Any())
                    {
                        Assert.Fail("No ILoggable types found for testing.");
                    }
                }
                return loggableTypes;
            }
        }

        public static IEnumerable<ILoggable<T>> GetLoggableInstances<T>()
        {
            return LoggableTypes
                .Select(t => t.MakeGenericType(typeof (T)))
                .Select(t => t.GetConstructors().First().Invoke(null))
                .Cast<ILoggable<T>>()
                .ToArray();
        }

        [NUnit.Framework.Ignore("Test not finished")]
        [Test]
        public virtual void Contention()
        {
            var instances = GetLoggableInstances<string>();
            var callsToMake = 200;

            using (TestHelper.LogToConsole())
            {
                var actions = Enumerable
                    .Range(1, callsToMake)
                    .Select(i => new Action(() =>
                                                {
                                                    var instance = instances.Skip(i).First();
                                                    instance.ActionWithLogEnter(i.ToString());
                                                }));

                actions.AsParallel()
                    .WithDegreeOfParallelism(20)
                    .ForAll(a => a());

                // TODO: (Contention) 
            }
        }
    }
}
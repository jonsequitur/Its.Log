// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Web;
using Its.Recipes;

namespace Its.Log.Monitoring
{
    internal class TargetConstraint : TestConstraint
    {
        public TargetConstraint(TestDefinition testDefinition)
        {
            if (typeof (IApplyToTarget).IsAssignableFrom(testDefinition.TestType))
            {
                TestDefinition = testDefinition;
            }
        }

        protected override bool Match(TestTarget target, HttpRequestMessage _) =>
            Match(target);

        internal bool Match(TestTarget target)
        {
            if (TestDefinition == null)
            {
                return true;
            }

            var key = $"{"TargetConstraint:"}({target.Environment}:{target.Application}):{TestDefinition.TestType}";

            Func<bool> resolve = () =>
            {
                var test = target.ResolveDependency(TestDefinition.TestType);

                return test.IfTypeIs<IApplyToTarget>()
                           .Then(t => t.AppliesToTarget(target))
                           .ElseDefault();
            };

            return HttpRuntime.Cache.GetOrAdd(key, resolve);
        }
    }
}
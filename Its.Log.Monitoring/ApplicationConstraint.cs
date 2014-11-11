using System;
using System.Collections.Concurrent;
using System.Net.Http;
using Its.Recipes;

namespace Its.Log.Monitoring
{
    internal class ApplicationConstraint : TestConstraint
    {
        private readonly ConcurrentDictionary<TestTarget, bool> cachedResults = new ConcurrentDictionary<TestTarget, bool>();

        public ApplicationConstraint(TestDefinition testDefinition)
        {
            if (typeof (IApplyToApplication).IsAssignableFrom(testDefinition.TestType))
            {
                TestDefinition = testDefinition;
            }
        }

        protected override bool Match(TestTarget target, HttpRequestMessage request)
        {
            if (TestDefinition == null)
            {
                return true;
            }
            return cachedResults.GetOrAdd(target,
                                          t => target.ResolveDependency(TestDefinition.TestType)
                                                     .IfTypeIs<IApplyToApplication>()
                                                     .Then(test => test.AppliesToApplication(t.Application))
                                                     .Else(() => false));
        }
    }
}
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using Its.Recipes;

namespace Its.Log.Monitoring
{
    internal class EnvironmentConstraint : TestConstraint
    {
        private readonly ConcurrentDictionary<TestTarget, bool> cachedResults = new ConcurrentDictionary<TestTarget, bool>();

        public EnvironmentConstraint(TestDefinition testDefinition)
        {
            if (typeof (IApplyToEnvironment).IsAssignableFrom(testDefinition.TestType))
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
                                                     .IfTypeIs<IApplyToEnvironment>()
                                                     .Then(test => test.AppliesToEnvironment(t.Environment))
                                                     .Else(() => false));
        }
    }
}
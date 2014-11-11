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

        protected override bool Match(TestTarget target, HttpRequestMessage request)
        {
            if (TestDefinition == null)
            {
                return true;
            }
            var key = string.Format("{0}{1}:{2}:{3}", "TargetConstraint:", target.Environment, target.Application, TestDefinition.TestType);

            return HttpRuntime.Cache.GetOrAdd(key,
                                              () =>
                                              {
                                                  var test = target.ResolveDependency(TestDefinition.TestType) as IApplyToTarget;

                                                  return test.IfNotNull()
                                                             .Then(t => t.AppliesToTarget(target))
                                                             .ElseDefault();
                                              });
        }
    }
}
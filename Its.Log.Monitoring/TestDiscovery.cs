using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Its.Log.Instrumentation;

namespace Its.Log.Monitoring
{
    internal static class TestDiscovery
    {
        public static IDictionary<string, TestDefinition> GetTestDefinitions(
            this IEnumerable<Type> monitoringTests)
        {
            var definitions =
                monitoringTests
                    .SelectMany(
                        t => t
                              .GetMethods(BindingFlags.Public |
                                          BindingFlags.Instance |
                                          BindingFlags.DeclaredOnly)
                              .Where(m => !m.GetParameters().Any())
                              .Where(m => !m.IsSpecialName)
                              .Select(TestDefinition.Create));

            var dictionary = new Dictionary<string, TestDefinition>();
            var collisionCount = 0;

            foreach (var testDefinition in definitions)
            {
                if (!dictionary.TryAdd(testDefinition.TestName, testDefinition))
                {
                    var name = "TEST_NAME_COLLISION_" + ++collisionCount;
                    var definition = testDefinition;
                    dictionary.Add(name,
                                   new AnonymousTestDefinition(name,
                                                               _ =>
                                                               {
                                                                   throw new InvalidOperationException("Test could not be routed:\n" + definition.ToLogString());
                                                               }));
                }
            }

            return dictionary;
        }
    }
}
// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Web.Http.Controllers;

namespace Its.Log.Monitoring
{
    internal abstract class TestDefinition
    {
        public abstract string TestName { get; }

        public string RouteName => "Its-Log-Monitoring-Test-" + TestName;

        public string RouteTemplate { get; set; }

        public string[] Tags { get; set; }

        internal Type TestType { get; set; }

        internal abstract dynamic Run(HttpActionContext actionContext, Func<Type, object> resolver = null);

        internal static TestDefinition Create(MethodInfo methodInfo)
        {
            var testType = methodInfo.DeclaringType;
            var testDefinitionType = typeof (TestDefinition<>).MakeGenericType(testType);
            var testDefinition = (TestDefinition) Activator.CreateInstance(
                testDefinitionType,
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new object[] { methodInfo },
                null);
            testDefinition.TestType = testType;
            return testDefinition;
        }
    }
}
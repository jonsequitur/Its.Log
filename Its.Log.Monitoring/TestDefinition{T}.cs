// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Web.Http.Controllers;

namespace Its.Log.Monitoring
{
    internal class TestDefinition<T> : TestDefinition
        where T : class, IMonitoringTest
    {
        private readonly Func<T, dynamic> executeTestMethod;
        private readonly MethodInfo methodInfo;

        internal TestDefinition(MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                throw new ArgumentNullException("methodInfo");
            }

            this.methodInfo = methodInfo;

            var test = Expression.Parameter(typeof (T), "test");

            if (methodInfo.ReturnType != typeof (void))
            {
                if (methodInfo.ReturnType.IsClass)
                {
                    executeTestMethod = Expression.Lambda<Func<T, dynamic>>(
                        Expression.Call(test, methodInfo),
                        test)
                                                  .Compile();
                }
                else
                {
                    dynamic testMethod = Expression.Lambda(typeof (Func<,>)
                                                               .MakeGenericType(typeof (T), methodInfo.ReturnType),
                                                           Expression.Call(test, methodInfo),
                                                           test)
                                                   .Compile();
                    executeTestMethod = testClassInstance => testMethod(testClassInstance);
                }
            }
            else
            {
                var voidRunMethod = Expression.Lambda<Action<T>>(
                    Expression.Call(test, methodInfo),
                    test).Compile();

                executeTestMethod = testClassInstance =>
                {
                    voidRunMethod(testClassInstance);
                    return new object();
                };
            }
        }

        public override string TestName
        {
            get
            {
                return methodInfo.Name;
            }
        }

        internal override dynamic Run(HttpActionContext context, Func<Type, object> resolver = null)
        {
            resolver = resolver ??
                       (t => (T) context.ControllerContext.Configuration.DependencyResolver.GetService(typeof (T)) ??
                             (Activator.CreateInstance<T>()));

            var testClassInstance = (T) resolver(typeof (T));

            return executeTestMethod(testClassInstance);
        }

        public override string ToString()
        {
            return string.Format("{0} ({1}.{2})",
                                 base.ToString(),
                                 methodInfo.DeclaringType,
                                 methodInfo.Name);
        }
    }
}
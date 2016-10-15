// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Web.Http.Controllers;

namespace Its.Log.Monitoring
{
    internal class AnonymousTestDefinition : TestDefinition
    {
        private readonly string testName;
        private readonly Func<HttpActionContext, dynamic> run;

        public AnonymousTestDefinition(string testName, Func<HttpActionContext, dynamic> run)
        {
            if (testName == null)
            {
                throw new ArgumentNullException(nameof(testName));
            }
            if (run == null)
            {
                throw new ArgumentNullException(nameof(run));
            }
            this.testName = testName;
            this.run = run;
        }

        public override string TestName => testName;

        internal override dynamic Run(HttpActionContext actionContext, Func<Type, object> resolver = null)
        {
            return run(actionContext);
        }
    }
}
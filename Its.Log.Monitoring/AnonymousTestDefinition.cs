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
                throw new ArgumentNullException("testName");
            }
            if (run == null)
            {
                throw new ArgumentNullException("run");
            }
            this.testName = testName;
            this.run = run;
        }

        public override string TestName
        {
            get
            {
                return testName;
            }
        }

        internal override dynamic Run(HttpActionContext actionContext, Func<Type, object> resolver = null)
        {
            return run(actionContext);
        }
    }
}
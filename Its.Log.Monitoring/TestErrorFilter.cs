// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;
using Its.Log.Instrumentation;

namespace Its.Log.Monitoring
{
    internal class TestErrorFilter : ExceptionFilterAttribute
    {
        private readonly string testUriRouteRoot;

        public TestErrorFilter(string testUriRouteRoot)
        {
            this.testUriRouteRoot = testUriRouteRoot;
        }

        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext.ActionContext.ControllerContext.RouteData.Route.RouteTemplate.StartsWith(testUriRouteRoot + "/", StringComparison.OrdinalIgnoreCase))
            {
                var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(actionExecutedContext.Exception.ToLogString())
                };
                actionExecutedContext.Response = response;
            }
        }

        public override bool AllowMultiple => false;
    }
}
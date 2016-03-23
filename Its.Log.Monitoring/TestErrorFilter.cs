// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http.Filters;
using Its.Log.Instrumentation;
using Its.Log.Instrumentation.Extensions;
using Its.Recipes;
using Newtonsoft.Json;

namespace Its.Log.Monitoring
{
    internal class TestErrorFilter : ExceptionFilterAttribute
    {
        private readonly string testUriRouteRoot;

        public TestErrorFilter(string testUriRouteRoot)
        {
            this.testUriRouteRoot = testUriRouteRoot;
        }

        public override bool AllowMultiple => false;

        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            if (!actionExecutedContext.ActionContext.ControllerContext.RouteData.Route.RouteTemplate
                                      .StartsWith(testUriRouteRoot + "/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var statusCode = actionExecutedContext.Exception
                                                  .IfTypeIs<MonitorParameterFormatException>()
                                                  .Then(_ => HttpStatusCode.BadRequest)
                                                  .Else(() => HttpStatusCode.InternalServerError);

            actionExecutedContext.Response = actionExecutedContext
                .Exception
                .IfTypeIs<AggregationAssertionException<IEnumerable<Telemetry>>>()
                .Then(e => new HttpResponseMessage(statusCode)
                           {
                               Content = new StringContent(JsonConvert.SerializeObject(new
                                                                                       {
                                                                                           Message = e.Message,
                                                                                           RelatedEvents = e.State,
                                                                                           Exception = e.ToLogString()
                                                                                       }),
                                                           Encoding.UTF8,
                                                           "application/json")
                           }).Else(() => new HttpResponseMessage(statusCode)
                                         {
                                             Content = new StringContent(JsonConvert.SerializeObject(new
                                                                                                     {
                                                                                                         Message = actionExecutedContext.Exception.Message,
                                                                                                         Exception = actionExecutedContext.Exception.ToLogString()
                                                                                                     }),
                                                                         Encoding.UTF8,
                                                                         "application/json")
                                         });
        }
    }
}
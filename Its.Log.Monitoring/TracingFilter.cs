// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Its.Log.Instrumentation;

namespace Its.Log.Monitoring
{
    public class TracingFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            TraceBuffer.Initialize();
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            var buffer = TraceBuffer.Current;

            if (buffer == null)
            {
                return;
            }

            TraceBuffer.Clear();

            if (buffer.HasContent)
            {
                string originalContent;
                HttpStatusCode statusCode;

                if (actionExecutedContext.Exception == null)
                {
                    originalContent = actionExecutedContext.Response.Content.ReadAsStringAsync().Result;
                    statusCode = actionExecutedContext.Response.StatusCode;
                }
                else
                {
                    originalContent = actionExecutedContext.Exception.ToLogString();
                    statusCode = HttpStatusCode.InternalServerError;
                }

                actionExecutedContext.Response = new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(string.Format("{0}\n\n{1}", buffer, originalContent).Trim())
                };
            }
        }

        public override bool AllowMultiple
        {
            get
            {
                return false;
            }
        }

        internal class TraceListener : System.Diagnostics.TraceListener
        {
            public static readonly TraceListener Instance = new TraceListener();

            public override void Write(string message)
            {
                WriteLine(message);
            }

            public override void WriteLine(string message)
            {
                var buffer = TraceBuffer.Current;

                if (buffer != null)
                {
                    buffer.Write(message);
                }
            }
        }
    }
}
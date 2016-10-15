// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Its.Log.Instrumentation;

namespace Its.Log.Monitoring
{
    public class TracingFilter : ActionFilterAttribute
    {
        /// <summary>
        /// Occurs before the action method is invoked.
        /// </summary>
        /// <param name="actionContext">The action context.</param>
        public override void OnActionExecuting(HttpActionContext actionContext) =>
            TraceBuffer.Initialize();

        public override async Task OnActionExecutedAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
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
                    originalContent = await actionExecutedContext.Response.Content.ReadAsStringAsync();
                    statusCode = actionExecutedContext.Response.StatusCode;
                }
                else
                {
                    originalContent = actionExecutedContext.Exception.ToLogString();
                    statusCode = HttpStatusCode.InternalServerError;
                }

                actionExecutedContext.Response = new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent($"{buffer}\n\n{originalContent}".Trim())
                };
            }
        }

        /// <summary>
        /// Gets a value that indicates whether multiple filters are allowed.
        /// </summary>
        /// <returns>
        /// true if multiple filters are allowed; otherwise, false.
        /// </returns>
        public override bool AllowMultiple => false;

        internal class TraceListener : System.Diagnostics.TraceListener
        {
            public static readonly TraceListener Instance = new TraceListener();

            public override void Write(string message) => WriteLine(message);

            public override void WriteLine(string message)
            {
                var buffer = TraceBuffer.Current;

                buffer?.Write(message);
            }
        }
    }
}
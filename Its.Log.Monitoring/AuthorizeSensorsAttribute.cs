// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Its.Log.Monitoring
{
    internal class AuthorizeSensorsAttribute : FilterAttribute, IAuthorizationFilter
    {
        private static Func<HttpActionContext, bool> authorizeRequest = context => false;

        public static Func<HttpActionContext, bool> AuthorizeRequest
        {
            get
            {
                return authorizeRequest;
            }
            set
            {
                authorizeRequest = value ?? (authorizeRequest = context => false);
            }
        }

        /// <summary>
        /// Executes the authorization filter.
        /// </summary>
        /// <param name="actionContext">The action context.</param><param name="cancellationToken">The cancellation token associated with the filter.</param><param name="continuation">The continuation.</param>
        public Task<HttpResponseMessage> ExecuteAuthorizationFilterAsync(
            HttpActionContext actionContext,
            CancellationToken cancellationToken,
            Func<Task<HttpResponseMessage>> continuation)
        {
            if (!AuthorizeRequest(actionContext))
            {
                return Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.Forbidden)
                    {
                        Content = new StringContent("Forbidden")
                    });
            }

            return continuation();
        }
    }
}
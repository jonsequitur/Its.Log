// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using Its.Log.Instrumentation;
using Its.Recipes;

namespace Its.Log.Monitoring
{
    public static class AssertionExtensions
    {
        static AssertionExtensions()
        {
            Formatter<ObjectContent>.RegisterForAllMembers();
            Formatter<StringContent>.Register(c => new
            {
                c.ReadAsStringAsync().Result,
                c.Headers
            }.ToLogString());
            Formatter<StreamContent>.Register(c => new
            {
                c.ReadAsStringAsync().Result,
                c.Headers
            }.ToLogString());
            Formatter<HttpContentHeaders>.RegisterForAllMembers();
            Formatter<HttpError>.RegisterForAllMembers();
        }

        public static HttpResponseMessage ShouldSucceed(
            this HttpResponseMessage response,
            HttpStatusCode? expected = null)
        {
            try
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(string.Format("Response does not indicate succeedfulness: {0}: {1}",
                                                                 response.StatusCode,
                                                                 response.ReasonPhrase));
                }

                var actualStatusCode = response.StatusCode;
                if (expected != null && actualStatusCode != expected.Value)
                {
                    throw new AssertionFailedException(
                        string.Format("Status code was successful but not of the expected type: {0} was expected but {1} was returned.",
                                      expected,
                                      actualStatusCode));
                }
            }
            catch
            {
                ThrowVerboseAssertion(response);
            }
            return response;
        }

        public static async Task<HttpResponseMessage> ShouldSucceed(
            this Task<HttpResponseMessage> response,
            HttpStatusCode? expected = null)
        {
            await response;
            return response.Result.ShouldSucceed(expected);
        }

        public static HttpResponseMessage ShouldFailWith(
            this HttpResponseMessage response,
            HttpStatusCode code)
        {
            if (response.StatusCode != code)
            {
                ThrowVerboseAssertion(response);
            }

            return response;
        }

        public static async Task<HttpResponseMessage> ShouldFailWith(
            this Task<HttpResponseMessage> response,
            HttpStatusCode code)
        {
            await response;
            return response.Result.ShouldFailWith(code);
        }

        private static void ThrowVerboseAssertion(HttpResponseMessage response)
        {
            var message = string.Format("{0}{1}{1}{2}",
                                        response,
                                        Environment.NewLine,
                                        response.Content.IfTypeIs<ObjectContent>()
                                                .Then(v => v.Value)
                                                .Else(() => response.Content).ToLogString());
            throw new AssertionFailedException(message);
        }
    }
}
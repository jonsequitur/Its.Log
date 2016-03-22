// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
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
                    throw new HttpRequestException(string.Format("Response does not indicate success: {0}: {1}",
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

        public static async Task<HttpResponseMessage> ShouldSucceedAsync(
            this Task<HttpResponseMessage> response,
            HttpStatusCode? expected = null) =>
                (await response).ShouldSucceed(expected);

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

        public static async Task<HttpResponseMessage> ShouldFailWithAsync(
            this Task<HttpResponseMessage> response,
            HttpStatusCode code) =>
                (await response).ShouldFailWith(code);

        public static Aggregation<IEnumerable<T>, long> CountOf<T>(this IEnumerable<T> sequence, Func<T, bool> selector)
        {
            var filteredResults = sequence.Where(selector).ToArray();

            return new Aggregation<IEnumerable<T>, long>(filteredResults, filteredResults.Length);
        }

        public static Aggregation<IEnumerable<T>, Percentage> PercentageOf<T>(this IEnumerable<T> sequence, Func<T, bool> selector)
        {
            var count = 0;

            var filteredResults = sequence.Where(_ =>
            {
                count++;
                return selector(_);
            }).ToArray();

            var percentage = count == 0 ? 0 : (double)filteredResults.Length / count;

            return new Aggregation<IEnumerable<T>, Percentage>(filteredResults, new Percentage((int)(percentage * 100)));
        }

        public static AggregationAssertions<TState, TResult> Should<TState, TResult>(this Aggregation<TState, TResult> aggregation) where TResult : IComparable<TResult>
        {
            return new AggregationAssertions<TState, TResult>(aggregation);
        }

        public static Percentage Percent(this int i)
        {
            return new Percentage(i);
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
// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Its.Log.Monitoring.UnitTests
{
    internal class FakeHttpClient : HttpClient
    {
        public readonly List<HttpRequestMessage> RequestsSent = new List<HttpRequestMessage>();

        public FakeHttpClient(Func<HttpRequestMessage, HttpResponseMessage> handle) : base(new FakeMessageHandler(handle))
        {
        }

        private class FakeMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> handle;

            public FakeMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handle)
            {
                this.handle = handle;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.Run(() => handle(request));
            }
        }
    }
}
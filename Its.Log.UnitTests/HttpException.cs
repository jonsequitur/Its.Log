// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Its.Log.Instrumentation.UnitTests
{
    public class HttpException : Exception
    {
        private readonly int httpStatusCode;

        public HttpException(int httpStatusCode, string message) : base(message)
        {
            this.httpStatusCode = httpStatusCode;
        }

        public int GetHttpCode()
        {
            return this.httpStatusCode;
        }
    }
}

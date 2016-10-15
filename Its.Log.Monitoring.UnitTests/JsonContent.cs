// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Net.Http.Formatting;

namespace Its.Log.Monitoring.UnitTests
{
    internal class JsonContent : ObjectContent
    {
        public JsonContent(object value) : base(value.GetType(), value, new JsonMediaTypeFormatter())
        {
        }
    }
}
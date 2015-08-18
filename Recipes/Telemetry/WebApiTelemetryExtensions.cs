// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// THIS FILE IS NOT INTENDED TO BE EDITED. 
// 
// It has been imported using NuGet. The original source is located in the Its.Log project (https://github.com/jonsequitur/its.log). 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System;
using System.Linq;
using System.Net.Http;
using Its.Log.Instrumentation.Extensions;

namespace Its.Log.Instrumentation
{
#if !RecipesProject
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    public static class WebApiTelemetryExtensions
    {
        public static void For(
            this Telemetry telemetry,
            HttpResponseMessage response)
        {
            if (response != null)
            {
                telemetry.Succeeded = response.IsSuccessStatusCode;
                telemetry.HttpStatusCode = response.StatusCode;

                if (response.RequestMessage != null)
                {
                    telemetry.RequestUri = response.RequestMessage.RequestUri;
                }
            }
        }
    }
}
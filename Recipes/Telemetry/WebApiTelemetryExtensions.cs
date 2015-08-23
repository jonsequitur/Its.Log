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
using System.ServiceModel.Channels;
using System.Web;
using Its.Log.Instrumentation.Extensions;

namespace Its.Log.Instrumentation
{
#if !RecipesProject
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal static class WebApiTelemetryExtensions
    {
        public static Telemetry For(
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

                    var callerIpAddress = response.RequestMessage.CallerIpAddress();

                    if (!string.IsNullOrWhiteSpace(callerIpAddress))
                    {
                        telemetry.IsIncomingRequest(true);
                        telemetry.CallerIpAddress(callerIpAddress);
                    }
                }
            }

            return telemetry;
        }

        /// <summary>
        /// Determines whether the telemetry is for an incoming request.
        /// </summary>
        /// <param name="telemetry">The telemetry.</param>
        /// <returns></returns>
        public static bool IsIncomingRequest(this Telemetry telemetry)
        {
            object isIncoming;
            if (telemetry.Properties.TryGetValue("IsIncoming", out isIncoming))
            {
                return (bool) isIncoming;
            }

            return false;
        }

        /// <summary>
        /// Sets a property indicating that the telemetry is for an incoming request.
        /// </summary>
        /// <param name="telemetry">The telemetry.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        public static void IsIncomingRequest(this Telemetry telemetry, bool value)
        {
            telemetry.Properties["IsIncoming"] = value;
        }

        public static string CallerIpAddress(this Telemetry telemetry)
        {
            object ipAddress;
            if (telemetry.Properties.TryGetValue("CallerIpAddress", out ipAddress))
            {
                return (string) ipAddress;
            }

            return null;
        }

        public static void CallerIpAddress(this Telemetry telemetry, string value)
        {
            telemetry.Properties["CallerIpAddress"] = value;
        }

        internal static string CallerIpAddress(this HttpRequestMessage request)
        {
            object o;

            // for ASP.NET hosting
            if (request.Properties.TryGetValue("MS_HttpContext", out o))
            {
                return ((HttpContextWrapper) o).Request.UserHostAddress;
            }

            // for in-memory hosting
            if (request.Properties.TryGetValue(RemoteEndpointMessageProperty.Name, out o))
            {
                return ((RemoteEndpointMessageProperty) o).Address;
            }

            return null;
        }
    }
}
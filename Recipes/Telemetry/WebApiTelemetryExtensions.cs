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
using System.Net.Http;
using System.ServiceModel.Channels;
using System.Threading;
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
        public static Telemetry WithPropertiesBasedOn(
            this Telemetry telemetry,
            HttpResponseMessage response)
        {
            if (response != null)
            {
                telemetry.Succeeded = response.IsSuccessStatusCode;
                telemetry.HttpStatusCode = response.StatusCode;

                var request = response.RequestMessage;
                if (request != null)
                {
                    telemetry.RequestUri = request.RequestUri;
                    telemetry.WithCallerIpAddressBasedOn(request);
                    telemetry.WithIsIncomingRequestBasedOn(request);
                    telemetry.WithOperationNameBasedOn(request);
                    telemetry.WithUserIdentifierBasedOn(request);
                }
            }

            return telemetry;
        }

        private static void WithIsIncomingRequestBasedOn(this Telemetry telemetry, HttpRequestMessage request)
        {
            if (request.GetActionDescriptor() != null)
            {
                telemetry.IsIncomingRequest(true);
            }
        }

        private static void WithUserIdentifierBasedOn(this Telemetry telemetry, HttpRequestMessage request)
        {
            object userIdentifier;
            telemetry.UserIdentifier = request.Properties.TryGetValue("__Its_Log_UserIdentifier", out userIdentifier)
                                           ? userIdentifier == null
                                                 ? null
                                                 : userIdentifier.ToString()
                                           : Thread.CurrentPrincipal.Identity.Name;
        }

        private static void WithCallerIpAddressBasedOn(this Telemetry telemetry, HttpRequestMessage request)
        {
            var callerIpAddress = request.CallerIpAddress();

            if (!string.IsNullOrWhiteSpace(callerIpAddress))
            {
                telemetry.CallerIpAddress(callerIpAddress);
            }
        }

        private static void WithOperationNameBasedOn(this Telemetry telemetry, HttpRequestMessage request)
        {
            if (request.HasActionDescriptor())
            {
                telemetry.OperationName = request.GetActionDescriptor().ActionName;
            }
            else
            {
                var leftPart = request.RequestUri.GetLeftPart(UriPartial.Authority);
                telemetry.OperationName = new Uri(leftPart).MakeRelativeUri(request.RequestUri).ToString();
            }
        }

        private static bool HasActionDescriptor(this HttpRequestMessage request)
        {
            return request.GetActionDescriptor() != null;
        }

        /// <summary>
        ///     Determines whether the telemetry is for an incoming request.
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
        ///     Sets a property indicating that the telemetry is for an incoming request.
        /// </summary>
        public static void IsIncomingRequest(this Telemetry telemetry, bool value)
        {
            telemetry.Properties["IsIncoming"] = value;
        }

        /// <summary>
        ///     Gets a property indicating the caller's IP address.
        /// </summary>
        public static string CallerIpAddress(this Telemetry telemetry)
        {
            object ipAddress;
            if (telemetry.Properties.TryGetValue("CallerIpAddress", out ipAddress))
            {
                return (string) ipAddress;
            }

            return null;
        }

        /// <summary>
        ///     Sets the caller's the ip address in <see cref="Telemetry.Properties" />.
        /// </summary>
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
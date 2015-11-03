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
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.ServiceModel.Channels;
using System.Threading;
using System.Web;
using System.Web.Http.Controllers;
using Its.Log.Instrumentation.Extensions;

namespace Its.Log.Instrumentation
{
#if !RecipesProject
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif

    public static class WebApiTelemetryExtensions
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
                    telemetry.SetCallerIpAddressBasedOn(request);
                    telemetry.SetIsIncomingRequestBasedOn(request);
                    telemetry.SetOperationNameBasedOn(request);
                    telemetry.SetUserIdentifierBasedOn(request);
                }
            }

            return telemetry;
        }

        private static void SetCallerIpAddressBasedOn(this Telemetry telemetry, HttpRequestMessage request)
        {
            var callerIpAddress = request.CallerIpAddress();

            if (!string.IsNullOrWhiteSpace(callerIpAddress))
            {
                telemetry.CallerIpAddress(callerIpAddress);
            }
        }

        private static void SetIsIncomingRequestBasedOn(this Telemetry telemetry, HttpRequestMessage request)
        {
            if (request.GetActionDescriptor() != null)
            {
                telemetry.IsIncomingRequest(true);
            }
        }

        private static void SetUserIdentifierBasedOn(this Telemetry telemetry, HttpRequestMessage request)
        {
            object userIdentifier;
            if (telemetry.Properties.TryGetValue("UserIdentifier", out userIdentifier))
            {
                telemetry.UserIdentifier = userIdentifier.ToString();
            }
            else
            {
                telemetry.UserIdentifier = Thread.CurrentPrincipal.Identity.Name;
            }
            
        }

        private static void SetOperationNameBasedOn(this Telemetry telemetry, HttpRequestMessage request)
        {
            var actionDescriptor = request.GetActionDescriptor();
            if (actionDescriptor != null)
            {
                telemetry.OperationName = actionDescriptor.ControllerDescriptor.ControllerName + "." + actionDescriptor.ActionName;
            }
            else
            {
                telemetry.OperationName = "NoActionDescriptorFound";
            }
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
                return (bool)isIncoming;
            }

            return false;
        }

        /// <summary>
        /// Sets a property indicating that the telemetry is for an incoming request.
        /// </summary>
        public static void IsIncomingRequest(this Telemetry telemetry, bool value)
        {
            telemetry.Properties["IsIncoming"] = value;
        }

        /// <summary>
        /// Gets a property indicating the caller's IP address.
        /// </summary>
        public static string CallerIpAddress(this Telemetry telemetry)
        {
            object ipAddress;
            if (telemetry.Properties.TryGetValue("CallerIpAddress", out ipAddress))
            {
                return (string)ipAddress;
            }

            return null;
        }

        /// <summary>
        /// Sets the caller's the ip address in <see cref="Telemetry.Properties" />.
        /// </summary>
        public static void CallerIpAddress(this Telemetry telemetry, string value)
        {
            telemetry.Properties["CallerIpAddress"] = value;
        }

        internal static string UserIdentifer(this HttpRequestMessage request)
        {
            object o;

            // for ASP.NET hosting
            if (request.Properties.TryGetValue("UserIdentifier", out o))
            {
                return ((HttpContextWrapper)o).Request.UserHostAddress;
            }

            // for in-memory hosting
            if (request.Properties.TryGetValue(RemoteEndpointMessageProperty.Name, out o))
            {
                return ((RemoteEndpointMessageProperty)o).Address;
            }

            return null;
        }

        internal static string CallerIpAddress(this HttpRequestMessage request)
        {
            object o;

            // for ASP.NET hosting
            if (request.Properties.TryGetValue("MS_HttpContext", out o))
            {
                return ((HttpContextWrapper)o).Request.UserHostAddress;
            }

            // for in-memory hosting
            if (request.Properties.TryGetValue(RemoteEndpointMessageProperty.Name, out o))
            {
                return ((RemoteEndpointMessageProperty)o).Address;
            }

            return null;
        }
    }
}
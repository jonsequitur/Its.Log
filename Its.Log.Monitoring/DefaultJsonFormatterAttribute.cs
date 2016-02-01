// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http.Formatting;
using System.Web.Http.Controllers;

namespace Its.Log.Monitoring
{
    internal sealed class DefaultJsonFormatterAttribute : Attribute, IControllerConfiguration
    {
        public void Initialize(
            HttpControllerSettings controllerSettings,
            HttpControllerDescriptor controllerDescriptor)
        {
            if (controllerSettings.Formatters.JsonFormatter != null)
            {
                controllerSettings.Formatters[controllerSettings.Formatters.IndexOf(controllerSettings.Formatters.JsonFormatter)] = new JsonMediaTypeFormatter();
            }
        }
    }
}
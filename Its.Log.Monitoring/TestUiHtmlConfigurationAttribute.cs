// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Web.Http.Controllers;
using Its.Recipes;

namespace Its.Log.Monitoring
{
    internal sealed class TestUiHtmlConfigurationAttribute : Attribute, IControllerConfiguration
    {
        public void Initialize(
            HttpControllerSettings controllerSettings,
            HttpControllerDescriptor controllerDescriptor) =>
                controllerSettings.Formatters.Add(
                    new TestUiScriptFormatter(
                        controllerDescriptor.Configuration
                                            .Properties
                                            .IfContains("Its.Log.Monitoring.TestUiUri")
                                            .And()
                                            .IfTypeIs<string>()
                                            .Else(() => "http://itsmonitoringux.azurewebsites.net/its.log.monitoring.js"),
                        controllerDescriptor.Configuration
                                            .Properties
                                            .IfContains("Its.Log.Monitoring.TestLibraryUris")
                                            .And()
                                            .IfTypeIs<IEnumerable<string>>()
                                            .Else(() => new string [] {})));
    }
}
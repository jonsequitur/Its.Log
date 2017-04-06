// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Its.Log.Instrumentation
{
    /// <summary>
    /// Marks a method as a diagnostic sensor.
    /// </summary>
    /// <remarks>The method can be a static or instance member and need not be public.</remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class DiagnosticSensorAttribute : Attribute
    {
    }
}
// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Its.Log.Instrumentation;

namespace Its.Log.Monitoring.UnitTests
{
    public class TestSensor
    {
        public static Func<object> GetSensorValue { get; set; }

        [DiagnosticSensor]
        public static object SensorMethod()
        {
            return GetSensorValue == null ? null : GetSensorValue();
        }
    }
}
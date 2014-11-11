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
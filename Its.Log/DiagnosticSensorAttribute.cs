using System;
using System.ComponentModel.Composition;

namespace Its.Log.Instrumentation
{
    /// <summary>
    /// Marks a method as a diagnostic sensor.
    /// </summary>
    /// <remarks>The method can be a static or instance member and need not be public.</remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class DiagnosticSensorAttribute : ExportAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticSensorAttribute"/> class.
        /// </summary>
        public DiagnosticSensorAttribute() : base("DiagnosticSensor")
        {
        }
    }
}
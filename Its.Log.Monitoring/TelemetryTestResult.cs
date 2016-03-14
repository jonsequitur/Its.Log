using System.Collections.Generic;
using Its.Log.Instrumentation;

namespace Its.Log.Monitoring
{
    public class TelemetryTestResult
    {
        public TelemetryTestResult(Telemetry[] telemetry)
        {
        }
    }

    public class Telemetry
    {
        private IDictionary<string, object> properties;

        /// <summary>
        ///     Initializes the <see cref="Instrumentation.Extensions.Telemetry" /> class.
        /// </summary>
        static Telemetry()
        {
            Formatter<Instrumentation.Extensions.Telemetry>.RegisterForAllMembers();
        }

        /// <summary>
        ///     Gets or sets a value indicating whether an operation succeeded.
        /// </summary>
        /// <value>
        ///     <c>true</c> if the operation succeeded; otherwise, <c>false</c>.
        /// </value>
        public bool Succeeded { get; set; }

        /// <summary>
        ///     Gets or sets the name of the source operation.
        /// </summary>
        public string OperationName { get; set; }

        /// <summary>
        ///     Gets or sets the number of milliseconds that the operation took.
        /// </summary>
        public int ElapsedMilliseconds { get; set; }

        /// <summary>
        ///     Gets or sets the user identifier. The user identifier is intended to uniquely represent a user in the context of an
        ///     event.
        /// </summary>
        public string UserIdentifier { get; set; }

        /// <summary>
        ///     A dictionary that can be used to associated additional properties with a telemetry event.
        /// </summary>
        /// <value>
        ///     The properties.
        /// </value>
        public IDictionary<string, object> Properties
        {
            get { return properties ?? (properties = new Dictionary<string, object>()); }
        }
    }
}
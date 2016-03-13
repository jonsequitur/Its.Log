using System;
using System.Threading.Tasks;
using Its.Log.Instrumentation.Extensions;

namespace Its.Log.Instrumentation
{
    public class TelemetryDispatchQueue : AsyncDispatchQueue<Telemetry>
    {
        public TelemetryDispatchQueue(
            Func<Telemetry, Task> send,
            IObservable<Telemetry> telemetryEvents = null) :
                base(send, telemetryEvents ?? Log.TelemetryEvents())
        {
        }
    }
}
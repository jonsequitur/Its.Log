using System;

namespace Its.Log.Monitoring
{
    public class AggregationAssertionException<TState> : Exception
    {
        public AggregationAssertionException(string message, TState state)
            : base(message)
        {
            State = state;
        }

        public TState State { get; private set; }
    }
}
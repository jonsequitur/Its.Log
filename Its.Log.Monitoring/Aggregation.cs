using System;

namespace Its.Log.Monitoring
{
    public class Aggregation<TState, TResult>
    {
        public Aggregation(TState state, TResult result)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }
            State = state;
            Result = result;
        }

        public TResult Result { get; }
        public TState State { get; }
    }
}
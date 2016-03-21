namespace Its.Log.Monitoring
{
    public class Aggregation<TState, TResult>
    {
        public Aggregation(TState state, TResult result)
        {
            State = state;
            Result = result;
        }

        public TResult Result { get; }
        public TState State { get; }
    }
}
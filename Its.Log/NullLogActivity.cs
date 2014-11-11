using System;

namespace Its.Log.Instrumentation
{
    internal struct NullLogActivity : ILogActivity
    {
        public static readonly NullLogActivity Instance;

        public void Complete(Action magicBarbell)
        {
        }

        public void Confirm(Func<object> value = null)
        {
        }

        public void Dispose()
        {
        }

        public void Trace(string comment)
        {
        }

        public void Trace<T>(Func<T> paramsAccessor) where T : class
        {
        }

        public void TraceAndWatch<T>(Func<T> paramsAccessor) where T : class
        {
        }
    }
}
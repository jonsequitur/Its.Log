using System;

namespace Its.Log.Instrumentation
{
    internal class AnonymousMethodInfo<T> : AnonymousMethodInfo
    {
        public AnonymousMethodInfo(Func<T> anonymousMethod) : base(anonymousMethod.Method)
        {
        }
    }
}
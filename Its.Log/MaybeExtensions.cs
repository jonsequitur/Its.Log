using System;
using System.Linq;

namespace Its.Log.Instrumentation
{
    internal static class MaybeExtensions
    {
        public static V Maybe<T, V>(
            this T self,
            Func<T, V> getter)
        {
            return self == null
                       ? default(V)
                       : getter(self);
        }
    }
}
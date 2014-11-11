using System;

namespace Its.Log.Instrumentation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    internal class SkipOnNullAttribute : Attribute
    {
    }
}
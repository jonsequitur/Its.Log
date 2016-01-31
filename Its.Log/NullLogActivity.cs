// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Its.Log.Instrumentation
{
    internal struct NullLogActivity : ILogActivity
    {
        public static readonly NullLogActivity Instance = new NullLogActivity();

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
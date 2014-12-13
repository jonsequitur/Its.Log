// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Its.Log.Instrumentation
{
    [DebuggerStepThrough]
    internal class RecursionCounter : IDisposable
    {
        [ThreadStatic]
        private static int depth = 0;

        public int Depth
        {
            get
            {
                return depth;
            }
        }

        public IDisposable Enter()
        {
            depth += 1;
            return this;
        }

        public void Dispose()
        {
            depth -= 1;
        }
    }
}
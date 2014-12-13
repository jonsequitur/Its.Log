// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
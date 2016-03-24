// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Its.Log.Monitoring
{
    public class AggregationAssertionException<TState> : AssertionFailedException
    {
        public AggregationAssertionException(string message, TState state)
            : base(message)
        {
            State = state;
        }

        public TState State { get; }
    }
}
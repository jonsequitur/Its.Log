// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Its.Log.Instrumentation.Extensions
{
    /// <summary>
    /// This is a marker class used for enabling and disabling parameter logging.
    /// </summary>
    public abstract class Params
    {
        /// <summary>
        /// Gets a closure that returns the parameters to be logged.
        /// </summary>
        /// <value>A delegate returning the paramters.</value>
        internal abstract Delegate ParamsAccessor { get; }
    }
}
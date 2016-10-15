// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Its.Log.Instrumentation
{
    /// <summary>
    /// Extends <see cref="LogEntry" /> objects as they are written, before they are published to the log event stream.
    /// </summary>
    public interface ILogExtension
    {
    }
}
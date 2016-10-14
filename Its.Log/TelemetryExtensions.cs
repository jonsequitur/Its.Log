// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Its.Log.Instrumentation.Extensions;

namespace Its.Log.Instrumentation
{
    public static class TelemetryExtensions
    {
        public static void MarkAsSuccessful(this ILogActivity activity) =>
            activity.Confirm(() => new Telemetry
            {
                Succeeded = true
            });
    }
}
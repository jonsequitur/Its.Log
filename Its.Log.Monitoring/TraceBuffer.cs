// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Remoting.Messaging;
using System.Text;

namespace Its.Log.Monitoring
{
    public class TraceBuffer
    {
        private const string TraceBufferKey = "Its.Log.Monitoring.TraceBuffer";

        private readonly StringBuilder buffer = new StringBuilder();

        public void Write(string message)
        {
            HasContent = true;
            buffer.AppendLine(message);
        }

        public bool HasContent { get; private set; }

        public override string ToString() => buffer.ToString();

        public static TraceBuffer Current =>
            CallContext.LogicalGetData(TraceBufferKey) as TraceBuffer;

        internal static void Initialize() =>
            CallContext.LogicalSetData(TraceBufferKey, new TraceBuffer());

        internal static void Clear() =>
            CallContext.LogicalSetData(TraceBufferKey, null);
    }
}
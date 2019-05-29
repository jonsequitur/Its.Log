// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Its.Log.Instrumentation
{
    internal class SingleLineTextFormatter : ILogTextFormatter
    {
        private const string PropertySeparator = " | ";
        private const string OpeningDelimiter = "{ ";
        private const string ClosingDelimiter = " }";
        private const string ItemSeparator = ", ";

        public void WriteStartProperty(TextWriter writer)
        {
        }

        public void WriteEndProperty(TextWriter writer)
        {
        }

        public void WriteStartObject(TextWriter writer) => writer.Write(OpeningDelimiter);

        public void WriteEndObject(TextWriter writer) => writer.Write(ClosingDelimiter);

        public void WriteStartSequence(TextWriter writer) => writer.Write(OpeningDelimiter);

        public void WriteEndSequence(TextWriter writer) => writer.Write(ClosingDelimiter);

        public void WriteNameValueDelimiter(TextWriter writer) => writer.Write(" = ");

        public void WritePropertyDelimiter(TextWriter writer) => writer.Write(PropertySeparator);

        public void WriteSequenceDelimiter(TextWriter writer) => writer.Write(ItemSeparator);

        public void WriteLogEntryHeader(LogEntry entry, TextWriter writer) => writer.Write(entry.EventType);

        public void WriteEndHeader(TextWriter writer) => writer.Write(": ");

        public void WriteStartSequenceItem(TextWriter writer)
        {
        }
    }
}
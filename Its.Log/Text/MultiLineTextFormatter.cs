// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Its.Log.Instrumentation
{
    internal class MultiLineTextFormatter : ILogTextFormatter
    {
        public static bool DebugMode = false;

        private int indentLevel = 0;

        public void WriteEndHeader(TextWriter writer)
        {
        }

        public void WriteStartSequenceItem(TextWriter writer)
        {
            WriteIndent(writer);
        }

        public void WriteEndObject(TextWriter writer)
        {
        }

        public void WriteEndProperty(TextWriter writer)
        {
        }

        public void WriteEndSequence(TextWriter writer)
        {
            WriteLine(writer);
            Unindent();
        }

        public void WriteLogEntryHeader(LogEntry entry, TextWriter writer)
        {
            indentLevel = 0;
            writer.Write(entry.EventType);
            writer.Write(" (");
            writer.Write(entry.TimeStamp);
            writer.Write(")");
            Indent();
        }

        public void WriteNameValueDelimiter(TextWriter writer) => writer.Write(": ");

        public void WritePropertyDelimiter(TextWriter writer)
        {
        }

        public void WriteSequenceDelimiter(TextWriter writer) => WriteLine(writer);

        private void WriteLine(TextWriter writer) => writer.WriteLine();

        public void WriteStartObject(TextWriter writer)
        {
        }

        public void WriteStartProperty(TextWriter writer)
        {
            WriteLine(writer);
            WriteIndent(writer);
        }

        public void WriteStartSequence(TextWriter writer)
        {
            WriteLine(writer);
            Indent();
        }

        private void Indent() => indentLevel++;

        private void Unindent() => indentLevel--;

        private void WriteIndent(TextWriter writer) => writer.Write(new string('\t', indentLevel));
    }
}
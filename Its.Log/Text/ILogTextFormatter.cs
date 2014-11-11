using System.IO;

namespace Its.Log.Instrumentation
{
    internal interface ILogTextFormatter
    {
        void WriteStartProperty(TextWriter writer);
        void WriteEndProperty(TextWriter writer);
        void WriteStartObject(TextWriter writer);
        void WriteEndObject(TextWriter writer);
        void WriteStartSequence(TextWriter writer);
        void WriteEndSequence(TextWriter writer);
        void WriteNameValueDelimiter(TextWriter writer);
        void WritePropertyDelimiter(TextWriter writer);
        void WriteSequenceDelimiter(TextWriter writer);
        void WriteLogEntryHeader(LogEntry entry, TextWriter writer);
        void WriteEndHeader(TextWriter writer);
        void WriteStartSequenceItem(TextWriter writer);
    }
}
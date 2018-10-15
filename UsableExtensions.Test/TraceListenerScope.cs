using System;
using System.Diagnostics;
using System.IO;

namespace UsableExtensions.Test
{
    public class TraceListenerScope : IDisposable
    {
        private readonly StringWriter writer;
        private readonly string name;

        public TraceListenerScope(string name = "default")
        {
            this.writer = new StringWriter();
            this.name = name;

            Trace.Listeners.Add(new TextWriterTraceListener(this.writer, this.name));
        }

        public void Dispose()
        {
            Trace.Listeners.Remove(name);
            this.writer.Dispose();
        }

        public override string ToString()
        {
            Trace.Flush();
            return this.writer.ToString();
        }

        public string[] ToLines()
        {
            return this.ToString()
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}

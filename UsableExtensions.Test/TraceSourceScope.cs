using System;
using System.Diagnostics;

namespace UsableExtensions.Test
{
    public class TraceSourceScope : IDisposable
    {
        private bool _isDisposed;

        public string Operation { get; }

        public TraceSourceScope(string operation)
        {
            this.Operation = operation;
            Trace.WriteLine($"Enter: {this.Operation}");
            Trace.Indent();
        }

        public void Dispose()
        {
            if (this._isDisposed)
            {
                throw new ObjectDisposedException(nameof(TraceSourceScope));
            }
            Trace.Unindent();
            Trace.WriteLine($"Leave: {this.Operation}");
            this._isDisposed = true;
        }
    }
}

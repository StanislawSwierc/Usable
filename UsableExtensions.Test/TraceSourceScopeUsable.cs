using System;

namespace UsableExtensions.Test
{
    public class TraceSourceScopeUsable : IUsable<TraceSourceScope>
    {
        public string Operation { get; }

        public TraceSourceScopeUsable (string operation)
        {
            Operation = operation;
        }

        public TResult Use<TResult>(Func<TraceSourceScope, TResult> func)
        {
            using( var scope = new TraceSourceScope(this.Operation))
            {
                return func(scope);
            }
        }
    }
}

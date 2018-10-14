using System;

namespace UsableExtensions
{
    public interface IUsable<T>
    {
        TResult Use<TResult>(Func<T, TResult> func);
    }
}

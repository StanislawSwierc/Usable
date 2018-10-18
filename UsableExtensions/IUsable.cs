using System;

namespace UsableExtensions
{
    public interface IUsable<out T>
    {
        TResult Use<TResult>(Func<T, TResult> func);
    }
}

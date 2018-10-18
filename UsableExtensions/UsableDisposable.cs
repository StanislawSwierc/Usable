using System;

namespace UsableExtensions
{
    public static class UsableDisposable
    {
        public static IUsable<TResult> SelectMany<TOuter, TInner, TResult>(
            this IUsable<TOuter> outerUsable,
            Func<TOuter, TInner> innerDisposableSelector,
            Func<TOuter, TInner, TResult> resultSelector)
            where TInner : IDisposable
        {
            return new SelectManyDisposableUsable<TOuter, TInner, TResult>(
                outerUsable, innerDisposableSelector, resultSelector);
        }

        private class SelectManyDisposableUsable<TOuter, TInner, T> : IUsable<T>
            where TInner : IDisposable
        {
            private readonly IUsable<TOuter> source;
            private readonly Func<TOuter, TInner> collectionSelector;
            private readonly Func<TOuter, TInner, T> resultSelector;

            public SelectManyDisposableUsable(
                IUsable<TOuter> outerUsable,
                Func<TOuter, TInner> innerDisposableSelector,
                Func<TOuter, TInner, T> resultSelector)
            {
                this.source = outerUsable;
                this.collectionSelector = innerDisposableSelector;
                this.resultSelector = resultSelector;
            }

            public TResult Use<TResult>(Func<T, TResult> func)
            {
                return source.Use(outer =>
                {
                    using (var inner = collectionSelector(outer))
                    {
                        return func(resultSelector(outer, inner));
                    }
                });
            }
        }
    }
}

using System;

namespace UsableExtensions
{
    public static class UsableDisposable
    {
        public static IUsable<TResult> SelectMany<TOuter, TInner, TResult>(
            this IUsable<TOuter> outerUsable,
            Func<TOuter, TInner> innerDisposableSelector,
            Func<TOuter, TInner, TResult> resultSelector,
            bool dummy = true)
            where TInner : IDisposable
        {
            return new SelectManyDisposableUsable<TOuter, TInner, TResult>(
                outerUsable, innerDisposableSelector, resultSelector);
        }

        public static IUsable<TResult> SelectMany<TOuter, TInner, TResult>(
            this Func<TOuter> outerDisposableFactory,
            Func<TOuter, TInner> innerDisposableSelector,
            Func<TOuter, TInner, TResult> resultSelector)
            where TOuter : IDisposable
            where TInner : IDisposable
        {
            return new SelectManyDisposableUsable<TOuter, TInner, TResult>(
                Usable.Using(outerDisposableFactory), innerDisposableSelector, resultSelector);
        }

#if DISPOSABLE_ENTRY_BAD_IDEA
        // This overload seemed like a good idea, but it turned out to be a terrible one. Since the
        // main (this) type parameter is TOuter (no interface), compiler selects this method before
        // checking type constraints of IDisposable. This leads to ambiguity during overload
        // resolution and breaks some of the examples.

        public static IUsable<TResult> SelectMany<TOuter, TInner, TResult>(
            this TOuter outerDisposable,
            Func<TOuter, TInner> innerDisposableSelector,
            Func<TOuter, TInner, TResult> resultSelector,
            bool dispose = true)
            where TOuter : IDisposable
            where TInner : IDisposable
        {
            return new SelectManyDisposableUsable<TOuter, TInner, TResult>(
                dispose ? outerDisposable.AsUsableOnece() : outerDisposable.AsUsable(),
                innerDisposableSelector,
                resultSelector);
        }
#endif

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

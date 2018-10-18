using System;

namespace UsableExtensions
{
    public static class UsableDisposable
    {
        public static IUsable<TResult> Select<T, TResult>(
            this IUsable<T> source,
            Func<T, TResult> selector)
        {
            // This is a workaround for the limitations of extension method selection procedure
            // by the compiler. Instead of using 'where' I can test selector if it can be assigned
            // to a Func<T, IDispatchable>. This works thanks to covariant type parameters of this
            // delegate.
            return (selector as Func<T, IDisposable> != null)
                ? new SelectCastDisposableUsable<T, TResult>(source, selector) as IUsable<TResult>
                : new SelectUsable<T, TResult>(source, selector) as IUsable<TResult>;
        }

        public static IUsable<TResult> SelectMany<TOuter, TInner, TResult>(
            this IUsable<TOuter> outerUsable,
            Func<TOuter, TInner> innerDisposableSelector,
            Func<TOuter, TInner, TResult> resultSelector)
            where TInner : IDisposable
        {
            return new SelectManyDisposableUsable<TOuter, TInner, TResult>(
                outerUsable, innerDisposableSelector, resultSelector);
        }

        private class SelectUsable<TOuter, T> : IUsable<T>
        {
            private readonly IUsable<TOuter> source;
            private readonly Func<TOuter, T> selector;

            public SelectUsable(
                IUsable<TOuter> source,
                Func<TOuter, T> selector)
            {
                this.source = source;
                this.selector = selector;
            }

            public TResult Use<TResult>(Func<T, TResult> func)
            {
                return source.Use(outer =>
                {
                    return func(selector(outer));
                });
            }
        }

        internal class SelectDisposableUsable<TOuter, T> : IUsable<T>
            where T : IDisposable
        {
            private readonly IUsable<TOuter> source;
            private readonly Func<TOuter, T> selector;

            public SelectDisposableUsable(
                IUsable<TOuter> source,
                Func<TOuter, T> selector)
            {
                this.source = source;
                this.selector = selector;
            }

            public TResult Use<TResult>(Func<T, TResult> func)
            {
                return source.Use(outer =>
                {
                    using (var inner = selector(outer))
                    {
                        return func(inner);
                    }
                });
            }
        }

        internal class SelectCastDisposableUsable<TOuter, T> : IUsable<T>
        {
            private readonly IUsable<TOuter> source;
            private readonly Func<TOuter, T> selector;

            public SelectCastDisposableUsable(
                IUsable<TOuter> source,
                Func<TOuter, T> selector)
            {
                this.source = source;
                this.selector = selector;
            }

            public TResult Use<TResult>(Func<T, TResult> func)
            {
                return source.Use(outer =>
                {
                    var inner = selector(outer);
                    try
                    {
                        return func(inner);
                    }
                    finally
                    {
                        if (inner is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                    }
                });
            }
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

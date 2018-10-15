using System;

namespace UsableExtensions
{
    public static partial class Usable
    {
        public static T Value<T>(this IUsable<T> usable)
        {
            return usable.Use(value => value);
        }

        public static IUsable<TResult> Select<T, TResult>(
                this IUsable<T> source,
                Func<T, TResult> selector)
        {
            return new SelectUsable<T, TResult>(source, selector);
        }

        public static IUsable<TResult> SelectMany<TOuter, TInner, TResult>(
            this IUsable<TOuter> source,
            Func<TOuter, IUsable<TInner>> collectionSelector,
            Func<TOuter, TInner, TResult> resultSelector)
        {
            return new SelectManyUsable<TOuter, TInner, TResult>(
                source, collectionSelector, resultSelector);
        }

        public static IUsable<TResult> SelectMany<TOuter, TInner, TResult>(
            this IUsable<TOuter> outerUsable,
            Func<TOuter, TInner> innerDisposableSelector,
            Func<TOuter, TInner, TResult> resultSelector,
            bool dispose = true)
            where TInner : IDisposable
        {
            return new SelectManyDisposableUsable<TOuter, TInner, TResult>(
                outerUsable, innerDisposableSelector, resultSelector, dispose);
        }


        #region Inner classes

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

        private class SelectManyUsable<TOuter, TInner, T> : IUsable<T>
        {
            private readonly IUsable<TOuter> source;
            private readonly Func<TOuter, IUsable<TInner>> collectionSelector;
            private readonly Func<TOuter, TInner, T> resultSelector;

            public SelectManyUsable(
                IUsable<TOuter> source,
                Func<TOuter, IUsable<TInner>> collectionSelector,
                Func<TOuter, TInner, T> resultSelector)
            {
                this.source = source;
                this.collectionSelector = collectionSelector;
                this.resultSelector = resultSelector;
            }

            public TResult Use<TResult>(Func<T, TResult> func)
            {
                return source.Use(outerScope =>
                {
                    return collectionSelector(outerScope).Use(innerScope =>
                    {
                        return func(resultSelector(outerScope, innerScope));
                    });
                });
            }
        }

        private class SelectManyDisposableUsable<TOuter, TInner, T> : IUsable<T>
            where TInner : IDisposable
        {
            private readonly IUsable<TOuter> source;
            private readonly Func<TOuter, TInner> collectionSelector;
            private readonly Func<TOuter, TInner, T> resultSelector;
            private bool dispose;

            public SelectManyDisposableUsable(
                IUsable<TOuter> outerUsable,
                Func<TOuter, TInner> innerDisposableSelector,
                Func<TOuter, TInner, T> resultSelector,
                bool dispose)
            {
                this.source = outerUsable;
                this.collectionSelector = innerDisposableSelector;
                this.resultSelector = resultSelector;
                this.dispose = dispose;
            }

            public TResult Use<TResult>(Func<T, TResult> func)
            {
                return source.Use(outer =>
                {
                    var inner = default(TInner);
                    try
                    {
                        inner = collectionSelector(outer);
                        return func(resultSelector(outer, inner));
                    }
                    finally
                    {
                        if (dispose && inner != null)
                        {
                            inner.Dispose();
                        }
                    }
                });
            }
        }

        #endregion
    }
}

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
            this IUsable<TOuter> outerUsable,
            Func<TOuter, IUsable<TInner>> innerUsableSelector,
            Func<TOuter, TInner, TResult> resultSelector)
        {
            return new SelectManyUsable<TOuter, TInner, TResult>(
                outerUsable, innerUsableSelector, resultSelector);
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

        internal class SelectManyUsable<TOuter, TInner, T> : IUsable<T>
        {
            private readonly IUsable<TOuter> outerUsable;
            private readonly Func<TOuter, IUsable<TInner>> innerUsableSelector;
            private readonly Func<TOuter, TInner, T> resultSelector;

            public SelectManyUsable(
                IUsable<TOuter> outerUsable,
                Func<TOuter, IUsable<TInner>> innerUsableSelector,
                Func<TOuter, TInner, T> resultSelector)
            {
                this.outerUsable = outerUsable;
                this.innerUsableSelector = innerUsableSelector;
                this.resultSelector = resultSelector;
            }

            public TResult Use<TResult>(Func<T, TResult> func)
            {
                return outerUsable.Use(outerScope =>
                {
                    return innerUsableSelector(outerScope).Use(innerScope =>
                    {
                        return func(resultSelector(outerScope, innerScope));
                    });
                });
            }
        }
    }
}

using System;

namespace UsableExtensions
{
    public static partial class Usable
    {
        public static T Value<T>(this IUsable<T> usable)
        {
            return usable.Use(value => value);
        }

        public static IUsable<TResult> SelectMany<TOuter, TInner, TResult>(
            this IUsable<TOuter> outerUsable,
            Func<TOuter, IUsable<TInner>> innerUsableSelector,
            Func<TOuter, TInner, TResult> resultSelector)
        {
            return new SelectManyUsable<TOuter, TInner, TResult>(
                outerUsable, innerUsableSelector, resultSelector);
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

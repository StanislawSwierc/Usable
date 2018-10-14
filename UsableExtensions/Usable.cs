using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace UsableExtensions
{
    public static class Usable
    {
        public static IUsable<T> Default<T>() =>
            DefaultUsable<T>.Instance;

        public static IUsable<T> Create<T>(Func<T> create)
            where T : IDisposable =>
            CreateUsable<T>(create);

        public static IUsable<Stopwatch> Stopwatch() =>
            new StopwatchUsable();

        public static IUsable<T> AsUsable<T>(this T value) =>
            new ValueUsable<T>(value);

        public static IUsable<T> AsUsableOnece<T>(this T value)
            where T : IDisposable =>
            new UsableOnce<T>(value);

        public static T Value<T>(this IUsable<T> usable)
        {
            return usable.Use(value => value);
        }

        public static IUsable<TResult> SelectMany<TOuter, TInner, TResult>(
            this IUsable<TOuter> source,
            Func<TOuter, IUsable<TInner>> collectionSelector,
            Func<TOuter, TInner, TResult> resultSelector)
        {
            return new SelectMany2Usable<TOuter, TInner, TResult>(
                source, collectionSelector, resultSelector);
        }

        public static IUsable<TResult> SelectMany<TOuter, TInner, TResult>(
            this IUsable<TOuter> outerUsable,
            Func<TOuter, TInner> innerDisposableSelector,
            Func<TOuter, TInner, TResult> resultSelector,
            bool dispose = true)
            where TInner : IDisposable
        {
            return new SelectMany3Usable<TOuter, TInner, TResult>(
                outerUsable, innerDisposableSelector, resultSelector, dispose);
        }

        public static IUsable<TResult> Select<T, TResult>(
            this IUsable<T> source,
            Func<T, TResult> selector)
        {
            return new SelectUsable<T, TResult>(source, selector);
        }

        #region Inner classes

        public class DefaultUsable<T> : IUsable<T>
        {
            public static readonly IUsable<T> Instance =
                new DefaultUsable<T>();

            public TResult Use<TResult>(Func<T, TResult> func) =>
                func(default(T));
        }

        public class CreateUsable<T> : IUsable<T>
            where T : IDisposable
        {
            private readonly Func<T> create;

            public CreateUsable(Func<T> create) =>
                this.create = create;

            public TResult Use<TResult>(Func<T, TResult> func)
            {
                using (var scope = this.create())
                {
                    return func(scope);
                }
            }
        }

        public class ValueUsable<T> : IUsable<T>
        {
            private readonly T value;

            public ValueUsable(T value) =>
                this.value = value;

            public TResult Use<TResult>(Func<T, TResult> func) =>
                func(this.value);
        }

        public class StopwatchUsable : IUsable<Stopwatch>
        {
            public TResult Use<TResult>(Func<Stopwatch, TResult> func)
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var result = func(stopwatch);

                stopwatch.Stop();
                return result;
            }
        }

        public class UsableOnce<T> : IUsable<T>
            where T : IDisposable
        {
            private readonly T value;

            public UsableOnce(T value) =>
                this.value = value;

            public TResult Use<TResult>(Func<T, TResult> func)
            {
                using (this.value)
                {
                    return func(value);
                }
            }
        }

        public class SelectManyUsable<TOuter, TInner> : IUsable<TInner>
        {
            private readonly IUsable<TOuter> source;
            private readonly Func<TOuter, IUsable<TInner>> selector;

            public SelectManyUsable(
                IUsable<TOuter> source,
                Func<TOuter, IUsable<TInner>> selector)
            {
                this.source = source;
                this.selector = selector;
            }

            public TResult Use<TResult>(Func<TInner, TResult> func)
            {
                return source.Use(outerScope =>
                    selector(outerScope).Use(func));
            }
        }

        public class SelectMany2Usable<TOuter, TInner, T> : IUsable<T>
        {
            private readonly IUsable<TOuter> source;
            private readonly Func<TOuter, IUsable<TInner>> collectionSelector;
            private readonly Func<TOuter, TInner, T> resultSelector;

            public SelectMany2Usable(
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

        public class SelectMany3Usable<TOuter, TInner, T> : IUsable<T>
            where TInner : IDisposable
        {
            private readonly IUsable<TOuter> source;
            private readonly Func<TOuter, TInner> collectionSelector;
            private readonly Func<TOuter, TInner, T> resultSelector;
            private bool dispose;

            public SelectMany3Usable(
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

        public class SelectUsable<TOuter, T> : IUsable<T>
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


        #endregion
    }
}

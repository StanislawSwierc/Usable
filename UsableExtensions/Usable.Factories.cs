using System;
using System.Diagnostics;

namespace UsableExtensions
{
    public static partial class Usable
    {
        public static IUsable<T> Default<T>() =>
            DefaultUsable<T>.Instance;

        public static IUsable<T> Create<T>(Func<T> setup, Action<T> cleanup) =>
            new CreateUsable<T>(setup, cleanup);

        public static IUsable<T> Using<T>(Func<T> create)
            where T : IDisposable =>
            new DisposableUsable<T>(create);

        public static IUsable<T> AsUsable<T>(this T value) =>
            new ValueUsable<T>(value);

        public static IUsable<T> AsUsableOnece<T>(this T value)
            where T : IDisposable =>
            new UsableOnce<T>(value);

        public static IUsable<Stopwatch> Stopwatch() =>
            new StopwatchUsable();


        #region Inner classes

        private class DefaultUsable<T> : IUsable<T>
        {
            public static readonly IUsable<T> Instance =
                new DefaultUsable<T>();

            public TResult Use<TResult>(Func<T, TResult> func) =>
                func(default(T));
        }

        private class CreateUsable<T> : IUsable<T>
        {
            private readonly Func<T> setup;
            private readonly Action<T> cleanup;

            public CreateUsable(Func<T> setup, Action<T> cleanup)
            {
                this.setup = setup;
                this.cleanup = cleanup;
            }

            public TResult Use<TResult>(Func<T, TResult> func)
            {
                var scope = this.setup();
                try
                {
                    return func(scope);
                }
                finally
                {
                    this.cleanup(scope);
                }
            }
        }

        private class DisposableUsable<T> : IUsable<T>
            where T : IDisposable
        {
            private readonly Func<T> create;

            public DisposableUsable(Func<T> create) =>
                this.create = create;

            public TResult Use<TResult>(Func<T, TResult> func)
            {
                using (var scope = this.create())
                {
                    return func(scope);
                }
            }
        }

        private class UsableOnce<T> : IUsable<T>
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

        private class ValueUsable<T> : IUsable<T>
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

        #endregion
    }
}

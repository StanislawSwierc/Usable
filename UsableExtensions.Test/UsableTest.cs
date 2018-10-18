using System;
using Xunit;

namespace UsableExtensions.Test
{
    public class UsableTest
    {
        [Fact]
        public void WhenInnerCreatedFromOuter_ThenDisposedInTheRightOrder()
        {
            using (var trace = new TraceListenerScope())
            {
                var usable =
                    from outer in new TraceSourceScope("outer").AsUsableOnece()
                    from inner in new TraceSourceScope($"{outer.Operation}/inner")
                    select $"{inner.Operation}/value".Trace();
                var expectedTrace = new string[]
                {
                    "Enter: outer",
                    "    Enter: outer/inner",
                    "        Value: outer/inner/value",
                    "    Leave: outer/inner",
                    "Leave: outer"
                };

                var value = usable.Value();

                Assert.Equal("outer/inner/value", value);
                Assert.Equal(expectedTrace, trace.ToLines());
            }
        }

        [Fact]
        public void WhenUsableCreatedInFirstFrom_ThenInstanceExists()
        {
            using (var trace = new TraceListenerScope())
            {
                var usable =
                    from outer in new TraceSourceScope("outer").AsUsableOnece()
                    from inner in new TraceSourceScope($"{outer.Operation}/inner")
                    select $"{inner.Operation}/value".Trace();

                // TraceSourceScope is created and then converted to IUsable. This instance exists
                // whether usable was used or not. It gets disposed after usable is used.
                var expectedTrace = new string[]
                {
                    "Enter: outer",
                };

                Assert.Equal(expectedTrace, trace.ToLines());

                // Clean up.
                var value = usable.Value();
            }
        }

        [Fact]
        public void WhenTraceSourceScopeCreatedWithUsableCreate_ThenItIsLazy()
        {
            using (var trace = new TraceListenerScope())
            {
                var usable =
                    from outer in Usable.Create(() => new TraceSourceScope("outer"))
                    from inner in new TraceSourceScope($"{outer.Operation}/inner")
                    select $"{inner.Operation}/value".Trace();

                Assert.Equal(new string[] { }, trace.ToLines());

                var value = usable.Value();
            }
        }

        [Fact]
        public void WhenTraceSourceScopeCreatedFromUsable_ThenItIsLazy()
        {
            using (var trace = new TraceListenerScope())
            {
                var usable =
                    from name in "outer".AsUsable()
                    from outer in new TraceSourceScope(name)
                    from inner in new TraceSourceScope($"{outer.Operation}/inner")
                    select $"{inner.Operation}/value".Trace();

                Assert.Equal(new string[] { }, trace.ToLines());

                var value = usable.Value();
            }
        }

        [Fact]
        public void Select_WhenDisposableSelector_ThenUsableDoesNotcallDispose()
        {
            using (var trace = new TraceListenerScope())
            {
                var usable =
                    from name in "outer".AsUsable()
                    select new TraceSourceScope(name);
                var expectedTrace = new string[]
                {
                    "Enter: outer",
                };

                var value = usable.Value();

                Assert.Equal(expectedTrace, trace.ToLines());

                value.Dispose();
            }
        }

        /// <remarks>
        /// This tests highlights how <see cref="IUsable{T}"/> should NOT be used. Disposable
        /// resource is returned directly in the select expression without wrapping it into
        /// usable. Since there is no usable created, the resource will not be released and the
        /// tracess will be a mess.
        /// </remarks>
        [Fact]
        public void SelectMany_WhenDisposableSelector_ThenUsableDoesNotcallDispose()
        {
            using (var trace = new TraceListenerScope())
            {
                var usable =
                    from name in "outer".AsUsable()
                    from outer in new TraceSourceScope(name)
                    select new TraceSourceScope("inner");
                var expectedTrace = new string[]
                {
                    "Enter: outer",
                    "    Enter: inner",
                    "    Leave: outer",
                };

                var value = usable.Value();

                Assert.Equal(expectedTrace, trace.ToLines());

                value.Dispose();
            }
        }

        /// <remarks>
        /// This test demonstrates how <see cref="IUsable{T}"/> should be used. It correctly
        /// composes all the resources with from expressions. Although, inner is used in the select
        /// expression, it is already wrapped in an usable, thus it is correctly relesed after
        /// value is retrieved. This is visible both in traces and in the assertion which checks
        /// for <see cref="ObjectDisposedException"/> exception.
        /// </remarks>
        [Fact]
        public void SelectMany_WhenDisposableSelectorFrom_ThenUsableCallsDispose()
        {
            using (var trace = new TraceListenerScope())
            {
                var usable =
                    from name in "outer".AsUsable()
                    from outer in new TraceSourceScope(name)
                    from inner in new TraceSourceScope("inner")
                    select inner;
                var expectedTrace = new string[]
                {
                    "Enter: outer",
                    "    Enter: inner",
                    "    Leave: inner",
                    "Leave: outer",
                };

                var value = usable.Value();

                Assert.Equal(expectedTrace, trace.ToLines());
                Assert.Throws<ObjectDisposedException>(() => value.Dispose());
            }
        }

        /// <remarks>
        /// In this test an explicit cast to <see cref="IUsable{TraceSourceScope}"/> is needed
        /// because when compiler encounters a class which implements <see cref="IUsable{T}"/>
        /// it still gives higher priority to the SelectMany which accepts <see cref="IDisposable"/>.
        /// </remarks>
        [Fact]
        public void SelectMany_WhenCompositingWithClassWhichImplementsIUsable_ThenCastIsNeeded()
        {
            using (var trace = new TraceListenerScope())
            {
                var usable =
                    from outer in new TraceSourceScopeUsable("outer")
                    from inner in new TraceSourceScopeUsable("inner") as IUsable<TraceSourceScope>
                    select string.Join('/', outer.Operation, inner.Operation, "value").Trace();
                var expectedTrace = new string[]
                {
                    "Enter: outer",
                    "    Enter: inner",
                    "        Value: outer/inner/value",
                    "    Leave: inner",
                    "Leave: outer"
                };

                var value = usable.Value();

                Assert.Equal(expectedTrace, trace.ToLines());
            }
        }
    }
}

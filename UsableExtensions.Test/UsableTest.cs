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
        public void Select_WhenDisposableSelector_ThenUsableCallsDispose()
        {
            using (var trace = new TraceListenerScope())
            {
                var usable =
                    from name in "outer".AsUsable()
                    select new TraceSourceScope(name);
                var expectedTrace = new string[]
                {
                    "Enter: outer",
                    "Leave: outer"
                };

                var value = usable.Value();

                Assert.Throws<ObjectDisposedException>(() => value.Dispose());
                Assert.Equal(expectedTrace, trace.ToLines());
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

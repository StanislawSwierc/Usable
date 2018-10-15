namespace UsableExtensions.Test
{
    public static class TraceExtensions
    {
        public static T Trace<T>(this T value)
        {
            System.Diagnostics.Trace.WriteLine($"Value: {value}");
            return value;
        }
    }
}

using System;
using System.Diagnostics;

public class Program
{
    private class MockComponent { }

    private class MockGameObject
    {
        private MockComponent _component = new MockComponent();

        public bool TryGetComponent<T>(out T component) where T : class
        {
            if (typeof(T) == typeof(MockComponent))
            {
                component = _component as T;
                return true;
            }
            component = null;
            return false;
        }
    }

    public static void Main(string[] args)
    {
        const int iterations = 100_000_000;
        var gameObject = new MockGameObject();
        MockComponent cachedComponent;

        // Initial setup
        bool hasComponent = gameObject.TryGetComponent(out cachedComponent);

        Console.WriteLine($"Running benchmark with {iterations:N0} iterations...");

        // Benchmark 1: Redundant TryGetComponent
        var sw = Stopwatch.StartNew();
        MockComponent tempComponent;
        for (int i = 0; i < iterations; i++)
        {
            bool success = gameObject.TryGetComponent(out tempComponent);
            if (success) { var x = tempComponent; }
        }
        sw.Stop();
        double timeWithCall = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine($"With redundant call: {timeWithCall:F2} ms");

        // Benchmark 2: Cached Access (Optimized)
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            if (hasComponent) { var x = cachedComponent; }
        }
        sw.Stop();
        double timeCached = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine($"With cached access: {timeCached:F2} ms");

        if (timeCached > 0)
        {
            double improvement = timeWithCall / timeCached;
            Console.WriteLine($"Improvement factor: {improvement:F2}x faster");
        }
    }
}

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace Benchmark
{
    class Program
    {
        class VisitorSpawner
        {
            public string Name = "Spawner";
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Running Benchmark: FindObjectOfType vs Direct Reference");

            // Setup
            List<object> sceneObjects = new List<object>();
            for (int i = 0; i < 1000; i++)
            {
                sceneObjects.Add(new object());
            }
            VisitorSpawner spawner = new VisitorSpawner();
            sceneObjects.Add(spawner); // Add spawner at the end to simulate worst case or average case

            // Shuffle
            Random rng = new Random();
            int n = sceneObjects.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                object value = sceneObjects[k];
                sceneObjects[k] = sceneObjects[n];
                sceneObjects[n] = value;
            }

            int iterations = 100000;
            VisitorSpawner cachedSpawner = spawner;

            // Benchmark 1: FindObjectOfType Simulation
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                VisitorSpawner found = null;
                foreach (var obj in sceneObjects)
                {
                    if (obj is VisitorSpawner)
                    {
                        found = (VisitorSpawner)obj;
                        break;
                    }
                }
            }
            sw.Stop();
            long findTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"FindObjectOfType Simulation ({iterations} iterations): {findTime} ms");

            // Benchmark 2: Direct Reference
            sw.Restart();
            for (int i = 0; i < iterations; i++)
            {
                VisitorSpawner found = cachedSpawner;
            }
            sw.Stop();
            long directTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"Direct Reference ({iterations} iterations): {directTime} ms");

            Console.WriteLine($"Improvement: {(double)findTime / directTime}x faster");
        }
    }
}
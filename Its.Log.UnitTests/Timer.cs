using System;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Its.Log.Instrumentation.UnitTests
{
    public static class Timer
    {
        /// <summary>
        /// Times the specified operation and writes the result to the console.
        /// </summary>
        /// <param name="operation">A delegate to an operation to be timed.</param>
        /// <param name="iterations">The number of iterations to perform.</param>
        /// <param name="label">The label for the console output.</param>
        /// <returns>
        /// The time that the operation took.
        /// </returns>
        public static TimeSpan TimeOperation(Action<int> operation, int iterations, string label = "time", bool parallelize = false)
        {
            GC.Collect();

            // JIT run
            operation(-1);

            // warmup
            Thread.Sleep(2000);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (!parallelize)
            {
                for (var i = 0; i < iterations; i++)
                {
                    operation(i);
                }
            }
            else
            {
                Parallel.For(0, iterations, new ParallelOptions { MaxDegreeOfParallelism = 50 }, operation);
            }

            stopwatch.Stop();

            Console.WriteLine("{0}: {1}ms ({2} iterations)", label, stopwatch.ElapsedMilliseconds, iterations);

            return stopwatch.Elapsed;
        }
    }
}
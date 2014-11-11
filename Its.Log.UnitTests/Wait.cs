using System;
using System.Diagnostics;
using System.Threading;

namespace Its.Log.Instrumentation.UnitTests
{
    /// <summary>
    /// Methods for waiting on async operations
    /// </summary>
    [DebuggerStepThrough]
    public static class Wait
    {
        /// <summary>
        /// Waits until the specified function evaluates to true
        /// </summary>
        /// <param name="isTrue">A function to be evaluated periodically</param>
        /// <returns>True if the function evaluates to true; otherwise, false.</returns>
        public static bool Until(Func<bool> isTrue)
        {
            while (!isTrue())
            {
                Thread.Sleep(10);
            }
            return true;
        }

        /// <summary>
        /// Waits until the specified function evaluates to true
        /// </summary>
        /// <param name="isTrue">A function to be evaluated periodically</param>
        /// <returns>True if the function evaluates to true before the specified timeout; otherwise, false.</returns>
        public static bool Until(Func<bool> isTrue, int timeOutMs)
        {
            int count = 0;
            while (!isTrue())
            {
                count += 100;
                Thread.Sleep(100);
                if (count > timeOutMs)
                {
                    return false;
                }
            }
            return true;
        }

        public static void For(TimeSpan duration)
        {
            var i = 0;
            var millisecondsToWait = duration.TotalMilliseconds;
            while (i < millisecondsToWait)
            {
                i += 10;
                Thread.Sleep(10);
            }
        }
    }
}
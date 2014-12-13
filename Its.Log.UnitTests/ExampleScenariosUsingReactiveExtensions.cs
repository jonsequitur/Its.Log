// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Web;
using Its.Log.Instrumentation.Extensions;
using Moq;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Its.Log.Instrumentation.UnitTests
{
    [Explicit("Samples only"), Category("Demo")]
    [TestFixture]
    public class ExampleScenariosUsingReactiveExtensions
    {
        public static IObservable<LogEntry> LogEvents()
        {
            return Observable
                .FromEventPattern<InstrumentationEventArgs>(
                    h => Log.EntryPosted += h, h => Log.EntryPosted -= h)
                .Select(e => e.EventArgs.LogEntry);
        }

        public static IObservable<LogEntry> LogErrorEvents()
        {
            return Observable
                .FromEventPattern<InstrumentationEventArgs>(
                    h => Log.InternalErrors += h, h => Log.InternalErrors -= h)
                .Select(e => e.EventArgs.LogEntry);
        }

        [SetUp]
        public void CleanUp()
        {
            Log.UnsubscribeAllFromEntryPosted();
            Log.Formatters.Clear();
            Extension.EnableAll();
        }

        /// <summary>
        ///   Demonstrates a continuing observable sequence of new instances at a defined interval.
        /// </summary>
        [Test]
        public void Heartbeat()
        {
            var ping = Observable.Interval(TimeSpan.FromSeconds(5)).Select(e => DateTime.Now);

            using (ping.Subscribe(time => Console.WriteLine(time)))
            {
                Thread.Sleep(TimeSpan.FromSeconds(45));
            }
        }

        public void Cumulative_digest()
        {
            var digestUpdates = LogEvents()
                .Scan(new ConcurrentDictionary<string, int>(),
                      (digest, e) =>
                      {
                          digest.AddOrUpdate(e.Message, 0, (m, i) => digest[m] + 1);
                          return digest;
                      })
                .Sample(TimeSpan.FromSeconds(1));

            using (digestUpdates.Subscribe(digest => Console.WriteLine(digest.ToLogString())))
            {
                for (var i = 0; i < 1000; i++)
                {
                    Log.Write("x");
                    Log.Write("y");
                    Log.Write("y");
                    Log.Write("z");
                    Log.Write("z");
                    Log.Write("z");
                    Thread.Sleep(10);
                }
            }
        }

        [Test]
        public void Count_of_a_specific_event_type_per_time_period()
        {
            // we just want to count 404s because there are so many
            var _404s = Observable.Interval(TimeSpan.FromSeconds(.011))
                .Select(_ => new HttpException(404, "Not found"))
                .Publish();

            var countOf404s = LogEvents()
                .Where(e => e.SubjectIs<HttpException>())
                .Where(e => e.GetSubject<HttpException>().GetHttpCode() == 404)
                .Buffer(TimeSpan.FromSeconds(1))
                .Select(es => es.Count());

            using (_404s.Connect())
            using (_404s.Subscribe(Log.Write))
            using (countOf404s.LogToConsole())
            {
                Wait.For(TimeSpan.FromSeconds(10));
            }
        }

        [Test]
        public virtual void Different_verbosity_based_on_event_type()
        {
            // we just want to count 404s because there are so many
            var _404s = Observable.Interval(TimeSpan.FromSeconds(.02))
                .Select(_ => new HttpException(404, "Not found"));

            // 500s are always interesting so we want to log them fully
            var _500s = Observable.Interval(TimeSpan.FromSeconds(2.5))
                .Select(_ => new HttpException(500, "Internal server error"));
            var exceptions = _404s.Merge(_500s);
            var allEvents = LogEvents().Publish();

            var throttled = allEvents
                .Where(e => e.SubjectIs<HttpException>(ex => ex.GetHttpCode() == 404))
                .Buffer(TimeSpan.FromSeconds(3))
                .Select(i => new LogEntry(string.Format("Found {0} 404s", i.Count)))
                .Merge(
                    allEvents.Where(e => !e.SubjectIs<HttpException>(ex => ex.GetHttpCode() == 404)));

            using (exceptions.Subscribe(Log.Write))
            using (allEvents.Connect())
            using (throttled.LogToConsole())
            {
                Wait.For(TimeSpan.FromSeconds(10));
            }
        }

        [Test]
        public void Throttling_per_timespan()
        {
            var throttledEvents = LogEvents().Sample(TimeSpan.FromSeconds(1));
            var count = 0;

            using (throttledEvents.Subscribe(e =>
            {
                count++;
                Console.WriteLine(e.Message);
            }))
            {
                foreach (var i in Enumerable.Range(1, 1000))
                {
                    Log.Write(i);
                }

                Thread.Sleep(TimeSpan.FromSeconds(1.1));

                Log.Write("one more");

                Thread.Sleep(TimeSpan.FromSeconds(1.1));
            }

            Assert.That(count,
                        Is.EqualTo(2));
        }

        [Test]
        public virtual void Log_only_after_a_threshold_is_reached()
        {
            var problems = LogEvents().SkipUntil(
                LogEvents().Where(e => e.Subject is Exception));
            var observer = new Mock<IObserver<LogEntry>>();
            observer.Setup(o => o.OnNext(It.IsAny<LogEntry>()));

            using (problems.Subscribe(e => Console.WriteLine(e.Message)))
            using (problems.Subscribe(observer.Object))
            {
                var thread = new Thread(
                    () =>
                    {
                        for (var i = 0; i <= 5000; i++)
                        {
                            // this won't appear in the log until the exceptions start happening:
                            Log.Write("Message " + i);

                            if (i > 4995)
                            {
                                Log.Write(() => new InvalidOperationException());
                            }
                        }
                    });
                thread.Start();
                Thread.Sleep(5000);
                thread.Abort();
            }

            observer.Verify(
                o => o.OnNext(It.IsAny<LogEntry>()),
                Times.AtMost(8));
        }
    }
}
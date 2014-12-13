// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using Its.Log.Instrumentation.Extensions;
using Its.Recipes;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Its.Log.Instrumentation.UnitTests
{
    [Category("Demo")]
    [TestFixture]
    public class Demo
    {
        [Test]
        public void Basics1_Logging_an_object_without_subscribing_to_EntryPosted_does_nothing()
        {
            Log.Write(Movie.StarWars);
            Log.Write(() => Movie.StarWars);
        }

        [Test]
        public void Basics2_Logging_an_object_to_the_console()
        {
            Log.EntryPosted += (sender, e) => Console.WriteLine(e.LogEntry.ToLogString());

            // note the difference in the output between these two methods
            Log.Write(Movie.StarWars);
            Log.Write(() => Movie.StarWars);
        }

        [Test]
        public void Basics3_You_control_where_entries_get_written()
        {
            Log.EntryPosted += (sender, e) =>
            {
                LogEntry logEntry = e.LogEntry;

                if (logEntry.Subject is Exception)
                {
                    Trace.TraceError(logEntry.ToLogString());
                }

                Trace.TraceInformation(logEntry.ToLogString());
            };

            Log.Write(() => "hello");
            Log.Write(() => new Exception("oops"));
        }

        [Test]
        public void Formatting1_Format_the_log_output_so_that_it_is_more_descriptive()
        {
            Log.EntryPosted += (sender, e) => Console.WriteLine(e.LogEntry.ToLogString());
            Formatter<Movie>.RegisterForAllMembers();
            Formatter<Person>.RegisterForAllMembers();

            Log.Write(() => Movie.StarWars);
        }

        [Test]
        public void Formatting2_Format_the_log_output_so_that_it_not_tooooo_descriptive()
        {
            Log.EntryPosted += (sender, e) => Console.WriteLine(e.LogEntry.ToLogString());

            // only certain properties...
            Formatter<Movie>.RegisterForMembers(m => m.Title, m => m.Stars);

            // or specify your own function...
            Formatter<Person>.Register(p => string.Format("{0} (born: {1})", p.Name, p.DateOfBirth));

            Log.Write(() => Movie.StarWars);
        }

        [Test]
        public void Formatting3_Exception_formatting_is_very_descriptive_by_default()
        {
            Log.EntryPosted += (sender, e) => Console.WriteLine(e.LogEntry.ToLogString());

            var dataException = new DataException("oops!");
            dataException.Data["why?"] = "because";
            var outerException = new ReflectionTypeLoadException(new[] { GetType() }, new[] { dataException });

            try
            {
                throw outerException;
            }
            catch (Exception ex)
            {
                Log.Write(() => ex);
            }
        }

        [Test]
        public void Formatting4_Every_exception_gets_an_unique_id_which_is_shared_among_inner_and_outer_exceptions()
        {
            Log.EntryPosted += (sender, e) => Console.WriteLine(e.LogEntry.ToLogString());

            // scope down the formatting to show just the relevant bits
            Formatter<LogEntry>.RegisterForMembers(e => e.ExceptionId, e => e.EventType, e => e.Subject);
            Formatter<DataException>.RegisterForMembers(e => e.Data, e => e.InnerException);
            Formatter<NullReferenceException>.RegisterForMembers(e => e.Data, e => e.InnerException);
            Formatter<InvalidOperationException>.RegisterForMembers(e => e.Data, e => e.InnerException);
            ;

            var dataException = new DataException("oops!");
            dataException.Data["why?"] = "because";
            var nullReferenceException = new NullReferenceException("oh my.", dataException);
            var outerException = new InvalidOperationException("oh noes!", nullReferenceException);

            try
            {
                throw outerException;
            }
            catch (Exception ex)
            {
                Log.Write(() => ex);
            }
        }

        [Test]
        public void Formatting5_Logging_several_objects()
        {
            Log.EntryPosted += (sender, e) => Console.WriteLine(e.LogEntry.ToLogString());
            Formatter<Movie>.RegisterForAllMembers();
            Formatter<Person>.RegisterForAllMembers();

            var carrie = Person.CarrieFisher;
            Log.Write(() => new { carrie, Person.GeorgeLucas });
        }

        [Test]
        public void Formatting6_What_about_long_lists()
        {
            Log.EntryPosted += (sender, e) => Console.WriteLine(e.LogEntry.ToLogString());
            Formatter<Movie>.RegisterForAllMembers();
            Formatter<Person>.RegisterForAllMembers();

            Formatter.ListExpansionLimit = 5;
            Log.Write(() => Movie.StarWars);

            Formatter.ListExpansionLimit = 10;
            Log.Write(() => Movie.StarWars);
        }

        [Test]
        public void Formatting8_What_about_recursion()
        {
            Log.EntryPosted += (sender, e) => Console.WriteLine(e.LogEntry.ToLogString());
            Formatter<Movie>.RegisterForAllMembers();
            Formatter<Person>.RegisterForAllMembers();

            // set up a recursive relationship that the above properties formatters will traverse
            Person.GeorgeLucas.Movies = new[] { Movie.StarWars };

            Formatter.RecursionLimit = 3;
            Log.Write(() => Person.GeorgeLucas);

            Formatter.RecursionLimit = 10;
            Log.Write(() => Person.GeorgeLucas);
        }

        [Test]
        public void LogEntryObject1_Has_a_subject()
        {
            Log.EntryPosted += (sender, e) =>
                               Assert.That(e.LogEntry.Subject,
                                           Is.EqualTo(Person.GeorgeLucas));

            Log.Write(() => new { Person.GeorgeLucas });
        }

        [Test]
        public void LogEntryObject2_Has_information_about_the_call_source()
        {
            Log.EntryPosted += (sender, e) => Console.WriteLine(e.LogEntry.ToLogString());

            // this is already in the default output, but it's also in the LogEntry object, so we'll create a formatter that exposes it directly
            Formatter<LogEntry>.RegisterForMembers(
                e => e.CallingType,
                e => e.CallingMethod);

            Log.Write(() => new { Person.GeorgeLucas });
        }

        [Test]
        public void LogEntryObject3_Has_a_time_stamp()
        {
            Log.EntryPosted += (sender, e) => Console.WriteLine(e.LogEntry.ToLogString());

            Formatter<LogEntry>.RegisterForMembers(e => e.TimeStamp);

            Log.Write(() => new { Person.GeorgeLucas });
        }

        [Test]
        public void LogEntryObject4_Has_a_unique_exception_id_if_the_subject_is_an_exception()
        {
            Log.EntryPosted += (sender, e) => Console.WriteLine(e.LogEntry.ToLogString());

            Formatter<LogEntry>.RegisterForMembers(e => e.ExceptionId);

            Log.Write(() => new { Person.GeorgeLucas });
            Log.Write(() => new Exception("oops"));
        }

        [Test]
        public void MethodBoundaries1_Logging_method_boundaries()
        {
            Log.EntryPosted += (sender, e) => Console.WriteLine(e.LogEntry.ToLogString());

            // again, isolating just the parts that are specific to boundary logging:
            Formatter<LogEntry>.RegisterForMembers(
                e => e.Message,
                e => e.Params,
                e => e.ElapsedMilliseconds);

            var birthdays = new List<DateTime>();
            using (var activity = Log.Enter(() => new { birthdays }))
            {
                birthdays.Add(Person.MarkHamill.DateOfBirth);
                activity.Trace("adding a birthday...");
                birthdays.Add(Person.CarrieFisher.DateOfBirth);
            }
        }

        [Test]
        public void MethodBoundaries2_Disabling_method_boundary_logging()
        {
            Log.EntryPosted += (sender, e) => Console.WriteLine(e.LogEntry.ToLogString());

            Extension<Boundaries>.Disable();

            var foo = "foo";
            using (var activity = Log.Enter(() => new { foo }))
            {
                foo = " bar";
                activity.Trace(() => foo);
                Log.Write(() => "just a normal log entry");
                foo = "baz";
            }
        }

        [Test]
        public void MethodBoundaries3_Counting_method_calls()
        {
            Extension<Counter>.Enable();
            Console.WriteLine(Counter.For<Movie>(m => m.Watch()).Count);

            for (int i = 0; i < 500; i++)
            {
                Movie.StarWars.Watch();
            }

            Console.WriteLine(Counter.For<Movie>(m => m.Watch()).Count);
        }

        [Test]
        public void MethodBoundaries4_Getting_method_timings()
        {
            Log.EntryPosted += (s, e) =>
            {
                if (e.LogEntry.EventType == TraceEventType.Stop &&
                    e.LogEntry.HasExtension<MyPerfCounter>())
                {
                    e.LogEntry.GetExtension<MyPerfCounter>().Write(e.LogEntry.ElapsedMilliseconds.Value);
                }
            };

            using (Log.With<MyPerfCounter>(counter => counter.Name = "MethodBoundaries4").Enter(() => { }))
            {
                Thread.Sleep(Any.Int(10, 300));
            }
        }

        public class MyPerfCounter
        {
            public string Name { get; set; }

            public void Write(long milliseconds)
            {
                Console.WriteLine(string.Format("Perf counter {0} recorded {1}ms", Name, milliseconds));
            }
        }

        [Test]
        public void Reactive1_Subscribing_to_events_using_Reactive()
        {
            IObservable<LogEntry> logEvents = Observable
                .FromEventPattern<InstrumentationEventArgs>(
                    h => Log.EntryPosted += h, h => Log.EntryPosted -= h)
                .Select(e => e.EventArgs.LogEntry);

            using (logEvents.Subscribe(entry => Console.WriteLine(entry.ToLogString())))
            {
                Log.Write(() => Movie.StarWars);
            }
        }

        [Test]
        public void Reactive2_Verbosity_can_be_left_up_to_the_subscriber()
        {
            var logEvents = Observable
                .FromEventPattern<InstrumentationEventArgs>(
                    h => Log.EntryPosted += h, h => Log.EntryPosted -= h)
                .Select(e => e.EventArgs.LogEntry);

            var exceptions = logEvents.Where(e => e.Subject is Exception);

            using (exceptions.Subscribe(entry => Console.WriteLine(entry.ToLogString())))
            {
                for (var i = 0; i < 100; i++)
                {
                    Log.Write(() => Movie.StarWars);
                    if (i == 99)
                    {
                        Log.Write(() => new InvalidOperationException());
                    }
                }
            }
        }

        [Test]
        public void Reactive3_Subscribing_to_events_only_from_specific_classes()
        {
            var logEvents = Observable
                .FromEventPattern<InstrumentationEventArgs>(
                    h => Log.EntryPosted += h, h => Log.EntryPosted -= h)
                .Select(e => e.EventArgs.LogEntry);

            var movieLogEvents = logEvents
                .Where(e => e.CallingType == typeof (Movie));

            using (movieLogEvents.Subscribe(entry => Console.WriteLine(entry.ToLogString())))
            {
                Log.Write(() => "hello from Demo class");
                Movie.StarWars.Watch();
            }
        }

        [Test]
        public void Reactive4_Disable_logging_from_specific_classes()
        {
            var logEvents = Observable
                .FromEventPattern<InstrumentationEventArgs>(
                    h => Log.EntryPosted += h, h => Log.EntryPosted -= h)
                .Select(e => e.EventArgs.LogEntry);

            Extension<Boundaries>.DisableFor<Movie>();

            using (logEvents.Subscribe(entry => Console.WriteLine(entry.ToLogString())))
            {
                Log.Write(() => "hello from Demo class");
                Movie.StarWars.Watch();
            }
        }

        [SetUp]
        public void SetUp()
        {
            Log.UnsubscribeAllFromEntryPosted();
            Formatter.ResetToDefault();
            Extension.EnableAll();
        }
    }


    #region test classes

    public class Movie
    {
        public string Title { get; set; }
        public IEnumerable<Person> Stars { get; set; }
        public Person Director { get; set; }
        public int Year { get; set; }

        public void Watch()
        {
            using (Log.Enter(() => { }))
            {
            }
        }

        public static readonly Movie StarWars = new Movie
        {
            Title = "Star Wars: Episode IV - A New Hope",
            Director = Person.GeorgeLucas,
            Stars =
                new[]
                {
                    Person.CarrieFisher, Person.HarrisonFord,
                    Person.MarkHamill
                },
            Year = 1977,
            FullCast = new[]
            {
                new Person { Name = "Mark Hamill" },
                new Person { Name = "Harrison Ford" },
                new Person { Name = "Carrie Fisher" },
                new Person { Name = "Peter Cushing" },
                new Person { Name = "Alec Guinness" },
                new Person { Name = "Anthony Daniels" },
                new Person { Name = "Kenny Baker" },
                new Person { Name = "Peter Mayhew" },
                new Person { Name = "David Prowse" },
                new Person { Name = "James Earl Jones" },
                new Person { Name = "Phil Brown" },
                new Person { Name = "Shelagh Fraser" },
                new Person { Name = "Jack Purvis" },
                new Person { Name = "Alex McCrindle" },
                new Person { Name = "Eddie Byrne" },
                new Person { Name = "Drewe Henley" },
                new Person { Name = "Denis Lawson" },
                new Person { Name = "Garrick Hagon" },
                new Person { Name = "Jack Klaff" },
                new Person { Name = "William Hootkins" },
                new Person { Name = "Angus MacInnes" },
                new Person { Name = "Jeremy Sinden" },
                new Person { Name = "Graham Ashley" },
                new Person { Name = "Don Henderson" },
                new Person { Name = "Richard LeParmentier" },
                new Person { Name = "Leslie Schofield" },
                new Person { Name = "David Ankrum" },
                new Person { Name = "Mark Austin" },
                new Person { Name = "Scott Beach" },
                new Person { Name = "Lightning Bear" },
                new Person { Name = "Jon Berg" },
                new Person { Name = "Doug Beswick" },
                new Person { Name = "Paul Blake" },
                new Person { Name = "Janice Burchette" },
                new Person { Name = "Ted Burnett" },
                new Person { Name = "John Chapman" },
                new Person { Name = "Gilda Cohen" },
                new Person { Name = "Tim Condren" },
                new Person { Name = "Barry Copping" },
                new Person { Name = "Alfie Curtis" },
                new Person { Name = "Robert Davies" },
                new Person { Name = "Maria De Aragon" },
                new Person { Name = "Robert A. Denham" },
                new Person { Name = "Frazer Diamond" },
                new Person { Name = "Peter Diamond" },
                new Person { Name = "Warwick Diamond" },
                new Person { Name = "Sadie Eden" },
                new Person { Name = "Kim Falkinburg" },
                new Person { Name = "Harry Fielder" },
                new Person { Name = "Ted Gagliano" },
                new Person { Name = "Salo Gardner" },
                new Person { Name = "Steve Gawley" },
                new Person { Name = "Barry Gnome" },
                new Person { Name = "Rusty Goffe" },
                new Person { Name = "Isaac Grand" },
                new Person { Name = "Nelson Hall" },
                new Person { Name = "Reg Harding" },
                new Person { Name = "Alan Harris" },
                new Person { Name = "Frank Henson" },
                new Person { Name = "Christine Hewett" },
                new Person { Name = "Arthur Howell" },
                new Person { Name = "Tommy Ilsley" },
                new Person { Name = "Joe Johnston" },
                new Person { Name = "Annette Jones" },
                new Person { Name = "Linda Jones" },
                new Person { Name = "Joe Kaye" },
                new Person { Name = "Colin Michael Kitchens" },
                new Person { Name = "Melissa Kurtz" },
                new Person { Name = "Tiffany L. Kurtz" },
                new Person { Name = "Al Lampert" },
                new Person { Name = "Anthony Lang" },
                new Person { Name = "Laine Liska" },
                new Person { Name = "Derek Lyons" },
                new Person { Name = "Mahjoub" },
                new Person { Name = "Alf Mangan" },
                new Person { Name = "Rick McCallum" },
                new Person { Name = "Grant McCune" },
                new Person { Name = "Jeff Moon" },
                new Person { Name = "Mandy Morton" },
                new Person { Name = "Lorne Peterson" },
                new Person { Name = "Marcus Powell" },
                new Person { Name = "Shane Rimmer" },
                new Person { Name = "Pam Rose" },
                new Person { Name = "George Roubicek" },
                new Person { Name = "Erica Simmons" },
                new Person { Name = "Angela Staines" },
                new Person { Name = "George Stock" },
                new Person { Name = "Roy Straite" },
                new Person { Name = "Peter Sturgeon" },
                new Person { Name = "Peter Sumner" },
                new Person { Name = "John Sylla" },
                new Person { Name = "Tom Sylla" },
                new Person { Name = "Malcolm Tierney" },
                new Person { Name = "Phil Tippett" },
                new Person { Name = "Burnell Tucker" },
                new Person { Name = "Morgan Upton" },
                new Person { Name = "Jerry Walter" },
                new Person { Name = "Hal Wamsley" },
                new Person { Name = "Larry Ward" },
                new Person { Name = "Diana Sadley Way" },
                new Person { Name = "Harold Weed" },
                new Person { Name = "Bill Weston" },
                new Person { Name = "Steve 'Spaz' Williams" },
                new Person { Name = "Fred Wood" }
            },
        };

        public IEnumerable<Person> FullCast { get; set; }
    }

    public class Person
    {
        public string Name { get; set; }
        public DateTime DateOfBirth { get; set; }
        public IEnumerable<Movie> Movies { get; set; }

        public static readonly Person GeorgeLucas = new Person
        {
            Name = "George Lucas",
            DateOfBirth = new DateTime(1944, 5, 14)
        };

        public static readonly Person CarrieFisher = new Person
        {
            Name = "Carrie Fisher",
            DateOfBirth = new DateTime(1956, 10, 21)
        };

        public static readonly Person HarrisonFord = new Person
        {
            Name = "Harrison Ford",
            DateOfBirth = new DateTime(1942, 7, 13)
        };

        public static readonly Person MarkHamill = new Person
        {
            Name = "Mark Hamill",
            DateOfBirth = new DateTime(1951, 9, 25)
        };
    }

    #endregion
}
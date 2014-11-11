using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Its.Log.Instrumentation.Extensions;
using NUnit.Framework;
using Moq;
using Assert = NUnit.Framework.Assert;

namespace Its.Log.Instrumentation.UnitTests
{
    /// <summary>
    ///   Unit tests for <see cref = "LogEntry" />
    /// </summary>
    [TestFixture]
    public class LogEntryTests
    {
        [SetUp]
        public void SetUp()
        {
            Formatter.ResetToDefault();
        }

        [Test]
        public void When_subject_is_string_message_is_subject()
        {
            LogEntry entry = null;
            using (Log.Events().Subscribe(e => entry = e))
            {
                Log.Write("this is the message");
            }

            Assert.AreEqual(entry.Subject, entry.Message);
            Assert.AreEqual("this is the message", entry.Message);
        }
        
        [Test]
        public void When_subject_is_Func_of_string_then_message_is_subject()
        {
            LogEntry entry = null;
            using (Log.Events().Subscribe(e => entry = e))
            {
                Log.Write(() => "this is the message");
            }

            Assert.That(entry.Message, Is.EqualTo("this is the message"));
            Assert.That(entry.Subject, Is.EqualTo("this is the message"));
        }

        [Test]
        public void When_subject_is_string_and_comment_is_specified_then_the_message_is_set_to_the_value_of_the_comment()
        {
            LogEntry entry = null;
            using (Log.Events().Subscribe(e => entry = e))
            {
                Log.Write("this is the message", "this is the comment");
            }

            Assert.AreEqual("this is the comment", entry.Message);
        }

        [Test]
        public void When_subject_is_Func_and_comment_is_specified_then_the_message_is_set_to_the_value_of_the_comment()
        {
            LogEntry entry = null;
            using (Log.Events().Subscribe(e => entry = e))
            {
                Log.Write(() => new Widget(), "this is the comment");
            }

            Assert.AreEqual("this is the comment", entry.Message);
            entry.Subject.Should().BeOfType<Widget>();
        }

        [Test]
        public void Default_EventType_is_Information()
        {
            Assert.That(new LogEntry(new object()).EventType,
                        Is.EqualTo(TraceEventType.Information));
        }

        [Test]
        public void EventType_defaults_to_Error_when_Subject_is_Exception()
        {
            Assert.That(new LogEntry(new Exception()).EventType,
                        Is.EqualTo(TraceEventType.Error));
        }

        [Test]
        public void EventType_defaults_to_Warning_when_Subject_is_Exception_that_has_been_handled()
        {
            Assert.That(new LogEntry(new Exception().MarkAsHandled()).EventType,
                        Is.EqualTo(TraceEventType.Warning));
        }

        [Test]
        public void EventType_defaults_to_Critical_when_Subject_is_fatal_Exception()
        {
            Assert.That(new LogEntry(new OutOfMemoryException()).EventType,
                        Is.EqualTo(TraceEventType.Critical));
        }

        [Test]
        public void Write_passing_func_that_returns_string_correctly_describes_subject()
        {
            var observer = new Mock<IObserver<LogEntry>>();
            observer.Setup(
                e => e.OnNext(It.Is<LogEntry>((LogEntry entry) => entry.Message == "hello")));

            using (observer.Object.SubscribeToLogEvents())
            {
                Log.Write(() => "hello");
            }

            observer.VerifyAll();
        }

        [Test]
        public void Write_passing_func_that_returns_anonymous_type_describes_subject_properties_without_formatter()
        {
            var observer = new Mock<IObserver<LogEntry>>();
            observer.Setup(
                e => e.OnNext(
                    It.Is<LogEntry>(
                        entry => entry.ToString().Contains("hello") &&
                                 entry.ToString().Contains("there") &&
                                 entry.ToString().Contains("number") &&
                                 entry.ToString().Contains("42")
                        )));

            using (TestHelper.LogToConsole())
            using (observer.Object.SubscribeToLogEvents())
            {
                Log.Write(() => new { hello = "there", number = 42 });
            }

            observer.VerifyAll();
        }

        [Test]
        public virtual void Write_passing_func_that_returns_anonymous_type_describes_subject_properties_using_formatter()
        {
            Formatter<Widget>.RegisterForAllMembers();
            var log = "";
            var widget = new Widget { Name = "Hula Hoop" };

            using (Log.Events().Subscribe(e => log += e.ToLogString()))
            {
                Log.Write(() => new { widget });
            }

            Assert.That(log, Contains.Substring("Hula Hoop"));
        }

        [Test]
        public virtual void Write_passing_func_LogEntry_has_correct_calling_method()
        {
            var thisMethodName = MethodBase.GetCurrentMethod().Name;
            var observer = new Mock<IObserver<LogEntry>>();
            observer.Setup(
                e => e.OnNext(It.Is<LogEntry>(entry =>
                                                    entry.CallingMethod == thisMethodName)));

            using (TestHelper.OnEntryPosted(e => Console.WriteLine(e.ToLogString())))
            using (observer.Object.SubscribeToLogEvents())
            {
                Log.Write(() => "hello");
            }

            observer.VerifyAll();
        }

        [Test]
        public void Generic_LogEntry_can_be_passed_to_Log_Write()
        {
            Log.Write(new LogEntry<string>("hello"));
        }

        [Test]
        public void WithExtension_adds_new_extension_instance()
        {
            var logEntry = new LogEntry(new object());
            logEntry.WithExtension<EventLogInfo>();

            Assert.That(logEntry.HasExtension<EventLogInfo>());
        }

        [Test]
        public void WithExtension_returns_existing_extension_of_specified_type()
        {
            var logEntry = new LogEntry(new object());
            logEntry.WithExtension<EventLogInfo>().EventId = 2;

            Assert.That(logEntry.WithExtension<EventLogInfo>().EventId,
                        Is.EqualTo(2));
        }

        [Test]
        public void Non_generic_LogEntry_ToString_uses_ToLogString()
        {
            var logEntryString = "this is a log entry";
            Log.Formatters.RegisterFormatter<LogEntry>(e => logEntryString);

            Assert.That(new LogEntry("non generic").ToString(),
                        Is.EqualTo(logEntryString));
        }
        
        [Test]
        public void Generic_LogEntry_ToString_uses_ToLogString()
        {
            var logEntryString = "this is a log entry";
            
            Log.Formatters.RegisterFormatter<LogEntry>(e => logEntryString);

            Assert.That(new LogEntry<string>("generic").ToString(),
                        Is.EqualTo(logEntryString));
        }

        [Test]
        public void LogEntry_T_does_not_generate_formatter_for_string()
        {
            var log = "";
            using (Log.Events().Subscribe(e => log += e.ToLogString()))
            {
                Log.Write(() => "hello");
            }

            Assert.That(log,
                        Contains.Substring("hello"));
        }

        [Test]
        public void LogEntry_T_does_not_generate_formatter_for_array()
        {
            var log = "";
            using (Log.Events().Subscribe(e => log += e.ToLogString()))
            {
                Log.Write(() => new[] { 1, 2, 3, 4 });
            }

            Assert.That(log,
                        !Contains.Substring("Length"));
        }

        [Test]
        public void Subject_does_not_apear_in_params()
        {
            var entry = new LogEntry<int>(123);

            Assert.That(
                entry.ToString().Split(new[] { "123" }, StringSplitOptions.None).Length, 
                Is.EqualTo(2));
        }

        [Test]
        public void Keys_and_values_added_using_AddInfo_appear_in_formatted_output()
        {
            var logEntry = new LogEntry("hello");
            var guid = Guid.NewGuid();
            logEntry.AddInfo("some_guid", guid);

            Console.WriteLine(logEntry.ToString());

            Assert.That(
                logEntry.ToString(),
                Is.StringContaining("some_guid"));

            Assert.That(
                logEntry.ToString(),
                Is.StringContaining(guid.ToString()));
        }
    }
}
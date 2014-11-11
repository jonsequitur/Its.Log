using System;
using System.Linq.Expressions;
using NUnit.Framework;

namespace Its.Log.Instrumentation.UnitTests
{
    [TestFixture]
    public class AnonymousMethodInfoTests
    {
        [Test]
        public void AnonymousMethodInfo_does_not_throw_when_created_using_expression()
        {
            using (TestHelper.OnEntryPosted(e => Console.WriteLine("{0}.{1}", e.CallingType, e.CallingMethod)))
            {
                Expression<Func<string, Logger>> getLogger = s =>
                                                             new Logger
                                                                 {
                                                                     // because this is within an expression, it compiles as an expression rather than an anonymous method, which leads to a somewhat different evaluation
                                                                     Log = o => Log.Write(() => new { o })
                                                                 };

                getLogger.Compile().Invoke("my logger").Log(new Widget());
            }
        }

        private class Logger
        {
            public Action<object> Log { get; set; }
        }
    }
}
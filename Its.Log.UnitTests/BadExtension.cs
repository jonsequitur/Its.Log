using System;

namespace Its.Log.Instrumentation.UnitTests
{
    public class ExtensionThatThrowsInProperty
    {
        static ExtensionThatThrowsInProperty()
        {
            Log.Formatters.RegisterPropertiesFormatter<ExtensionThatThrowsInProperty>();
        }

        public string Oooops
        {
            get
            {
                throw new Exception("This is a test exception");
            }
        }
    }

    public class ExtensionThatThrowsInCtor
    {
        public ExtensionThatThrowsInCtor()
        {
            throw new Exception("This is a test exception");
        }
    }
}
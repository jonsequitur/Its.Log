// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
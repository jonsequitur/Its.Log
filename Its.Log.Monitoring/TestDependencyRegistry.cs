// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Its.Log.Monitoring
{
    public class TestDependencyRegistry
    {
        private readonly Action<Type, Func<object>> onRegister;

        internal TestDependencyRegistry(Action<Type, Func<object>> onRegister)
        {
            this.onRegister = onRegister;
        }

        public TestDependencyRegistry Register<T>(Func<T> getDependency)
        {
            onRegister(typeof (T), () => getDependency());
            return this;
        }
    }
}
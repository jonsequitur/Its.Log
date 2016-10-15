// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Its.Log.Instrumentation.Extensions
{
    /// <summary>
    /// Provides control over which extensions are enabled and for which classes.
    /// </summary>
    /// <typeparam name="T">The type of the extension to configure.</typeparam>
    public class Extension<T> : Extension
    {
        private static readonly ConcurrentDictionary<Type, bool> disabledExtensions =
            new ConcurrentDictionary<Type, bool>();

        private static bool isEnabled = true;

        static Extension()
        {
            if (typeof (ILogExtension).IsAssignableFrom(typeof (T)))
            {
                ActivateOnEnter = typeof (IApplyOnEnter).IsAssignableFrom(typeof (T));
                ActivateOnExit = typeof (IApplyOnExit).IsAssignableFrom(typeof (T));
            }
            else
            {
                ActivateOnEnter = true;
                ActivateOnExit = false;
            }

            // subscribe to the global change event
            EnableAllSignaled += (o, e) =>
                                     {
                                         isEnabled = true;
                                         disabledExtensions.Clear();
                                     };
        }

        public static bool ActivateOnEnter { get; private set; }

        public static bool ActivateOnExit { get; private set; }

        /// <summary>
        ///   Gets a value indicating whether extension type <typeparamref name = "T" /> is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if extension type <typeparamref name = "T" /> is enabled; otherwise, <c>false</c>.
        /// </value>
        public static bool IsEnabled => isEnabled;

        /// <summary>
        ///   Disables the specified Extension type <typeparamref name = "T" /> for all instrumented classes.
        /// </summary>
        public static void Disable() => isEnabled = false;

        /// <summary>
        ///   Disables the specified Extension type <typeparamref name = "T" /> for instrumented class <typeparamref
        ///    name = "TForClass" />.
        /// </summary>
        public static void DisableFor<TForClass>() where TForClass : class => 
            DisableFor(typeof (TForClass));

        /// <summary>
        ///   Enables the specified Extension type <typeparamref name="T" /> for all instrumented classes.
        /// </summary>
        public static void Enable() => isEnabled = true;

        /// <summary>
        ///   Enables the specified Extension type <typeparamref name = "T" /> for instrumented class <typeparamref name = "TForClass" />.
        /// </summary>
        public static void EnableFor<TForClass>() where TForClass : class => EnableFor(typeof (TForClass));

        /// <summary>
        ///   Determines whether extension <typeparamref name = "T" /> is enable the specified for instrumenation events sent from instances of type <paramref name = "forClass" />.
        /// </summary>
        /// <param name = "forClass">The instrumented class.</param>
        public static bool IsEnabledFor(Type forClass)
        {
            if (forClass == null || disabledExtensions.ContainsKey(forClass))
            {
                return false;
            }
            return IsEnabled;
        }

        /// <summary>
        ///   Determines whether extension <typeparamref name = "T" /> is enable the specified for instrumenation events sent from instances of type <typeparamref name = "TForClass" />.
        /// </summary>
        /// <typeparam name = "TForClass">The instrumented class.</typeparam>
        public static bool IsEnabledFor<TForClass>() => IsEnabledFor(typeof (TForClass));

        /// <summary>
        ///   Disables the specified Extension type <typeparamref name = "T" /> for instrumented class <paramref name = "forClass" />.
        /// </summary>
        public static void DisableFor(Type forClass) => disabledExtensions.TryAdd(forClass, true);

        /// <summary>
        ///   Enables the specified Extension type <typeparamref name = "T" /> for instrumented class <paramref name = "forClass" />.
        /// </summary>
        public static void EnableFor(Type forClass)
        {
            bool result;
            disabledExtensions.TryRemove(forClass, out result);
        }
    }
}
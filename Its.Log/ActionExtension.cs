// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Its.Log.Instrumentation
{
    internal static class ActionExtensions
    {
        private static readonly ConcurrentDictionary<MethodInfo, AnonymousMethodInfo> anonymousMethodInfos =
            new ConcurrentDictionary<MethodInfo, AnonymousMethodInfo>();

        public static AnonymousMethodInfo GetAnonymousMethodInfo<T>(this Func<T> anonymousMethod)
        {
            return anonymousMethodInfos.GetOrAdd(anonymousMethod.Method, m => new AnonymousMethodInfo<T>(anonymousMethod));
        }

        public static AnonymousMethodInfo GetAnonymousMethodInfo(this Delegate anonymousMethod)
        {
            return anonymousMethodInfos.GetOrAdd(anonymousMethod.Method, m => new AnonymousMethodInfo(anonymousMethod.Method));
        }

        internal static void InvokeSafely<T>(this Action<T> action, T target)
        {
            try
            {
                action(target);
            }
            catch (Exception ex)
            {
                ex.RaiseErrorEvent();
                if (ex.ShouldThrow())
                {
                    throw;
                }
            }
        }

        internal static ILogActivity InvokeSafely<T>(this Func<T, ILogActivity> action, T target)
        {
            try
            {
                return action(target);
            }
            catch (Exception ex)
            {
                ex.RaiseErrorEvent();
                if (ex.ShouldThrow())
                {
                    throw;
                }
                return NullLogActivity.Instance;
            }
        }
        
        internal static object InvokeSafely(this Func<object> func)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                ex.RaiseErrorEvent();
                if (ex.ShouldThrow())
                {
                    throw;
                }
                return ex;
            }
        }
    }
}
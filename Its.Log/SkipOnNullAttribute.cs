// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Its.Log.Instrumentation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    internal class SkipOnNullAttribute : Attribute
    {
    }
}
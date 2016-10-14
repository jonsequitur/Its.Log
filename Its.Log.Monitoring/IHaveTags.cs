// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Its.Log.Monitoring
{
    public interface IHaveTags : IMonitoringTest
    {
        string[] Tags { get; }
    }
}
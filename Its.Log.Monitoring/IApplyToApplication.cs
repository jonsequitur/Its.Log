// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Its.Log.Monitoring
{
    public interface IApplyToApplication : IMonitoringTest
    {
        bool AppliesToApplication(string application);
    }
}
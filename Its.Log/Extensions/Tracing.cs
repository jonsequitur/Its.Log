// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Its.Log.Instrumentation.Extensions
{
    /// <summary>
    /// Used to correlate trace activity.
    /// </summary>
    public class Tracing
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Tracing"/> class.
        /// </summary>
        public Tracing()
        {
            ActivityId = Trace.CorrelationManager.ActivityId;
        }

        /// <summary>
        /// Gets or sets the activity id.
        /// </summary>
        /// <value>
        /// The activity id.
        /// </value>
        /// <remarks>The default value is obtained from Trace.CorrelationManager.ActivityId at the time the Tracing instance is created.</remarks>
        public Guid ActivityId { get; set; }

        /// <summary>
        /// Gets or sets the related activity id.
        /// </summary>
        /// <value>
        /// The related activity id.
        /// </value>
        /// <remarks>This value can be set to related activities.</remarks>
        public Guid RelatedActivityId { get; set; }
    }
}
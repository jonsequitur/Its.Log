// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http.Routing;

namespace Its.Log.Monitoring
{
    public class AmongConstraint : IHttpRouteConstraint
    {
        public readonly string[] AllowedValues;

        public AmongConstraint(string value)
        {
            AllowedValues = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public bool Match(HttpRequestMessage request,
                          IHttpRoute route,
                          string parameterName,
                          IDictionary<string, object> values,
                          HttpRouteDirection routeDirection)
        {
            object value;

            if (values.TryGetValue(parameterName, out value) && value != null)
            {
                return AllowedValues.Any(allowed => string.Equals(allowed,
                                                                  value.ToString(),
                                                                  StringComparison.OrdinalIgnoreCase));
            }
            return false;
        }
    }
}
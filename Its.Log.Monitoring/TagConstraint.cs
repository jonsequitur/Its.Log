// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Its.Recipes;

namespace Its.Log.Monitoring
{
    internal class TagConstraint : TestConstraint
    {
        private string[] _tags;

        public TagConstraint(TestDefinition testDefinition)
        {
            if (typeof (IHaveTags).IsAssignableFrom(testDefinition.TestType))
            {
                TestDefinition = testDefinition;
            }
            else
            {
                _tags = new string[0];
            }
        }

        protected override bool Match(TestTarget target, HttpRequestMessage request)
        {
            _tags = _tags ?? (_tags = target.ResolveDependency(TestDefinition.TestType)
                                            .IfTypeIs<IHaveTags>()
                                            .Then(test =>
                                            {
                                                var tags = test.Tags;
                                                TestDefinition.Tags = tags;
                                                return tags;
                                            })
                                            .ElseDefault()
                                            .OrEmpty()
                                            .ToArray());

            return DoTestsMatchFilterRequest(_tags, request.GetQueryNameValuePairs().ToArray());
        }

        private static bool DoTestsMatchFilterRequest(string[] testTags, KeyValuePair<string, string>[] filterTags)
        {
            //If no tags were requested, then then it is a match
            if (!filterTags.Any())
            {
                return true;
            }

            var includeTags = filterTags.Where(t => t.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
                                        .Select(t => t.Key)
                                        .ToArray();
            var excludeTags = filterTags.Where(t => t.Value.Equals("false", StringComparison.OrdinalIgnoreCase))
                                        .Select(t => t.Key)
                                        .ToArray();

            return !excludeTags.Intersect(testTags, StringComparer.OrdinalIgnoreCase).Any()  &&
                   includeTags.Intersect(testTags, StringComparer.OrdinalIgnoreCase).Count() == includeTags.Length;
        }
    }
}
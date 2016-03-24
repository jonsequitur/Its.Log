// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Its.Log.Monitoring
{
    public class AggregationAssertions<TState, TResult> where TResult : IComparable<TResult>
    {
        private readonly Aggregation<TState, TResult> Subject;

        public AggregationAssertions(Aggregation<TState, TResult> aggregation)
        {
            if (aggregation == null)
            {
                throw new ArgumentNullException(nameof(aggregation));
            }
            if (aggregation.Result == null)
            {
                throw new ArgumentNullException(nameof(aggregation.Result));
            }
            Subject = aggregation;
        }

        public AggregationAssertions<TState, TResult> BeLessThan(TResult expected, string because = "")
        {
            var compareTo = Subject.Result.CompareTo(expected);
            if (compareTo < 0)
            {
                return this;
            }

            throw new AggregationAssertionException<TState>($"Expected a value less than {expected} {because}, " +
                                                            $"but found {Subject.Result}.", Subject.State);
        }

        public AggregationAssertions<TState, TResult> BeLessThanOrEqualTo(TResult expected, string because = "")
        {
            if (Subject.Result.CompareTo(expected) <= 0)
            {
                return this;
            }
            throw new AggregationAssertionException<TState>($"Expected a value less than or equal to {expected} {because}, " +
                                                            $"but found {Subject.Result}.", Subject.State);
        }

        public AggregationAssertions<TState, TResult> BeGreaterThan(TResult expected, string because = "")
        {
            if (Subject.Result.CompareTo(expected) > 0)
            {
                return this;
            }
            throw new AggregationAssertionException<TState>($"Expected a value greater than {expected} {because}, " +
                                                            $"but found {Subject.Result}.", Subject.State);
        }

        public AggregationAssertions<TState, TResult> BeGreaterThanOrEqualTo(TResult expected, string because = "")
        {
            if (Subject.Result.CompareTo(expected) >= 0)
            {
                return this;
            }
            throw new AggregationAssertionException<TState>($"Expected a value greater than or equal to {expected} {because}, " +
                                                            $"but found {Subject.Result}.", Subject.State);
        }

        public AggregationAssertions<TState, TResult> BeEqualTo(TResult expected, string because = "")
        {
            if (Subject.Result.CompareTo(expected) == 0)
            {
                return this;
            }
            throw new AggregationAssertionException<TState>($"Expected a value equal to {expected} {because}, " +
                                                            $"but found {Subject.Result}.", Subject.State);
        }
    }
}
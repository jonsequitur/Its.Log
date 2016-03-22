using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Its.Log.Monitoring.UnitTests
{
    [TestFixture]
    public class AssertionTests
    {
        private readonly Aggregation<IEnumerable<int>, long> five = Enumerable.Range(1, 5).CountOf(_ => true);
        private readonly IEnumerable<Result> results = Enumerable.Range(1, 10).Select(i => new Result(i%2 == 0));

        public class Result
        {
            public Result(bool success)
            {
                Success = success;
            }

            public bool Success { get; }
        }

        [Test]
        public void BeEqualTo_does_not_throw_when_the_expected_value_is_equal_to_the_actual()
        {
            Action assert = () => five.Should().BeEqualTo(5);
            assert.ShouldNotThrow();
        }

        [Test]
        public void BeEqualTo_throws_when_the_expected_value_is_greater_than_the_actual()
        {
            Action assert = () => five.Should().BeEqualTo(4);
            assert.ShouldThrow<AggregationAssertionException<IEnumerable<int>>>();
        }

        [Test]
        public void BeEqualTo_throws_when_the_expected_value_is_less_than_the_actual()
        {
            Action assert = () => five.Should().BeEqualTo(6);
            assert.ShouldThrow<AggregationAssertionException<IEnumerable<int>>>();
        }

        [Test]
        public void BeGreaterThan_does_not_throw_when_the_expected_value_is_greater_than_the_actual()
        {
            Action assert = () => five.Should().BeGreaterThan(4);
            assert.ShouldNotThrow();
        }

        [Test]
        public void BeGreaterThan_throws_when_the_expected_value_is_equal_to_the_actual()
        {
            Action assert = () => five.Should().BeGreaterThan(5);
            assert.ShouldThrow<AggregationAssertionException<IEnumerable<int>>>();
        }

        [Test]
        public void BeGreaterThan_throws_when_the_expected_value_is_less_than_the_actual()
        {
            Action assert = () => five.Should().BeGreaterThan(6);
            assert.ShouldThrow<AggregationAssertionException<IEnumerable<int>>>();
        }

        [Test]
        public void BeGreaterThanOrEqualTo_does_not_throw_when_the_expected_value_is_equal_to_the_actual()
        {
            Action assert = () => five.Should().BeGreaterThanOrEqualTo(5);
            assert.ShouldNotThrow();
        }

        [Test]
        public void BeGreaterThanOrEqualTo_does_not_throw_when_the_expected_value_is_greater_than_the_actual()
        {
            Action assert = () => five.Should().BeGreaterThanOrEqualTo(4);
            assert.ShouldNotThrow();
        }

        [Test]
        public void BeGreaterThanOrEqualTo_throws_when_the_expected_value_is_less_than_the_actual()
        {
            Action assert = () => five.Should().BeGreaterThanOrEqualTo(6);
            assert.ShouldThrow<AggregationAssertionException<IEnumerable<int>>>();
        }

        [Test]
        public void BeLessThan_does_not_throw_when_the_expected_value_is_less_than_the_actual()
        {
            Action assert = () => five.Should().BeLessThan(6);
            assert.ShouldNotThrow();
        }

        [Test]
        public void BeLessThan_throws_when_the_expected_value_is_equal_to_the_actual()
        {
            Action assert = () => five.Should().BeLessThan(5);
            assert.ShouldThrow<AggregationAssertionException<IEnumerable<int>>>();
        }

        [Test]
        public void BeLessThan_throws_when_the_expected_value_is_greater_than_the_actual()
        {
            Action assert = () => five.Should().BeLessThan(4);
            assert.ShouldThrow<AggregationAssertionException<IEnumerable<int>>>();
        }

        [Test]
        public void BeLessThanOrEqualTo_does_not_throw_when_the_expected_value_is_equal_to_the_actual()
        {
            Action assert = () => five.Should().BeLessThanOrEqualTo(5);
            assert.ShouldNotThrow();
        }

        [Test]
        public void BeLessThanOrEqualTo_does_not_throw_when_the_expected_value_is_less_than_the_actual()
        {
            Action assert = () => five.Should().BeLessThanOrEqualTo(6);
            assert.ShouldNotThrow();
        }

        [Test]
        public void BeLessThanOrEqualTo_throws_when_the_expected_value_is_greater_than_the_actual()
        {
            Action assert = () => five.Should().BeLessThanOrEqualTo(4);
            assert.ShouldThrow<AggregationAssertionException<IEnumerable<int>>>();
        }

        [Test]
        public void CountOf_is_callable_on_an_empty_enumerable()
        {
            Enumerable.Empty<Result>().CountOf(r => true).Result.Should().Be(0);
        }

        [Test]
        public void CountOf_returns_the_expected_amount()
        {
            results.CountOf(r => r.Success).Result.Should().Be(5);
        }

        [Test]
        public void CountOf_returns_the_expected_amount_when_the_amount_of_matches_is_0()
        {
            results.CountOf(r => false).Result.Should().Be(0);
        }

        [Test]
        public void CountOf_returns_the_values_of_the_enumerable_that_met_the_condition()
        {
            results.CountOf(r => r.Success).State.ShouldBeEquivalentTo(results.Where(r => r.Success));
        }

        [Test]
        public void PercentageOf_is_callable_on_an_empty_enumerable()
        {
            Enumerable.Empty<Result>().PercentageOf(r => true).Result.Should().Be(0.Percent());
        }

        [Test]
        public void PercentageOf_returns_the_expected_percentage()
        {
            results.PercentageOf(r => r.Success).Result.Should().Be(50.Percent());
        }

        [Test]
        public void PercentageOf_returns_the_values_of_the_enumerable_that_met_the_condition()
        {
            results.PercentageOf(r => r.Success).State.ShouldBeEquivalentTo(results.Where(r => r.Success));
        }

        [Test]
        public void AggregationAssertionException_contains_TState()
        {
            Action assert = () => five.Should().BeLessThan(4);
            assert.ShouldThrow<AggregationAssertionException<IEnumerable<int>>>()
                  .And.State.ShouldBeEquivalentTo(Enumerable.Range(1, 5));
        }
    }
}
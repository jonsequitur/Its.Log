// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// THIS FILE IS NOT INTENDED TO BE EDITED. 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CSharp.RuntimeBinder;

namespace Its.Recipes
{
    /// <summary>
    ///     Supports chaining of expressions when intermediate values may be null, to support a fluent API style using common .NET types.
    /// </summary>
#if !RecipesProject
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal static partial class MaybeExtensions
    {
        /// <summary>
        ///     Returns either a value or, if it's null, the result of a function that provides an alternate value.
        /// </summary>
        /// <typeparam name="T"> </typeparam>
        /// <param name="value"> The primary value. </param>
        /// <param name="otherValue">
        ///     A function to provide an alternate value if <paramref name="value" /> is null.
        /// </param>
        /// <returns>
        ///     The value of parameter <paramref name="value" /> , unless it's null, in which case the result of calling
        ///     <paramref
        ///         name="otherValue" />
        ///     .
        /// </returns>
        [Obsolete("This will be removed in v2.0.0. Instead, do this: source.IfNotNull().Else(() => ...)")]
        public static T Else<T>(this T value, Func<T> otherValue)
            where T : class
        {
            if (value == null)
            {
                return otherValue();
            }

            return value;
        }

        /// <summary>
        ///     Specifies a function that will be evaluated if the source <see cref="Recipes.Maybe{T}" /> has no value.
        /// </summary>
        /// <typeparam name="T">
        ///     The type held by the <see cref="Recipes.Maybe{T}" />.
        /// </typeparam>
        /// <param name="maybe">The source maybe.</param>
        /// <param name="otherValue">The value to be returned if the <see cref="Recipes.Maybe{T}" /> has no value.</param>
        /// <returns>
        ///     The value of the Maybe if it has a value; otherwise, the value returned by <paramref name="otherValue" />.
        /// </returns>
        public static T Else<T>(this Maybe<T> maybe, Func<T> otherValue)
        {
            if (maybe.HasValue)
            {
                return maybe.Value;
            }

            return otherValue();
        }

        /// <summary>
        ///     Specifies a function that will be evaluated if the source <see cref="Recipes.Maybe{T}" /> has no value.
        /// </summary>
        /// <typeparam name="T">
        ///     The type held by the <see cref="Recipes.Maybe{T}" />.
        /// </typeparam>
        /// <param name="maybe">The source maybe.</param>
        /// <param name="other">The value to be returned if the <see cref="Recipes.Maybe{T}" /> has no value.</param>
        /// <returns>
        ///     The value of the Maybe if it has a value; otherwise, the value returned by <paramref name="other" />.
        /// </returns>
        public static Maybe<T> Else<T>(this Maybe<T> maybe, Maybe<T> other)
        {
            return maybe.HasValue
                       ? maybe
                       : other;
        }

        /// <summary>
        ///     Specifies a function that will be evaluated if the source <see cref="Recipes.Maybe{T}" /> has no value.
        /// </summary>
        /// <typeparam name="T">
        ///     The type held by the <see cref="Recipes.Maybe{T}" />.
        /// </typeparam>
        /// <param name="maybe">The source maybe.</param>
        /// <param name="otherValue">The value to be returned if the <see cref="Recipes.Maybe{T}" /> has no value.</param>
        /// <returns>
        ///     The value of the Maybe if it has a value; otherwise, the value returned by <paramref name="otherValue" />.
        /// </returns>
        public static T Else<T>(this Maybe<Maybe<T>> maybe, Func<T> otherValue)
        {
            if (maybe.HasValue)
            {
                return maybe.Value.Else(otherValue);
            }

            return otherValue();
        }

        /// <summary>
        ///     Specifies a function that will be evaluated if the source <see cref="Recipes.Maybe{T}" /> has no value.
        /// </summary>
        /// <typeparam name="T">
        ///     The type held by the <see cref="Recipes.Maybe{T}" />.
        /// </typeparam>
        /// <param name="maybe">The source maybe.</param>
        /// <param name="otherValue">The value to be returned if the <see cref="Recipes.Maybe{T}" /> has no value.</param>
        /// <returns>
        ///     The value of the Maybe if it has a value; otherwise, the value returned by <paramref name="otherValue" />.
        /// </returns>
        public static T Else<T>(this Maybe<Maybe<Maybe<T>>> maybe, Func<T> otherValue)
        {
            if (maybe.HasValue)
            {
                return maybe.Value.Else(otherValue);
            }

            return otherValue();
        }

        /// <summary>
        ///     Specifies a function that will be evaluated if the source <see cref="Recipes.Maybe{T}" /> has no value.
        /// </summary>
        /// <typeparam name="T">
        ///     The type held by the <see cref="Recipes.Maybe{T}" />.
        /// </typeparam>
        /// <param name="maybe">The source maybe.</param>
        /// <param name="otherValue">The value to be returned if the <see cref="Recipes.Maybe{T}" /> has no value.</param>
        /// <returns>
        ///     The value of the Maybe if it has a value; otherwise, the value returned by <paramref name="otherValue" />.
        /// </returns>
        public static T Else<T>(this Maybe<Maybe<Maybe<Maybe<T>>>> maybe, Func<T> otherValue)
        {
            if (maybe.HasValue)
            {
                return maybe.Value.Else(otherValue);
            }

            return otherValue();
        }

        /// <summary>
        ///     Executes an action if the value of a condition is false.
        /// </summary>
        /// <param name="condition">
        ///     if set to <c>true</c> execute <paramref name="action" /> .
        /// </param>
        /// <param name="action">
        ///     The action to be executed it <paramref name="condition" /> is false..
        /// </param>
        /// <exception cref="ArgumentNullException">action</exception>
        [Obsolete("This will be removed in v2.0.0.")]
        public static void Else(
            this bool condition,
            Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            if (!condition)
            {
                action();
            }
        }

        /// <summary>
        /// Returns the default value for <typeparamref name="T" /> if the <see cref="Recipes.Maybe{T}" /> has no value.
        /// </summary>
        /// <typeparam name="T">
        ///     The type held by the <see cref="Recipes.Maybe{T}" />.
        /// </typeparam>
        public static T ElseDefault<T>(this Maybe<T> maybe)
        {
            return maybe.Else(() => default(T));
        }
        
        /// <summary>
        /// Returns the default value for <typeparamref name="T" /> if the <see cref="Recipes.Maybe{T}" /> has no value.
        /// </summary>
        /// <typeparam name="T">
        ///     The type held by the <see cref="Recipes.Maybe{T}" />.
        /// </typeparam>
        public static T ElseDefault<T>(this Maybe<Maybe<T>> maybe)
        {
            return maybe.Else(() => default(T));
        }

        /// <summary>
        /// Returns the default value for <typeparamref name="T" /> if the <see cref="Recipes.Maybe{T}" /> has no value.
        /// </summary>
        /// <typeparam name="T">
        ///     The type held by the <see cref="Recipes.Maybe{T}" />.
        /// </typeparam>
        public static T ElseDefault<T>(this Maybe<Maybe<Maybe<T>>> maybe)
        {
            return maybe.Else(() => default(T));
        }
        
        /// <summary>
        /// Returns the default value for <typeparamref name="T" /> if the <see cref="Recipes.Maybe{T}" /> has no value.
        /// </summary>
        /// <typeparam name="T">
        ///     The type held by the <see cref="Recipes.Maybe{T}" />.
        /// </typeparam>
        public static T ElseDefault<T>(this Maybe<Maybe<Maybe<Maybe<T>>>> maybe)
        {
            return maybe.Else(() => default(T));
        }

        /// <summary>
        /// Returns null if the source has no value.
        /// </summary>
        /// <typeparam name="T">The type held by the <see cref="Recipes.Maybe{T}" />.</typeparam>
        public static T? ElseNull<T>(this Maybe<T> maybe)
            where T : struct
        {
            if (maybe.HasValue)
            {
                return maybe.Value;
            }

            return null;
        }

        /// <summary>
        /// Performs an action if the <see cref="Recipes.Maybe{T}" /> has no value.
        /// </summary>
        /// <typeparam name="T">
        ///     The type held by the <see cref="Recipes.Maybe{T}" />.
        /// </typeparam>
        public static void ElseDo<T>(this Maybe<T> maybe, Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            if (!maybe.HasValue)
            {
                action();
            }
        }

        /// <summary>
        ///     If the dictionary contains a value for a specified key, executes an action passing the corresponding value.
        /// </summary>
        /// <typeparam name="TKey"> The type of the key. </typeparam>
        /// <typeparam name="TValue"> The type of the value. </typeparam>
        /// <param name="dictionary"> The dictionary. </param>
        /// <param name="key"> The key. </param>
        /// <param name="then">
        ///     An action to be invoked with the value corresponding to <paramref name="key" /> .
        /// </param>
        /// <exception cref="ArgumentNullException">dictionary</exception>
        [Obsolete("This will be removed in v2.0.0. Instead, do this: source.IfContains(\"key\").ThenDo(value => ...)")]
        public static void IfContains<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            Action<TValue> then)
        {
            TValue value;
            if (dictionary != null && dictionary.TryGetValue(key, out value))
            {
                then(value);
            }
        }

        /// <summary>
        ///     If the dictionary contains a value for a specified key, executes an action passing the corresponding value.
        /// </summary>
        /// <typeparam name="TKey"> The type of the key. </typeparam>
        /// <typeparam name="TValue"> The type of the value. </typeparam>
        /// <param name="dictionary"> The dictionary. </param>
        /// <param name="key"> The key. </param>
        /// <exception cref="ArgumentNullException">dictionary</exception>
        public static Maybe<TValue> IfContains<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key)
        {
            TValue value;
            if (dictionary != null && dictionary.TryGetValue(key, out value))
            {
                return Recipes.Maybe<TValue>.Yes(value);
            }

            return Recipes.Maybe<TValue>.No();
        }

        /// <summary>that 
        /// Allows two maybes to be combined.
        /// </summary>
        public static T1 And<T1>(
            this Maybe<T1> first)
        {
            if (first.HasValue)
            {
                return first.Value;
            }

            return default(T1);
        }

        /// <summary>
        /// Attempts to retrieve a value dynamically.
        /// </summary>
        /// <typeparam name="T">The type of the value expected to be returned.</typeparam>
        /// <param name="source">The source object.</param>
        /// <param name="getValue">A delegate that attempts to return a value via a dynamic invocation on the source object.</param>
        /// <remarks>This method will not cast the result value to <typeparamref name="T" />. If the returned value is not of this type, then a negative <see cref="Recipes.Maybe{T}" /> will be returned.</remarks>
        public static Maybe<T> IfHas<T>(
            this object source,
            Func<dynamic, T> getValue)
        {
            try
            {
                var value = getValue(source);
                return value.IfTypeIs<T>();
            }
            catch (RuntimeBinderException)
            {
                return Recipes.Maybe<T>.No();
            }
        }

        /// <summary>
        /// Attempts to perform a series of calls depending on whether the previous calls returned non-null values.
        /// </summary>
        public static Maybe<T3> IfNoneNull<T1, T2, T3>(
            this T1 source,
            Func<T1, T2> first,
            Func<T2, T3> second)
            where T1 : class
            where T2 : class
            where T3 : class
        {
            return source.IfNotNull()
                         .Then(v => first(v).IfNotNull()
                                            .Then(second)
                                            .IfNotNull())
                         .Else(Recipes.Maybe<T3>.No);
        }

        /// <summary>
        /// Attempts to perform a series of calls depending on whether the previous calls returned non-null values.
        /// </summary>
        public static Maybe<T4> IfNoneNull<T1, T2, T3, T4>(
            this T1 source,
            Func<T1, T2> first,
            Func<T2, T3> second,
            Func<T3, T4> third)
            where T1 : class
            where T2 : class
            where T3 : class
            where T4 : class
        {
            return source.IfNoneNull(first, second)
                         .Then(third)
                         .IfNotNull();
        }

        /// <summary>
        /// Creates a <see cref="Recipes.Maybe{T}" />, allowing <see cref="Then" /> and <see cref="Else" /> operations to be chained and evaluated conditionally based on whether source is null. 
        /// </summary>
        /// <typeparam name="T">The type of the instance wrapped by the <see cref="Recipes.Maybe{T}" />.</typeparam>
        /// <param name="source">The source instance, which may be null.</param>
        /// <remarks>This method is equivalent to <see cref="Maybe{T}(T)" />.</remarks>
        public static Maybe<T> IfNotNull<T>(this T source) where T : class
        {
            if (source != null)
            {
                return Recipes.Maybe<T>.Yes(source);
            }

            return Recipes.Maybe<T>.No();
        }
        
        public static Maybe<T> IfNotNull<T>(this Maybe<T> source) where T : class
        {
            if (source.HasValue && source.Value != null)
            {
                return source;
            }

            return Recipes.Maybe<T>.No();
        }

        /// <summary>
        /// Creates a <see cref="Recipes.Maybe{T}" />, allowing <see cref="Then" /> and <see cref="Else" /> operations to be chained and evaluated conditionally based on whether source is null. 
        /// </summary>
        /// <typeparam name="T">The type of the instance wrapped by the <see cref="Recipes.Maybe{T}" />.</typeparam>
        /// <param name="source">The source instance, which may be null.</param>
        public static Maybe<T> IfNotNull<T>(this T? source)
            where T : struct
        {
            if (source.HasValue)
            {
                return Recipes.Maybe<T>.Yes(source.Value);
            }

            return Recipes.Maybe<T>.No();
        }

        /// <summary>
        /// Determines whether a string is null, empty, or consists entirely of whitespace.
        /// </summary>
        /// <param name="value">The string.</param>
        public static Maybe<string> IfNotNullOrEmptyOrWhitespace(this string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return Recipes.Maybe<string>.Yes(value);
            }

            return Recipes.Maybe<string>.No();
        }

        /// <summary>
        ///     Executes a specified action only if the <paramref name="source" /> object is of type <typeparamref name="T" />.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The object.</param>
        /// <param name="action">
        ///     The action to be executed against the specified object, cast to type <typeparamref name="T" />.
        /// </param>
        /// <returns>
        ///     True if the object is of type <typeparamref name="T" />; otherwise, false.
        /// </returns>
        [Obsolete("This will be removed in v2.0.0. Instead, do this: source.IfTypeIs<T>().ThenDo(s => ...)")]
        public static bool IfTypeIs<T>(
            this object source,
            Action<T> action)
        {
            if (source is T)
            {
                action((T) source);

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Returns a Maybe.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <returns></returns>
        public static Maybe<T> IfTypeIs<T>(
            this object source)
        {
            if (source is T)
            {
                return Recipes.Maybe<T>.Yes((T) source);
            }

            return Recipes.Maybe<T>.No();
        }

        /// <summary>
        ///     Returns the value of <paramref name="getValue" /> if <paramref name="source" /> is not null. Otherwise, returns null.
        /// </summary>
        /// <typeparam name="T"> The type of the source object. </typeparam>
        /// <typeparam name="TResult"> The type of the result. </typeparam>
        /// <param name="source"> The source object. </param>
        /// <param name="getValue">
        ///     A delegate specifying a value to retrieve if <paramref name="source" /> is not null.
        /// </param>
        /// <returns>
        ///     If <paramref name="source" /> is not null, the value produced by <paramref name="getValue" /> ; otherwise, null.
        /// </returns>
        [Obsolete("This will be removed in v2.0.0. Instead, do this: source.IfNotNull().Then(s => ...)")]
        public static TResult Maybe<T, TResult>(
            this T source,
            Func<T, TResult> getValue)
            where T : class
            where TResult : class
        {
            return source == null
                       ? null
                       : getValue(source);
        }

        /// <summary>
        ///     Executes an action of <paramref name="action" /> if <paramref name="source" /> is not null. Otherwise, do nothing.
        /// </summary>
        /// <typeparam name="T"> The type of the source object. </typeparam>
        /// <param name="source"> The source object. </param>
        /// <param name="action">
        ///     A delegate specifying an action to take if <paramref name="source" /> is not null.
        /// </param>
        [Obsolete("This will be removed in v2.0.0. Instead, do this: source.IfNotNull().ThenDo(s => ...)")]
        public static void Maybe<T>(this T source, Action<T> action) where T : class
        {
            if (source != null)
            {
                action(source);
            }
        }

        /// <summary>
        /// Creates a <see cref="Recipes.Maybe{T}" />, allowing <see cref="Then" /> and <see cref="Else" /> operations to be chained and evaluated conditionally based on whether source is null. 
        /// </summary>
        /// <typeparam name="T">The type of the instance wrapped by the <see cref="Recipes.Maybe{T}" />.</typeparam>
        /// <param name="source">The source instance, which may be null.</param>
        /// <remarks>This method is equivalent to <see cref="IfNotNull{T}" />.</remarks>
        [Obsolete("This will be removed in v2.0.0. Instead, do this: source.IfNotNull()")]
        public static Maybe<T> Maybe<T>(this T source) where T : class
        {
            return source.IfNotNull();
        }

        /// <summary>
        /// Creates a <see cref="Recipes.Maybe{T}" />, allowing <see cref="Then" /> and <see cref="Else" /> operations to be chained and evaluated conditionally based on whether source is null. 
        /// </summary>
        /// <typeparam name="T">The type of the instance wrapped by the <see cref="Recipes.Maybe{T}" />.</typeparam>
        /// <param name="source">The source instance, which may be null.</param>
        [Obsolete("This will be removed in v2.0.0. Instead, do this: source.IfNotNull()")]
        public static Maybe<T> Maybe<T>(this T? source)
            where T : struct
        {
            return source.IfNotNull();
        }

        /// <summary>
        ///     Returns either the source or, if it is null, an empty <see cref="IEnumerable{T}" /> sequence.
        /// </summary>
        /// <typeparam name="T"> The type of the objects in the sequence. </typeparam>
        /// <param name="source"> The source sequence. </param>
        /// <returns> The source sequence or, if it is null, an empty sequence. </returns>
        public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T> source)
        {
            return source ?? Enumerable.Empty<T>();
        }

        /// <summary>
        /// Attempts to get the value of a Try* method with an out parameter, for example <see cref="Dictionary{TKey,TValue}.TryGetValue" /> or <see cref="ConcurrentQueue{T}.TryDequeue" />.
        /// </summary>
        /// <typeparam name="T">The type of the source object.</typeparam>
        /// <typeparam name="TOut">The type the out parameter.</typeparam>
        /// <param name="source">The source object exposing the Try* method.</param>
        /// <param name="tryTryGetValue">A delegate to call the Try* method.</param>
        /// <returns></returns>
        public static Maybe<TOut> Out<T, TOut>(this T source, TryGetOutParameter<T, TOut> tryTryGetValue)
        {
            TOut result;

            if (tryTryGetValue(source, out result))
            {
                return Recipes.Maybe<TOut>.Yes(result);
            }

            return Recipes.Maybe<TOut>.No();
        }

        /// <summary>
        ///     Executes a function and returns its value if the value of a condition is true.
        /// </summary>
        /// <typeparam name="T"> </typeparam>
        /// <param name="condition">
        ///     if set to <c>true</c> execute <paramref name="getValue" /> and return its result.
        /// </param>
        /// <param name="getValue">
        ///     A function that will be executed and whose value will be returned if <paramref name="condition" /> is true.
        /// </param>
        /// <returns>
        ///     The value returned by getValue or, if <paramref name="condition" /> is false, null.
        /// </returns>
        /// <exception cref="ArgumentNullException">getValue</exception>
        [Obsolete("This will be removed in v2.0.0.")]
        public static T Then<T>(
            this bool condition,
            Func<T> getValue)
            where T : class
        {
            if (getValue == null)
            {
                throw new ArgumentNullException("getValue");
            }

            return condition
                       ? getValue()
                       : null;
        }

        /// <summary>
        ///     Invokes an action if the source condition is true, otherwise does nothing.
        /// </summary>
        /// <param name="condition">
        ///     if set to <c>true</c> , then <paramref name="action" /> is executed.
        /// </param>
        /// <param name="action">
        ///     The action to be invoked if <paramref name="condition" /> is true.\
        /// </param>
        /// <exception cref="ArgumentNullException">action</exception>
        public static void Then(
            this bool condition,
            Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            if (condition)
            {
                action();
            }
        }

        /// <summary>
        /// Specifies the result of a <see cref="Recipes.Maybe{T}" /> if the <see cref="Recipes.Maybe{T}" /> has a value.
        /// </summary>
        /// <typeparam name="TIn">The type of source object.</typeparam>
        /// <typeparam name="TOut">The type of result.</typeparam>
        /// <param name="maybe">The maybe.</param>
        /// <param name="getValue">A delegate to get the value from the source object.</param>
        /// <returns></returns>
        public static Maybe<TOut> Then<TIn, TOut>(
            this Maybe<TIn> maybe,
            Func<TIn, TOut> getValue)
        {
            return maybe.HasValue
                       ? Recipes.Maybe<TOut>.Yes(getValue(maybe.Value))
                       : Recipes.Maybe<TOut>.No();
        }

        /// <summary>
        /// Performs an action if the <see cref="Recipes.Maybe{T}" /> has a value.
        /// </summary>
        /// <typeparam name="T">
        ///     The type held by the <see cref="Recipes.Maybe{T}" />.
        /// </typeparam>
        public static Maybe<Unit> ThenDo<T>(this Maybe<T> maybe, Action<T> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            if (maybe.HasValue)
            {
                action(maybe.Value);
                return Recipes.Maybe<Unit>.Yes(Unit.Default);
            }

            return Recipes.Maybe<Unit>.No();
        }

        /// <summary>
        /// Tries to call the specified method and catches exceptions if they occur.
        /// </summary>
        /// <typeparam name="TIn">The type of source object.</typeparam>
        /// <typeparam name="TOut">The type of result.</typeparam>
        /// <param name="source">The source object.</param>
        /// <param name="getValue">A delegate to get the value from the source object.</param>
        /// <param name="ignore">A predicate to determine whether the exception should be ignored. If this is not specified, all exceptions are ignored. If it is specified and an exception is thrown that matches the predicate, the exception is ignored and a <see cref="Recipes.Maybe{TOut}" /> having no value is returned. If it is specified and an exception is thrown that does not match the predicate, the exception is allowed to propagate.</param>
        /// <returns></returns>
        public static Maybe<TOut> Try<TIn, TOut>(
            this TIn source,
            Func<TIn, TOut> getValue,
            Func<Exception, bool> ignore)
        {
            if (getValue == null)
            {
                throw new ArgumentNullException("getValue");
            }
            if (ignore == null)
            {
                throw new ArgumentNullException("ignore");
            }

            try
            {
                return Recipes.Maybe<TOut>.Yes(getValue(source));
            }
            catch (Exception ex)
            {
                if (!ignore(ex))
                {
                    throw;
                }
            }

            return Recipes.Maybe<TOut>.No();
        }
    }

    /// <summary>
    /// Represents an object that may or may not contain a value, allowing optional chained results to be specified for both possibilities.
    /// </summary>
    /// <typeparam name="T">The type of the possible value.</typeparam>
#if !RecipesProject
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal struct Maybe<T>
    {
        private static readonly Maybe<T> no = new Maybe<T>
        {
            HasValue = false
        };

        private T value;

        /// <summary>
        /// Returns a <see cref="Recipes.Maybe{T}" /> that contains a value.
        /// </summary>
        /// <param name="value">The value.</param>
        public static Maybe<T> Yes(T value)
        {
            return new Maybe<T>
            {
                HasValue = true,
                value = value
            };
        }

        /// <summary>
        /// Returns a <see cref="Recipes.Maybe{T}" /> that does not contain a value.
        /// </summary>
        public static Maybe<T> No()
        {
            return no;
        }

        /// <summary>
        /// Gets the value contained by the <see cref="Recipes.Maybe{T}" />.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public T Value
        {
            get
            {
                if (!HasValue)
                {
                    throw new InvalidOperationException("The Maybe does not contain a value.");
                }
                return value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has a value.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has value; otherwise, <c>false</c>.
        /// </value>
        public bool HasValue { get; private set; }
    }

    /// <summary>
    /// A delegate used to return an out parameter from a Try* method that indicates success via a boolean return value.
    /// </summary>
    /// <typeparam name="T">The type of the source object.</typeparam>
    /// <typeparam name="TOut">The type of the out parameter.</typeparam>
    /// <param name="source">The source.</param>
    /// <param name="outValue">The out parameter's value.</param>
    /// <returns>true if the out parameter was set; otherwise, false.</returns>
    internal delegate bool TryGetOutParameter<in T, TOut>(T source, out TOut outValue);

    /// <summary>
    /// A type representing a void return type.
    /// </summary>
#if !RecipesProject
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal struct Unit
    {
        /// <summary>
        /// The default instance.
        /// </summary>
        public static readonly Unit Default = new Unit();
    }
}
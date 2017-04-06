// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using static Its.Log.Instrumentation.Discover;

namespace Its.Log.Instrumentation
{
    /// <summary>
    ///   Provides diagnostic information on demand.
    /// </summary>
    public class DiagnosticSensor
    {
        private readonly Delegate @delegate;
        private readonly string name;
        private readonly Type declaringType;
        private readonly Type returnType;
        private bool isAsync;

        /// <summary>
        ///   Initializes a new instance of the <see cref="DiagnosticSensor" /> class.
        /// </summary>
        internal DiagnosticSensor(MethodInfo method)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            name = GetName(method);
            declaringType = method.DeclaringType;
            returnType = method.ReturnType;

            NewExpression newExpression;

            if (method.IsStatic)
            {
                newExpression = null;
            }
            else
            {
                var defaultConstructor = method.DeclaringType?.GetConstructors().FirstOrDefault(c => !c.GetParameters().Any());

                if (defaultConstructor == null)
                {
                    throw new ArgumentException($"Type {method.DeclaringType} must have a default public constructor in order to expose a non-static diagnostic sensor.");
                }

                newExpression = Expression.New(defaultConstructor);
            }

            var expression = Expression.Lambda<Func<object>>(
                Expression.Call(
                    newExpression,
                    method));

            @delegate = expression.Compile();

            ValidateAndInitialize();
        }

        internal DiagnosticSensor(Type returnType, string name, Type declaringType, Delegate @delegate)
        {
            this.returnType = returnType;
            this.name = name;
            this.declaringType = declaringType;
            this.@delegate = @delegate;

            ValidateAndInitialize();
        }

        private void ValidateAndInitialize()
        {
            if (returnType == null)
            {
                throw new ArgumentNullException(nameof(returnType));
            }
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (declaringType == null)
            {
                throw new ArgumentNullException(nameof(declaringType));
            }
            if (@delegate == null)
            {
                throw new ArgumentNullException(nameof(@delegate));
            }

            isAsync = returnType.IsAsync();
        }

        /// <summary>
        ///   Gets the type on which the sensor method is declared.
        /// </summary>
        /// <value> The type of the declaring. </value>
        public Type DeclaringType => declaringType;

        /// <summary>
        ///   Gets the name of the sensor method.
        /// </summary>
        public string Name => name;

        /// <summary>
        ///   Gets the return type of the sensor method.
        /// </summary>
        /// <value> The type of the return. </value>
        public Type ReturnType => returnType;

        /// <summary>
        /// Gets a value indicating whether the sensor's return value is async.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the sensor is async; otherwise, <c>false</c>.
        /// </value>
        public bool IsAsync => isAsync;

        /// <summary>
        ///   Reads the sensor and returns its value.
        /// </summary>
        /// <remarks>
        ///   If the sensor throws an exception, the exception is returned.
        /// </remarks>
        public object Read()
        {
            try
            {
                return @delegate.DynamicInvoke();
            }
            catch (Exception exception)
            {
                if (exception.ShouldThrow())
                {
                    throw;
                }
                return exception;
            }
        }

        private static readonly Lazy<ConcurrentDictionary<string, DiagnosticSensor>> allSensors =
            new Lazy<ConcurrentDictionary<string, DiagnosticSensor>>(Discover);

        /// <summary>
        /// Discovers sensors found in all loaded assemblies.
        /// </summary>
        public static ConcurrentDictionary<string, DiagnosticSensor> Discover() =>
            AppDomainTypes(true)
                .SelectMany(t =>
                                t.GetMethods(
                                     BindingFlags.InvokeMethod |
                                     BindingFlags.Instance |
                                     BindingFlags.Static |
                                     BindingFlags.Public |
                                     BindingFlags.NonPublic)
                                 .Where(m => m.GetCustomAttributes()
                                              .Any(a =>
                                              {
                                                  if (a.GetType().Name == nameof(DiagnosticSensorAttribute))
                                                  {
                                                      return true;
                                                  }

                                                  if (a.GetType().Name != "ExportAttribute")
                                                  {
                                                      return false;
                                                  }

                                                  return ((dynamic) a).ContractName == "DiagnosticSensor";
                                              })))
                .Select(methodInfo => new DiagnosticSensor(methodInfo))
                .OrderBy(sensor => sensor.Name)
                .ThenBy(sensor => sensor.DeclaringType.Assembly.FullName)
                .Aggregate(new ConcurrentDictionary<string, DiagnosticSensor>(),
                           (sensors, sensor) =>
                           {
                               // FIX: (Discover) we should be graceful about collisions or deterministic about ordering
                               sensors[sensor.Name] = sensor;
                               return sensors;
                           });

        /// <summary>
        /// Gets the name of a sensor.
        /// </summary>
        /// <param name="sensorMethod">The sensor method.</param>
        /// <returns>The sensor's name</returns>
        /// <remarks>The sensor name can be set by decorating the sensor method with <see cref="DisplayNameAttribute" />. Otherwise, the name of the method is used.</remarks>
        public static string GetName(MethodInfo sensorMethod)
        {
            var displayName = sensorMethod
                .GetCustomAttributes(typeof(DisplayNameAttribute), false)
                .OfType<DisplayNameAttribute>()
                .FirstOrDefault();

            return displayName != null
                       ? displayName.DisplayName
                       : sensorMethod.Name;
        }

        /// <summary>
        ///   Returns all of the diagnostic sensors found in the application.
        /// </summary>
        public static IEnumerable<DiagnosticSensor> KnownSensors() => allSensors.Value.Values.ToArray();

        /// <summary>
        ///   Registers the specified sensor.
        /// </summary>
        /// <param name="sensor"> A function that returns the sensor result. </param>
        /// <param name="name"> The name of the sensor. </param>
        public static void Register<T>(Func<T> sensor, string name = null)
        {
            var anonymousMethodInfo = sensor.GetAnonymousMethodInfo();
            name = name ?? anonymousMethodInfo.MethodName;
            allSensors.Value[name] = new DiagnosticSensor(
                @delegate: sensor,
                returnType: typeof(T),
                name: name,
                declaringType: anonymousMethodInfo.EnclosingType);
        }

        /// <summary>
        ///   Removes any sensor having the specified name.
        /// </summary>
        /// <param name="name"> The sensor name. </param>
        public static void Remove(string name)
        {
            DiagnosticSensor sensor;
            allSensors.Value.TryRemove(name, out sensor);
        }
    }
}
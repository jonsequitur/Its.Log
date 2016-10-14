// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Its.Log.Instrumentation.UnitTests
{
    public class Widget<T> : Widget
    {
        public override void DoStuff()
        {
            using (Log.Enter(() => { }))
            {
                base.DoStuff();
            }
        }
    }

    public class Widget
    {
        public Widget()
        {
            Name = "Default";
        }

        public string Name { get; set; }

        public List<Part> Parts { get; set; }

        public virtual void DoStuff()
        {
            using (Log.Enter(() => { }))
            {
                Log.Write("Doing some stuff...");
            }
        }

        public virtual void DoStuff(int widgetParam)
        {
            using (Log.Enter(() => new { widgetParam }))
            {
                Log.Write("Doing some stuff...");
            }
        }

        public virtual void DoStuff(string widgetParam)
        {
            var activity = Log.Enter(() => new { widgetParam });
            Log.Write("Doing some stuff...");
            Log.Exit(activity);
        }

        public virtual async Task DoStuffAsync()
        {
            using (Log.Enter(() => { }))
            {
                await Task.Run(() => { Log.Write("Doing some stuff..."); });
            }
        }

        public virtual async Task DoStuffAsync(string nestedWidgetParam)
        {
            await Task.Run(() =>
            {
                var activity = Log.Enter(() => new { nestedWidgetParam });

                Log.Write("Doing some stuff...");

                Log.Exit(activity);
            });
        }

        public static void DoStuffStatically()
        {
            using (Log.Enter(() => { }))
            {
                Log.Write("Doing some stuff...");
            }
        }

        public static void DoStuffStatically(string widgetStaticParam)
        {
            using (Log.Enter(() => new { widgetStaticParam }))
            {
                Log.Write("Doing some stuff...");
            }
        }

        public static async Task DoStuffStaticallyAsync()
        {
            using (Log.Enter(() => { }))
            {
                await Task.Run(() => { Log.Write("Doing some stuff..."); });
            }
        }

        public static async Task DoStuffStaticallyAsync(string nestedWidgetParam)
        {
            await Task.Run(() =>
            {
                var activity = Log.Enter(() => new { nestedWidgetParam });

                Log.Write("Doing some stuff...");

                Log.Exit(activity);
            });
        }

        public void DoStuff(string inheritedWidgetParam, ref string outParam)
        {
            using (Log.Enter(() => new { inheritedWidgetParam }))
            {
                Log.Write("Doing some stuff...");
                outParam = "Done doing stuff";
            }
        }

        public void DoStuffThatThrows()
        {
            throw new InvalidOperationException("Why'd you call this method? You knew it was going to throw.");
        }
    }

    public class InheritedWidget : Widget
    {
        public override void DoStuff()
        {
            using (Log.Enter(() => { }))
            {
                Log.Write("Doing some stuff...");
            }
        }

        public override void DoStuff(string inheritedWidgetParam)
        {
            using (Log.Enter(() => new { inheritedWidgetParam }))
            {
                Log.Write("Doing some stuff...");
            }
        }

        public override async Task DoStuffAsync()
        {
            using (Log.Enter(() => { }))
            {
                await Task.Run(() => { Log.Write("Doing some stuff..."); });
            }
        }

        public override async Task DoStuffAsync(string nestedWidgetParam)
        {
            await Task.Run(() =>
            {
                var activity = Log.Enter(() => new { nestedWidgetParam });

                Log.Write("Doing some stuff...");

                Log.Exit(activity);
            });
        }
    }

    public class InheritedWidgetNoOverride : Widget
    {
    }

    public static class WidgetExtensions
    {
        private static readonly Action lambdaField = () =>
        {
            using (Log.Enter(() => { }))
            {
            }
        };

        private static readonly Func<Task> asyncLambdaField = async () =>
        {
            using (Log.Enter(() => { }))
            {
                await Task.Run(() => Console.WriteLine("hello"));
            }
        };

        public static void ExtensionWithLoggingInLocallyScopedLambda(this Widget widget)
        {
            Action action = () =>
            {
                using (Log.Enter(() => { }))
                {
                }
            };
            action();
        }

        public static async Task ExtensionWithLoggingInLocallyScopedAsyncLambdaAsync(this Widget widget)
        {
            Func<Task> action = async () =>
            {
                using (Log.Enter(() => { }))
                {
                    await Task.Run(() => Console.WriteLine("hello"));
                }
            };
            await action();
        }

        public static void ExtensionWithLoggingInStaticallyScopedLambda(this Widget widget)
        {
            lambdaField();
        }

        public static async Task ExtensionWithLoggingInStaticallyScopedAsyncLambdaAsync(this Widget widget)
        {
            await asyncLambdaField();
        }
    }

    public class Part
    {
        public string PartNumber { get; set; }
        public Widget Widget { get; set; }
    }

    public struct SomeStruct
    {
        public DateTime DateField;
        public DateTime DateProperty { get; set; }
    }

    public class SomethingWithLotsOfProperties
    {
        public DateTime DateProperty { get; set; }
        public string StringProperty { get; set; }
        public int IntProperty { get; set; }
        public bool BoolProperty { get; set; }
        public Uri UriProperty { get; set; }
    }

    public struct EntityId
    {
        public EntityId(string typeName, string id) : this()
        {
            TypeName = typeName;
            Id = id;
        }

        public string TypeName { get; private set; }
        public string Id { get; private set; }
    }

    public class Node
    {
        // ReSharper disable InconsistentNaming
        private string _id;
        // ReSharper restore InconsistentNaming
        // ReSharper disable ConvertToAutoProperty
        public string Id
            // ReSharper restore ConvertToAutoProperty
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
            }
        }

        public IEnumerable<Node> Nodes { get; set; }
        public Node[] NodesArray { get; set; }

        internal string InternalId
        {
            get
            {
                return Id;
            }
        }
    }

    public class SomePropertyThrows
    {
        public string Fine
        {
            get
            {
                return "Fine";
            }
        }

        public string NotOk
        {
            get
            {
                throw new Exception("not ok");
            }
        }

        public string Ok
        {
            get
            {
                return "ok";
            }
        }

        public string PerfectlyFine
        {
            get
            {
                return "PerfectlyFine";
            }
        }
    }
}
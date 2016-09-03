// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Its.Log.Instrumentation
{
    public class AsyncDispatchQueue<T> : IDisposable
    {
        private readonly Func<T, Task> send;
        private readonly IDisposable subscription;
        private readonly BlockingCollection<T> blockingCollection;
        private readonly Thread dispatcherThread;

        protected AsyncDispatchQueue(IObservable<T> events, Func<T, Task> send)
        {
            if (send == null)
            {
                throw new ArgumentNullException(nameof(send));
            }
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            this.send = send;
            blockingCollection = new BlockingCollection<T>();
            subscription = events.Subscribe(new Observer(blockingCollection));

            dispatcherThread = new Thread(Send);
            dispatcherThread.Start();
        }

        private void Send()
        {
            while (!blockingCollection.IsCompleted)
            {
                try
                {
                    var value = blockingCollection.Take();
                    send(value).Wait();
                }
                catch (Exception exception)
                {
                    exception.RaiseErrorEvent();
                }
            }
        }

        private async Task Drain()
        {
            while (!blockingCollection.IsCompleted)
            {
                await Task.Delay(50);
            }
        }

        public void Dispose()
        {
            blockingCollection.CompleteAdding();
            subscription.Dispose();
            Drain().Wait(TimeSpan.FromSeconds(30));
        }

        private class Observer : IObserver<T>
        {
            private readonly BlockingCollection<T> blockingCollection;

            public Observer(BlockingCollection<T> blockingCollection)
            {
                if (blockingCollection == null)
                {
                    throw new ArgumentNullException(nameof(blockingCollection));
                }
                this.blockingCollection = blockingCollection;
            }

            public void OnNext(T value) => blockingCollection.Add(value);

            public void OnError(Exception error)
            {
            }

            public void OnCompleted()
            {
            }
        }
    }
}
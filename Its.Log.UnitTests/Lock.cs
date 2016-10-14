// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;

namespace Its.Log.Instrumentation
{
    internal enum LockType
    {
        Read,
        UpgradeableRead,
        Write
    }

    internal interface ILockHelper : IDisposable
    {
        void Enter();
    }

    [DebuggerStepThrough]
    internal static class Lock
    {
        public static ILockHelper Prepare(this ReaderWriterLockSlim lok, LockType type)
        {
            switch (type)
            {
                case LockType.Read:
                    return new ReadLockHelper(lok);
                case LockType.UpgradeableRead:
                    return new UpgradeableReadLockHelper(lok);
                case LockType.Write:
                default:
                    return new WriteLockHelper(lok);
            }
        }

        [DebuggerStepThrough]
        internal abstract class LockHelper : ILockHelper
        {
            protected ReaderWriterLockSlim ReaderWriterLock;

            protected LockHelper(ReaderWriterLockSlim readerWriterLock)
            {
                ReaderWriterLock = readerWriterLock;
            }

            public abstract void Enter();

            public abstract void Dispose();
        }

        [DebuggerStepThrough]
        internal class ReadLockHelper : LockHelper
        {
            public ReadLockHelper(ReaderWriterLockSlim readerWriterLock) : base(readerWriterLock)
            {
            }

            public override void Enter()
            {
                ReaderWriterLock.EnterReadLock();
            }

            public override void Dispose()
            {
                ReaderWriterLock.ExitReadLock();
            }
        }

        [DebuggerStepThrough]
        internal class UpgradeableReadLockHelper : LockHelper
        {
            public UpgradeableReadLockHelper(ReaderWriterLockSlim readerWriterLock)
                : base(readerWriterLock)
            {
            }

            public override void Enter()
            {
                ReaderWriterLock.EnterUpgradeableReadLock();
            }

            public override void Dispose()
            {
                ReaderWriterLock.ExitUpgradeableReadLock();
            }
        }

        [DebuggerStepThrough]
        internal class WriteLockHelper : LockHelper
        {
            public WriteLockHelper(ReaderWriterLockSlim readerWriterLock)
                : base(readerWriterLock)
            {
            }

            public override void Enter()
            {
                ReaderWriterLock.EnterWriteLock();
            }

            public override void Dispose()
            {
                ReaderWriterLock.ExitWriteLock();
            }
        }
    }
}
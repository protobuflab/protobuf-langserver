﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ProtobufLanguageServer
{
    public class ForegroundThreadManager
    {
        public bool IsForegroundThread => Thread.CurrentThread.ManagedThreadId == ForegroundTaskScheduler.Instance.ForegroundThreadId;

        public TaskScheduler ForegroundScheduler { get; } = ForegroundTaskScheduler.Instance;

        public TaskScheduler BackgroundScheduler { get; } = TaskScheduler.Default;

        public virtual void AssertForegroundThread([CallerMemberName] string caller = null)
        {
            if (!IsForegroundThread)
            {
                throw new InvalidOperationException($"{caller} was not on the foreground thread.");
            }
        }

        public virtual void AssertBackgroundThread([CallerMemberName] string caller = null)
        {
            if (IsForegroundThread)
            {
                caller = $"'{caller}'";
                throw new InvalidOperationException($"{caller} was not on the background thread.");
            }
        }

        internal class ForegroundTaskScheduler : TaskScheduler
        {
            public static ForegroundTaskScheduler Instance = new ForegroundTaskScheduler();

            private readonly Thread _thread;
            private readonly BlockingCollection<Task> _tasks = new BlockingCollection<Task>();

            private ForegroundTaskScheduler()
            {
                _thread = new Thread(ThreadStart)
                {
                    IsBackground = true,
                };

                _thread.Start();
            }

            public int ForegroundThreadId => _thread.ManagedThreadId;

            public override int MaximumConcurrencyLevel => 1;

            protected override void QueueTask(Task task) => _tasks.Add(task);

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                // If the task was previously queued it means that we're ensuring it's running on our single threaded scheduler.
                // Otherwise, we can't enforce that behavior and therefore need it to be re-queued before execution.
                if (taskWasPreviouslyQueued)
                {
                    return TryExecuteTask(task);
                }

                return false;
            }

            protected override IEnumerable<Task> GetScheduledTasks() => _tasks.ToArray();

            private void ThreadStart()
            {
                while (true)
                {
                    try
                    {
                        var task = _tasks.Take();
                        TryExecuteTask(task);
                    }
                    catch (ThreadAbortException)
                    {
                        // Fires when things shut down or in tests. Swallow thread abort exceptions and bail out.
                        return;
                    }
                }
            }
        }
    }
}
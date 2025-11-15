
namespace Contigo
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Windows.Threading;
    using Standard;

    internal class DispatcherPool : IDisposable
    {
        private class _ActionData
        {
            public Action<object> Action;
            public object Arg;
        }

        private readonly object _lock = new object();
        private int _pendingActionsCount = 0;
        private readonly Dispatcher _masterDispatcher;

        private readonly Dictionary<DispatcherPriority, Stack<_ActionData>> _pendingActions = new Dictionary<DispatcherPriority, Stack<_ActionData>>
        {
            { DispatcherPriority.Background, new Stack<_ActionData>() },
            { DispatcherPriority.Normal, new Stack<_ActionData>() },
            { DispatcherPriority.Send, new Stack<_ActionData>() },
        };

        private readonly Dispatcher[] _asyncDispatchers;
        private readonly Dictionary<int, object> _dispatcherTags;

        private bool _disposed = false;

        public DispatcherPool(string name, int threadCount)
            : this(name, threadCount, null)
        {}

        public DispatcherPool(string name, int threadCount, Func<object> tagGenerator)
        {
            Verify.IsNeitherNullNorEmpty(name, "name");
            Verify.BoundedInteger(1, threadCount, 10, "threadCount");
            _masterDispatcher = Dispatcher.CurrentDispatcher;

            _asyncDispatchers = new Dispatcher[threadCount];
            _dispatcherTags = new Dictionary<int, object>();

            for (int i = 0; i < _asyncDispatchers.Length; ++i)
            {
                var dispatcherThread = new Thread(_DispatcherThreadProc) { ApartmentState = ApartmentState.STA };
                if (_asyncDispatchers.Length > 1)
                {
                    dispatcherThread.Name = name + " (" + (i+1).ToString() + ")";
                }
                else
                {
                    dispatcherThread.Name = name;
                }

                using (var dispatcherCreated = new AutoResetEvent(false))
                {
                    dispatcherThread.Start(dispatcherCreated);
                    dispatcherCreated.WaitOne();
                }

                Dispatcher currentDispatcher = Dispatcher.FromThread(dispatcherThread);
                _asyncDispatchers[i] = currentDispatcher;

                // When the thread that created this starts to shut down, shut down this as well.
                _masterDispatcher.ShutdownStarted += (sender, e) => currentDispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);

                Assert.IsNotNull(_asyncDispatchers[i]);

                if (tagGenerator != null)
                {
                    _dispatcherTags.Add(currentDispatcher.Thread.ManagedThreadId, tagGenerator());
                }
            }
        }

        private static void _DispatcherThreadProc(object handle)
        {
            var d = Dispatcher.CurrentDispatcher;

            ((AutoResetEvent)handle).Set();
            Dispatcher.Run();
        }
        
        public void QueueRequest(Action<object> action, object arg)
        {
            QueueRequest(DispatcherPriority.Normal, action, arg);
        }

        public void QueueRequest(DispatcherPriority priority, Action<object> action, object arg)
        {
            _VerifyState();

            Assert.IsTrue(_pendingActions.ContainsKey(priority),
                "Attempting to use a DispatcherPriority other than one supported by this class.");

            lock (_lock)
            {
                _pendingActions[priority].Push(new _ActionData { Action = action, Arg = arg });
                ++_pendingActionsCount;
            }

            // Queue the request on all of the dispatchers.
            // It's the responsibility of the derived class to only do the processing once.
            foreach (Dispatcher d in _asyncDispatchers)
            {
                d.BeginInvoke((Action<DispatcherPriority>)_ProcessNextRequest, priority, priority);
            }
        }

        public object Tag
        {
            get
            {
                if (_disposed)
                {
                    return null;
                }
                Assert.IsTrue(_dispatcherTags.ContainsKey(Thread.CurrentThread.ManagedThreadId));
                return _dispatcherTags[Thread.CurrentThread.ManagedThreadId];
            }
        }

        public bool HasPendingRequests { get { return _pendingActionsCount != 0; } }
        
        private void _ProcessNextRequest(DispatcherPriority priority)
        {
            if (Dispatcher.CurrentDispatcher.HasShutdownStarted)
            {
                return;
            }

            _ActionData request = null;
            lock (_lock)
            {
                // There may not be any items left.
                // When there are multiple dispatchers we tell all of them about the item
                // So the first one can try to take it.
                if (_pendingActions[priority].Count > 0)
                {
                    request = _pendingActions[priority].Pop();
                    --_pendingActionsCount;
                }
            }

            if (request != null)
            {
                try
                {
                    request.Action(request.Arg);
                }
                catch (Exception e)
                {
                    ETWLogger.EventWriteUnhandledDispatcherPoolExceptionEvent(e.Message, e.StackTrace);
                    // Don't let exceptions propagate outside the dispatcher.
                    // The Actions should be blocking this from ever happening.
                    Assert.Fail(e.Message);
                }
            }
        }

        private void _VerifyState()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("this");
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            foreach (Dispatcher d in _asyncDispatchers)
            {
                d.BeginInvokeShutdown(DispatcherPriority.Send);
            }
            foreach (object o in _dispatcherTags.Values)
            {
                var disposable = o as IDisposable;
                Utility.SafeDispose(ref disposable); 
            }
        }

        #endregion
    }

}

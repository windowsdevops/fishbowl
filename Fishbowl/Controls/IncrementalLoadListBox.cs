namespace FacebookClient
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Threading;
    using Standard;
    using Contigo;

    public class IncrementalLoadListBox : ListBox
    {
        private const int PerItemMillisecondDelay = 10;

        private readonly ObservableCollection<object> _collection = new ObservableCollection<object>();
        private readonly Queue<CollectionChange> _pendingChanges = new Queue<CollectionChange>();
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        // Guard against multi-threaded access to the Queue
        private readonly object _lock = new object();

        public static DependencyProperty ActualItemsSourceProperty = DependencyProperty.Register(
            "ActualItemsSource", 
            typeof(IEnumerable), 
            typeof(IncrementalLoadListBox),
            new FrameworkPropertyMetadata(
                (d, e) => ((IncrementalLoadListBox)d)._OnActualItemsSourceChanged(e)),
            target => target == null || target is INotifyCollectionChanged);

        public IEnumerable ActualItemsSource
        {
            get { return (IEnumerable)GetValue(ActualItemsSourceProperty); }
            set { SetValue(ActualItemsSourceProperty, value); }
        }

        public IncrementalLoadListBox()
        {
            base.ItemsSource = _collection;

            this.Loaded += (sender, e) =>
            {
                _timer.Interval = TimeSpan.FromMilliseconds(PerItemMillisecondDelay);
                _timer.Tick += _OnTimerTick;
                _timer.Start();

                if (ActualItemsSource != null)
                {
                    lock (_lock)
                    {
                        _QueueInitialInserts();
                        _OnTimerTick(null, null);
                    }
                }
            };

            this.Unloaded += (sender, e) =>
            {
                _pendingChanges.Clear();
                _collection.Clear();

                _timer.Stop();
                _timer.Tick -= _OnTimerTick;
                _timer.IsEnabled = false;

                if (ActualItemsSource != null)
                {
                    ((INotifyCollectionChanged)ActualItemsSource).CollectionChanged -= _OnActualItemsSourceCollectionChanged;
                }
            };
        }

        private void _QueueInitialInserts()
        {
            IEnumerable items = FacebookCollection<object>.EnumerateAndAddNotify(ActualItemsSource, _OnActualItemsSourceCollectionChanged);
            int index = 0;

            foreach (object item in items)
            {
                _pendingChanges.Enqueue(new CollectionChange(item, index, CollectionChangeType.Add));
                index++;
            }
        }

        private void _OnActualItemsSourceChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!this.IsLoaded)
            {
                return;
            }

            lock (_lock)
            {
                if (e.OldValue != null)
                {
                    ((INotifyCollectionChanged)e.OldValue).CollectionChanged -= _OnActualItemsSourceCollectionChanged;
                }

                _pendingChanges.Clear();
                _collection.Clear();

                if (e.NewValue == null)
                {
                    return;
                }

                _QueueInitialInserts();
            }
            _OnTimerTick(null, null);
        }

        private void _OnTimerTick(object sender, EventArgs e)
        {
            lock (_lock)
            {
                if (_pendingChanges.Count > 0)
                {
                    CollectionChange change = _pendingChanges.Dequeue();

                    switch (change.Type)
                    {
                        case CollectionChangeType.Add:
                            Assert.IsTrue(change.Index <= _collection.Count);
                            _collection.Insert(change.Index, change.Item);
                            break;
                        case CollectionChangeType.Remove:
                            Assert.IsTrue(change.Index < _collection.Count);
                            _collection.RemoveAt(change.Index);
                            break;
                        case CollectionChangeType.Reset:
                            _collection.Clear();
                            break;
                        default:
                            Assert.Fail();
                            break;
                    }
                }
            }
        }

        private void _OnActualItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            VerifyAccess();

            lock (_lock)
            {
                if (sender != ActualItemsSource)
                {
                    return;
                }

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        Assert.IsTrue(e.NewStartingIndex != -1);
                        Assert.IsTrue(e.NewItems.Count == 1);

                        _pendingChanges.Enqueue(new CollectionChange(e.NewItems[0], e.NewStartingIndex, CollectionChangeType.Add));
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        Assert.IsTrue(e.OldItems.Count == 1);

                        _pendingChanges.Enqueue(new CollectionChange(e.OldItems[0], e.OldStartingIndex, CollectionChangeType.Remove));
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        // We have a lock on the collection.  If we've reset then we want the collection to immediately clear.
                        _pendingChanges.Clear();
                        _pendingChanges.Enqueue(new CollectionChange(null, 0, CollectionChangeType.Reset));
                        break;
                    case NotifyCollectionChangedAction.Move:
                        Assert.Fail();
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        Assert.Fail();
                        break;
                }
            }
        }

        private enum CollectionChangeType
        {
            Invalid,
            Add,
            Remove,
            Reset
        }

        private class CollectionChange
        {
            public CollectionChange(object item, int index, CollectionChangeType type)
            {
                Item = item;
                Index = index;
                Type = type;
            }

            public object Item { get; private set; }
            public int Index { get; private set; }
            public CollectionChangeType Type { get; private set; }
        }
    }
}

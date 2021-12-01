// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.UtilityDataStructures
{
    internal interface IBlockableItem
    {
        bool IsBlocking { get; }
    }

    // Contains both blocking and non-blocking item groups to be executed as concurrently as possible.
    internal sealed class ConcurrentGroupedQueue<T> where T : IBlockableItem
    {
#region Item Groups
        internal abstract class ItemGroup
        {
            public abstract bool IsBlocking { get; }
            protected HashSet<T> _items;
            public bool IsOpenGroup;
            public int Count
            {
                get
                {
                    if (_items == null)
                        return 0;
                    return _items.Count;
                }
            }

            public ItemGroup()
            {
                IsOpenGroup = true;
                _items = new HashSet<T>();
            }

            public void CloseGroup()
            {
                Contracts.Assert(IsOpenGroup);
                // Maybe collect telemetry?
                IsOpenGroup = false;
            }

            public abstract void Enqueue(T change);
            public abstract void Run(RunItemAction action);
            public void Remove(T item)
            {
                Contracts.Assert(IsOpenGroup);
                _items.Remove(item);
            }
            public void Clear()
            {
                _items.Clear();
            }
        }

        internal sealed class BlockingItemGroup : ItemGroup
        {
            public override bool IsBlocking => true;

            public BlockingItemGroup() : base() { }

            public override void Enqueue(T change)
            {
                Contracts.Assert(change.IsBlocking);
                Contracts.Assert(IsOpenGroup);

                _items.Add(change);
            }

            public override void Run(RunItemAction action)
            {
                Contracts.Assert(!IsOpenGroup);

                foreach (var item in _items)
                {
                    action(item);
                }
            }
        }

        internal sealed class NonBlockingItemGroup : ItemGroup
        {
            public override bool IsBlocking => false;

            public NonBlockingItemGroup()
                : base() { }

            public override void Enqueue(T change)
            {
                Contracts.Assert(!change.IsBlocking);
                Contracts.Assert(IsOpenGroup);

                _items.Add(change);
            }

            public override void Run(RunItemAction action)
            {
                Contracts.Assert(!IsOpenGroup);
                Parallel.ForEach(_items, (evt) => action(evt));
            }
        }
#endregion

        private readonly RunItemAction _runItemAction;
        private readonly GroupEndAction _groupEndAction;
        private Queue<ItemGroup> _itemGroupQueue;
        private ItemGroup _currentGroup;

        internal ItemGroup CurrentGroup => _currentGroup;

        public delegate void RunItemAction(T change);
        public delegate void GroupEndAction();

        public ConcurrentGroupedQueue(RunItemAction action, GroupEndAction endAction)
        {
            _runItemAction = action;
            _groupEndAction = endAction;
            _itemGroupQueue = new Queue<ItemGroup>();
        }

        private ItemGroup CreateNewItemGroup(T change)
        {
            if (change.IsBlocking)
                return new BlockingItemGroup();
            else
                return new NonBlockingItemGroup();
        }

        public void Enqueue(T change)
        {
            if (_currentGroup == null || !_currentGroup.IsOpenGroup)
            {
                _currentGroup = CreateNewItemGroup(change);
                _itemGroupQueue.Enqueue(_currentGroup);
            }
            else if(_currentGroup.IsBlocking != change.IsBlocking)
            {
                _currentGroup.CloseGroup();
                _currentGroup = CreateNewItemGroup(change);
                _itemGroupQueue.Enqueue(_currentGroup);
            }

            _currentGroup.Enqueue(change);
        }

        public void Run()
        {
            _currentGroup.CloseGroup();

            while (_itemGroupQueue.Count > 0)
            {
                var group = _itemGroupQueue.Dequeue();
                group.Run(_runItemAction);
                _groupEndAction();
            }
        }

        public void Clear()
        {
            foreach (var group in _itemGroupQueue)
            {
                group.Clear();
            }
            _itemGroupQueue.Clear();
            _currentGroup = null;
        }

        public int Count
        {
            get
            {
                return _itemGroupQueue.Sum(group => group.Count);
            }
        }
    }
}
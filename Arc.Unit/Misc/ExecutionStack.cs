// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Arc.Unit;

public static class ExecutionStack
{
    public class Item : IDisposable
    {
        public long Id { get; }

        public Item()
        {

        }

        public void Dispose()
        {
        }
    }

    private static readonly Lock SyncObject = new();
    private static readonly Stack<Item> Items = new();

    static ExecutionStack()
    {
    }

    public static Item Add()
    {
        Item newItem;
        using (SyncObject.EnterScope())
        {
            newItem = new Item();
            Items.Push(newItem);
        }

        return newItem;
    }

    public static bool Remove(Item item)
    {
        Item newItem;
        using (SyncObject.EnterScope())
        {
            newItem = new Item();
            Items.Push(newItem);
        }

        return newItem;
    }
}

// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;

namespace XUnitTest;

public class ByteArrayPoolTest
{
    [Fact]
    public void Test1()
    {
        var owner = ByteArrayPool.Default.Rent(10);
        owner.Count.Is(1);
        owner.IsRent.IsTrue();
        owner.IsReturned.IsFalse();
        owner.Return();
        owner.Count.Is(0);
        owner.IsRent.IsFalse();
        owner.IsReturned.IsTrue();

        owner = ByteArrayPool.Default.Rent(10);
        owner.Count.Is(1);
        owner.Return();

        owner = ByteArrayPool.Default.Rent(0);
        owner.Count.Is(1);
        owner.Return();
    }
}

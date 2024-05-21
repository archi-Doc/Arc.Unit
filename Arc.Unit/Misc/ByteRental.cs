// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

#pragma warning disable SA1124
#pragma warning disable SA1202

namespace Arc.Unit;

/// <summary>
/// A thread-safe pool of byte arrays (uses <see cref="ConcurrentQueue{T}"/>).<br/>
/// <see cref="ByteRental"/> is slightly slower than 'new byte[]' or <see cref="System.Buffers.ArrayPool{T}"/> (especially byte arrays of 1kbytes or less), but it has some advantages.<br/>
/// 1. Can handle a rent byte array and a created ('new byte[]') byte array in the same way.<br/>
/// 2. By using <see cref="ByteRental.Memory"/>, you can handle a rent byte array in the same way as <see cref="Memory{T}"/>.<br/>
/// 3. Can be used by multiple users by incrementing the reference count.<br/>
/// ! It is recommended to use <see cref="ByteRental"/> within a class, and not between classes, as the responsibility for returning the buffer becomes unclear.
/// </summary>
public class ByteRental
{
    private const int DefaultMaxArrayLength = 1024 * 1024 * 16; // 16MB
    private const int DefaultPoolLimit = 100;

    public static readonly ByteRental Default = ByteRental.Create();

    /// <summary>
    /// Represents an owner of a byte array (one owner instance for each byte array).<br/>
    /// <see cref="Array"/> has a reference count, and when it reaches zero, it returns the byte array to the pool.
    /// </summary>
    public class Array : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Array"/> class from a byte array.<br/>
        /// This is a feature for compatibility with conventional memory management (e.g new byte[]), <br/>
        /// The byte array will not be returned when <see cref="Return"/> is called.
        /// </summary>
        /// <param name="byteArray">A byte array (allocated with 'new').</param>
        public Array(byte[] byteArray)
        {
            this.bucket = null;
            this.ByteArray = byteArray;
            this.SetCount1();
        }

        internal Array(Bucket bucket)
        {
            this.bucket = bucket;
            this.ByteArray = new byte[bucket.ArrayLength];
            this.SetCount1();
        }

        #region FieldAndProperty

        /// <summary>
        /// Gets a rent byte array.
        /// </summary>
        public byte[] ByteArray { get; }

        private Bucket? bucket;
        private int count;

        /// <summary>
        /// Gets a value indicating whether the owner (byte array) is rent or not.
        /// </summary>
        public bool IsRent => Volatile.Read(ref this.count) > 0;

        /// <summary>
        /// Gets a value indicating whether the owner (byte array) is returned or not.
        /// </summary>
        public bool IsReturned => Volatile.Read(ref this.count) <= 0;

        /// <summary>
        /// Gets the reference count of the owner.
        /// </summary>
        public int Count => Volatile.Read(ref this.count);

        #endregion

        /// <summary>
        ///  Increment the reference count and get an <see cref="Array"/> instance.
        /// </summary>
        /// <returns><see cref="Array"/> instance (<see langword="this"/>).</returns>
        public Array IncrementAndShare()
        {
            Interlocked.Increment(ref this.count);
            return this;
        }

        /// <summary>
        ///  Increment the counter and attempt to share the byte array.
        /// </summary>
        /// <returns><see langword="true"/>; Success.</returns>
        public bool TryIncrement()
        {
            int currentCount;
            int newCount;
            do
            {
                currentCount = this.count;
                if (currentCount == 0)
                {
                    return false;
                }

                newCount = currentCount + 1;
            }
            while (Interlocked.CompareExchange(ref this.count, newCount, currentCount) != currentCount);

            return true;
        }

        /// <summary>
        /// Create a <see cref="Memory"/> object from <see cref="Array"/>.
        /// </summary>
        /// <returns><see cref="Memory"/>.</returns>
        public Memory AsMemory()
            => new(this);

        /// <summary>
        /// Create a <see cref="Memory"/> object by specifying the index and length.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <returns><see cref="Memory"/>.</returns>
        public Memory AsMemory(int start)
            => new(this, this.ByteArray, start, this.ByteArray.Length - start);

        /// <summary>
        /// Create a <see cref="Memory"/> object by specifying the index and length.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <param name="length">The number of elements to include in the slice.</param>
        /// <returns><see cref="Memory"/>.</returns>
        public Memory AsMemory(int start, int length)
            => new(this, this.ByteArray, start, length);

        /// <summary>
        /// Create a <see cref="ReadOnlyMemory"/> object from <see cref="Array"/>.
        /// </summary>
        /// <returns><see cref="ReadOnlyMemory"/>.</returns>
        public ReadOnlyMemory AsReadOnly()
            => new(this);

        /// <summary>
        /// Create a <see cref="Memory"/> object by specifying the index and length.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <returns><see cref="Memory"/>.</returns>
        public ReadOnlyMemory AsReadOnly(int start)
            => new(this, this.ByteArray, start, this.ByteArray.Length - start);

        /// <summary>
        /// Create a <see cref="ReadOnlyMemory"/> object by specifying the index and length.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <param name="length">The number of elements to include in the slice.</param>
        /// <returns><see cref="ReadOnlyMemory"/>.</returns>
        public ReadOnlyMemory AsReadOnly(int start, int length)
            => new(this, this.ByteArray, start, length);

        internal void SetCount1()
            => Volatile.Write(ref this.count, 1);

        /// <summary>
        /// Decrement the reference count.<br/>
        /// When it reaches zero, it returns the byte array to the pool.<br/>
        /// Failure to return a rented array is not a fatal error (eventually be garbage-collected).
        /// </summary>
        /// <returns><see langword="null"></see>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Array? Return()
        {
            var count = Interlocked.Decrement(ref this.count);
            if (count == 0 && this.bucket != null)
            {
                if (this.bucket.QueueCount < this.bucket.PoolLimit)
                {
                    this.bucket.Queue.Enqueue(this);
                    Interlocked.Increment(ref this.bucket.QueueCount);
                }
            }

            return null;
        }

        public void Dispose()
            => this.Return();
    }

    /// <summary>
    /// Represents an owner of a byte array and a <see cref="Memory{T}"/> object.
    /// </summary>
    public readonly struct Memory : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Memory"/> struct from a byte array.<br/>
        /// This is a feature for compatibility with <see cref="ByteRental"/>, and the byte array will not be returned when <see cref="Return"/> is called.
        /// </summary>
        /// <param name="byteArray">A byte array (other than <see cref="ByteRental"/>).</param>
        public Memory(byte[] byteArray)
        {
            this.byteArray = byteArray;
            this.length = byteArray.Length;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Memory"/> struct from a byte array.<br/>
        /// This is a feature for compatibility with <see cref="ByteRental"/>, and the byte array will not be returned when <see cref="Return"/> is called.
        /// </summary>
        /// <param name="byteArray">A byte array (other than <see cref="ByteRental"/>).</param>
        /// <param name="start">The index at which to begin the memory.</param>
        /// <param name="length">The number of items in the memory.</param>
        public Memory(byte[] byteArray, int start, int length)
        {
            this.byteArray = byteArray;
            this.start = start;
            this.length = length;
        }

        internal Memory(Array array)
        {
            this.array = array;
            this.byteArray = array.ByteArray;
            this.length = array.ByteArray.Length;
        }

        internal Memory(ByteRental.Array? array, byte[]? byteArray, int start, int length)
        {
            if (byteArray is null)
            {
                return;
            }
            else if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)byteArray.Length)
            {
                throw new ArgumentOutOfRangeException();
            }

            this.array = array;
            this.byteArray = byteArray;
            this.start = start;
            this.length = length;
        }

        #region FieldAndProperty

        private readonly ByteRental.Array? array;
        private readonly byte[]? byteArray;
        private readonly int start;
        private readonly int length;

        /// <summary>
        /// Gets a value indicating whether the owner (byte array) is rent or not.
        /// </summary>
        public bool IsRent => this.array != null && this.array.IsRent;

        /// <summary>
        /// Gets a value indicating whether the owner (byte array) is returned or not.
        /// </summary>
        public bool IsReturned => this.array == null || this.array.IsReturned;

        /// <summary>
        /// Gets a value indicating whether the memory is empty.
        /// </summary>
        public bool IsEmpty => this.length == 0;

        /// <summary>
        /// Gets a <see cref="Span{T}"/> from <see cref="Memory"/>.
        /// </summary>
        public Span<byte> Span => new(this.byteArray, this.start, this.length);

        /// <summary>
        /// Gets a <see cref="Memory{T}"/> from <see cref="Memory"/>.
        /// </summary>
        /// <returns><see cref="Memory{T}"/>.</returns>
        public Memory<byte> AsMemory() => new(this.byteArray, this.start, this.length);

        #endregion

        /// <summary>
        ///  Increment the reference count.
        /// </summary>
        /// <returns><see cref="Array"/> instance (<see langword="this"/>).</returns>
        public Memory IncrementAndShare()
        {
            if (this.array == null)
            {
                throw new InvalidOperationException();
            }

            return new(this.array.IncrementAndShare(), this.byteArray, this.start, this.length);
        }

        /// <summary>
        ///  Increment the reference count and create a <see cref="Memory"/> object by specifying the index and length.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <param name="length">The number of elements to include in the slice.</param>
        /// <returns><see cref="Memory"/> object.</returns>
        public Memory IncrementAndShare(int start, int length)
        {
            if (this.array == null)
            {
                throw new InvalidOperationException();
            }

            return new(this.array.IncrementAndShare(), this.byteArray, start, length);
        }

        /// <summary>
        ///  Increment the reference count.
        /// </summary>
        /// <returns><see cref="Array"/> instance (<see langword="this"/>).</returns>
        public ReadOnlyMemory IncrementAndShareReadOnly()
        {
            if (this.array == null)
            {
                throw new InvalidOperationException();
            }

            return new(this.array.IncrementAndShare(), this.byteArray, this.start, this.length);
        }

        /// <summary>
        ///  Increment the reference count and create a <see cref="Memory"/> object by specifying the index and length.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <param name="length">The number of elements to include in the slice.</param>
        /// <returns><see cref="Memory"/> object.</returns>
        public ReadOnlyMemory IncrementAndShareReadOnly(int start, int length)
        {
            if (this.array == null)
            {
                throw new InvalidOperationException();
            }

            return new(this.array.IncrementAndShare(), this.byteArray, start, length);
        }

        /// <summary>
        ///  Increment the counter and attempt to share the <see cref="Array"/>.
        /// </summary>
        /// <returns><see langword="true"/>; Success.</returns>
        public bool TryIncrement()
        {
            if (this.array == null)
            {
                throw new InvalidOperationException();
            }

            return this.array.TryIncrement();
        }

        /// <summary>
        /// Forms a slice out of the current memory that begins at a specified index.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <returns><see cref="Memory"/>.</returns>
        public Memory Slice(int start)
            => new(this.array, this.byteArray, this.start + start, this.length - start);

        /// <summary>
        /// Forms a slice out of the current memory starting at a specified index for a specified length.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <param name="length">The number of elements to include in the slice.</param>
        /// <returns><see cref="Memory"/>.</returns>
        public Memory Slice(int start, int length)
            => new(this.array, this.byteArray, this.start + start, length);

        /// <summary>
        /// Create a <see cref="ReadOnlyMemory"/> object from <see cref="Memory"/>.
        /// </summary>
        /// <returns><see cref="ReadOnlyMemory"/>.</returns>
        public ReadOnlyMemory AsReadOnly()
            => new(this.array, this.byteArray, this.start, this.length);

        /// <summary>
        /// Create a <see cref="ReadOnlyMemory"/> object by specifying the index and length.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <returns><see cref="ReadOnlyMemory"/>.</returns>
        public ReadOnlyMemory AsReadOnly(int start)
            => new(this.array, this.byteArray, this.start + start, this.length - start);

        /// <summary>
        /// Create a <see cref="ReadOnlyMemory"/> object by specifying the index and length.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <param name="length">The number of elements to include in the slice.</param>
        /// <returns><see cref="ReadOnlyMemory"/>.</returns>
        public ReadOnlyMemory AsReadOnly(int start, int length)
            => new(this.array, this.byteArray, this.start + start, length);

        /// <summary>
        /// Decrement the reference count.<br/>
        /// When it reaches zero, it returns the <see cref="Array"/> to the pool.<br/>
        /// Failure to return a rented array is not a fatal error (eventually be garbage-collected).
        /// </summary>
        /// <returns><see langword="default"></see>.</returns>
        public Memory Return()
        {
            this.array?.Return();
            return default;
        }

        public void Dispose()
            => this.Return();
    }

    /// <summary>
    /// Represents an owner of a byte array and a <see cref="Memory{T}"/> object.
    /// </summary>
    public readonly struct ReadOnlyMemory : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyMemory"/> struct from a byte array.<br/>
        /// This is a feature for compatibility with <see cref="ByteRental"/>, and the byte array will not be returned when <see cref="Return"/> is called.
        /// </summary>
        /// <param name="byteArray">A byte array (other than <see cref="ByteRental"/>).</param>
        public ReadOnlyMemory(byte[] byteArray)
        {
            this.byteArray = byteArray;
            this.length = byteArray.Length;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyMemory"/> struct from a byte array.<br/>
        /// This is a feature for compatibility with <see cref="ByteRental"/>, and the byte array will not be returned when <see cref="Return"/> is called.
        /// </summary>
        /// <param name="byteArray">A byte array (other than <see cref="ByteRental"/>).</param>
        /// <param name="start">The index at which to begin the memory.</param>
        /// <param name="length">The number of items in the memory.</param>
        public ReadOnlyMemory(byte[] byteArray, int start, int length)
        {
            this.byteArray = byteArray;
            this.start = start;
            this.length = length;
        }

        internal ReadOnlyMemory(Array array)
        {
            this.array = array;
            this.byteArray = array.ByteArray;
            this.length = array.ByteArray.Length;
        }

        internal ReadOnlyMemory(ByteRental.Array? array, byte[]? byteArray, int start, int length)
        {
            if (byteArray is null)
            {
                return;
            }
            else if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)byteArray.Length)
            {
                throw new ArgumentOutOfRangeException();
            }

            this.array = array;
            this.byteArray = byteArray;
            this.start = start;
            this.length = length;
        }

        #region FieldAndProperty

        private readonly ByteRental.Array? array;
        private readonly byte[]? byteArray;
        private readonly int start;
        private readonly int length;

        /// <summary>
        /// Gets a value indicating whether the owner (byte array) is rent or not.
        /// </summary>
        public bool IsRent => this.array != null && this.array.IsRent;

        /// <summary>
        /// Gets a value indicating whether the owner (byte array) is returned or not.
        /// </summary>
        public bool IsReturned => this.array == null || this.array.IsReturned;

        /// <summary>
        /// Gets a value indicating whether the memory is empty.
        /// </summary>
        public bool IsEmpty => this.length == 0;

        /// <summary>
        /// Gets a <see cref="Span{T}"/> from <see cref="ReadOnlyMemory"/>.
        /// </summary>
        public Span<byte> Span => new(this.byteArray, this.start, this.length);

        /// <summary>
        /// Gets a span from <see cref="ReadOnlyMemory"/>.
        /// </summary>
        public ReadOnlyMemory<byte> AsMemory => new(this.byteArray, this.start, this.length);

        #endregion

        /// <summary>
        ///  Increment the reference count.
        /// </summary>
        /// <returns><see cref="Array"/> instance (<see langword="this"/>).</returns>
        public ReadOnlyMemory IncrementAndShare()
        {
            if (this.array == null)
            {
                throw new InvalidOperationException();
            }

            return new(this.array.IncrementAndShare(), this.byteArray, this.start, this.length);
        }

        /// <summary>
        ///  Increment the reference count and create a <see cref="ReadOnlyMemory"/> object by specifying the index and length.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <param name="length">The number of elements to include in the slice.</param>
        /// <returns><see cref="ReadOnlyMemory"/> object.</returns>
        public ReadOnlyMemory IncrementAndShare(int start, int length)
        {
            if (this.array == null)
            {
                throw new InvalidOperationException();
            }

            if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)this.length)
            {
                throw new ArgumentOutOfRangeException();
            }

            return new(this.array.IncrementAndShare(), this.byteArray, start, length);
        }

        /// <summary>
        ///  Increment the reference count.
        /// </summary>
        /// <returns><see cref="Array"/> instance (<see langword="this"/>).</returns>
        public ReadOnlyMemory IncrementAndShareReadOnly()
        {
            if (this.array == null)
            {
                throw new InvalidOperationException();
            }

            return new(this.array.IncrementAndShare(), this.byteArray, this.start, this.length);
        }

        /// <summary>
        ///  Increment the counter and attempt to share the <see cref="Array"/>.
        /// </summary>
        /// <returns><see langword="true"/>; Success.</returns>
        public bool TryIncrement()
        {
            if (this.array == null)
            {
                throw new InvalidOperationException();
            }

            return this.array.TryIncrement();
        }

        /// <summary>
        /// Forms a slice out of the current memory that begins at a specified index.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <returns><see cref="ReadOnlyMemory"/>.</returns>
        public ReadOnlyMemory Slice(int start)
        {
            return new(this.array, this.byteArray, this.start + start, this.length - start);
        }

        /// <summary>
        /// Forms a slice out of the current memory starting at a specified index for a specified length.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <param name="length">The number of elements to include in the slice.</param>
        /// <returns><see cref="ReadOnlyMemory"/>.</returns>
        public ReadOnlyMemory Slice(int start, int length)
        {
            return new(this.array, this.byteArray, this.start + start, length);
        }

        /// <summary>
        /// Decrement the reference count.<br/>
        /// When it reaches zero, it returns the <see cref="Array"/> to the pool.<br/>
        /// Failure to return a rented array is not a fatal error (eventually be garbage-collected).
        /// </summary>
        /// <returns><see langword="default"></see>.</returns>
        public ReadOnlyMemory Return()
        {
            this.array?.Return();
            return default;
        }

        public void Dispose()
            => this.Return();
    }

    internal sealed class Bucket
    {
        public Bucket(ByteRental byteRental, int arrayLength, int poolLimit)
        {
            this.byteRental = byteRental;
            this.ArrayLength = arrayLength;
            this.PoolLimit = poolLimit;
        }

        public int ArrayLength { get; }

        public int PoolLimit { get; private set; }

#pragma warning disable SA1401 // Fields should be private
        internal ConcurrentQueue<Array> Queue = new();
        internal int QueueCount; // Queue.Count is slow.
#pragma warning restore SA1401 // Fields should be private

        private ByteRental byteRental;

        public void SetPoolLimit(int poolLimit)
        {
            this.PoolLimit = poolLimit;
        }

        public override string ToString()
            => $"{this.ArrayLength} ({this.QueueCount}/{this.PoolLimit})";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ByteRental"/> class.<br/>
    /// </summary>
    /// <param name="maxArrayLength">The maximum length of a byte array instance that may be stored in the pool.</param>
    /// <param name="poolLimit">The maximum number of array instances that may be stored in each bucket in the pool.</param>
    private ByteRental(int maxArrayLength, int poolLimit)
    {
        if (maxArrayLength <= 0)
        {
            maxArrayLength = DefaultMaxArrayLength;
        }

        var leadingZero = BitOperations.LeadingZeroCount((uint)maxArrayLength - 1);
        this.buckets = new Bucket[33];
        var limit = 1;
        for (var i = 0; i <= 32; i++)
        {
            if (i < leadingZero)
            {
                this.buckets[i] = null;
            }
            else
            {
                this.buckets[i] = new(this, 1 << (32 - i), limit);
                limit <<= 1;
                limit = limit > poolLimit ? poolLimit : limit;
            }
        }
    }

    /// <summary>
    /// Creates a new instance of the <see cref="ByteRental"/> class.<br/>
    /// </summary>
    /// <param name="maxArrayLength">The maximum length of a byte array instance that may be stored in the pool.</param>
    /// <param name="poolLimit">The maximum number of array instances that may be stored in each bucket in the pool.</param>
    /// <returns>A new instance of the <see cref="ByteRental"/> class.</returns>
    public static ByteRental Create(int maxArrayLength = DefaultMaxArrayLength, int poolLimit = DefaultPoolLimit)
        => new(maxArrayLength, poolLimit);

    #region FieldAndProperty

    private Bucket?[] buckets;

    #endregion

    /// <summary>
    /// Gets a <see cref="Array"/> from the pool or allocate a new byte array if not available.<br/>
    /// </summary>
    /// <param name="minimumLength">The minimum length of the byte array.</param>
    /// <returns>A rent <see cref="Array"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Array Rent(int minimumLength)
    {
        var bucket = this.buckets[BitOperations.LeadingZeroCount((uint)minimumLength - 1)];
        if (bucket == null)
        {// Since the bucket is empty, allocate and return the byte array using the conventional method.
            return new Array(new byte[minimumLength]);
        }

        if (!bucket.Queue.TryDequeue(out var array))
        {// Allocate a new byte array.
            return new Array(bucket);
        }

        // Rent a byte array from the pool.
        Interlocked.Decrement(ref bucket.QueueCount);
        array.SetCount1();
        return array;
    }

    public long CalculateMaxMemoryUsage()
    {
        var usage = 0L;
        foreach (var x in this.buckets)
        {
            if (x is not null)
            {
                usage += (long)x.ArrayLength * (long)x.PoolLimit;
            }
        }

        return usage;
    }
}

using System;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace DuckovVP.Console;

public unsafe class RingBuffer<T> : IDisposable where T : unmanaged
{
    private NativeArray<T> _buffer;
    private readonly int _capacity;

    private volatile int _disposed = 0;
    private volatile int _head = 0;
    private volatile int _tail = 0;
    private volatile int _activeReaders = 0;
    public int Size => (_tail - _head + _capacity) % _capacity;

    public RingBuffer(int capacity)
    {
        _capacity = capacity;
        _buffer = new NativeArray<T>(_capacity, Allocator.Persistent);
    }

    public bool TryEnqueue(ReadOnlySpan<T> data)
    {
        if (data.IsEmpty) return true;
        
        if (data.Length >= _capacity)
        {
            data = data.Slice(data.Length - (_capacity - 1));
        }
        
        int tail = _tail;
        int head = _head;
        int used = (tail - head + _capacity) % _capacity;
        int available = _capacity - used - 1;

        if (data.Length > available)
        {
            int overflow = data.Length - available;
            
            head = (head + overflow) % _capacity;
            _head = head;
        }

        tail = _tail;

        Span<T> destSpan = new Span<T>(_buffer.GetUnsafePtr(), _capacity);

        int firstPart = Math.Min(data.Length, _capacity - tail);

        data.Slice(0, firstPart).CopyTo(destSpan.Slice(tail, firstPart));

        if (data.Length > firstPart)
        {
            int secondPart = data.Length - firstPart;
            data.Slice(firstPart, secondPart).CopyTo(destSpan.Slice(0, secondPart));
        }

        Interlocked.Exchange(ref _tail, (tail + data.Length) % _capacity);
        return true;
    }

    public int Dequeue(IntPtr dest, int count)
    {
        int head = _head;
        int tail = _tail;
        int available = (tail - head + _capacity) % _capacity;
        int toRead = Math.Min(count, available);

        Span<T> destSpan = new Span<T>((void*)dest, count);
        if (toRead <= 0)
        {
            destSpan.Clear();
            return 0;
        }

        Span<T> sourceSpan = new Span<T>(_buffer.GetUnsafePtr(), _capacity);

        int firstPart = Math.Min(toRead, _capacity - head);

        sourceSpan.Slice(head, firstPart).CopyTo(destSpan.Slice(0, firstPart));

        if (toRead > firstPart)
        {
            int secondPart = toRead - firstPart;
            sourceSpan.Slice(0, secondPart).CopyTo(destSpan.Slice(firstPart, secondPart));
        }

        if (toRead < count)
        {
            destSpan.Slice(toRead).Clear();
        }

        Interlocked.Exchange(ref _head, (head + toRead) % _capacity);
        return toRead;
    }

    public void Clear()
    {
        _head = 0;
        _tail = 0;
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0) return;
        while (Interlocked.CompareExchange(ref _activeReaders, 0, 0) != 0) ;
        if (_buffer.IsCreated)
        {
            _buffer.Dispose();
        }
    }
}
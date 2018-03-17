using System;
using System.Runtime.CompilerServices;

namespace MyImmutableInlined
{
    static class ImmutableArray
    {
            public static ImmutableArray<T>.Builder CreateBuilder<T>()
            {
                return Create<T>().ToBuilder();
            }

            public static ImmutableArray<T> Create<T>()
            {
                return ImmutableArray<T>.Empty;
            }
    }

    struct ImmutableArray<T>
    {
        public static readonly ImmutableArray<T> Empty = new ImmutableArray<T>(new T[0]);

        internal T[] array;

        internal ImmutableArray(T[] items)
        {
            this.array = items;
        }

        public int Length
        {
            get
            {
                return this.array.Length;
            }
        }        

        public ImmutableArray<T>.Builder ToBuilder()
        {
            var self = this;
            if (self.Length == 0)
            {
                return new Builder(); // allow the builder to create itself with a reasonable default capacity
            }

            var builder = new Builder(self.Length);
            builder.AddRange(self);
            return builder;
        }

        public class Builder
        {
            private T[] _elements;

            private int _count;

            internal Builder(int capacity)
            {
                Requires.Range(capacity >= 0, nameof(capacity));
                _elements = new T[capacity];
                _count = 0;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Builder"/> class.
            /// </summary>
            internal Builder()
                : this(8)
            {
            }

            public int Capacity
            {
                get { return _elements.Length; }
                set
                {
                    if (value < _count)
                    {
                        throw new ArgumentException("CapacityMustBeGreaterThanOrEqualToCount", paramName: nameof(value));
                    }

                    if (value != _elements.Length)
                    {
                        if (value > 0)
                        {
                            var temp = new T[value];
                            if (_count > 0)
                            {
                                Array.Copy(_elements, 0, temp, 0, _count);
                            }

                            _elements = temp;
                        }
                        else
                        {
                            _elements = ImmutableArray<T>.Empty.array;
                        }
                    }
                }
            }

            public void Add(T item)
            {
                var newCount = _count + 1;
                this.EnsureCapacity(newCount);
                _elements[_count] = item;
                _count = newCount;
            }

            public void AddRange(T[] items, int length)
            {
                Requires.NotNull(items, nameof(items));
                Requires.Range(length >= 0 && length <= items.Length, nameof(length));

                var offset = this.Count;
                this.Count += length;

                Array.Copy(items, 0, _elements, offset, length);
            }

            public void AddRange(ImmutableArray<T> items)
            {
                this.AddRange(items, items.Length);
            }

            public void AddRange(ImmutableArray<T> items, int length)
            {
                Requires.Range(length >= 0, nameof(length));

                if (items.array != null)
                {
                    this.AddRange(items.array, length);
                }
            }

            public int Count
            {
                get
                {
                    return _count;
                }

                set
                {
                    Requires.Range(value >= 0, nameof(value));
                    if (value < _count)
                    {
                        // truncation mode
                        // Clear the elements of the elements that are effectively removed.

                        // PERF: Array.Clear works well for big arrays, 
                        //       but may have too much overhead with small ones (which is the common case here)
                        if (_count - value > 64)
                        {
                            Array.Clear(_elements, value, _count - value);
                        }
                        else
                        {
                            for (int i = value; i < this.Count; i++)
                            {
                                _elements[i] = default(T);
                            }
                        }
                    }
                    else if (value > _count)
                    {
                        // expansion
                        this.EnsureCapacity(value);
                    }

                    _count = value;
                }
            }

            private static void ThrowIndexOutOfRangeException() => throw new IndexOutOfRangeException();

            public T this[int index]
            {
                get
                {
                    if (index >= this.Count)
                    {
                        throw new IndexOutOfRangeException();
                    }

                    return _elements[index];
                }

                set
                {
                    if (index >= this.Count)
                    {
                        ThrowIndexOutOfRangeException();
                    }

                    _elements[index] = value;
                }
            }
            private void EnsureCapacity(int capacity)
            {
                if (_elements.Length < capacity)
                {
                    int newCapacity = Math.Max(_elements.Length * 2, capacity);
                    Array.Resize(ref _elements, newCapacity);
                }
            }

            public ImmutableArray<T> MoveToImmutable()
            {
                if (Capacity != Count)
                {
                    throw new InvalidOperationException("CapacityMustEqualCountOnMove");
                }

                T[] temp = _elements;
                _elements = ImmutableArray<T>.Empty.array;
                _count = 0;
                return new ImmutableArray<T>(temp);
            }
        }
    }

    static class Requires
    {
        public static void NotNull<T>(T value, string parameterName)
            where T : class // ensures value-types aren't passed to a null checking method
        {
            if (value == null)
            {
                FailArgumentNullException(parameterName);
            }
        }

        private static void FailArgumentNullException(string parameterName)
        {
            // Separating out this throwing operation helps with inlining of the caller
            throw new ArgumentNullException(parameterName);
        }        

        public static void Range(bool condition, string parameterName, string message = null)
        {
            if (!condition)
            {
                FailRange(parameterName, message);
            }
        }

        public static void FailRange(string parameterName, string message = null)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
            else
            {
                throw new ArgumentOutOfRangeException(parameterName, message);
            }
        }        
    }
}
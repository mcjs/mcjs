// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
﻿using System;
using System.Collections.Generic;
using System.Text;

using m.Util.Diagnose;

namespace mdr
{
    public class TypedArray<T>
    {
        const int MaxLength = 20000000;
        T[] _items;
        //List<T> _items;
        public T[] Items { get { return _items; } }
        public int Capacity { get { return _items.Length; } }
        int _length;
        public void SetLength(int value)
        {
            if (value > Capacity)
            {
                int newCapacity = Math.Min(value * 2, MaxLength);
                if (newCapacity == Capacity)
                    Trace.Fail("Array index too big!");
                T[] newItems = new T[newCapacity];
                System.Array.Copy(_items, 0, newItems, 0, _items.Length);
                if (Initializer != null)
                    Initializer(newItems, _items.Length, newItems.Length - _items.Length);
                _items = newItems;
            }
            _length = value;
        }
        public int Length
        {
            get { return _length; }
            set { SetLength(value); }
        }

        //This is to avoid linking agains yet another DLL
        public delegate void Action<in T1, in T2, in T3>(T1 arg1, T2 arg2, T3 arg3);
        Action<T[], int, int> Initializer;

        public TypedArray(int initialSize, Action<T[], int, int> initializer)
        {
            _items = new T[initialSize];
            Initializer = initializer;
            if (Initializer != null)
                Initializer(_items, 0, _items.Length);
        }
        public TypedArray() : this(10, null) { }
        public TypedArray(int initialSize) : this(initialSize, null) { }

        public void EnsureIndex(int index)
        {
            if (index >= Length)
            {
                //Console.WriteLine("index {0} >= lengh {1}", index, Length);
                SetLength(index + 1);
            }
        }
        public T GetEnsured(int index)
        {
            EnsureIndex(index);
            return _items[index];
        }
        public void Set(int index, T value)
        {
            if (index >= Length)
                Length = index + 1;
            _items[index] = value;
        }
        //protected virtual void Initialize(T[] array, int fromIndex, int length)
        //{
        //    //This if mostly for specilized children
        //    if (Allocator != null)
        //        for (int i = 0; i < length; ++i)
        //            array[i + fromIndex] = Allocator();
        //}
        public T this[int index]
        {
            get
            {
                //return _items[index];
                //if (index < _items.Length)
                //    return _items[index];
                //else
                //    return default(T);
                return GetEnsured(index);
            }
            set { Set(index, value); }
        }
    }

    public class BitArray : TypedArray<ulong>
    {
        const int BitsPerElement = sizeof(ulong);
        public BitArray(int initialSize)
            : base(initialSize / BitsPerElement + 1)
        { }

        public new int Length; //This represents number of bits and should hide parent imp

        public bool Get(int index)
        {
            if (index >= Length)
                return false;

            var elementIndex = index / BitsPerElement;
            var bitIndex = index % BitsPerElement;
            //if (elementIndex >= Length)
            //    return false;

            //We don't need to use GetEnsured since we're sure (elementIndex < Length) is true now
            var element = Items[elementIndex];
            ulong mask = 1UL << bitIndex;
            bool value = (element & mask) != 0;
            return value;
        }
        public void Set(int index, bool value)
        {
            if (index >= Length)
                Length = index + 1;

            var elementIndex = index / BitsPerElement;
            var bitIndex = index % BitsPerElement;
            var element = GetEnsured(elementIndex);
            ulong mask = 1UL << bitIndex;
            if (value)
                element = element | mask;
            else
                element = element & ~mask;
            Items[elementIndex] = element;
        }
        public void Set(int fromIndex, int toIndex, bool value)
        {
            Debug.Assert(toIndex >= fromIndex, string.Format("Invalid indexes: ({0}<={1}) is not true!", fromIndex, toIndex));

            //slow version
            //for (var i = toIndex; i >= fromIndex; --i)
            //    Set(i, value);
            //return;

            if (toIndex >= Length)
                Length = toIndex + 1;

            var fromElementIndex = fromIndex / BitsPerElement;
            var fromBitIndex = fromIndex % BitsPerElement;
            ulong mask1 = ulong.MaxValue << fromBitIndex; //0b1..10..0

            var toElementIndex = toIndex / BitsPerElement;
            var toBitIndex = toIndex % BitsPerElement;
            ulong mask2 = ulong.MaxValue << (toBitIndex + 1); //0b1..10..0

            var lastElement = GetEnsured(toElementIndex);

            if (fromElementIndex == toElementIndex)
            {
                var mask = mask1 ^ mask2; //0b0..01..10..0
                if (value)
                    lastElement |= mask;
                else
                    lastElement &= ~mask;
                Items[toElementIndex] = lastElement;
            }
            else
            {
                if (value)
                    lastElement |= ~mask2;
                else
                    lastElement &= mask2;
                Items[toElementIndex] = lastElement;

                var firstElement = Items[fromElementIndex];
                if (value)
                    firstElement |= mask1;
                else
                    firstElement &= ~mask1;
                Items[fromElementIndex] = firstElement;

                for (var i = fromElementIndex + 1; i < toElementIndex; ++i)
                    Items[i] = value ? ulong.MaxValue : ulong.MinValue;
            }
        }
    }
    public class DObjectArray : TypedArray<DObject>
    {
        public DObjectArray(int initialSize)
            : base(initialSize) { }
        //    : base(initialSize, (array, startIndex, length) =>
        //    {
        //        for (int i = 0; i < length; ++i)
        //            array[i + startIndex] = DObject.Undefined;
        //    })
        //{ }
        //protected override void Initialize(DObject[] array, int fromIndex, int length)
        //{
        //    for (int i = 0; i < length; ++i)
        //        array[i + fromIndex] = DObject.Undefined;
        //}
    }
    public class DValueArray : TypedArray<DValue>
    {
        public DValueArray(int initialSize)
            : base(initialSize)
        { }
    }
}

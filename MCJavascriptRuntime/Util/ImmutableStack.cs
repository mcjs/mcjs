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
using System.Collections;
using System.Collections.Generic;

namespace mjr.Util
{
    public interface IStack<T>
    {
        T Peek();
        IStack<T> Pop();
        IStack<T> Push(T value);
        bool IsEmpty { get; }
    }

    // <summary>
    // An immutable stack; useful for the parser, which has to constantly copy its stacks because it may have to rewind them to an earlier state due to
    // semicolon insertion. With an immutable stack, the copy is free; the tradeoff is that the stack uses a linked-list representation that incurs
    // an overhead of one pointer per item in the stack, instead of storing the stack contents in an array.
    // </summary>
    public sealed class ImmutableStack<T> : IStack<T>, IEnumerable<T>
    {
        private static readonly IStack<T> empty = new EmptyImmutableStack();
        private readonly T head;
        private readonly IStack<T> tail;

        private ImmutableStack(T head_, IStack<T> tail_)
        {
            head = head_;
            tail = tail_;
        }

        public T Peek()
        {
            return head;
        }

        public IStack<T> Pop()
        {
            return tail;
        }

        public IStack<T> Push(T value)
        {
            return new ImmutableStack<T>(value, this);
        }

        public bool IsEmpty
        {
            get { return false; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (IStack<T> stack = this; !stack.IsEmpty; stack = stack.Pop())
                yield return stack.Peek();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public static IStack<T> Empty
        {
            get { return empty; }
        }

        private sealed class EmptyImmutableStack : IStack<T>, IEnumerable<T>
        {
            public EmptyImmutableStack()
            {
            }

            public T Peek()
            {
                throw new InvalidOperationException("Tried to Peek() on empty ImmutableStack");
            }

            public IStack<T> Pop()
            {
                throw new InvalidOperationException("Tried to Pop() on empty ImmutableStack");
            }

            public IStack<T> Push(T value)
            {
                return new ImmutableStack<T>(value, this);
            }

            public bool IsEmpty
            {
                get { return true; }
            }

            public IEnumerator<T> GetEnumerator()
            {
                yield break;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }
    }
}

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

using m.Util.Diagnose;

namespace mdr
{
    public sealed class DArray : DArrayBase
    {
        public override ValueTypes ValueType { get { return ValueTypes.Array; } }

        public DArray(int initialSize = 0)
            : base(Runtime.Instance.DArrayMap)
        {
            //We don't need the high overhead of ResizeElements
            ElementsLength = initialSize;
            if (initialSize == 0)
                initialSize = 10; //this ensures we have at least a few items in the array and will not resize a lot

            if (Elements == null || Elements.Length < initialSize)
                Elements = new DValue[initialSize];
        }

        #region Value
        public override string ToString()
        {
            var l = Length;
            if (l == 0)
                return "";
            var s = new System.Text.StringBuilder();
            for (var i = 0; i < l - 1; ++i)
            {
                //s.Append(Elements[i].ToString());
                if (Elements[i].ValueType != mdr.ValueTypes.Undefined)
                    s.Append(Elements[i].AsString());
                s.Append(",");
            }
            for (var i = l - 1; i < l; ++i)
                s.Append(Elements[i].AsString());
            return s.ToString();
        }
        public override DArray ToDArray() { return this; }
        #endregion

        #region Elements //////////////////////////////////////////////////////////////////////////////////////////////////////
        public int Length
        {
            get { return ElementsLength; }
            set
            {
                ResizeElements(value - 1);
                ElementsLength = value;
            }
        }
        const int MaxElementsCount = 20000000;

        /// <summary>
        /// Elements is used to store properties whose key can be converted to int
        /// </summary>
        public int ElementsLength = 0;
        public DValue[] Elements; //This is explicityly left unprotected to reduce access overhead
        public void ResizeElements(int maxIndex)
        {
            var capacity = Elements.Length;
            if (maxIndex >= capacity)
            {
                int newCapacity = Math.Min(maxIndex * 2, MaxElementsCount);
                if (newCapacity == capacity)
                    Trace.Fail("Element index is too big!");

                var newElements = new DValue[newCapacity];
                Array.Copy(Elements, newElements, Elements.Length);
                Elements = newElements;
            }
        }
        public void RemoveElements(int startIndex, int count)
        {
            if (startIndex >= Length)
                return;
            if (startIndex + count > Length)
                count = Length - startIndex;

            //even after the above adjustment of count, still count + startIndex can be equal to Length. In that case no copying is needed.
            if (startIndex + count < Length)
                Array.Copy(Elements, startIndex + count, Elements, startIndex, count);
            Length = Length - count;
        }
        #endregion

        #region GetPropertyDescriptor
        public override PropertyDescriptor GetPropertyDescriptor(int field)
        {
            if (field < 0)
                return base.GetPropertyDescriptor(field);

            if (field < ElementsLength)
            {
                var accessor = Runtime.Instance.ArrayItemAccessor;
                accessor.Index = field;
                return accessor;
            }
            else
                return UndefinedItemAccessor;
            ///what we should leter here:
            ///  search the proto chain, and return an inherited or undefined PD instead.
        }
        #endregion

        #region AddPropertyDescriptor
        public override PropertyDescriptor AddPropertyDescriptor(int field)
        {
            if (field < 0)
                return base.AddPropertyDescriptor(field);

            if (field >= ElementsLength)
            {
                ElementsLength = field + 1;
                ResizeElements(field);
            }
            var accessor = Runtime.Instance.ArrayItemAccessor;
            accessor.Index = field;
            return accessor;
            ///What we should do later here:
            /// assing a new PD to the proper element if not there
        }
        #endregion

        #region DeletePropertyDescriptor
        public override PropertyMap.DeleteStatus DeletePropertyDescriptor(int field)
        {
            if (field < ElementsLength)
            {
                Elements[field].SetUndefined();
                return PropertyMap.DeleteStatus.Deleted;
            }
            return PropertyMap.DeleteStatus.NotFound;
        }
        #endregion

        [System.Diagnostics.DebuggerStepThrough]
        public override void Accept(IMdrVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}

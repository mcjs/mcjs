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

namespace mjr.Builtins.TypedArray
{
    class DFloat64Array : DTypedArray
    {
        public DFloat64Array(mdr.DObject prototype, int bytelength, int typesize)
            : base(prototype, bytelength, typesize) { }

        public DFloat64Array(mdr.DObject prototype, DArrayBuffer array, int byteoffset, int bytelength, int typesize)
            : base(prototype, array, byteoffset, bytelength, typesize) { }

        static mdr.PropertyDescriptor ItemAccessor = new mdr.PropertyDescriptor(null)
        {
            Getter = (mdr.PropertyDescriptor pd, mdr.DObject obj, ref mdr.DValue value) =>
            {
                DTypedArray array = (obj as DTypedArray);
                int index = pd.Index * array.TypeSize + array.ByteOffset;
                Int64 result = array.Elements_[index];
                result ^= (Int64)array.Elements_[index + 1] << 8;
                result ^= (Int64)array.Elements_[index + 2] << 16;
                result ^= (Int64)array.Elements_[index + 3] << 24;
                result ^= (Int64)array.Elements_[index + 4] << 32;
                result ^= (Int64)array.Elements_[index + 5] << 40;
                result ^= (Int64)array.Elements_[index + 6] << 48;
                result ^= (Int64)array.Elements_[index + 7] << 56;
                double val = BitConverter.Int64BitsToDouble(result);
                value.Set(val);
            }, 

            Setter = (mdr.PropertyDescriptor pd, mdr.DObject obj, ref mdr.DValue value) =>
            {
                DTypedArray array = (obj as DTypedArray);
                int index = pd.Index * array.TypeSize + array.ByteOffset;
                Int64 int64val = BitConverter.DoubleToInt64Bits(value.AsDouble());
                array.Elements_[index] = (byte)(int64val & 0xff);
                array.Elements_[index + 1] = (byte)((int64val >> 8) & 0xff);
                array.Elements_[index + 2] = (byte)((int64val >> 16) & 0xff);
                array.Elements_[index + 3] = (byte)((int64val >> 24) & 0xff);
                array.Elements_[index + 4] = (byte)((int64val >> 32) & 0xff);
                array.Elements_[index + 5] = (byte)((int64val >> 40) & 0xff);
                array.Elements_[index + 6] = (byte)((int64val >> 48) & 0xff);
                array.Elements_[index + 7] = (byte)((int64val >> 56) & 0xff);
            },
        };

        public override mdr.PropertyDescriptor AddPropertyDescriptor(int field)
        {
            if (field * TypeSize >= ByteLength)
                return base.AddPropertyDescriptor(field);
            ItemAccessor.Index = field;
            return ItemAccessor;
        }

        public override mdr.PropertyDescriptor GetPropertyDescriptor(int field)
        {
            if (field * TypeSize >= ByteLength)
                return base.GetPropertyDescriptor(field);
            ItemAccessor.Index = field;
            return ItemAccessor;
        }
    }
}

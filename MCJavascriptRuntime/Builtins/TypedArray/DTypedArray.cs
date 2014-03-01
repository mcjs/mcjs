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
    public class DTypedArray : mdr.DArrayBase
    {
        #region Constructors
        protected DTypedArray(mdr.DObject prototype, int bytelength, int typesize)
            : base(prototype)
        {
            ByteLength_ = Math.Min(bytelength, MaxElementsCount);
            TypeSize_ = typesize;
            Elements_ = new byte[ByteLength_];
            for (int i = 0; i < ByteLength_; ++i)
                Elements_[i] = 0x00;
        }

        protected DTypedArray(mdr.DObject prototype, DArrayBuffer array, int byteoffset, int bytelength, int typesize)
            : base(prototype)
        {
            ByteOffset_ = byteoffset;
            TypeSize_ = typesize;
            ByteLength_ = bytelength;
            Elements_ = array.Elements_;
        }
        #endregion

        public override mdr.PropertyDescriptor AddPropertyDescriptor(int field)
        {
            return UndefinedItemAccessor;
        }

        public override mdr.PropertyDescriptor GetPropertyDescriptor(int field)
        {
            return UndefinedItemAccessor;
        }

        #region Elements
        public int ByteLength
        {
            get { return ByteLength_; }
            set { ByteLength_ = value; }
        }

        public int TypeSize
        {
            get { return TypeSize_; }
            set { TypeSize_ = value; }
        }

        public int ByteOffset
        {
            get { return ByteOffset_; }
            set { ByteOffset_ = value; }
        }

        const int MaxElementsCount = 20000000;
        private int ByteLength_ = 0;
        private int TypeSize_ = 0;
        private int ByteOffset_ = 0;
        public byte[] Elements_;
        #endregion
    }
}

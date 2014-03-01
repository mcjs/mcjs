// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
﻿using m.Util.Diagnose;

namespace mjr.Builtins.TypedArray
{
    class JSTypedArrayBase : JSBuiltinConstructor
    {
        private int TypeSize;

        public JSTypedArrayBase(mdr.DObject prototype, string arrayname, int typesize)
            : base(prototype, arrayname)
        {
            TypeSize = typesize;
            
            TargetPrototype.DefineOwnProperty("length", new mdr.DProperty() {
                TargetValueType = mdr.ValueTypes.Int32,
                OnGetDValue = (mdr.DObject This, ref mdr.DValue v) => {
                    v.Set((This as DTypedArray).ByteLength / TypeSize);
                },
                OnSetDValue = (mdr.DObject This, ref mdr.DValue v) => { /* do nothing */ },
                OnSetInt = (mdr.DObject This, int v) => { /* do nothing */ },
            });

            TargetPrototype.DefineOwnProperty("byteLength", new mdr.DProperty() {
                TargetValueType = mdr.ValueTypes.Int32,
                OnGetDValue = (mdr.DObject This, ref mdr.DValue v) => {
                    v.Set((This as DTypedArray).ByteLength);
                },
                OnSetDValue = (mdr.DObject This, ref mdr.DValue v) => { /* do nothing */ },
                OnSetInt = (mdr.DObject This, int v) => { /* do nothing */ },
            });

            // Constants
            this.DefineOwnProperty("BYTES_PER_ELEMENT", TypeSize, mdr.PropertyDescriptor.Attributes.NotConfigurable | mdr.PropertyDescriptor.Attributes.NotWritable | mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            TargetPrototype.DefineOwnProperty("BYTES_PER_ELEMENT", TypeSize, mdr.PropertyDescriptor.Attributes.NotConfigurable | mdr.PropertyDescriptor.Attributes.NotWritable | mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
        }

        protected void checkOffsetCompatibility(int byteoffset, int bytelength) {
            if (byteoffset % TypeSize != 0 || byteoffset > bytelength)
                Trace.Fail("invalid Arguments");
        }

        protected void checkOffsetMemBoundary(int byteoffset) {
            if (byteoffset % TypeSize != 0)
                Trace.Fail("invalid Arguments");
        }
    }
}

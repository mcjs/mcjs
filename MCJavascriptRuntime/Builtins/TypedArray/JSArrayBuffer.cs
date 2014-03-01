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
    class JSArrayBuffer : JSBuiltinConstructor
    {
        const int ByteSize = sizeof(byte);

        public JSArrayBuffer()
            : base(new mdr.DObject(), "ArrayBuffer")
        {
            JittedCode = (ref mdr.CallFrame callFrame) =>
            {
                DArrayBuffer buffer;
                var len = 0;
                var argsCount = callFrame.PassedArgsCount;
                if (argsCount > 0)
                    len = Math.Max(0, callFrame.Arg0.AsInt32());

                buffer = new DArrayBuffer(TargetPrototype, len);
                if (IsConstrutor)
                    callFrame.This = (buffer);
                else
                    callFrame.Return.Set(buffer);
            };

            TargetPrototype.DefineOwnProperty("byteLength", new mdr.DProperty()
            {
                OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                {
                    v.Set((This as DArrayBuffer).ByteLength);
                },
                OnSetDValue = (mdr.DObject This, ref mdr.DValue v) => { /* do nothing */ },
                OnSetInt = (mdr.DObject This, int v) => { /* do nothing */ },
            });
        }
    }
}
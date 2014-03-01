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

namespace mdr
{
    public sealed class DNull : DObject
    {
        public override ValueTypes ValueType { get { return ValueTypes.Null; } }
        public override string GetTypeOf() { return "object"; }

        internal DNull()
            : base(Runtime.Instance.EmptyPropertyMapMetadata.Root)
        {
            Fields = new DValue[1];
            Fields[0].Set(this);
        }

        public override string ToString() { return GetTypeOf(); }
        public override char ToChar() { return '\0'; }
        public override bool ToBoolean() { return false; }
        public override float ToFloat() { return 0; }
        public override double ToDouble() { return 0; }

        public override sbyte ToInt8() { return 0; }
        public override short ToInt16() { return 0; }
        public override int ToInt32() { return 0; }
        public override long ToInt64() { return 0; }
        public override byte ToUInt8() { return 0; }
        public override ushort ToUInt16() { return 0; }
        public override uint ToUInt32() { return 0; }
        public override ulong ToUInt64() { return 0; }

        [System.Diagnostics.DebuggerStepThrough]
        public override void Accept(IMdrVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}

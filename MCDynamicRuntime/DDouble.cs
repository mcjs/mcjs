// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
﻿namespace mdr
{
    public sealed class DDouble : DObject
    {
        public override ValueTypes ValueType { get { return ValueTypes.Double; } }

        double Value;
        public DDouble(double v)
            : base(Runtime.Instance.DNumberMap)
        {
            Value = v;
        }

        public override string ToString() { return Value.ToString(); }
        public override double ToDouble() { return Value; }
        public override int ToInt() { return (int)Value; }
        public override bool ToBoolean() { return Value != 0; }
        public override long ToLong() { return (long)Value; }

        public override DObject Set(double v)
        {
            if (v == Value)
                return this;
            return base.Set(v);
        }
        //public override DObject Set(int v) { return this.Set((double)v; return this; }
        //public override DObject Set(long v) { Value = v; return this; }


        //public override void CopyTo(DVar v) { v.Set(Value); }

        public override string GetTypeOf() { return "number"; }

        [System.Diagnostics.DebuggerStepThrough]
        public override void Accept(IMdrVisitor visitor)
        {
            visitor.Visit(this);
        }

    }
}

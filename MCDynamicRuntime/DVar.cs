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
using System.Linq;
using System.Text;

namespace mdr
{
    public class DVar
    {
        public DObject Object;
        //public DObject Object { get { return _value; } }

        public DVar(double v) { Object = new DDouble(v); }
        public DVar(long v) { Object = new DLong(v); }
        public DVar(int v) { Object = new DInt(v); }
        public DVar(bool v) { Object = new DBoolean(v); }
        public DVar(string v) { Object = new DString(v); }
        public DVar(DObject v) { Object = v; }
        public DVar() : this(DObject.Undefined) { }

        public void Set(double v)
        {
            Object = Object.Set(v);
        }
        public void Set(int v)
        {
            Object = Object.Set(v);
        }
        public void Set(long v)
        {
            Object = Object.Set(v);
        }
        public void Set(string v)
        {
            Object = Object.Set(v);
        }
        public void Set(bool v)
        {
            Object = Object.Set(v);
        }
        public void Set(DObject v)
        {
            if (v == null)
                Object = DObject.Undefined;
            else
                v.CopyTo(this);
        }

        public void Set(DVar v) { v.Object.CopyTo(this); }

        public static implicit operator double(DVar v) { return v.Object.ToDouble(); }
        public static implicit operator int(DVar v) { return v.Object.ToInt(); }
        public static implicit operator long(DVar v) { return v.Object.ToLong(); }
        public static implicit operator string(DVar v) { return v.Object.ToString(); }
        public static implicit operator bool(DVar v) { return v.Object.ToBoolean(); }

        public static implicit operator DVar(double v) { return new DVar(v); }
        public static implicit operator DVar(int v) { return new DVar(v); }
        public static implicit operator DVar(long v) { return new DVar(v); }
        public static implicit operator DVar(string v) { return new DVar(v); }
        public static implicit operator DVar(bool v) { return new DVar(v); }
        public static implicit operator DVar(DObject v) { return new DVar(v); }

        [System.Diagnostics.DebuggerStepThrough]
        public virtual void Accept(IMdrVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}

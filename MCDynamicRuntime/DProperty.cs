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
    public class DProperty : DObject
    {
        public override ValueTypes ValueType { get { return ValueTypes.Property; } }
        //public override ValueTypes ValueType { get { return TargetValueType; } }

        public delegate T OnGet<T>(DObject This);
        public delegate void OnSet<T>(DObject This, T v);
        public delegate void OnGetRef<T>(DObject This, ref T v);
        public delegate void OnSetRef<T>(DObject This, ref T v);

        public DProperty() : base() { }


        public virtual DProperty GetEffectiveProperty() {
            return this;
        }

        public ValueTypes TargetValueType { private get; set; }

        public OnGet<int> OnGetInt { get; set; }
        public OnSet<int> OnSetInt { get; set; }
        public OnGet<double> OnGetDouble { get; set; }
        public OnSet<double> OnSetDouble { get; set; }
        public OnGet<string> OnGetString { get; set; }
        public OnSet<string> OnSetString { get; set; }
        public OnGet<bool> OnGetBoolean { get; set; }
        public OnSet<bool> OnSetBoolean { get; set; }
        public OnGet<DObject> OnGetDObject { get; set; }
        public OnSet<DObject> OnSetDObject { get; set; }
        public OnGetRef<DValue> OnGetDValue { get; set; }
        public OnSetRef<DValue> OnSetDValue { get; set; }
        //public OnSet<DVar> OnCopyTo {get; set;}
        public DFunction Getter { get; set; }
        public DFunction Setter { get; set; }

        //#region ToX
        //public string ToString(DObject This) { return (OnGetString != null) ? OnGetString(This) : ToDValue().ToString(); }// base.ToString(); }
        //public double ToDouble(DObject This) { return (OnGetDouble != null) ? OnGetDouble(This) : ToDValue().ToDouble(); }// base.ToDouble(); }
        //public int ToInt(DObject This) { return (OnGetInt != null) ? OnGetInt(This) : ToDValue().ToInt32(); }// base.ToInt(); }
        //public bool ToBoolean(DObject This) { return (OnGetBoolean != null) ? OnGetBoolean(This) : ToDValue().ToBoolean(); }// base.ToBoolean(); }
        //public DObject ToDObject(DObject This) { return (OnGetDObject != null) ? OnGetDObject(This) : ToDValue().ToDObject(); }// base.ToDObject(); }
        //public DValue ToDValue(DObject This)
        //{
        //    if (OnGetDValue != null)
        //    {
        //        DValue temp = new DValue();
        //        OnGetDValue(This, ref temp);
        //        return temp;
        //    }
        //    else
        //    {
        //        return base.ToDValue();
        //    }
        //}
        //#endregion

        #region Get
        public void Get(DObject This, ref DValue v)
        {
            if (OnGetDValue != null)
                OnGetDValue(This, ref v);
            else if (Getter != null)
            {
                var callFrame = new CallFrame();
                callFrame.Function = Getter;
                callFrame.This = (This);
                Getter.Call(ref callFrame);
                v = callFrame.Return;
            }
            else
                v = base.ToDValue();
        }
        #endregion
        #region Set
        //public override DObject Set(double v) { if (OnSetDouble != null) OnSetDouble(v); return this; }
        //public override DObject Set(int v) { if (OnSetInt != null)  OnSetInt(v); return this; }
        //public override DObject Set(bool v) { if (OnSetBoolean != null) OnSetBoolean(v); return this; }
        //public override DObject Set(string v) { if (OnSetString != null) OnSetString(v); return this; }
        //public override DObject Set(ref DValue v) { if (OnSetDValue != null) OnSetDValue(ref v); return this; }
        public DObject Set(DObject This, double v)
        {
            if (OnSetDouble == null)
            {
                var tmp = new DValue();
                tmp.Set(v);
                return Set(This, ref tmp);
            }
            else
            {
                OnSetDouble(This, v);
                return this;
            }
        }
        public DObject Set(DObject This, int v)
        {
            if (OnSetInt == null)
            {
                var tmp = new DValue();
                tmp.Set(v);
                return Set(This, ref tmp);
            }
            else
            {
                OnSetInt(This, v);
                return this;
            }
        }
        public DObject Set(DObject This, bool v)
        {
            if (OnSetBoolean == null)
            {
                var tmp = new DValue();
                tmp.Set(v);
                return Set(This, ref tmp);
            }
            else
            {
                OnSetBoolean(This, v);
                return this;
            }
        }
        public DObject Set(DObject This, string v)
        {
            if (OnSetString == null)
            {
                var tmp = new DValue();
                tmp.Set(v);
                return Set(This, ref tmp);
            }
            else
            {
                OnSetString(This, v);
                return this;
            }
        }
        public DObject Set(DObject This, DObject v)
        {
            if (OnSetDObject == null)
            {
                var tmp = new DValue();
                tmp.Set(v);
                return Set(This, ref tmp);
            }
            else
            {
                OnSetDObject(This, v);
                return this;
            }
        }
        public DObject Set(DObject This, ref DValue v)
        {
            if (OnSetDValue != null)
                OnSetDValue(This, ref v);
            else if (Setter != null)
            {
                var callFrame = new CallFrame();
                callFrame.Function = Setter;
                callFrame.This = (This);
                callFrame.PassedArgsCount = 1;
                callFrame.Arg0 = v;
                callFrame.Signature.InitArgType(0, v.ValueType);
                Setter.Call(ref callFrame);
            }
            else
                Trace.Fail(new NotSupportedException(string.Format("Cannot find setter for {0}:{1} on property {2}", v, typeof(DValue), base.ToString())));
            return this;
        }
        #endregion

        //We always want to keep the property object arrount!
        //public override void CopyTo(DVar v) { if (OnCopyTo != null) OnCopyTo(v);}

        [System.Diagnostics.DebuggerStepThrough]
        public override void Accept(IMdrVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}

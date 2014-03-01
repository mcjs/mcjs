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
    public class DString : DArrayBase
    {
        //public override ValueTypes ValueType { get { return ValueTypes.String; } }
        public override string GetTypeOf() { return "string"; }

        public DString(string v)
            : base(Runtime.Instance.DStringMap)
        {
          PrimitiveValue.Set(v);
        }

        public override string ToString() { return PrimitiveValue.AsString(); }
        //public override bool ToBoolean() { return !string.IsNullOrEmpty(Value);/* return System.Convert.ToBoolean(Value); */}
        //public override float ToFloat() { return (float)ToDouble(); }
        //public override double ToDouble()
        //{
        //    if (Value.StartsWith("0x"))
        //        return (Double)Convert.ToInt64(Value, 16);
        //    if (Value.StartsWith("0."))
        //        return Convert.ToDouble(Value);
        //    if (Value.StartsWith("0"))
        //        return (Double)Convert.ToInt64(Value, 8);
        //    if (Value == "Infinity" || Value == "+Infinity")
        //        return double.PositiveInfinity;
        //    if (Value == "-Infinity")
        //        return double.NegativeInfinity;
        //    try
        //    {
        //        return System.Convert.ToDouble(Value);
        //    }
        //    catch (FormatException)
        //    {
        //      return double.NaN;
        //    }
        //    catch (OverflowException)
        //    {
        //      return double.NaN;
        //    }
        //}
        //public override sbyte ToInt8() { return (sbyte)ToDouble(); }
        //public override short ToInt16() { return (short)ToDouble(); }
        //public override int ToInt32() { return (int)ToDouble(); }
        //public override long ToInt64() { return (long)ToDouble(); }
        //public override byte ToUInt8() { return (byte)ToDouble(); }
        //public override ushort ToUInt16() { return (ushort)ToDouble(); }
        //public override uint ToUInt32() { return (uint)ToDouble(); }
        //public override ulong ToUInt64() { return (ulong)ToDouble(); }

        //public override DObject Set(string v)
        //{
        //    if (v == Value)
        //        return this;
        //    return base.Set(v);
        //}

        #region GetPropertyDescriptor
        public override PropertyDescriptor GetPropertyDescriptor(int field)
        {
            if (field < PrimitiveValue.AsString().Length)
            {
                var accessor = Runtime.Instance.StringItemAccessor;
                accessor.Index = field;
                return accessor;
            }
            else
                return UndefinedItemAccessor;
        }
        #endregion

        #region AddPropertyDescriptor
        public override PropertyDescriptor AddPropertyDescriptor(int field)
        {
            return GetPropertyDescriptor(field);
        }
        #endregion

        #region DeletePropertyDescriptor
        public override PropertyMap.DeleteStatus DeletePropertyDescriptor(int field)
        {
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

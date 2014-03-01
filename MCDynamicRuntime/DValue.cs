// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
﻿//#define COMPACT_VALUE
using System;
using System.Runtime.InteropServices;

using m.Util.Diagnose;

namespace mdr
{
#if !COMPACT_VALUE
  [StructLayout(LayoutKind.Explicit)]
  public struct DValue
  {
    const int TypeOffset = 0;
    const int ValueOffset = 4;
    //We have to keep objects aligned to 4 for 32-bit and 8 for 64-bit
    const int ObjectOffset = 12 + 4; // for aligning correctly in 64-bit

    [FieldOffset(TypeOffset)]
    ValueTypes _ValueType;
    public ValueTypes ValueType { get { return _ValueType; } } //This is to make sure both COMPACT and normal cases behave the same. 

    //[FieldOffset(ElementValueTypeOffset)]
    //public ValueTypes ElementValueType;

    [FieldOffset(ValueOffset)]
    char CharValue;

    [FieldOffset(ValueOffset)]
    bool BooleanValue;

    [FieldOffset(ValueOffset)]
    float FloatValue;

    [FieldOffset(ValueOffset)]
    double DoubleValue;

    [FieldOffset(ValueOffset)]
    sbyte Int8Value;

    [FieldOffset(ValueOffset)]
    short Int16Value;

    [FieldOffset(ValueOffset)]
    int Int32Value;

    [FieldOffset(ValueOffset)]
    long Int64Value;

    [FieldOffset(ValueOffset)]
    byte UInt8Value;

    [FieldOffset(ValueOffset)]
    ushort UInt16Value;

    [FieldOffset(ValueOffset)]
    uint UInt32Value;

    [FieldOffset(ValueOffset)]
    ulong UInt64Value;

    [FieldOffset(ValueOffset)]
    int IntValue;

    [FieldOffset(ObjectOffset)]
    object ObjectValue;
    //public DObject DObjectValue;


    //The followings can be used to read each byte of the value
    [FieldOffset(ValueOffset + 0)]
    public byte Byte0;
    [FieldOffset(ValueOffset + 1)]
    public byte Byte1;
    [FieldOffset(ValueOffset + 2)]
    public byte Byte2;
    [FieldOffset(ValueOffset + 3)]
    public byte Byte3;
    [FieldOffset(ValueOffset + 4)]
    public byte Byte4;
    [FieldOffset(ValueOffset + 5)]
    public byte Byte5;
    [FieldOffset(ValueOffset + 6)]
    public byte Byte6;
    [FieldOffset(ValueOffset + 7)]
    public byte Byte7;


    public bool AsBoolean() { Debug.Assert(ValueType == ValueTypes.Boolean, "cannot convert from {0} to Boolean", ValueType); return BooleanValue; }

    public string AsString() { Debug.Assert(ValueType == ValueTypes.String, "cannot convert from {0} to String", ValueType); return ObjectValue as string; }

    public char AsChar() { Debug.Assert(ValueType == ValueTypes.Char, "cannot convert from {0} to Char", ValueType); return CharValue; }

    public float AsFloat() { Debug.Assert(ValueType == ValueTypes.Float, "cannot convert from {0} to Float", ValueType); return FloatValue; }

    public double AsDouble() { Debug.Assert(ValueType == ValueTypes.Double, "cannot convert from {0} to Double", ValueType); return DoubleValue; }

    public sbyte AsInt8() { Debug.Assert(ValueType == ValueTypes.Int8, "cannot convert from {0} to Int8", ValueType); return Int8Value; }

    public short AsInt16() { Debug.Assert(ValueType == ValueTypes.Int16, "cannot convert from {0} to Int16", ValueType); return Int16Value; }

    public int AsInt32() { Debug.Assert(ValueType == ValueTypes.Int32, "cannot convert from {0} to Int32", ValueType); return Int32Value; }

    public long AsInt64() { Debug.Assert(ValueType == ValueTypes.Int64, "cannot convert from {0} to Int64", ValueType); return Int64Value; }

    public byte AsUInt8() { Debug.Assert(ValueType == ValueTypes.UInt8, "cannot convert from {0} to UInt8", ValueType); return UInt8Value; }

    public ushort AsUInt16() { Debug.Assert(ValueType == ValueTypes.UInt16, "cannot convert from {0} to UInt16", ValueType); return UInt16Value; }

    public uint AsUInt32() { Debug.Assert(ValueType == ValueTypes.UInt32, "cannot convert from {0} to UInt32", ValueType); return UInt32Value; }

    public ulong AsUInt64() { Debug.Assert(ValueType == ValueTypes.UInt64, "cannot convert from {0} to UInt64", ValueType); return UInt64Value; }

    public DObject AsDObject() { Debug.Assert(ValueTypesHelper.IsObject(ValueType), "cannot convert from {0} to DObject", ValueType); return ObjectValue as DObject; }

    public DFunction AsDFunction() { Debug.Assert(ValueType == ValueTypes.Function, "cannot convert from {0} to DFunction", ValueType); return ObjectValue as DFunction; }

    public DArray AsDArray() { Debug.Assert(ValueType == ValueTypes.Array, "cannot convert from {0} to DArray", ValueType); return ObjectValue as DArray; }

    public DProperty AsDProperty() { Debug.Assert(ValueType == ValueTypes.Property, "cannot convert from {0} to DProperty", ValueType); return ObjectValue as DProperty; }

    public object AsObject() { Debug.Assert(ValueType == ValueTypes.Any, "cannot from {0} to object", ValueType); return ObjectValue; }

    public DUndefined AsDUndefined() { Debug.Assert(ValueType == ValueTypes.Undefined, "cannot from {0} to object", ValueType); return Runtime.Instance.DefaultDUndefined; }
    
    public DNull AsDNull() { Debug.Assert(ValueType == ValueTypes.Null, "cannot from {0} to object", ValueType); return Runtime.Instance.DefaultDNull; }


    public T As<T>() where T : class
    {
      if (ValueTypesHelper.IsObject(ValueType))
        return ObjectValue as T;
      else
        return null;
    }

#if ENABLE_CONVERSIONS
        public new string ToString()
        {
          switch (ValueType)
          {
            case ValueTypes.Undefined: return "undefined";
            case ValueTypes.String: return ObjectValue as string;
            case ValueTypes.Char: return CharValue.ToString();
            case ValueTypes.Boolean: return (BooleanValue ? "true" : "false");
            case ValueTypes.Float: return FloatValue.ToString();
            case ValueTypes.Double: return DoubleValue.ToString();
            case ValueTypes.Int8: return Int8Value.ToString();
            case ValueTypes.Int16: return Int16Value.ToString();
            case ValueTypes.Int32: return Int32Value.ToString();
            case ValueTypes.Int64: return Int64Value.ToString();
            case ValueTypes.UInt8: return UInt8Value.ToString();
            case ValueTypes.UInt16: return UInt16Value.ToString();
            case ValueTypes.UInt32: return UInt32Value.ToString();
            case ValueTypes.UInt64: return UInt64Value.ToString();
            case ValueTypes.Object:
            case ValueTypes.Function:
            case ValueTypes.Array:
              return AsDObject().ToString();
            case ValueTypes.Null: return "null";
            //case ValueTypes.Property:
            //default:
            //    break;
          }
          Trace.Fail(new InvalidOperationException(string.Format("Cannot convert {0} to String", ValueType)));
          return Runtime.Instance.DefaultDUndefined.ToString();
        }


        public string GetTypeOf()
        {
            switch (ValueType)
            {
                case ValueTypes.Undefined:
                    return "undefined";
                case ValueTypes.String:
                    return "string";
                case ValueTypes.Boolean:
                    return "boolean";
                case ValueTypes.Char: //=UInt16
                case ValueTypes.Float:
                case ValueTypes.Double:
                case ValueTypes.Int8:
                case ValueTypes.Int16:
                case ValueTypes.Int32:
                case ValueTypes.Int64:
                case ValueTypes.UInt8:
                case ValueTypes.UInt16:
                case ValueTypes.UInt32:
                case ValueTypes.UInt64:
                    return "number";
                case ValueTypes.Object:
                case ValueTypes.Function:
                case ValueTypes.Array:
                case ValueTypes.Null:
                case ValueTypes.Property:
                    return DObjectValue.GetTypeOf();// "object";
                default:
                    Trace.Fail(new InvalidOperationException(string.Format("Cannot get typeof {0}", ValueType)));
                    break;
            }
            return Runtime.Instance.DefaultDUndefined.GetTypeOf();
        }

        public DObject GetFieldContainer()
        {
            if (DObjectValue != null)
                return DObjectValue;
            else
                return Runtime.Instance.DefaultDUndefined;

            //switch (ValueType)
            //{
            //    //case ValueTypes.String: return Prototypes.DStringPrototype;
            //    case ValueTypes.Double: return Prototypes.DNumberPrototype;
            //    case ValueTypes.Int: return Prototypes.DNumberPrototype;
            //    case ValueTypes.Boolean: return Prototypes.DBooleanPrototype;

            //    case ValueTypes.String:
            //    case ValueTypes.Object:
            //    case ValueTypes.Function:
            //    case ValueTypes.Array:
            //    case ValueTypes.Property:
            //        return DObjectValue;
            //    default:
            //        return Prototypes.DefaultDUndefined;
            //}
        }

        public char ToChar()
        {
            switch (ValueType)
            {
                case ValueTypes.Undefined: return Runtime.Instance.DefaultDUndefined.ToChar();
                case ValueTypes.String: return DObjectValue.ToChar();
                case ValueTypes.Char: return CharValue;
                //case ValueTypes.Boolean: return (BoolValue? '1':'\0');
                //case ValueTypes.Float: return FloatValue != 0.0;
                //case ValueTypes.Double: return DoubleValue != 0.0;
                //case ValueTypes.Int8: return Int8Value != 0;
                //case ValueTypes.Int16: return Int16Value != 0;
                //case ValueTypes.Int32: return Int32Value != 0;
                //case ValueTypes.Int64: return Int64Value != 0;
                //case ValueTypes.UInt8: return UInt8Value != 0;
                //case ValueTypes.UInt16: return UInt16Value != 0;
                //case ValueTypes.UInt32: return UInt32Value != 0;
                //case ValueTypes.UInt64: return UInt64Value != 0;
                case ValueTypes.Object:
                case ValueTypes.Function:
                case ValueTypes.Array:
                    return DObjectValue.ToChar();
                case ValueTypes.Null: return Runtime.Instance.DefaultDNull.ToChar();
                //case ValueTypes.Property: return DObjectValue.ToBoolean();
            }
            Trace.Fail(new InvalidOperationException(string.Format("Cannot convert {0} to Char (UInt16)", ValueType)));
            return Runtime.Instance.DefaultDUndefined.ToChar();
        }

        public bool ToBoolean()
        {
            switch (ValueType)
            {
                case ValueTypes.Undefined: return Runtime.Instance.DefaultDUndefined.ToBoolean();
                case ValueTypes.String: return DObjectValue.ToBoolean();
                case ValueTypes.Char: return CharValue != '\0';
                case ValueTypes.Boolean: return BooleanValue;
                case ValueTypes.Float: return (FloatValue != 0.0 && !float.IsNaN(FloatValue));
                case ValueTypes.Double: return (DoubleValue != 0.0 && !double.IsNaN(DoubleValue));
                case ValueTypes.Int8: return Int8Value != 0;
                case ValueTypes.Int16: return Int16Value != 0;
                case ValueTypes.Int32: return Int32Value != 0;
                case ValueTypes.Int64: return Int64Value != 0;
                case ValueTypes.UInt8: return UInt8Value != 0;
                case ValueTypes.UInt16: return UInt16Value != 0;
                case ValueTypes.UInt32: return UInt32Value != 0;
                case ValueTypes.UInt64: return UInt64Value != 0;
                case ValueTypes.Object:
                case ValueTypes.Function:
                case ValueTypes.Array:
                    Debug.Assert(DObjectValue != null, "Invalid situation! DObject cannot be null here!");
                    return true;
                case ValueTypes.Null: return Runtime.Instance.DefaultDNull.ToBoolean();
                //case ValueTypes.Property: return DObjectValue.ToBoolean();
            }
            Trace.Fail(new InvalidOperationException(string.Format("Cannot convert {0} to Boolean", ValueType)));
            return Runtime.Instance.DefaultDUndefined.ToBoolean();
        }


        public float ToFloat()
        {
            switch (ValueType)
            {
                case ValueTypes.Undefined: return Runtime.Instance.DefaultDUndefined.ToFloat();
                case ValueTypes.String: return DObjectValue.ToFloat();
                case ValueTypes.Char: return CharValue;
                case ValueTypes.Boolean: return (BooleanValue ? 1 : 0);
                case ValueTypes.Float: return FloatValue;
                case ValueTypes.Double: return (float)DoubleValue;
                case ValueTypes.Int8: return Int8Value;
                case ValueTypes.Int16: return Int16Value;
                case ValueTypes.Int32: return Int32Value;
                case ValueTypes.Int64: return Int64Value;
                case ValueTypes.UInt8: return UInt8Value;
                case ValueTypes.UInt16: return UInt16Value;
                case ValueTypes.UInt32: return UInt32Value;
                case ValueTypes.UInt64: return UInt64Value;
                case ValueTypes.Object:
                case ValueTypes.Function:
                case ValueTypes.Array:
                    return DObjectValue.ToFloat();
                case ValueTypes.Null: return Runtime.Instance.DefaultDNull.ToFloat();
            }
            Trace.Fail(new InvalidOperationException(string.Format("Cannot convert {0} to Float", ValueType)));
            return Runtime.Instance.DefaultDUndefined.ToFloat();
        }

        public double ToDouble()
        {
            switch (ValueType)
            {
                case ValueTypes.Undefined: return Runtime.Instance.DefaultDUndefined.ToDouble();
                case ValueTypes.String: return DObjectValue.ToDouble();
                case ValueTypes.Char: return CharValue;
                case ValueTypes.Boolean: return (BooleanValue ? 1 : 0);
                case ValueTypes.Float: return FloatValue;
                case ValueTypes.Double: return DoubleValue;
                case ValueTypes.Int8: return Int8Value;
                case ValueTypes.Int16: return Int16Value;
                case ValueTypes.Int32: return Int32Value;
                case ValueTypes.Int64: return Int64Value;
                case ValueTypes.UInt8: return UInt8Value;
                case ValueTypes.UInt16: return UInt16Value;
                case ValueTypes.UInt32: return UInt32Value;
                case ValueTypes.UInt64: return UInt64Value;
                case ValueTypes.Object:
                case ValueTypes.Function:
                case ValueTypes.Array:
                    return DObjectValue.ToDouble();
                case ValueTypes.Null: return Runtime.Instance.DefaultDNull.ToDouble();
            }
            Trace.Fail(new InvalidOperationException(string.Format("Cannot convert {0} to Double", ValueType)));
            return Runtime.Instance.DefaultDUndefined.ToDouble();
        }

        public sbyte ToInt8()
        {
            switch (ValueType)
            {
                case ValueTypes.Undefined: return Runtime.Instance.DefaultDUndefined.ToInt8();
                case ValueTypes.String: return DObjectValue.ToInt8();
                case ValueTypes.Char: return (sbyte)CharValue;
                case ValueTypes.Boolean: return (BooleanValue ? (sbyte)1 : (sbyte)0);
                case ValueTypes.Float: return (sbyte)(byte)FloatValue;
                case ValueTypes.Double: return (sbyte)(byte)DoubleValue;
                case ValueTypes.Int8: return Int8Value;
                case ValueTypes.Int16: return (sbyte)Int16Value;
                case ValueTypes.Int32: return (sbyte)Int32Value;
                case ValueTypes.Int64: return (sbyte)Int64Value;
                case ValueTypes.UInt8: return (sbyte)UInt8Value;
                case ValueTypes.UInt16: return (sbyte)UInt16Value;
                case ValueTypes.UInt32: return (sbyte)UInt32Value;
                case ValueTypes.UInt64: return (sbyte)UInt64Value;
                case ValueTypes.Object:
                case ValueTypes.Function:
                case ValueTypes.Array:
                    return DObjectValue.ToInt8();
                case ValueTypes.Null: return Runtime.Instance.DefaultDNull.ToInt8();
                //case ValueTypes.Property:
                //default:
            }
            Trace.Fail(new InvalidOperationException(string.Format("Cannot convert {0} to Int8", ValueType)));
            return Runtime.Instance.DefaultDUndefined.ToInt8();
        }
        public short ToInt16()
        {
            switch (ValueType)
            {
                case ValueTypes.Undefined: return Runtime.Instance.DefaultDUndefined.ToInt16();
                case ValueTypes.String: return DObjectValue.ToInt16();
                case ValueTypes.Char: return (short)CharValue;
                case ValueTypes.Boolean: return (BooleanValue ? (short)1 : (short)0);
                case ValueTypes.Float: return (short)(ushort)FloatValue;
                case ValueTypes.Double: return (short)(ushort)DoubleValue;
                case ValueTypes.Int8: return (short)Int8Value;
                case ValueTypes.Int16: return (short)Int16Value;
                case ValueTypes.Int32: return (short)Int32Value;
                case ValueTypes.Int64: return (short)Int64Value;
                case ValueTypes.UInt8: return UInt8Value;
                case ValueTypes.UInt16: return (short)UInt16Value;
                case ValueTypes.UInt32: return (short)UInt32Value;
                case ValueTypes.UInt64: return (short)UInt64Value;
                case ValueTypes.Object:
                case ValueTypes.Function:
                case ValueTypes.Array:
                    return DObjectValue.ToInt16();
                case ValueTypes.Null: return Runtime.Instance.DefaultDNull.ToInt16();
                //case ValueTypes.Property:
                //default:
            }
            Trace.Fail(new InvalidOperationException(string.Format("Cannot convert {0} to Int16", ValueType)));
            return Runtime.Instance.DefaultDUndefined.ToInt16();
        }

        public int ToInt32()
        {
            switch (ValueType)
            {
                case ValueTypes.Undefined: return Runtime.Instance.DefaultDUndefined.ToInt32();
                case ValueTypes.String: return DObjectValue.ToInt32();
                case ValueTypes.Char: return CharValue;
                case ValueTypes.Boolean: return (BooleanValue ? 1 : 0);
                case ValueTypes.Float: return (int)(uint)FloatValue;
                case ValueTypes.Double: return (int)(uint)DoubleValue;//if we don't convert to uint first, we might get the boundary numbers (e.g. 0xffffffff) wrong!
                case ValueTypes.Int8: return Int8Value;
                case ValueTypes.Int16: return Int16Value;
                case ValueTypes.Int32: return Int32Value;
                case ValueTypes.Int64: return (int)Int64Value;
                case ValueTypes.UInt8: return UInt8Value;
                case ValueTypes.UInt16: return UInt16Value;
                case ValueTypes.UInt32: return (int)UInt32Value;
                case ValueTypes.UInt64: return (int)UInt64Value;
                case ValueTypes.Object:
                case ValueTypes.Function:
                case ValueTypes.Array:
                    return DObjectValue.ToInt32();
                case ValueTypes.Null: return Runtime.Instance.DefaultDNull.ToInt32();
                //case ValueTypes.Property:
                //default:
            }
            Trace.Fail(new InvalidOperationException(string.Format("Cannot convert {0} to Int32", ValueType)));
            return Runtime.Instance.DefaultDUndefined.ToInt32();
        }

        public long ToInt64()
        {
            switch (ValueType)
            {
                case ValueTypes.Undefined: return Runtime.Instance.DefaultDUndefined.ToInt64();
                case ValueTypes.String: return DObjectValue.ToInt64();
                case ValueTypes.Char: return CharValue;
                case ValueTypes.Boolean: return (BooleanValue ? 1L : 0L);
                case ValueTypes.Float: return (long)(ulong)FloatValue;
                case ValueTypes.Double: return (long)(ulong)DoubleValue;
                case ValueTypes.Int8: return Int8Value;
                case ValueTypes.Int16: return Int16Value;
                case ValueTypes.Int32: return Int32Value;
                case ValueTypes.Int64: return Int64Value;
                case ValueTypes.UInt8: return UInt8Value;
                case ValueTypes.UInt16: return UInt16Value;
                case ValueTypes.UInt32: return UInt32Value;
                case ValueTypes.UInt64: return (long)UInt64Value;
                case ValueTypes.Object:
                case ValueTypes.Function:
                case ValueTypes.Array:
                    return DObjectValue.ToInt64();
                case ValueTypes.Null: return Runtime.Instance.DefaultDNull.ToInt64();
                //case ValueTypes.Property:
                //default:
            }
            Trace.Fail(new InvalidOperationException(string.Format("Cannot convert {0} to Int64", ValueType)));
            return Runtime.Instance.DefaultDUndefined.ToInt64();
        }
        public byte ToUInt8()
        {
            switch (ValueType)
            {
                case ValueTypes.Undefined: return Runtime.Instance.DefaultDUndefined.ToUInt8();
                case ValueTypes.String: return DObjectValue.ToUInt8();
                case ValueTypes.Char: return (byte)CharValue;
                case ValueTypes.Boolean: return (BooleanValue ? (byte)1 : (byte)0);
                case ValueTypes.Float: return (byte)FloatValue;
                case ValueTypes.Double: return (byte)DoubleValue;
                case ValueTypes.Int8: return (byte)Int8Value;
                case ValueTypes.Int16: return (byte)Int16Value;
                case ValueTypes.Int32: return (byte)Int32Value;
                case ValueTypes.Int64: return (byte)Int64Value;
                case ValueTypes.UInt8: return UInt8Value;
                case ValueTypes.UInt16: return (byte)UInt16Value;
                case ValueTypes.UInt32: return (byte)UInt32Value;
                case ValueTypes.UInt64: return (byte)UInt64Value;
                case ValueTypes.Object:
                case ValueTypes.Function:
                case ValueTypes.Array:
                    return DObjectValue.ToUInt8();
                case ValueTypes.Null: return Runtime.Instance.DefaultDNull.ToUInt8();
                //case ValueTypes.Property:
                //default:
            }
            Trace.Fail(new InvalidOperationException(string.Format("Cannot convert {0} to UInt8", ValueType)));
            return Runtime.Instance.DefaultDUndefined.ToUInt8();
        }
        public ushort ToUInt16()
        {
            switch (ValueType)
            {
                case ValueTypes.Undefined: return Runtime.Instance.DefaultDUndefined.ToUInt16();
                case ValueTypes.String: return DObjectValue.ToUInt16();
                case ValueTypes.Char: return CharValue;
                case ValueTypes.Boolean: return (BooleanValue ? (ushort)1 : (ushort)0);
                case ValueTypes.Float: return (ushort)FloatValue;
                case ValueTypes.Double: return (ushort)DoubleValue;
                case ValueTypes.Int8: return (ushort)Int8Value;
                case ValueTypes.Int16: return (ushort)Int16Value;
                case ValueTypes.Int32: return (ushort)Int32Value;
                case ValueTypes.Int64: return (ushort)Int64Value;
                case ValueTypes.UInt8: return UInt8Value;
                case ValueTypes.UInt16: return UInt16Value;
                case ValueTypes.UInt32: return (ushort)UInt32Value;
                case ValueTypes.UInt64: return (ushort)UInt64Value;
                case ValueTypes.Object:
                case ValueTypes.Function:
                case ValueTypes.Array:
                    return DObjectValue.ToUInt16();
                case ValueTypes.Null: return Runtime.Instance.DefaultDNull.ToUInt16();
                //case ValueTypes.Property:
                //default:
            }
            Trace.Fail(new InvalidOperationException(string.Format("Cannot convert {0} to UInt16", ValueType)));
            return Runtime.Instance.DefaultDUndefined.ToUInt16();
        }
        public uint ToUInt32()
        {
            switch (ValueType)
            {
                case ValueTypes.Undefined: return Runtime.Instance.DefaultDUndefined.ToUInt32();
                case ValueTypes.String: return DObjectValue.ToUInt32();
                case ValueTypes.Char: return CharValue;
                case ValueTypes.Boolean: return (BooleanValue ? 1U : 0U);
                case ValueTypes.Float: return (uint)FloatValue;
                case ValueTypes.Double: return (uint)DoubleValue;
                case ValueTypes.Int8: return (uint)Int8Value;
                case ValueTypes.Int16: return (uint)Int16Value;
                case ValueTypes.Int32: return (uint)Int32Value;
                case ValueTypes.Int64: return (uint)Int64Value;
                case ValueTypes.UInt8: return UInt8Value;
                case ValueTypes.UInt16: return UInt16Value;
                case ValueTypes.UInt32: return UInt32Value;
                case ValueTypes.UInt64: return (uint)UInt64Value;
                case ValueTypes.Object:
                case ValueTypes.Function:
                case ValueTypes.Array:
                    return DObjectValue.ToUInt32();
                case ValueTypes.Null: return Runtime.Instance.DefaultDNull.ToUInt32();
                //case ValueTypes.Property:
                //default:
            }
            Trace.Fail(new InvalidOperationException(string.Format("Cannot convert {0} to UInt32", ValueType)));
            return Runtime.Instance.DefaultDUndefined.ToUInt32();
        }

        public ulong ToUInt64()
        {
            switch (ValueType)
            {
                case ValueTypes.Undefined: return Runtime.Instance.DefaultDUndefined.ToUInt64();
                case ValueTypes.String: return DObjectValue.ToUInt64();
                case ValueTypes.Char: return CharValue;
                case ValueTypes.Boolean: return (BooleanValue ? 1UL : 0UL);
                case ValueTypes.Float: return (ulong)FloatValue;
                case ValueTypes.Double: return (ulong)DoubleValue;
                case ValueTypes.Int8: return (ulong)Int8Value;
                case ValueTypes.Int16: return (ulong)Int16Value;
                case ValueTypes.Int32: return (ulong)Int32Value;
                case ValueTypes.Int64: return (ulong)Int64Value;
                case ValueTypes.UInt8: return UInt8Value;
                case ValueTypes.UInt16: return UInt16Value;
                case ValueTypes.UInt32: return UInt32Value;
                case ValueTypes.UInt64: return UInt64Value;
                case ValueTypes.Object:
                case ValueTypes.Function:
                case ValueTypes.Array:
                    return DObjectValue.ToUInt64();
                case ValueTypes.Null: return Runtime.Instance.DefaultDNull.ToUInt64();
                //case ValueTypes.Property:
                //default:
            }
            Trace.Fail(new InvalidOperationException(string.Format("Cannot convert {0} to UInt64", ValueType)));
            return Runtime.Instance.DefaultDUndefined.ToUInt64();
        }

        public DObject ToDObject()
        {
            switch (ValueType)
            {
                case ValueTypes.String:
                case ValueTypes.Object:
                case ValueTypes.Function:
                case ValueTypes.Array:
                case ValueTypes.Property:
                    return DObjectValue;
            }
            Trace.Fail(new InvalidOperationException(string.Format("Cannot convert {0} to object", ValueType)));
            return Runtime.Instance.DefaultDUndefined;
        }
        
        public DFunction ToDFunction()
        {
            if (DObjectValue != null)
                return DObjectValue.ToDFunction();
            else
            {
                Trace.Fail(new InvalidOperationException(string.Format("Cannot convert {0} to function", ValueType)));
                return Runtime.Instance.DefaultDUndefined.ToDFunction();
            }
        }

        public DArray ToDArray()
        {
            switch (ValueType)
            {
                case ValueTypes.Array:
                case ValueTypes.Object:
                case ValueTypes.Property:
                    return DObjectValue.ToDArray();
            }
            Trace.Fail(new InvalidOperationException(string.Format("Cannot convert {0} to array", ValueType)));
            return Runtime.Instance.DefaultDUndefined.ToDArray();
        }
#endif



    public void Set(DUndefined v) { SetUndefined(); }
    public void SetUndefined()
    {
      _ValueType = ValueTypes.Undefined;
      ObjectValue = Runtime.Instance.DefaultDUndefined;
    }
    public void Set(DNull v) { SetNull(); }
    public void SetNull()
    {
      _ValueType = ValueTypes.Null;
      ObjectValue = Runtime.Instance.DefaultDNull;
    }
    public void Set(string v)
    {
      ObjectValue = v;
      _ValueType = ValueTypes.String;
    }
    public void SetNullable(string v)
    {
      // XXX: Presumably the separation of Set(string) and CheckNull(string) existed because
      // sometimes we know for sure that the string is not null and we can elide the check.
      // If that's not true, this method should be merged into Set(string).
      if (v == null) SetNull();
      else Set(v);
    }
    public void Set(char v)
    {
      CharValue = v;
      _ValueType = ValueTypes.Char;
      //DObjectValue = Runtime.Instance.DNumberPrototype;
    }
    public void Set(bool v)
    {
      BooleanValue = v;
      _ValueType = ValueTypes.Boolean;
      //DObjectValue = Runtime.Instance.DBooleanPrototype;
    }
    public void Set(float v)
    {
      FloatValue = v;
      _ValueType = ValueTypes.Float;
      //DObjectValue = Runtime.Instance.DNumberPrototype;
    }
    public void Set(double v)
    {
      DoubleValue = v;
      _ValueType = ValueTypes.Double;
      //DObjectValue = Runtime.Instance.DNumberPrototype;
    }
    public void Set(sbyte v)
    {
      Int8Value = v;
      _ValueType = ValueTypes.Int8;
      //DObjectValue = Runtime.Instance.DNumberPrototype;
    }
    public void Set(short v)
    {
      Int16Value = v;
      _ValueType = ValueTypes.Int16;
      //DObjectValue = Runtime.Instance.DNumberPrototype;
    }
    public void Set(int v)
    {
      Int32Value = v;
      _ValueType = ValueTypes.Int32;
      //DObjectValue = Runtime.Instance.DNumberPrototype;
    }
    public void Set(long v)
    {
      Int64Value = v;
      _ValueType = ValueTypes.Int64;
      //DObjectValue = Runtime.Instance.DNumberPrototype;
    }
    public void Set(byte v)
    {
      UInt8Value = v;
      _ValueType = ValueTypes.UInt8;
      //DObjectValue = Runtime.Instance.DNumberPrototype;
    }
    public void Set(ushort v)
    {
      UInt16Value = v;
      _ValueType = ValueTypes.UInt16;
      //DObjectValue = Runtime.Instance.DNumberPrototype;
    }
    public void Set(uint v)
    {
      UInt32Value = v;
      _ValueType = ValueTypes.UInt32;
      //DObjectValue = Runtime.Instance.DNumberPrototype;
    }
    public void Set(ulong v)
    {
      UInt64Value = v;
      _ValueType = ValueTypes.UInt64;
      //DObjectValue = Runtime.Instance.DNumberPrototype;
    }
    public void Set(DObject v)
    {
      _ValueType = v.ValueType;
      ObjectValue = v;
    }
    public void SetNullable(DObject v)
    {
      // XXX: Presumably the separation of Set(DObject) and CheckNull(DObject) existed because
      // sometimes we know for sure that the DObject is not null and we can elide the check.
      // If that's not true, this method should be merged into Set(DObject).
      if (v == null) SetNull();
      else Set(v);
    }
    public void Set(DFunction v) { Set((DObject)v); }
    public void Set(DArray v) { Set((DObject)v); }
    public void Set(ref DValue v)
    {
      this = v;
    }
    //public void Set(DValue v) { this = v; }
    public void Set(object v)
    {
      _ValueType = ValueTypes.Any;
      ObjectValue = v;
    }

    public static DValue Create(bool v) { var dv = new DValue(); dv.Set(v); return dv; }
    public static DValue Create(string v) { var dv = new DValue(); dv.Set(v); return dv; }
    public static DValue Create(char v) { var dv = new DValue(); dv.Set(v); return dv; }
    public static DValue Create(double v) { var dv = new DValue(); dv.Set(v); return dv; }
    public static DValue Create(int v) { var dv = new DValue(); dv.Set(v); return dv; }
    public static DValue Create(DObject v) { var dv = new DValue(); dv.Set(v); return dv; }


    /// <summary>
    /// Never call this function, this is here to make debugging easier!
    /// </summary>
    public override string ToString() 
    {
      var sb = new System.Text.StringBuilder();
      sb.Append(ValueType);
      sb.Append(" ");
      switch (ValueType)
      {
        case mdr.ValueTypes.Undefined: sb.Append(Runtime.Instance.DefaultDUndefined); break;
        case mdr.ValueTypes.Null: sb.Append(Runtime.Instance.DefaultDNull); break;
        case mdr.ValueTypes.Boolean: sb.Append(AsBoolean()); break;
        case mdr.ValueTypes.String: sb.Append(AsString()); break;
        case mdr.ValueTypes.Char: sb.Append(AsChar()); break;
        case mdr.ValueTypes.Float: sb.Append(AsFloat()); break;
        case mdr.ValueTypes.Double: sb.Append(AsDouble()); break;
        case mdr.ValueTypes.Int8: sb.Append(AsInt8()); break;
        case mdr.ValueTypes.Int16: sb.Append(AsInt16()); break;
        case mdr.ValueTypes.Int32: sb.Append(AsInt32()); break;
        case mdr.ValueTypes.Int64: sb.Append(AsInt64()); break;
        case mdr.ValueTypes.UInt8: sb.Append(AsUInt8()); break;
        case mdr.ValueTypes.UInt16: sb.Append(AsUInt16()); break;
        case mdr.ValueTypes.UInt32: sb.Append(AsUInt32()); break;
        case mdr.ValueTypes.UInt64: sb.Append(AsUInt64()); break;
        case mdr.ValueTypes.Object: sb.Append(AsDObject()); break;
        case mdr.ValueTypes.Function: sb.Append(AsDFunction()); break;
        case mdr.ValueTypes.Array: sb.Append(AsDArray()); break;
        default:
          throw new System.InvalidOperationException(string.Format("Invalid operand type {0}", ValueType));
      }
      //return string.Format("{0} {1} {2}", ValueType, ObjectValue, DoubleValue); 
      return sb.ToString();
    }

  }
#else
  public struct DValue
  {
    const int ValueOffset = 5;
    int InlineValue;
    object ObjectValue;

    public ValueTypes ValueType { get { return (ValueTypes)(InlineValue & 0x1F); } }

    public void Set(DUndefined v) { SetUndefined(); }
    public void SetUndefined()
    {
      InlineValue = (int)ValueTypes.Undefined;
      ObjectValue = Runtime.Instance.DefaultDUndefined;
    }
    public void Set(DNull v) { SetNull(); }
    public void SetNull()
    {
      InlineValue = (int)ValueTypes.Null;
      ObjectValue = Runtime.Instance.DefaultDNull;
    }

    public string AsString()
    {
      Debug.Assert(ValueType == ValueTypes.String, "cannot convert from {0} to String", ValueType);
      return ObjectValue as string;
    }
    public void Set(string v)
    {
      InlineValue = (int)ValueTypes.String;
      ObjectValue = v;
    }
    public void SetNullable(string v)
    {
      // XXX: Presumably the separation of Set(string) and CheckNull(string) existed because
      // sometimes we know for sure that the string is not null and we can elide the check.
      // If that's not true, this method should be merged into Set(string).
      if (v == null) SetNull();
      else Set(v);
    }


    const int TrueValue = (1 << ValueOffset) | (int)ValueTypes.Boolean;
    const int FalseValue = (0 << ValueOffset) | (int)ValueTypes.Boolean;
    public bool AsBoolean()
    {
      Debug.Assert(ValueType == ValueTypes.Boolean, "cannot convert from {0} to Boolean", ValueType);
      return InlineValue == TrueValue;
    }
    public void Set(bool v)
    {
      InlineValue = v ? TrueValue : FalseValue;
    }


    public char AsChar()
    {
      Debug.Assert(ValueType == ValueTypes.Char, "cannot convert from {0} to Char", ValueType);
      return (char)(InlineValue >> ValueOffset);
    }
    public void Set(char v)
    {
      InlineValue = ((int)v << ValueOffset) | (int)ValueTypes.Char;
    }

    public float AsFloat()
    {
      Debug.Assert(ValueType == ValueTypes.Float, "cannot convert from {0} to Float", ValueType);
      return (float)ObjectValue;
    }
    public void Set(float v)
    {
      InlineValue = (int)ValueTypes.Float;
      ObjectValue = v;
    }

    public double AsDouble()
    {
      Debug.Assert(ValueType == ValueTypes.Double, "cannot convert from {0} to Double", ValueType);
      return (double)ObjectValue;
    }
    public void Set(double v)
    {
      InlineValue = (int)ValueTypes.Double;
      ObjectValue = v;
    }

    public sbyte AsInt8()
    {
      Debug.Assert(ValueType == ValueTypes.Int8, "cannot convert from {0} to Int8", ValueType);
      return (sbyte)(InlineValue >> ValueOffset);
    }
    public void Set(sbyte v)
    {
      InlineValue = ((int)v << ValueOffset) | (int)ValueTypes.Int8;
    }

    public short AsInt16()
    {
      Debug.Assert(ValueType == ValueTypes.Int16, "cannot convert from {0} to Int16", ValueType);
      return (short)(InlineValue >> ValueOffset);
    }
    public void Set(short v)
    {
      InlineValue = ((int)v << ValueOffset) | (int)ValueTypes.Int16;
    }

    public int AsInt32()
    {
      Debug.Assert(ValueType == ValueTypes.Int32, "cannot convert from {0} to Int32", ValueType);
      return (ObjectValue == null)
       ? (int)(InlineValue >> ValueOffset)
       : (int)ObjectValue;
    }
    public void Set(int v)
    {
      //If last 6 bits are alll 0 or 1, it is safe to store inline.
      var last6bits = v & 0xFC000000;
      if (last6bits == 0 || last6bits == 0xFC000000)
      {
        InlineValue = ((int)v << ValueOffset) | (int)ValueTypes.Int32;
        ObjectValue = null;
      }
      else
      {
        InlineValue = (int)ValueTypes.Int32;
        ObjectValue = v;
      }
    }

    public long AsInt64()
    {
      Debug.Assert(ValueType == ValueTypes.Int64, "cannot convert from {0} to Int64", ValueType);
      return (long)ObjectValue;
    }
    public void Set(long v)
    {
      InlineValue = (int)ValueTypes.Int64;
      ObjectValue = v;
    }

    public byte AsUInt8()
    {
      Debug.Assert(ValueType == ValueTypes.UInt8, "cannot convert from {0} to UInt8", ValueType);
      return (byte)(InlineValue >> ValueOffset);
    }
    public void Set(byte v)
    {
      InlineValue = ((int)v << ValueOffset) | (int)ValueTypes.UInt8;
    }

    public ushort AsUInt16()
    {
      Debug.Assert(ValueType == ValueTypes.UInt16, "cannot convert from {0} to UInt16", ValueType);
      return (ushort)(InlineValue >> ValueOffset);
    }
    public void Set(ushort v)
    {
      InlineValue = ((int)v << ValueOffset) | (int)ValueTypes.UInt16;
    }

    public uint AsUInt32()
    {
      Debug.Assert(ValueType == ValueTypes.UInt32, "cannot convert from {0} to UInt32", ValueType);
      return (ObjectValue == null)
        ? (uint)InlineValue >> ValueOffset
        : (uint)ObjectValue;
    }
    public void Set(uint v)
    {
      //If last 5 bits are alll 0, it is safe to store inline.
      var last5bits = v & 0xF8000000;
      if (last5bits == 0)
      {
        InlineValue = ((int)v << ValueOffset) | (int)ValueTypes.UInt32;
        ObjectValue = null;
      }
      else
      {
        InlineValue = (int)ValueTypes.UInt32;
        ObjectValue = v;
      }
    }

    public ulong AsUInt64()
    {
      Debug.Assert(ValueType == ValueTypes.UInt64, "cannot convert from {0} to UInt64", ValueType);
      return (ulong)ObjectValue;
    }
    public void Set(ulong v)
    {
      InlineValue = (int)ValueTypes.UInt64;
      ObjectValue = v;
    }

    public DObject AsDObject()
    {
      Debug.Assert(ValueTypesHelper.IsObject(ValueType), "cannot convert from {0} to DObject", ValueType);
      return ObjectValue as DObject;
    }
    public void Set(DObject v)
    {
      InlineValue = (int)v.ValueType;
      ObjectValue = v;
    }
    public void SetNullable(DObject v)
    {
      // XXX: Presumably the separation of Set(DObject) and CheckNull(DObject) existed because
      // sometimes we know for sure that the DObject is not null and we can elide the check.
      // If that's not true, this method should be merged into Set(DObject).
      if (v == null) SetNull();
      else Set(v);
    }

    public DFunction AsDFunction() { Debug.Assert(ValueType == ValueTypes.Function, "cannot convert from {0} to DFunction", ValueType); return ObjectValue as DFunction; }
    public void Set(DFunction v) { Set((DObject)v); }

    public DArray AsDArray() { Debug.Assert(ValueType == ValueTypes.Array, "cannot convert from {0} to DArray", ValueType); return ObjectValue as DArray; }
    public void Set(DArray v) { Set((DObject)v); }

    public DProperty AsDProperty() { Debug.Assert(ValueType == ValueTypes.Property, "cannot convert from {0} to DProperty", ValueType); return ObjectValue as DProperty; }

    public object AsObject() { Debug.Assert(ValueType == ValueTypes.Any, "cannot from {0} to object", ValueType); return ObjectValue; }
    public void Set(object v)
    {
      InlineValue = (int)ValueTypes.Any;
      ObjectValue = v;
    }

    public void Set(ref DValue v)
    {
      this = v;
    }
    //public void Set(DValue v) { this = v; }

    public static DValue Create(bool v) { var dv = new DValue(); dv.Set(v); return dv; }
    public static DValue Create(string v) { var dv = new DValue(); dv.Set(v); return dv; }
    public static DValue Create(char v) { var dv = new DValue(); dv.Set(v); return dv; }
    public static DValue Create(double v) { var dv = new DValue(); dv.Set(v); return dv; }
    public static DValue Create(int v) { var dv = new DValue(); dv.Set(v); return dv; }
    public static DValue Create(DObject v) { var dv = new DValue(); dv.Set(v); return dv; }
  }
#endif
  /// <summary>
  /// This type is only used for the code generation and debugging since C# does not support ByRef variables.
  /// </summary>
  public struct DValueRef
  {
    public DValue[] Array;
    public int Index;
    public DValueRef(DValue[] array, int index)
    {
      Array = array;
      Index = index;
    }
  }

}

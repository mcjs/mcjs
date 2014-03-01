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
using mdr;

namespace mjr.Operations.Binary
{
  /// <summary>
  /// ECMA-262, 11.9.1
  /// </summary>
  public static partial class Equal
  {
    /// <summary>
    /// Based on ECMA-262, 11.9.3: The Abstract Equality Comparison Algorithm
    /// </summary>
    //public static bool Run(ref mdr.DValue i0, ref mdr.DValue i1)
    //{
    //    mdr.ValueTypes i0Type = i0.ValueType;
    //    mdr.ValueTypes i1Type = i1.ValueType;
    //    if (i0Type == i1Type)
    //    {
    //        switch (i0Type)
    //        {
    //            case mdr.ValueTypes.Undefined:
    //                return true;
    //            case mdr.ValueTypes.Null:
    //                return true;
    //            case mdr.ValueTypes.Int32:
    //                if (i0.IntValue == i1.IntValue)
    //                    return true;
    //                return false;
    //            case mdr.ValueTypes.Double:
    //                if (Double.IsNaN(i0.DoubleValue) || Double.IsNaN(i1.DoubleValue))
    //                    return false;
    //                if (i0.DoubleValue == i1.DoubleValue)
    //                    return true;
    //                return false;
    //            case mdr.ValueTypes.String:
    //                return (String.Equals(i0.ToString(), i1.ToString()));
    //            case mdr.ValueTypes.Boolean:
    //                return (i0.BooleanValue == i1.BooleanValue);
    //        }
    //        if (ValueTypesHelper.IsObject(i0Type))
    //            return (System.Object.ReferenceEquals(i0.DObjectValue, i1.DObjectValue));

    //        return false;
    //    }

    //    // Types of the args are not the same
    //    switch (i0Type)
    //    {
    //        case mdr.ValueTypes.Null:
    //            if (i1Type == mdr.ValueTypes.Undefined)
    //                return true;
    //            break;
    //        case mdr.ValueTypes.UInt32:
    //            if (i0.IntValue == i1.IntValue)
    //                return true;
    //            break;
    //        case mdr.ValueTypes.Undefined:
    //            if (i1Type == mdr.ValueTypes.Null)
    //                return true;
    //            break;
    //        case mdr.ValueTypes.Int32:
    //            if (i1Type == mdr.ValueTypes.String)
    //                return ((double)i0.IntValue == Convert.ToDouble.Run(i1.ToString()));

    //            // Since we have different types for int and double, as oppose to the standard that has one type for numbers,
    //            // we must check for number equivalency here
    //            if (i1Type == mdr.ValueTypes.Double)
    //                return ((double)i0.IntValue == i1.DoubleValue);
    //            if (ValueTypesHelper.IsObject(i1Type))
    //            {
    //                mdr.DValue i1PrimitiveValue;
    //                Convert.ToPrimitive.Run(ref i1, out i1PrimitiveValue);
    //                return Run(i0.IntValue, ref i1PrimitiveValue);
    //            }
    //            break;
    //        case mdr.ValueTypes.Double:
    //            if (i1Type == mdr.ValueTypes.String)
    //                return (i0.DoubleValue == Convert.ToDouble.Run(i1.ToString()));

    //            // The same as the above case
    //            if (i1Type == mdr.ValueTypes.Int32)
    //                return (i0.DoubleValue == (double)i1.IntValue);
    //            if (ValueTypesHelper.IsObject(i1Type))
    //            {
    //                mdr.DValue i1PrimitiveValue;
    //                Convert.ToPrimitive.Run(ref i1, out i1PrimitiveValue);
    //                return Run(i0.DoubleValue, ref i1PrimitiveValue);
    //            }
    //            break;
    //        case mdr.ValueTypes.String:
    //            if (i1Type == mdr.ValueTypes.Int32 || i1Type == mdr.ValueTypes.Double)
    //                return (i0.ToDouble() == Convert.ToDouble.Run(i1.ToString()));
    //            if (ValueTypesHelper.IsObject(i1Type))
    //            {
    //                mdr.DValue i1PrimitiveValue;
    //                Convert.ToPrimitive.Run(ref i1, out i1PrimitiveValue);
    //                return Run(i0.ToString(), ref i1PrimitiveValue);
    //            }
    //            break;
    //        case mdr.ValueTypes.Boolean:
    //            return Run(i0.BooleanValue ? 1 : 0, ref i1);
    //    }

    //    if (i1Type == mdr.ValueTypes.Boolean)
    //        return Run(i1.BooleanValue ? 1 : 0, ref i0);

    //    // this is item 9 in the standard
    //    if (ValueTypesHelper.IsObject(i0Type))
    //    {
    //        switch (i1Type)
    //        {
    //            case mdr.ValueTypes.Int32:
    //                {
    //                    mdr.DValue i0PrimitiveValue;
    //                    Convert.ToPrimitive.Run(ref i0, out i0PrimitiveValue);
    //                    return Run(i1.IntValue, ref i0PrimitiveValue);
    //                }
    //            case mdr.ValueTypes.Double:
    //                {
    //                    mdr.DValue i0PrimitiveValue;
    //                    Convert.ToPrimitive.Run(ref i0, out i0PrimitiveValue);
    //                    return Run(i1.DoubleValue, ref i0PrimitiveValue);
    //                }
    //            case mdr.ValueTypes.String:
    //                {
    //                    mdr.DValue i0PrimitiveValue;
    //                    Convert.ToPrimitive.Run(ref i0, out i0PrimitiveValue);
    //                    return Run(i1.ToString(), ref i0PrimitiveValue);
    //                }
    //        }
    //    }
    //    return false;
    //}

    ////Sometimes objects can be DValue.Undefined. In that case, we will have only one instance, so just compare instances.
    ////public static bool Run(/*const*/ ref mdr.DValue i0, /*const*/ ref mdr.DValue i1)
    ////{
    ////    switch (i0.ValueType)
    ////    {
    ////        case mdr.ValueTypes.String: return Run(i0.ToString(), ref i1);
    ////        case mdr.ValueTypes.Double: return Run(i0.ToDouble(), ref i1);
    ////        case mdr.ValueTypes.Int: return Run(i0.ToInt(), ref i1);
    ////        case mdr.ValueTypes.Boolean: return Run(i0.ToInt(), ref i1);
    ////        case mdr.ValueTypes.Undefined:
    ////        case mdr.ValueTypes.Null:
    ////            return i1.ValueType == mdr.ValueTypes.Undefined || i1.ValueType == mdr.ValueTypes.Null;
    ////        default:
    ////            //return i0 == i1;
    ////            //throw new InvalidOperationException(string.Format("Invalid operand type {0}", i0.ValueType));
    ////            //Only Objects are left
    ////            return i0.ValueType == i1.ValueType && i0.DObjectValue == i1.DObjectValue;
    ////    }
    ////}

    //public static bool Run(/*const*/ ref mdr.DValue i0, String i1)
    //{
    //    switch (i0.ValueType)
    //    {
    //        case mdr.ValueTypes.String: return i0.ToString() == i1;
    //        case mdr.ValueTypes.Double: return i0.DoubleValue == Convert.ToDouble.Run(i1);
    //        case mdr.ValueTypes.Int32: return (double)i0.IntValue == Convert.ToDouble.Run(i1);
    //        case mdr.ValueTypes.UInt32: return (double)i0.ToUInt32() == Convert.ToDouble.Run(i1);
    //        default:
    //            return false;
    //    }
    //}
    //public static bool Run(/*const*/ ref mdr.DValue i0, Double i1)
    //{
    //    switch (i0.ValueType)
    //    {
    //        case mdr.ValueTypes.Undefined:
    //        case mdr.ValueTypes.Null:
    //            return false;
    //        case mdr.ValueTypes.Int32:
    //            return (double)i0.IntValue == i1;
    //        case mdr.ValueTypes.Double:
    //            if (double.IsNaN(i0.DoubleValue))
    //                return false;
    //            return i0.DoubleValue == (double)i1;
    //        case mdr.ValueTypes.String:
    //            return Convert.ToDouble.Run(i0.ToString()) == (double)i1;
    //        default:
    //            if (ValueTypesHelper.IsObject(i0.ValueType))
    //            {
    //                mdr.DValue i0PrimitiveValue;
    //                Convert.ToPrimitive.Run(ref i0, out i0PrimitiveValue);
    //                return Run(ref i0PrimitiveValue, i1);
    //            }
    //            else
    //                return false;
    //    }
    //}
    //public static bool Run(/*const*/ ref mdr.DValue i0, Int32 i1)
    //{
    //    switch (i0.ValueType)
    //    {
    //        case mdr.ValueTypes.Undefined:
    //        case mdr.ValueTypes.Null:
    //            return false;
    //        case mdr.ValueTypes.Boolean:
    //            return (i0.BooleanValue ? 1 : 0) == i1;
    //        case mdr.ValueTypes.Int32:
    //            return i0.IntValue == i1;
    //        case mdr.ValueTypes.UInt32:
    //            return i0.IntValue == i1;
    //        case mdr.ValueTypes.Double:
    //            return i0.DoubleValue == (double)i1;
    //        case mdr.ValueTypes.String:
    //            return Convert.ToDouble.Run(i0.ToString()) == (double)i1;
    //        default:
    //            if (ValueTypesHelper.IsObject(i0.ValueType))
    //            {
    //                mdr.DValue i0PrimitiveValue;
    //                Convert.ToPrimitive.Run(ref i0, out i0PrimitiveValue);
    //                return Run(ref i0PrimitiveValue, i1);
    //            }
    //            else
    //                return false;
    //    }
    //}
    //public static bool Run(/*const*/ ref mdr.DValue return Run(i0, Convert.ToNumber.Run(i1));1)
    //{
    //    return Run(ref i0, i1 ? 1 : 0);
    //}
    //public static bool Run(/*const*/ ref mdr.DValue i0, mdr.DObject i1)
    //{
    //    switch (i0.ValueType)
    //    {
    //        case mdr.ValueTypes.Undefined:
    //        case mdr.ValueTypes.Null:
    //            return i1.ValueType == mdr.ValueTypes.Undefined || i1.ValueType == mdr.ValueTypes.Null;
    //        case mdr.ValueTypes.Double:
    //        case mdr.ValueTypes.Int32:
    //        case mdr.ValueTypes.String:
    //            {
    //                mdr.DValue i1PrimitiveValue;
    //                Convert.ToPrimitive.Run(i1, out i1PrimitiveValue);
    //                return Run(ref i1PrimitiveValue, ref i0);
    //            }

    //        default:
    //            return i0.DObjectValue == i1;
    //    }
    //}

    //public static bool Run(string i0, /*const*/ ref mdr.DValue i1) { return Run(ref i1, i0); }
    //public static bool Run(double i0, /*const*/ ref mdr.DValue i1) { return i0 == i1.ToDouble(); }
    //public static bool Run(int i0, /*const*/ ref mdr.DValue i1) { return Run(ref i1, i0); }
    //public static bool Run(bool i0, /*const*/ ref mdr.DValue i1) { return Run(ref i1, i0); }
    //public static bool Run(mdr.DObject i0, /*const*/ ref mdr.DValue i1)
    //{
    //    switch (i1.ValueType)
    //    {
    //        case mdr.ValueTypes.Undefined:
    //        case mdr.ValueTypes.Null:
    //            return i0.ValueType == mdr.ValueTypes.Undefined || i0.ValueType == mdr.ValueTypes.Null;
    //        default:
    //            return i0 == i1.DObjectValue;
    //    }
    //}

  }
}

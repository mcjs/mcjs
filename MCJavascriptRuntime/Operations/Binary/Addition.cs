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
  /// ECMA-262, 11.6.1
  /// </summary>
  public static partial class Addition
  {
    //public static void Run(/*const*/ ref mdr.DValue i0, /*const*/ ref mdr.DValue i1, ref mdr.DValue result)
    //{
    //    switch (i0.ValueType)
    //    {
    //        case mdr.ValueTypes.Undefined:
    //            if (i1.ValueType == mdr.ValueTypes.String)
    //                result.Set(mdr.Runtime.Instance.DefaultDUndefined.ToString() + i1.DObjectValue.ToString());
    //            else
    //                result.Set(double.NaN);
    //            break;
    //        case mdr.ValueTypes.String: Run(i0.DObjectValue.ToString(), ref i1, ref result); break;
    //        case mdr.ValueTypes.Char: Run(i0.CharValue.ToString(), ref i1, ref result); break;
    //        case mdr.ValueTypes.Boolean: Run(i0.ToInt32(), ref i1, ref result); break;
    //        case mdr.ValueTypes.Float: Run(i0.FloatValue, ref i1, ref result); break;
    //        case mdr.ValueTypes.Double: Run(i0.DoubleValue, ref i1, ref result); break;
    //        case mdr.ValueTypes.Int32: Run(i0.IntValue, ref i1, ref result); break;
    //        case mdr.ValueTypes.UInt32: Run(i0.UInt32Value, ref i1, ref result); break;
    //        case mdr.ValueTypes.Null:
    //            if (i1.ValueType == mdr.ValueTypes.String)
    //                result.Set(mdr.Runtime.Instance.DefaultDNull.ToString() + i1.DObjectValue.ToString());
    //            else
    //                result.Set(ref i1);
    //            break;
    //        case mdr.ValueTypes.Object:
    //        case mdr.ValueTypes.Function:
    //        case mdr.ValueTypes.Array:
    //        case mdr.ValueTypes.Property:
    //            Run(i0.DObjectValue, ref i1, ref result);
    //            break;
    //        default:
    //            throw new InvalidOperationException(string.Format("Invalid operand type {0}", i0.ValueType));
    //    }
    //}

    //public static void Run(/*const*/ ref mdr.DValue i0, string i1, ref mdr.DValue result) { result.Set(i0.ToString() + i1); }
    //public static void Run(/*const*/ ref mdr.DValue i0, double i1, ref mdr.DValue result)
    //{
    //    switch (i0.ValueType)
    //    {
    //        case mdr.ValueTypes.String:
    //            result.Set(i0.ToString() + i1.ToString());
    //            break;
    //        case mdr.ValueTypes.Undefined:
    //        case mdr.ValueTypes.Double:
    //        case mdr.ValueTypes.Int32:
    //        case mdr.ValueTypes.UInt32:
    //        case mdr.ValueTypes.Boolean:
    //        case mdr.ValueTypes.Null:
    //            result.Set(i0.ToDouble() + i1);
    //            break;
    //        case mdr.ValueTypes.Object:
    //        case mdr.ValueTypes.Function:
    //        case mdr.ValueTypes.Array:
    //        case mdr.ValueTypes.Property:
    //            result.Set(i0.DObjectValue.ToDouble() + i1);
    //            break;

    //        default:
    //            throw new InvalidOperationException(string.Format("Invalid operand type {0}", i0.ValueType));
    //    }
    //}
    //public static void Run(/*const*/ ref mdr.DValue i0, Int32 i1, ref mdr.DValue result)
    //{
    //    switch (i0.ValueType)
    //    {
    //        case mdr.ValueTypes.String:
    //            result.Set(i0.ToString() + i1.ToString());
    //            break;
    //        case mdr.ValueTypes.Undefined:
    //        case mdr.ValueTypes.Double:
    //            result.Set(i0.ToDouble() + (double)i1);
    //            break;
    //        case mdr.ValueTypes.Int32:
    //        case mdr.ValueTypes.Boolean:
    //        case mdr.ValueTypes.Null:
    //            result.Set(i0.ToInt32() + i1);
    //            break;
    //        case mdr.ValueTypes.UInt32:
    //            result.Set(i0.UInt32Value + i1);
    //            break;
    //        case mdr.ValueTypes.Object:
    //        case mdr.ValueTypes.Function:
    //        case mdr.ValueTypes.Array:
    //        case mdr.ValueTypes.Property:
    //            result.Set(i0.DObjectValue.ToInt32() + i1);
    //            break;
    //        default:
    //            throw new InvalidOperationException(string.Format("Invalid operand type {0}", i0.ValueType));
    //    }
    //}
    //public static void Run(/*const*/ ref mdr.DValue i0, bool i1, ref mdr.DValue result)
    //{
    //    Run(ref i0, i1 ? 1 : 0, ref result);
    //}
    //public static void Run(/*const*/ ref mdr.DValue i0, mdr.DObject i1, ref mdr.DValue result)
    //{
    //    result.Set(i0.ToString() + i1.ToString());
    //}

    //public static void Run(string i0, /*const*/ ref mdr.DValue i1, ref mdr.DValue result) { result.Set(Convert.ToNumber.Run(i0) + Convert.ToNumber.Run(i1).ToString()); }
    //public static void Run(double i0, /*const*/ ref mdr.DValue i1, ref mdr.DValue result)
    //{
    //    switch (i1.ValueType)
    //    {
    //        case mdr.ValueTypes.String:
    //            result.Set(i0.ToString() + i1.ToString());
    //            break;
    //        case mdr.ValueTypes.Undefined:
    //        case mdr.ValueTypes.Double:
    //        case mdr.ValueTypes.Int32:
    //        case mdr.ValueTypes.UInt32:
    //        case mdr.ValueTypes.Boolean:
    //        case mdr.ValueTypes.Null:
    //            result.Set(Convert.ToNumber.Run(i0) + Convert.ToNumber.Run(i1).ToDouble());
    //            break;
    //        case mdr.ValueTypes.Object:
    //        case mdr.ValueTypes.Function:
    //        case mdr.ValueTypes.Array:
    //        case mdr.ValueTypes.Property:
    //            result.Set(Convert.ToNumber.Run(i0) + Convert.ToNumber.Run(i1).DObjectValue.ToDouble());
    //            break;

    //        default:
    //            throw new InvalidOperationException(string.Format("Invalid operand type {0}", i1.ValueType));
    //    }
    //}
    //public static void Run(int i0, /*const*/ ref mdr.DValue i1, ref mdr.DValue result)
    //{
    //    switch (i1.ValueType)
    //    {
    //        case mdr.ValueTypes.String:
    //            result.Set(i0.ToString() + i1.ToString());
    //            break;
    //        case mdr.ValueTypes.Undefined:
    //        case mdr.ValueTypes.Double:
    //            result.Set((double)Convert.ToNumber.Run(i0) + Convert.ToNumber.Run(i1).ToDouble());
    //            break;
    //        case mdr.ValueTypes.Int32:
    //        case mdr.ValueTypes.Boolean:
    //        case mdr.ValueTypes.Null:
    //            result.Set(Convert.ToNumber.Run(i0) + Convert.ToNumber.Run(i1).ToInt32());
    //            break;
    //        case mdr.ValueTypes.UInt32:
    //            result.Set(Convert.ToNumber.Run(i0) + Convert.ToNumber.Run(i1).UInt32Value);
    //            break;
    //        case mdr.ValueTypes.Object:
    //        case mdr.ValueTypes.Function:
    //        case mdr.ValueTypes.Array:
    //        case mdr.ValueTypes.Property:
    //            result.Set(Convert.ToNumber.Run(i0) + Convert.ToNumber.Run(i1).DObjectValue.ToInt32());
    //            break;
    //        default:
    //            throw new InvalidOperationException(string.Format("Invalid operand type {0}", i1.ValueType));
    //    }
    //}
    //public static void Run(uint i0, /*const*/ ref mdr.DValue i1, ref mdr.DValue result)
    //{
    //    switch (i1.ValueType)
    //    {
    //        case mdr.ValueTypes.String:
    //            result.Set(i0.ToString() + i1.ToString());
    //            break;
    //        case mdr.ValueTypes.Undefined:
    //        case mdr.ValueTypes.Double:
    //            result.Set((double)Convert.ToNumber.Run(i0) + Convert.ToNumber.Run(i1).ToDouble());
    //            break;
    //        case mdr.ValueTypes.Int32:
    //        case mdr.ValueTypes.Boolean:
    //        case mdr.ValueTypes.Null:
    //            result.Set(Convert.ToNumber.Run(i0) + Convert.ToNumber.Run(i1).ToInt32());
    //            break;
    //        case mdr.ValueTypes.UInt32:
    //            result.Set(Convert.ToNumber.Run(i0) + Convert.ToNumber.Run(i1).ToUInt32());
    //            break;
    //        case mdr.ValueTypes.Object:
    //        case mdr.ValueTypes.Function:
    //        case mdr.ValueTypes.Array:
    //        case mdr.ValueTypes.Property:
    //            result.Set(Convert.ToNumber.Run(i0) + Convert.ToNumber.Run(i1).DObjectValue.ToInt32());
    //            break;
    //        default:
    //            throw new InvalidOperationException(string.Format("Invalid operand type {0}", i1.ValueType));
    //    }
    //}
    //public static void Run(bool i0, /*const*/ ref mdr.DValue i1, ref mdr.DValue result)
    //{
    //    Run(i0 ? 1 : 0, ref i1, ref result);
    //}
    //public static void Run(mdr.DObject i0, /*const*/ ref mdr.DValue i1, ref mdr.DValue result)
    //{
    //    result.Set(i0.ToString() + i1.ToString());
    //}

  }
}
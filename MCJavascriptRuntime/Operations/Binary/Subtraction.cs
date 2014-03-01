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
  /// ECMA-262, 11.6.2
  /// </summary>
  public static partial class Subtraction
  {
    //public static void Run(/*const*/ ref mdr.DValue i0, /*const*/ ref mdr.DValue i1, ref mdr.DValue result)
    //{
    //    switch (i0.ValueType)
    //    {
    //        case mdr.ValueTypes.Int32:
    //        case mdr.ValueTypes.Boolean:
    //        case mdr.ValueTypes.Null:
    //            Run(i0.ToInt32(), ref i1, ref result);
    //            break;
    //        default:
    //            Run(i0.ToDouble(), ref i1, ref result);
    //            break;
    //    }
    //}

    //public static void Run(/*const*/ ref mdr.DValue i0, string i1, ref mdr.DValue result) { result.Set(i0.ToDouble() - Convert.ToDouble.Run(i1)); }
    //public static void Run(/*const*/ ref mdr.DValue i0, double i1, ref mdr.DValue result) { result.Set(i0.ToDouble() - i1); }
    //public static void Run(/*const*/ ref mdr.DValue i0, Int32 i1, ref mdr.DValue result)
    //{
    //    switch (i0.ValueType)
    //    {
    //        case mdr.ValueTypes.Int32:
    //        case mdr.ValueTypes.Boolean:
    //        case mdr.ValueTypes.Null:
    //            result.Set(i0.ToInt32() - i1);
    //            break;
    //        default:
    //            result.Set(i0.ToDouble() - i1);
    //            break;
    //    }
    //}
    //public static void Run(/*const*/ ref mdr.DValue i0, bool i1, ref mdr.DValue result)
    //{
    //    Run(ref i0, i1 ? 1 : 0, ref result);
    //}
    //public static void Run(/*const*/ ref mdr.DValue i0, mdr.DObject i1, ref mdr.DValue result)
    //{
    //    throw new InvalidOperationException("Cannot subtract DValue from DObject");
    //}

    //public static void Run(string i0, /*const*/ ref mdr.DValue i1, ref mdr.DValue result) { result.Set(Convert.ToDouble.Run(i0) - i1.ToDouble()); }
    //public static void Run(double i0, /*const*/ ref mdr.DValue i1, ref mdr.DValue result) { result.Set(i0 - i1.ToDouble()); }
    //public static void Run(int i0, /*const*/ ref mdr.DValue i1, ref mdr.DValue result)
    //{
    //    switch (i1.ValueType)
    //    {
    //        case mdr.ValueTypes.Int32:
    //        case mdr.ValueTypes.Boolean:
    //        case mdr.ValueTypes.Null:
    //            result.Set(i0 - i1.ToInt32());
    //            break;
    //        default:
    //            result.Set(i0 - i1.ToDouble());
    //            break;
    //    }
    //}
    //public static void Run(bool i0, /*const*/ ref mdr.DValue i1, ref mdr.DValue result)
    //{
    //    Run(i0 ? 1 : 0, ref i1, ref result);
    //}
    //public static void Run(mdr.DObject i0, /*const*/ ref mdr.DValue i1, ref mdr.DValue result)
    //{
    //    throw new InvalidOperationException("Cannot subtract DValue from DObject");
    //}
  }
}

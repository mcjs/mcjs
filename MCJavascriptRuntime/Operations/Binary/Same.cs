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
    /// ECMA-262, 11.9.6
    /// </summary>

    public static partial class Same
    {
        //public static bool Run(/*const*/ ref mdr.DValue i0, /*const*/ ref mdr.DValue i1)
        //{
        //    switch (i0.ValueType)
        //    {
        //        case mdr.ValueTypes.Undefined: return i1.ValueType == mdr.ValueTypes.Undefined;
        //        case mdr.ValueTypes.String: return Run(i0.ToString(), ref i1);
        //        case mdr.ValueTypes.Double: return Run(i0.ToDouble(), ref i1);
        //        case mdr.ValueTypes.Int32: return Run(i0.ToInt32(), ref i1);
        //        case mdr.ValueTypes.UInt16: return Run(i0.ToUInt16(), ref i1);
        //        case mdr.ValueTypes.UInt32: return Run(i0.ToUInt32(), ref i1);
        //        case mdr.ValueTypes.Boolean: return Run(i0.ToBoolean(), ref i1);
        //        case mdr.ValueTypes.Null: return i1.ValueType == mdr.ValueTypes.Null;
        //        case mdr.ValueTypes.Object:
        //        case mdr.ValueTypes.Array:
        //        case mdr.ValueTypes.Function:
        //            return i0.ValueType == i1.ValueType && i0.DObjectValue == i1.DObjectValue;
        //        default:
        //            throw new InvalidOperationException(string.Format("Invalid operand type {0}", i0.ValueType));
        //    }
        //}

        //public static bool Run(/*const*/ ref mdr.DValue i0, String i1)
        //{
        //    if (i0.ValueType == mdr.ValueTypes.String)
        //        return i0.ToString() == i1;
        //    else
        //        return false;
        //}
        //public static bool Run(/*const*/ ref mdr.DValue i0, Double i1)
        //{
        //    if (ValueTypesHelper.IsNumber(i0.ValueType))
        //        return i0.ToDouble() == i1;
        //    else
        //        return false;
        //}
        //public static bool Run(/*const*/ ref mdr.DValue i0, Int32 i1)
        //{
        //    if (ValueTypesHelper.IsNumber(i0.ValueType))
        //        return i0.ToDouble() == (double)i1;
        //    else
        //        return false;
        //}
        //public static bool Run(/*const*/ ref mdr.DValue i0, bool i1)
        //{
        //    if (i0.ValueType == mdr.ValueTypes.Boolean)
        //        return i0.BooleanValue == i1;
        //    else
        //        return false;
        //}

        //public static bool Run(/*const*/ ref mdr.DValue i0, mdr.DObject i1)
        //{
        //    return i0.DObjectValue == i1;
        //}

        //public static bool Run(string i0, /*const*/ ref mdr.DValue i1) { return Run(ref i1, i0); }
        //public static bool Run(double i0, /*const*/ ref mdr.DValue i1) { return Run(ref i1, i0); }
        //public static bool Run(int i0, /*const*/ ref mdr.DValue i1) { return Run(ref i1, i0); }
        //public static bool Run(bool i0, /*const*/ ref mdr.DValue i1) { return Run(ref i1, i0); }
        //public static bool Run(mdr.DObject i0, /*const*/ ref mdr.DValue i1)
        //{
        //    return i0 == i1.DObjectValue;
        //}
    }
}
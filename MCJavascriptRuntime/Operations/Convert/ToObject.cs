// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
﻿using System.Runtime.CompilerServices;

namespace mjr.Operations.Convert
{
  /// <summary>
  /// ECMA-262, 9.9: Implements ToObject
  /// </summary>
  public static partial class ToObject
  {
    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static mdr.DFunction Run(mdr.DFunction i0) { return i0; }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static mdr.DArray Run(mdr.DArray i0) { return i0; }

    //public static mdr.DObject Run(ref mdr.DValue v)
    //{
    //  mdr.DObject obj = null;
    //  if (mdr.ValueTypesHelper.IsNumber(v.ValueType))
    //  {
    //    obj = new mdr.DObject(mdr.Runtime.Instance.DNumberPrototype);
    //    obj.PrimitiveValue = v;
    //  }
    //  else if (mdr.ValueTypesHelper.IsObject(v.ValueType) || mdr.ValueTypesHelper.IsString(v.ValueType)) //TODO: the primitivevalue of string must be set in the DString
    //  {
    //    obj = v.DObjectValue;
    //  }
    //  else if (mdr.ValueTypesHelper.IsBoolean(v.ValueType))
    //  {
    //    obj = new mdr.DObject(mdr.Runtime.Instance.DBooleanPrototype);
    //    obj.PrimitiveValue = v;
    //  }
    //  else
    //  {
    //    Trace.Fail("Cannot convert {0} to object", v.ValueType);
    //  }
    //  return obj;
    //}
  }
}

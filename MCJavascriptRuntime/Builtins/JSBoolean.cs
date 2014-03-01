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

namespace mjr.Builtins
{
  class JSBoolean : JSBuiltinConstructor
  {
    public JSBoolean()
      : base(mdr.Runtime.Instance.DBooleanPrototype, "Boolean")
    {
      JittedCode = ctor;

      TargetPrototype.DefineOwnProperty("toString", new mdr.DFunction(toString)
        , mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

      TargetPrototype.DefineOwnProperty("valueOf", new mdr.DFunction(valueOf)
        , mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
    }

    private void ctor(ref mdr.CallFrame callFrame)
    {
      if (IsConstrutor)
      {
        mdr.DObject newobject = new mdr.DObject(TargetPrototype);
        if (callFrame.PassedArgsCount > 0)
          newobject.PrimitiveValue.Set(Operations.Convert.ToBoolean.Run(ref callFrame.Arg0));
        else
          newobject.PrimitiveValue.Set(false);

        //newobject.Class = "Boolean";
        callFrame.This = (newobject);
      }
      else
      {
        if (callFrame.PassedArgsCount > 0)
          callFrame.Return.Set(Operations.Convert.ToBoolean.Run(ref callFrame.Arg0));
        else
          callFrame.Return.Set(false);
      }
    }

    // ECMA-262 section 15.6.4.2
    private static void toString(ref mdr.CallFrame callFrame)
    {
      Debug.WriteLine("Calling JSBoolean.toString");
      if (callFrame.This.ValueType != mdr.ValueTypes.Boolean)
        throw new Exception("Boolean.prototype.toString is not generic");
      callFrame.Return.Set(callFrame.This.ToString());
    }

    // ECMA-262 section 15.6.4.3
    private static void valueOf(ref mdr.CallFrame callFrame)
    {
      Debug.WriteLine("Calling JSBoolean.valueOf");
      if (callFrame.This.ValueType != mdr.ValueTypes.Boolean)
        throw new Exception("Boolean.prototype.valueOf is not generic");
      callFrame.Return.Set(callFrame.This.ToBoolean());
    }
  }
}

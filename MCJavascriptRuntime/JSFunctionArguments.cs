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

using m.Util.Diagnose;

namespace mjr
{
  public static class JSFunctionArguments
  {
    public const string Name = "arguments";

    // TODO optimization opportunity by reusing args array via freelist.
    // NOTE: used arrays must be cleared, otherwise objects are GCd in time
    //       (e.g., ContentWindow)
    public static mdr.DValue[] Allocate(int argCount)
    {
      return new mdr.DValue[argCount];
    }
    public static void Release(mdr.DValue[] args)
    {
    }
    private static mdr.DArray CreateArgumentsObject(ref mdr.CallFrame callFrame)
    {
      var argsCount = callFrame.ExpectedArgsCount;
      if (callFrame.PassedArgsCount > callFrame.ExpectedArgsCount)
        argsCount = callFrame.PassedArgsCount;
      var arguments = new mdr.DArray(argsCount);
      switch (callFrame.PassedArgsCount)
      {
        case 0: break;
        case 1: arguments.Elements[0] = callFrame.Arg0; goto case 0;
        case 2: arguments.Elements[1] = callFrame.Arg1; goto case 1;
        case 3: arguments.Elements[2] = callFrame.Arg2; goto case 2;
        case 4: arguments.Elements[3] = callFrame.Arg3; goto case 3;
        default:
          Array.Copy(callFrame.Arguments, 0, arguments.Elements, 4, callFrame.PassedArgsCount - mdr.CallFrame.InlineArgsCount);
          goto case 4;
      }
      ///To comply with 10.6 item 6, we add a "length" property to prevent it affecting the actual array,
      ///TODO: in future version os Javscript, this is going change and Arguments is acutally an array
      arguments.DefineOwnProperty("length", callFrame.PassedArgsCount, mdr.PropertyDescriptor.Attributes.Data | mdr.PropertyDescriptor.Attributes.NotEnumerable);

      return arguments;
    }

    class ArgumentAccessor : mdr.DProperty
    {
      //mdr.DArray Arguments;
      //int ParamIndex;

      public ArgumentAccessor(mdr.DArray arguments, int paramIndex)
      {
        OnGetDValue = (mdr.DObject This, ref mdr.DValue v) => { v.Set(ref arguments.Elements[paramIndex]); };
        OnSetDValue = (mdr.DObject This, ref mdr.DValue v) => { arguments.Elements[paramIndex].Set(ref v); };
      }
    }


    public static mdr.DArray CreateArgumentsObject(ref mdr.CallFrame callFrame, mdr.DObject context)
    {
      var metadata = (JSFunctionMetadata)callFrame.Function.Metadata;
      Debug.Assert(metadata.Scope.HasArgumentsSymbol, "Invalid situation, created arguments for the wrong scope!");
      mdr.DArray arguments = null;
      if (metadata.Scope.IsEvalFunction)
      {
        //Read from context
        var tmp = new mdr.DValue();
        context.GetField(JSFunctionArguments.Name, ref tmp);
        arguments = tmp.AsDArray();
      }
      else
      {
        arguments = CreateArgumentsObject(ref callFrame);
        var parameters = metadata.FunctionIR.Parameters;
        Debug.Assert(arguments.Length >= parameters.Count, "arguments array is not large enough to hold all arguments.");
        for (var i = parameters.Count - 1; i >= 0; --i)
        {
          var symbol = parameters[i].Symbol;
          var paramIndex = symbol.ParameterIndex;
          Debug.Assert(paramIndex == i, "Invalid situation!, Parameter indexes don't match!");

          if (symbol.SymbolType == JSSymbol.SymbolTypes.ClosedOnLocal)
          {
            var pd = context.AddOwnPropertyDescriptorByFieldId(symbol.FieldId, mdr.PropertyDescriptor.Attributes.Accessor | mdr.PropertyDescriptor.Attributes.NotConfigurable);
            context.Fields[pd.Index].Set(new ArgumentAccessor(arguments, paramIndex));
          }
        }
        if (metadata.Scope.HasEval)
          context.SetField(JSFunctionArguments.Name, arguments);
      }
      return arguments;
    }
  }
}

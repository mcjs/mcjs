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
using mdr;

namespace mjr
{
  /// <summary>
  /// This class is static for now, but later, we may want to inherit it from mdr.DObject and add some extended functinality
  /// </summary>
  public static class JSFunctionContext
  {
    private static void AddSymbolsToContext(List<JSSymbol> symbols, mdr.DObject context)
    {
      //This is called when we are sharing the context, so should be careful not to add fields that are already there
      //Technically, this is the generic and safe way to do this. 
      for (var i = symbols.Count - 1; i >= 0; --i)
      {
        var symbol = symbols[i];
        if (symbol.SymbolType == JSSymbol.SymbolTypes.ClosedOnLocal)
        {
          var pd = context.Map.GetPropertyDescriptorByFieldId(symbol.FieldId); //TODO: can this be made faster?
          if (pd == null)
            context.AddOwnPropertyDescriptorByFieldId(symbol.FieldId, PropertyDescriptor.Attributes.Data | PropertyDescriptor.Attributes.NotConfigurable);
        }
      }
    }

    public static mdr.DObject CreateProgramContext(ref mdr.CallFrame callFrame)
    {
      //this function should add everything to global context
      //since this is called once, it will also add all the symbols through a not so efficient loop
      var metadata = (JSFunctionMetadata)callFrame.Function.Metadata;
      Debug.Assert(metadata.Scope.IsProgram, "Function {0} is not a program", metadata.Declaration);
      var context = JSRuntime.Instance.GlobalContext;
      AddSymbolsToContext(metadata.Scope.Symbols, context);
      return context;
    }

    public static mdr.DObject CreateEvalContext(ref mdr.CallFrame callFrame)
    {
      //this function should add everything to its parent context
      //since we don't cache the generated code, it will also add all the symbols through a not so efficient loop
      var metadata = (JSFunctionMetadata)callFrame.Function.Metadata;
      Debug.Assert(metadata.Scope.IsEvalFunction, "Function {0} is not an eval", metadata.Declaration);
      Debug.Assert(callFrame.CallerContext != null, "Eval function {0} needs CallerContext in its call frame", metadata.Declaration);
      var context = callFrame.CallerContext;
      AddSymbolsToContext(metadata.Scope.Symbols, context);
      return context;
    }

    public static mdr.DObject CreateConstantContext(ref mdr.CallFrame callFrame)
    {
      //return CreateFunctionContext(ref callFrame);
      //TODO: the following optimization requires proper support in the code generation as well! For now, we don't do much!

      //this function will not change its context, so we can just reuse the outer context
      Debug.Assert(((JSFunctionMetadata)callFrame.Function.Metadata).Scope.IsConstContext, "Function {0} will need its own context", ((JSFunctionMetadata)callFrame.Function.Metadata).Declaration);
      var context = callFrame.Function.OuterContext;
      return context;
    }

    public static mdr.DObject CreateFunctionContext(ref mdr.CallFrame callFrame)
    {
      //this function will or may change its context, so create a new one; this is the safest option

      ///If callFrame.Function.ContextMap is null, it means it is the first time that function object is called
      ///in this case, we create a new PropertyMap and add all necessary fields to it. Then assign it to the .ContextMap.
      ///We don't add them one-by-one to the context itself to avoid resizing context.Fields several times.
      ///Also we first add all of them, so that we can use the reference of the context.Fields[i]
      //TODO: if we had the function object here, we could technically do the addition to map here and generate code that directly uses the indexes

      mdr.DObject context;
      var contextMap = callFrame.Function.Metadata.ContextMap;
      if (contextMap == null)
      {
        var outerContext = callFrame.Function.OuterContext;
        context = new mdr.DObject(outerContext); //We do this first to get the root of the map first

        contextMap = context.Map;
        var scope = ((JSFunctionMetadata)callFrame.Function.Metadata).Scope;
        if (scope.HasClosedOnSymbol)
        {
          var symbols = scope.Symbols;
          for (var i = symbols.Count - 1; i >= 0; --i)
          {
            var symbol = symbols[i];
            if (symbol.SymbolType == JSSymbol.SymbolTypes.ClosedOnLocal)
            {
              if (scope.HasArgumentsSymbol && symbol.IsParameter)
                contextMap = contextMap.AddOwnProperty(symbol.Name, symbol.FieldId, PropertyDescriptor.Attributes.Accessor | PropertyDescriptor.Attributes.NotConfigurable);
              else
                contextMap = contextMap.AddOwnProperty(symbol.Name, symbol.FieldId, PropertyDescriptor.Attributes.Data | PropertyDescriptor.Attributes.NotConfigurable);
            }
          }
        }

        if (scope.HasEval)
        {
          //Eval may use the arguments
          Debug.Assert(scope.HasArgumentsSymbol, "Exected arguments in the context for a function with eval");
          contextMap = contextMap.AddOwnProperty(JSFunctionArguments.Name, mdr.Runtime.Instance.GetFieldId(JSFunctionArguments.Name), PropertyDescriptor.Attributes.Data | PropertyDescriptor.Attributes.NotConfigurable);
        }

        context.Map = contextMap; //This will update the fields size
        callFrame.Function.Metadata.ContextMap = contextMap;
      }
      else
      {
        context = new mdr.DObject(contextMap);
      }
      return context;
    }
  }
}

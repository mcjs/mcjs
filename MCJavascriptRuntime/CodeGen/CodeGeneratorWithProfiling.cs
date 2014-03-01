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
using System.Reflection.Emit;
using System.Text;
using mjr.IR;
using m.Util.Diagnose;

namespace mjr.CodeGen
{
  class CodeGeneratorWithProfiling : CodeGenerator
  {
    static AlgorithmImplementation _pool = new AlgorithmPool<CodeGeneratorWithProfilingImp>("JS/Jit/Prof/", () => new CodeGeneratorWithProfilingImp(), () => JSRuntime.Instance.Configuration.ProfileJitTime);
    public new static void Execute(CodeGenerationInfo cgInfo) { _pool.Execute(cgInfo); }

    protected class CodeGeneratorWithProfilingImp : CodeGeneratorImp
    {
      protected LocalBuilder _profiler;

      protected override void GenProlog()
      {
        base.GenProlog();
        _profiler = _localVars.Declare(Types.Profiler.TypeOf, "profiler");
        Ldarg_CallFrame();
        _ilGen.Ldfld(Types.CallFrame.Function);
        _ilGen.Ldfld(Types.DFunction.Code);
        _ilGen.Castclass(Types.JSFunctionCode.TypeOf);
        _ilGen.Ldfld(Types.JSFunctionCode.Profiler);
        _ilGen.Stloc(_profiler);
      }

      public override void Visit(GuardedCast node)
      {
        base.Visit(node);
        if (_result.ValueType == mdr.ValueTypes.DValueRef && ((JSRuntime.Instance.Configuration.EnableGuardElimination && node.IsRequired) ||
                                                               !JSRuntime.Instance.Configuration.EnableGuardElimination))
        {
          int profIndex = _currFuncMetadata.GetProfileIndex(node);
          _ilGen.Dup();
          _ilGen.Call(Types.DValue.GetValueType);
          _ilGen.Ldloc(_profiler);
          _ilGen.Ldc_I4(profIndex);
          _ilGen.Call(Types.Operations.Internals.UpdateGuardProfile);
        }
      }

      protected override void PerformLookup(ReadIndexerExpression node, LocalBuilder obj, mdr.ValueTypes objType, mdr.ValueTypes indexType, LocalBuilder value)
      {
        if (objType == mdr.ValueTypes.Array && indexType == mdr.ValueTypes.Int32)
          base.PerformLookup(node, obj, objType, indexType, value);
        else
        {
          _ilGen.Callvirt(Types.DObject.GetPropertyDescriptor.Get(_result.ValueType));
          //_ilGen.Callvirt(Types.DObject.GetField(_result.ValueType));

          int profIndex = _currFuncMetadata.GetProfileIndex(node);
          _ilGen.Ldloc(_profiler);
          _ilGen.Ldc_I4(profIndex);
          _ilGen.Ldloc(obj);
          _ilGen.Call(Types.DObject.GetMap);
          _ilGen.Call(Types.Operations.Internals.UpdateMapProfile);

          _ilGen.Ldloc(obj);
          _ilGen.Ldloca(value);
          _ilGen.Callvirt(Types.PropertyDescriptor.Get_DObject_DValueRef);
        }
      }

      public override void Visit(ReadPropertyExpression node)
      {
        Visit(node as ReadIndexerExpression);
      }

      public override void Visit(WritePropertyExpression node)
      {
        PushLocation(node);

        var stackState = _localVars.GetTemporaryStackState();

        VisitNode(node.Container);
        AsDObject();
        var objType = _result.ValueType;
        var obj = _localVars.PushTemporary(objType);
        _ilGen.Stloc(obj);

        node.AssignFieldId();


        var oldMap = _localVars.PushTemporary(Types.PropertyMap.TypeOf);
        _ilGen.Ldloc(obj);
        _ilGen.Call(Types.DObject.GetMap);
        _ilGen.Stloc(oldMap);

        _ilGen.Ldloc(obj);
        _ilGen.Ldc_I4(node.FieldId);
        VisitNode(node.Value);
        var valueType = _result.ValueType;

        _localVars.PopTemporariesAfter(stackState);
        var value = _localVars.PushTemporary(valueType);
        _ilGen.Stloc(value);
        _ilGen.Ldloc(value);
        _ilGen.Call(Types.DObject.SetFieldByFieldId(valueType));

        int profIndex = _currFuncMetadata.GetProfileIndex(node);
        _ilGen.Ldloc(obj);
        _ilGen.Ldloc(_profiler);
        _ilGen.Ldc_I4(profIndex);
        _ilGen.Ldc_I4(node.FieldId);
        _ilGen.Ldloc(oldMap);
        _ilGen.Call(Types.Operations.Internals.UpdateMapProfileForWrite);

        _ilGen.Ldloc(value);
        _result.ValueType = valueType;

        PopLocation();
      }

      protected override LocalBuilder CreateCallFrame(Invocation node)
      {
        var callFrame = base.CreateCallFrame(node);

        int profIndex = _currFuncMetadata.GetProfileIndex(node);
        _ilGen.Ldloc(_profiler);
        _ilGen.Ldc_I4(profIndex);
        _ilGen.Ldloca(callFrame);
        _ilGen.Ldfld(Types.CallFrame.Function);
        _ilGen.Call(Types.Operations.Internals.UpdateCallProfile);

        return callFrame;
      }
      public override void Visit(ReadIdentifierExpression node)
      {
        PushLocation(node);
        var symbol = node.Symbol;
        var local = _localVars.Get(symbol);
        if (symbol.SymbolType == JSSymbol.SymbolTypes.Global && local.LocalType == Types.PropertyDescriptor.TypeOf)
        {
          int profIndex = _currFuncMetadata.GetProfileIndex(node);
          _ilGen.Ldloc(local);
          _ilGen.Ldloc(_profiler);
          _ilGen.Ldc_I4(profIndex);
          _ilGen.LoadRuntimeInstance();
          _ilGen.Ldfld(Types.Runtime.GlobalContext);
          _ilGen.Call(Types.DObject.GetMap);          
          _ilGen.Call(Types.Operations.Internals.UpdateMapProfile);

          var value = _localVars.PushTemporary(Types.DValue.TypeOf);
          //_ilGen.Ldloc(local);
          _ilGen.LoadRuntimeInstance();
          _ilGen.Ldfld(Types.Runtime.GlobalContext);
          _ilGen.Ldloca(value);
          _ilGen.Call(Types.PropertyDescriptor.Get_DObject_DValueRef);

          _ilGen.Ldloca(value);
          _result.ValueType = mdr.ValueTypes.DValueRef;
          PopLocation();
        }
        else
        {
          base.Visit(node);
        }
      }
    }
  }
}

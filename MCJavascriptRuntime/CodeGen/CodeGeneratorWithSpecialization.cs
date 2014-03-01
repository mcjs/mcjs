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
using System.Reflection.Emit;
using System.Linq;
using System.Text;
using m.Util.Diagnose;
using mjr.IR;

namespace mjr.CodeGen
{
  class CodeGeneratorWithSpecialization : CodeGenerator
  {
    static AlgorithmImplementation _pool = new AlgorithmPool<CodeGeneratorWithSpecializationImp>("JS/Jit/Specialization/", () => new CodeGeneratorWithSpecializationImp(), () => JSRuntime.Instance.Configuration.ProfileJitTime);
    public new static void Execute(CodeGenerationInfo cgInfo) { _pool.Execute(cgInfo); }

    protected class CodeGeneratorWithSpecializationImp : CodeGeneratorImp
    {
      protected Profiler _currProfiler;
      protected LocalBuilder _globalContext;
      protected Label _deoptimizer;

      protected override void ExecuteInitialize()
      {
        _currProfiler = _currFuncCode.Profiler;
        _globalContext = null;
        _deoptimizer = _ilGen.DefineLabel();

        Debug.Assert(JSRuntime.Instance.Configuration.EnableSpeculativeJIT && _currProfiler != null && _currFuncCode.IsHot && _currProfiler != null, "Invalid situation! Speculation conditions are not correct!");

        base.ExecuteInitialize();
      }

      protected override void DeclareNonParamSymbol(JSSymbol symbol, mdr.ValueTypes symbolValueType, LocalBuilder contextMap)
      {
        if (symbol.SymbolType == JSSymbol.SymbolTypes.Global && _currFuncMetadata.EnableInlineCache)
        {
          if (_globalContext == null)
          {
            _globalContext = _localVars.Declare(Types.DObject.TypeOf, "__gc");
            _ilGen.LoadRuntimeInstance();
            _ilGen.Ldfld(Types.Runtime.GlobalContext);
            _ilGen.Stloc(_globalContext);
          }
        }
        else
          base.DeclareNonParamSymbol(symbol, symbolValueType, contextMap);
      }

      protected override void GenBody()
      {
        var finalLabel = _ilGen.DefineLabel();
        _ilGen.BeginExceptionBlock();
        _labelInfos.PushProtectedRegion(_currFuncCode);
        base.GenBody();
        _labelInfos.PopProtectedRegion(_currFuncCode);
        _ilGen.Br(finalLabel);
        _ilGen.MarkLabel(_deoptimizer);
        if (JSRuntime.Instance.Configuration.EnableDeoptimization)
            GenDeoptimizer();
        else
        {
            _ilGen.BeginCatchBlock(Types.JSSpeculationFailedException.TypeOf);
            _labelInfos.PushProtectedRegion(_currFuncCode);
            _ilGen.Pop();
            _labelInfos.PopProtectedRegion(_currFuncCode);
            _ilGen.EndExceptionBlock();
        }
        _ilGen.MarkLabel(finalLabel);
      }

      private void StoreSymbols()
      {
          foreach (var symbol in _currFuncMetadata.Scope.Symbols)
          {
              if ((symbol.SymbolType == JSSymbol.SymbolTypes.Local || symbol.SymbolType == JSSymbol.SymbolTypes.HiddenLocal)
                  && !symbol.IsParameter)
              {
                  var stackState = _localVars.GetTemporaryStackState();
                  _ilGen.LoadValue(2 + symbol.Index);
                  Visit(new ReadIdentifierExpression(symbol));
                  //_ilGen.Ldstr("StoreSymbols is called as well!");
                  //_ilGen.Call(Types.Operations.Internals.PrintString);
                  _ilGen.Call(Types.DValue.Set.Get(_result.ValueType));
                  _localVars.PopTemporariesAfter(stackState);
              }
          }
      }

      private void GenDeoptimizer()
      {
          var icID = _ilGen.DeclareLocal(Types.ClrSys.Int32);
          var expectedType = _ilGen.DeclareLocal(Types.ClrSys.Int32);

          _ilGen.BeginCatchBlock(Types.JSSpeculationFailedException.TypeOf);
          _labelInfos.PushProtectedRegion(_currFuncCode);

          _ilGen.Dup();
          _ilGen.Ldfld(Types.JSSpeculationFailedException.icIndex);
          _ilGen.Stloc(icID);
          _ilGen.Ldfld(Types.JSSpeculationFailedException.expectedType);
          _ilGen.Stloc(expectedType);

          //Set the first field i.e. context
          _ilGen.Ldloc(_context);
          _ilGen.StoreValue(0, mdr.ValueTypes.Object);

          //Store the arguments value in the values array
          if (_currFuncMetadata.Scope.HasArgumentsSymbol)
          {
              _ilGen.Ldarg_CallFrame();
              _ilGen.Ldc_I4_1();
              //_ilGen.Ldstr("CreateArgumentsObject is called as well!");
              //_ilGen.Call(Types.Operations.Internals.PrintString);
              _ilGen.Call(Types.Operations.ICMethods.CreateArgumentsObject);
          }

          //Store the symbols into the values array
          StoreSymbols();

          _ilGen.Ldarg_CallFrame();
          _ilGen.Ldfld(Types.CallFrame.Function);
          //_ilGen.Ldstr("BlackList is called as well!");
          //_ilGen.Call(Types.Operations.Internals.PrintString);
          _ilGen.Call(Types.DFunction.BlackList);

          //Call IC based interpreter with the same callframe
          _ilGen.Ldarg_CallFrame();
          _ilGen.Ldloc(icID);
          _ilGen.Ldc_I4(_currFuncMetadata.InlineCache.Length - 1);
          //_ilGen.Ldstr("IC is called as well!");
          //_ilGen.Call(Types.Operations.Internals.PrintString);
          _ilGen.Call(Types.Operations.ICMethods.Execute);

          _labelInfos.PopProtectedRegion(_currFuncCode);
          _ilGen.EndExceptionBlock();
      }

      public override void Visit(GuardedCast node)
      {
        base.Visit(node);

        if (_result.ValueType != mdr.ValueTypes.DValueRef)
          return;

        var nodeProfile = _currProfiler.GetNodeProfile(node);

        if (nodeProfile != null)
        {
          var hotType = nodeProfile.GetHotType();
          if (hotType != mdr.ValueTypes.DValueRef)
          {
            var valueType = _localVars.Declare(typeof(int));
            _ilGen.Dup();
            _ilGen.Call(Types.DValue.GetValueType);
            _ilGen.Conv_I4();
            _ilGen.Dup();
            _ilGen.Stloc(valueType);
            _ilGen.Ldc_I4((int)hotType);
            var success = _ilGen.DefineLabel();
            _ilGen.Beq(success);

            if (JSRuntime.Instance.Configuration.EnableDeoptimization)
            {
                /*
                Get the types of the values in the stack right now from the ValidatingILGenerator
                Encode it in a string and push it onto the stack
                Push the icIndex of the GuardedCast node
                Add the jump to the deoptimizer here
                */
                var vILGen = _ilGen as mjr.ILGen.ValidatingILGenerator;
                if (vILGen != null) vILGen.DeactivateValidation();

                _ilGen.Ldarg_CallFrame();
                _ilGen.Ldc_I4(_currFuncMetadata.ValuesLength);
                _ilGen.NewArr(Types.DValue.TypeOf);
                _ilGen.Stfld(Types.CallFrame.Values);

                var types = _ilGen.GetValueTypes();
                var length = types.Length;
                var stackStartIndex = 2 + _currFuncMetadata.Scope.Symbols.Count;
                var index = 1;

                /// CallFrame.Values is usually assinged like this:
                /// +-------+---------+-------------+-----------+-----------------+
                /// |context|arguments|...symbols...|...stack...|...temporaries...|
                /// +-------+---------+-------------+-----------+-----------------+
                foreach (var t in types)
                {
                    var vType = Types.ValueTypeOf(t);
                    //Console.WriteLine("Storing Symbols is called as well!\n");
                    //_ilGen.Call(Types.Operations.Internals.PrintString);
                    _ilGen.StoreValue(stackStartIndex + length - index++, vType);
                }

                //_ilGen.Ldstr(valueTypesString);
                _ilGen.Ldc_I4(node.ICIndex);
                _ilGen.Ldloc(valueType);
                //_ilGen.Ldstr("JSSpeculationFailed is called as well!");
                //_ilGen.Call(Types.Operations.Internals.PrintString);
                _ilGen.Call(Types.JSSpeculationFailedException.Throw.Get(mdr.ValueTypes.Int32));

                if (vILGen != null) vILGen.ReactivateValidation();
            }
            else
            {
                _ilGen.Ldc_I4(node.ICIndex);
                _ilGen.Ldloc(valueType);
                _ilGen.Call(Types.JSSpeculationFailedException.Throw.Get(mdr.ValueTypes.Int32));
            }
            _ilGen.MarkLabel(success);
            AsX(hotType);
          }
        }
      }

      public override void Visit(ReadIndexerExpression node)
      {
        /*var nodeProfile = _currProfiler.GetNodeProfile(node);
        
        if (_currFuncMetadata.EnableInlineCache
          && nodeProfile != null
          && nodeProfile.Map != null
          && nodeProfile.PD == JSRuntime.Instance.GetArrayItemAccessor())
        {
          PushLocation(node);
          var value = _localVars.PushTemporary(Types.DValue.TypeOf);

          var stackState = _localVars.GetTemporaryStackState();

          VisitNode(node.Container);
          AsDObject();
          var objType = _result.ValueType;
          var obj = _localVars.PushTemporary(objType);

          _ilGen.Stloc(obj);
          _ilGen.Ldloc(obj);

          //TODO: we need to insert the inline cache here
          VisitNode(node.Index);

          var indexType = _result.ValueType;
          if (indexType != mdr.ValueTypes.Int32)
          {
            AsInt32();
          }
          PerformLookup(node, obj, mdr.ValueTypes.Array, mdr.ValueTypes.Int32, value);

          _localVars.PopTemporariesAfter(stackState);

          _ilGen.Ldloca(value);
          _result.ValueType = mdr.ValueTypes.DValueRef;

          PopLocation();
        }        
        else*/
          base.Visit(node);
      }
     
      public override void Visit(ReadPropertyExpression node)
      {
        var nodeProfile = _currProfiler.GetNodeProfile(node);
        if (_currFuncMetadata.EnableInlineCache
          && nodeProfile != null
          && nodeProfile.Map != null
          && nodeProfile.PD != null
          && nodeProfile.PD.IsDataDescriptor
          && !nodeProfile.PD.IsInherited)
        {
          PushLocation(node);

          var value = _localVars.PushTemporary(Types.DValue.TypeOf);
          var stackState = _localVars.GetTemporaryStackState();

          VisitNode(node.Container);
          AsDObject();
          node.AssignFieldId();
          _ilGen.Ldc_I4(node.FieldId);
          _ilGen.Ldloca(value);

          _ilGen.Ldc_I4(nodeProfile.Map.UniqueId);
          _ilGen.Ldc_I4(nodeProfile.PD.Index);
          _ilGen.Call(Types.Operations.Internals.GetFieldUsingIC);

          _ilGen.Ldloca(value);
          _result.ValueType = mdr.ValueTypes.DValueRef;

          PopLocation();
        }
        else if (_currFuncMetadata.EnableInlineCache
          && nodeProfile != null
          && nodeProfile.Map != null
          && nodeProfile.PD != null
          && nodeProfile.PD.IsDataDescriptor
          && nodeProfile.PD.IsInherited)
        {
          int inheritCacheIndex = JSRuntime.UpdateInheritPropertyObjectCache(nodeProfile.PD);
          if (inheritCacheIndex != -1)
          {
            PushLocation(node);
            var value = _localVars.PushTemporary(Types.DValue.TypeOf);
            VisitNode(node.Container);
            AsDObject();
            node.AssignFieldId();
            _ilGen.Ldc_I4(node.FieldId);

            _ilGen.Ldloca(value);
            _ilGen.Ldc_I4(nodeProfile.Map.UniqueId);
            _ilGen.Ldc_I4(nodeProfile.PD.Index);
            _ilGen.Ldc_I4(inheritCacheIndex);
            _ilGen.Call(Types.Operations.Internals.GetInheritFieldUsingIC);
            _ilGen.Ldloca(value);
            _result.ValueType = mdr.ValueTypes.DValueRef;
            PopLocation();
          }
          else
          {
            base.Visit(node);
          }
        }
        else
        {
//          if (nodeProfile == null)
//            nodeProfile.PD.HasAttributes(mdr.PropertyDescriptor.Attributes.NotConfigurable))
//            Trace.WriteLine("Missing profile data {0} {1} {2}!", node, node.ProfileIndex, _currFuncMetadata);

          base.Visit(node);
        }
      }

      public override void Visit(WritePropertyExpression node)
      {
        var nodeProfile = _currProfiler.GetNodeProfile(node);
        if (_currFuncMetadata.EnableInlineCache
          && nodeProfile != null
          && nodeProfile.Map != null
          && nodeProfile.PD != null
          && nodeProfile.PD.IsDataDescriptor
          && !nodeProfile.PD.IsInherited)
        {
          ///Let's keep this code as close to base.Visit so that updates are easy, 
          ///even soon we can decide to put this code in a function
          PushLocation(node);

          var stackState = _localVars.GetTemporaryStackState();

          VisitNode(node.Container);
          AsDObject();
          var objType = _result.ValueType;
          var obj = _localVars.PushTemporary(objType);
          _ilGen.Stloc(obj);
          _ilGen.Ldloc(obj);
          node.AssignFieldId();
          _ilGen.Ldc_I4(node.FieldId);
          VisitNode(node.Value);
          var valueType = _result.ValueType;

          _localVars.PopTemporariesAfter(stackState);
          var value = _localVars.PushTemporary(valueType);
          _ilGen.Stloc(value);

          _ilGen.Ldloc(value);
          _ilGen.Ldc_I4(nodeProfile.Map.UniqueId);
          _ilGen.Ldc_I4(nodeProfile.PD.Index);
          _ilGen.Call(Types.Operations.Internals.SetFieldUsingIC(valueType));

          _ilGen.Ldloc(value);
          _result.ValueType = valueType;

          PopLocation();
        }
        else
          base.Visit(node);

      }
      public override void Visit(ReadIdentifierExpression node)
      {
        var symbol = node.Symbol;
        var local = _localVars.Get(symbol);
        if (symbol.SymbolType == JSSymbol.SymbolTypes.Global && local == null)
        {
          var nodeProfile = _currProfiler.GetNodeProfile(node);
          if (_currFuncMetadata.EnableInlineCache
            && nodeProfile != null
            && nodeProfile.Map != null
            && nodeProfile.PD != null
            && nodeProfile.PD.IsDataDescriptor
            && !nodeProfile.PD.IsInherited)
          {
            PushLocation(node);
            var value = _localVars.PushTemporary(Types.DValue.TypeOf);
            _ilGen.Ldloc(_globalContext);
            _ilGen.Ldc_I4(symbol.FieldId);
            _ilGen.Ldloca(value);

            _ilGen.Ldc_I4(nodeProfile.Map.UniqueId);
            _ilGen.Ldc_I4(nodeProfile.PD.Index);
            _ilGen.Call(Types.Operations.Internals.GetFieldUsingIC);
            _ilGen.Ldloca(value);
            _result.ValueType = mdr.ValueTypes.DValueRef; 
            PopLocation();
          }
          else
          {
            base.Visit(node);
          }
        }
        else
        {
          base.Visit(node);
        }
      }
    }
  }
}

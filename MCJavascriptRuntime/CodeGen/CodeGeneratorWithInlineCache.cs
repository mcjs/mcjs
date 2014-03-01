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
using System.Threading.Tasks;

using mjr.IR;
using m.Util.Diagnose;

namespace mjr.CodeGen
{
  class CodeGeneratorWithInlineCache : CodeGeneratorBase
  {
    static AlgorithmImplementation _pool = new AlgorithmPool<Implementation>("JS/Jit/IC/", () => new Implementation(), () => JSRuntime.Instance.Configuration.ProfileJitTime);
    public static void Execute(JSFunctionMetadata funcMetadata) { _pool.Execute(funcMetadata); }

    protected class Implementation : CodeGeneratorBaseImp
    {
      struct StackModel
      {
        public int MaxStackDepth { get; private set; }

        public int StackPointer { get; private set; }

        public void Push(int itemCount)
        {
          StackPointer += itemCount;
          if (StackPointer > MaxStackDepth)
            MaxStackDepth = StackPointer;
          Debug.WriteLine(">>>{0} at {1}", new string('-', StackPointer), Diagnostics.GetStackTrace(2));
        }
        public void Pop(int itemCount)
        {
          StackPointer -= itemCount;
          Debug.Assert(StackPointer >= 0, "Stack empty!");
          Debug.WriteLine("<<<{0} at {1}", new string('-', StackPointer), Diagnostics.GetStackTrace(2));
        }
      }
      StackModel _stackModel;
      int _initMethodIndex;

      protected LabelInfoManager<LinkedList<int>> _labelInfos = new LabelInfoManager<LinkedList<int>>();

      #region WriteTemporaries
      class WriteTemporaryInfo
      {
        public int WriterICIndex;
        public int SourceValueIndex;
        public struct ReaderInfo
        {
          public int ReaderICIndex;
          public int DestinationValueIndex;
        }
        public List<ReaderInfo> Readers = new List<ReaderInfo>();
      }
      Dictionary<WriteTemporaryExpression, WriteTemporaryInfo> _writeTemporaries;
      #endregion

      #region ICs
      class ICInfo
      {
        public System.Threading.Tasks.Task JitTask;
        public ILGen.BaseILGenerator IlGen;
        public mdr.DFunctionMetadata.InlineCachedMethod ICMethod;
      }
      List<ICInfo> _ics;

      /// <summary>
      /// The index of the last Valid IC
      /// </summary>
      int GetCurrentICIndex() { return _ics.Count - 1; }

      /// <summary>
      /// Reserves a slot, and returns its index
      /// </summary>
      int ReserveNewICIndex()
      {
        _ics.Add(null);
        return GetCurrentICIndex();
      }

      ILGen.BaseILGenerator BeginICMethod(string namePostfix, bool updateIlGen = true)
      {
        var icMethod = JSRuntime.Instance.AsmGenerator.GetILGenerator();
        var methodName = _currFuncMetadata.FullName + "__" + _ics.Count.ToString() + "__" + namePostfix;
        Debug.WriteLine("Starting method {0} in {1}", methodName, Diagnostics.GetStackTrace(2));
        icMethod.BeginICMethod(methodName);
        if (updateIlGen)
        {
          Trace.Assert(_ilGen == null, "Another IC method is still open!");
          _ilGen = icMethod;
        }
        return icMethod;
      }
      ILGen.BaseILGenerator BeginICMethod(Node node, bool updateIlGen = true) { return BeginICMethod(node.GetType().Name, updateIlGen); }

      void EndICMethod(int index = -1, int nextIndex = -1)
      {
        Trace.Assert(_ilGen != null, "No IC method is open!");

        if (index == -1)
          index = ReserveNewICIndex();

        if (nextIndex == -1)
          nextIndex = index + 1;
        Br(nextIndex);
        _ilGen.WriteComment("Installing this at index {0}", index);

        var icInfo = new ICInfo();
        icInfo.IlGen = _ilGen;

        if (JSRuntime.Instance.Configuration.EnableParallelJit)
        {
          var runtime = JSRuntime.Instance;
          icInfo.JitTask = System.Threading.Tasks.Task.Factory.StartNew(() =>
          {
            mdr.Runtime.Instance = JSRuntime.Instance = runtime;
            icInfo.ICMethod = icInfo.IlGen.EndICMethod(_currFuncMetadata);
            //icInfo.IlGen = null;
          });
        }
        else
          icInfo.ICMethod = icInfo.IlGen.EndICMethod(_currFuncMetadata);
        _ics[index] = icInfo;
        _ilGen = null;
      }

      /// <summary>
      /// Sometime we need to gradually build an ICMethod while visiting other nodes
      /// in those case we use updateILGen=false and then use this version of EndICMethod
      /// </summary>
      void EndICMethod(ILGen.BaseILGenerator ilGen, int index = -1, int nextIndex = -1)
      {
        Debug.Assert(_ilGen == null, "An IC method is already open!");
        _ilGen = ilGen;
        EndICMethod(index, nextIndex);
      }


      void AddICMethod(mdr.DFunctionMetadata.InlineCachedMethod method, int index = -1)
      {
        if (index == -1)
          index = ReserveNewICIndex();

        var icInfo = new ICInfo();
        icInfo.ICMethod = method;
        _ics[index] = icInfo;
      }

      #endregion
      /// CallFrame.Values is usually assinged like this:
      /// +-------+---------+-------------+-----------+-----------------+
      /// |context|arguments|...symbols...|...stack...|...temporaries...|
      /// +-------+---------+-------------+-----------+-----------------+

      int _symbolIndexOffset; //We may have reserved values like context, arguments ... in the CallFrame.Values

      /// <summary>
      /// Returns the index of the symbols in the CallFrame.Values
      /// </summary>
      int GetIndex(JSSymbol symbol) { return symbol.Index + _symbolIndexOffset; }

      #region ILGen helpers
      public void Brfalse(int conditionIndex, int targetIndex)
      {
        Debug.Assert(_ilGen != null, "No IC method is open!");
        //if (!condition) goto targetIndex
        _ilGen.LoadValue(conditionIndex, mdr.ValueTypes.Boolean);
        var elseLabel = _ilGen.DefineLabel();
        _ilGen.Brtrue(elseLabel);
        _ilGen.Ldc_I4(targetIndex);
        _ilGen.Ret();
        _ilGen.MarkLabel(elseLabel);
      }

      public void Brtrue(int conditionIndex, int targetIndex)
      {
        Debug.Assert(_ilGen != null, "No IC method is open!");
        //if (condition) goto targetIndex
        _ilGen.LoadValue(conditionIndex, mdr.ValueTypes.Boolean);
        var elseLabel = _ilGen.DefineLabel();
        _ilGen.Brfalse(elseLabel);
        _ilGen.Ldc_I4(targetIndex);
        _ilGen.Ret();
        _ilGen.MarkLabel(elseLabel);
      }

      public void Br(int targetIndex)
      {
        Debug.Assert(_ilGen != null, "No IC method is open!");
        _ilGen.Ldc_I4(targetIndex);
        _ilGen.Ret();
      }

      /// <summary>
      /// We asume something is already on the CLR stack, and _result is assigned
      /// We then push it on the stack
      /// </summary>
      void StackPush()
      {
        Debug.Assert(_ilGen != null, "No IC method is open!");
        _ilGen.StoreValue(_stackModel.StackPointer, _result.ValueType);

        _result.ValueType = mdr.ValueTypes.DValueRef;
        _stackModel.Push(1);
      }


      /// <summary>
      /// load stack top without changing the stack pointer
      /// </summary>
      public void StackTop(mdr.ValueTypes valueType = mdr.ValueTypes.DValueRef)
      {
        Debug.Assert(_ilGen != null, "No IC method is open!");
        _ilGen.LoadValue(_stackModel.StackPointer - 1, valueType);
        _result.ValueType = valueType;
      }

      /// <summary>
      /// Decrement the stack pointer and load the top value
      /// </summary>
      public void StackPop(mdr.ValueTypes valueType = mdr.ValueTypes.DValueRef)
      {
        StackTop(valueType);
        _stackModel.Pop(1);
      }

      #endregion

      protected override void ExecuteInitialize()
      {
        base.ExecuteInitialize();
        _stackModel = new StackModel();
        _ics = new List<ICInfo>();
        _writeTemporaries = new Dictionary<WriteTemporaryExpression, WriteTemporaryInfo>();
        _initMethodIndex = ReserveNewICIndex();
      }

      protected override void ExecuteFinalize()
      {
        var valueIndex = _stackModel.MaxStackDepth + 1;
        foreach (var wtInfo in _writeTemporaries)
        {
          //wtInfo.Key.ValueIndex = valueIndex;
          BeginICMethod(wtInfo.Key);
          _ilGen.LoadValue(wtInfo.Value.SourceValueIndex);
          _ilGen.StoreValue(valueIndex, mdr.ValueTypes.DValueRef);
          EndICMethod(wtInfo.Value.WriterICIndex);

          var readers = wtInfo.Value.Readers;
          for (var i = readers.Count - 1; i >= 0; --i)
          {
            BeginICMethod(wtInfo.Key);
            _ilGen.LoadValue(valueIndex);
            _ilGen.StoreValue(readers[i].DestinationValueIndex, mdr.ValueTypes.DValueRef);
            EndICMethod(readers[i].ReaderICIndex);
          }
          ++valueIndex;
        }

        BeginICMethod("Init");
        _ilGen.Ldarg_CallFrame();
        _ilGen.Ldc_I4(valueIndex);
        _ilGen.NewArr(Types.DValue.TypeOf);
        _ilGen.Stfld(Types.CallFrame.Values);
        EndICMethod(_initMethodIndex);


        _currFuncMetadata.InlineCache = new mdr.DFunctionMetadata.InlineCachedMethod[_ics.Count];
        for (var i = 0; i < _ics.Count; ++i)
        {
          var icInfo = _ics[i];
          if (icInfo.JitTask != null)
            icInfo.JitTask.Wait();
          Debug.Assert(icInfo.ICMethod != null, "IC Method cannot be null");
          _currFuncMetadata.InlineCache[i] = icInfo.ICMethod;
        }

        // We store all the information about the context.values here
        _currFuncMetadata.TemporariesStartIndex = _stackModel.MaxStackDepth + 1;
        _currFuncMetadata.ValuesLength = valueIndex;

        _ics = null; //Release everything now
        _writeTemporaries = null;
        _labelInfos.Clear();
        base.ExecuteFinalize();
      }

      protected override void PrepareContext()
      {
        BeginICMethod("PrepareContext");
        _ilGen.Ldarg_CallFrame();
        base.PrepareContext();
        _ilGen.Call(Types.Operations.ICMethods.SetContext);
        EndICMethod();
        _stackModel.Push(1); //Context
      }

      protected override void DeclareParameterSymbols()
      {
        var scope = _currFuncMetadata.Scope;
        var parameters = _currFuncMetadata.FunctionIR.Parameters;

        BeginICMethod("DeclareParameterSymbols");

        //Extend missing arguments
        _ilGen.Ldarg_CallFrame();
        _ilGen.Ldc_I4(parameters.Count);
        _ilGen.Call(Types.CallFrame.SetExpectedArgsCount);

        if (scope.HasArgumentsSymbol)
        {
          ///In this case, we already read/write from the arguments.Elements[i] for the symbol. 
          ///however, we should always refer to the arguments variable itself since its .Elements 
          ///may be resized and changed
          ///we cannot rely on the argument symbol's variable for this since the progremmer
          ///may write the arguments symbol and we can lose the value, so we keep our own separately

          JSSymbol argumentsSymbol = null;
          var symbols = scope.Symbols;
          for (var i = parameters.Count; i < symbols.Count; ++i)
          {
            var symbol = symbols[i];
            if (symbol.SymbolType == JSSymbol.SymbolTypes.Arguments)
            {
              argumentsSymbol = symbol;
              break;
            }
          }

          _ilGen.Ldarg_CallFrame();
          _ilGen.Ldc_I4(GetIndex(argumentsSymbol));
          _ilGen.Call(Types.Operations.ICMethods.CreateArgumentsObject);

          _stackModel.Push(1); //Arguments
        }
        else
        {
          //_ilGen.Ldarg_CallFrame();
          //_ilGen.Ldnull();
          //_ilGen.Call(Types.Operations.ICMethods.SetArguments);
          _stackModel.Push(1); //Arguments

          for (var i = parameters.Count - 1; i >= 0; --i)
          {
            var symbol = parameters[i].Symbol;
            Debug.Assert(symbol.ParameterIndex == i, "Invalid situation!, symbol {0} should be paramter with parameter index {1} instead of {2}", symbol.Name, symbol.ParameterIndex, symbol.ParameterIndex);

            if (symbol.SymbolType == JSSymbol.SymbolTypes.ClosedOnLocal)
            {
              ///In this case, we have to copy the argument to the context and then
              ///call the PD.Getter/PD.Setter to access the actual argument
              symbol.AssignFieldId();

              var argNotPassed = _ilGen.DefineLabel();
              Ldarg_CallFrame();
              _ilGen.Ldfld(Types.CallFrame.PassedArgsCount);
              _ilGen.Ldc_I4(symbol.ParameterIndex);
              _ilGen.Ble(argNotPassed);
              _ilGen.Ldarg_CallFrame();
              _ilGen.Ldc_I4(GetIndex(symbol));
              _ilGen.Ldc_I4(symbol.FieldId);
              Ldarg_Parameter(symbol.ValueIndex);
              _ilGen.Call(Types.Operations.ICMethods.WriteValueToContext);
              _ilGen.MarkLabel(argNotPassed);


            }
          }
        }

        EndICMethod();
      }

      protected override void DeclareSymbols(Scope scope)
      {
        Debug.Assert(scope == _currFuncMetadata.Scope, "Invalid situlation!");
        _symbolIndexOffset = _stackModel.StackPointer;
        //We do these on demand!
        _stackModel.Push(scope.Symbols.Count); //TODO: only allocate space for certain symbols
        foreach (var symbol in scope.Symbols)
        {
          switch (symbol.SymbolType)
          {
            case JSSymbol.SymbolTypes.ClosedOnLocal:
            case JSSymbol.SymbolTypes.ParentLocal:
            case JSSymbol.SymbolTypes.Global:
            case JSSymbol.SymbolTypes.Unknown:
              symbol.AssignFieldId();
              break;
          }
        }
      }

      #region AsX
      /// <summary>
      /// Function in this region convert the top of the stack to the corresponding type. 
      /// This is purely for CIL code generation and independent of JS semantics. 
      /// </summary>

      //void AsX(mdr.ValueTypes type)
      //{
      //  Debug.Assert(_result.ValueType == mdr.ValueTypes.DValueRef, "Make sure that the result type is DvalueRef before calling AsX.");

      //  switch (type)
      //  {
      //    case mdr.ValueTypes.Int32:
      //      _ilGen.Call(Types.DValue.AsInt32);
      //      break;
      //    case mdr.ValueTypes.Double:
      //      _ilGen.Call(Types.DValue.AsDouble);
      //      break;
      //    case mdr.ValueTypes.Boolean:
      //      _ilGen.Call(Types.DValue.AsBoolean);
      //      break;
      //    case mdr.ValueTypes.String:
      //      _ilGen.Call(Types.DValue.AsString);
      //      break;
      //    case mdr.ValueTypes.UInt16:
      //      _ilGen.Call(Types.DValue.AsUInt16);
      //      break;
      //    case mdr.ValueTypes.UInt32:
      //      _ilGen.Call(Types.DValue.AsUInt32);
      //      break;
      //    case mdr.ValueTypes.Object:
      //      _ilGen.Call(Types.DValue.AsDObject);
      //      break;
      //    case mdr.ValueTypes.Array:
      //      _ilGen.Call(Types.DValue.AsDArray);
      //      break;
      //    case mdr.ValueTypes.Char:
      //      _ilGen.Call(Types.DValue.AsChar);
      //      break;
      //    case mdr.ValueTypes.Float:
      //      _ilGen.Call(Types.DValue.AsFloat);
      //      break;
      //    case mdr.ValueTypes.Function:
      //      _ilGen.Call(Types.DValue.AsDFunction);
      //      break;
      //    case mdr.ValueTypes.Int16:
      //      _ilGen.Call(Types.DValue.AsInt16);
      //      break;
      //    case mdr.ValueTypes.Int64:
      //      _ilGen.Call(Types.DValue.AsInt64);
      //      break;
      //    case mdr.ValueTypes.Int8:
      //      _ilGen.Call(Types.DValue.AsInt8);
      //      break;
      //    case mdr.ValueTypes.UInt64:
      //      _ilGen.Call(Types.DValue.AsUInt64);
      //      break;
      //    case mdr.ValueTypes.UInt8:
      //      _ilGen.Call(Types.DValue.AsUInt8);
      //      break;
      //    default:
      //      Trace.Fail("Not implemented!");
      //      break;
      //  }

      //  _result.ValueType = type;
      //}

      protected override void AsVoid()
      {
        _stackModel.Pop(1);
      }

      //      void AsPrimitive()  //AsDValue()
      //      {
      //        //var type = _resultType;
      //        //if (type == mdr.ValueTypes.DValue)
      //        //  return;

      //        //_ilGen.Call(Types.DValue.Create(type));
      //        //_resultType = mdr.ValueTypes.DValue;
      //        throw new NotImplementedException();
      //      }

      //      void AsBoolean()
      //      {
      //        var type = _result.ValueType;
      //        if (type == mdr.ValueTypes.Boolean)
      //          return;

      //        switch (type)
      //        {
      //          //case mdr.ValueTypes.Undefined:
      //          case mdr.ValueTypes.String:
      //            _ilGen.Call(Types.Operations.Convert.ToBoolean.Get(mdr.ValueTypes.String));
      //            break;
      //          case mdr.ValueTypes.Double:
      //            _ilGen.Conv_I4();
      //            break;
      //          case mdr.ValueTypes.Int32:
      //            break;
      //          case mdr.ValueTypes.Boolean:
      //            break;
      //          //case mdr.ValueTypes.Null:
      //          case mdr.ValueTypes.Object:
      //          case mdr.ValueTypes.Function:
      //          case mdr.ValueTypes.Array:
      //          case mdr.ValueTypes.Property:
      //            _ilGen.Callvirt(Types.DObject.ToBoolean);
      //            break;
      //          case mdr.ValueTypes.DValueRef:
      //            _ilGen.Call(Types.DValue.AsBoolean);
      //            break;
      //          default:
      //            Trace.Fail("Cannot convert type {0} to boolean", type);
      //            break;
      //        }
      //#if DEBUG
      //        //This is to help the CSharpILGenerator produce valid C# code
      //        if (type == mdr.ValueTypes.Double || type == mdr.ValueTypes.Int32)
      //        {
      //          _ilGen.Ldc_I4_0();
      //          _ilGen.Ceq();
      //          _ilGen.Ldc_I4(false);
      //          _ilGen.Ceq();
      //        }
      //#endif
      //        _result.ValueType = mdr.ValueTypes.Boolean;
      //      }
      //      protected override void AsBoolean()
      //      {
      //        LoadStackPop();
      //        _result.ValueType = mdr.ValueTypes.DValueRef;
      //        base.AsBoolean();
      //      }

      //      void AsInt32()
      //      {
      //        var type = _result.ValueType;
      //        switch (type)
      //        {
      //          //case mdr.ValueTypes.Undefined:
      //          case mdr.ValueTypes.String:
      //            _ilGen.Call(Types.Operations.Convert.ToInt32.Get(type));
      //            break;
      //          case mdr.ValueTypes.Double:
      //            _ilGen.Conv_I4();
      //            break;
      //          case mdr.ValueTypes.Int32:
      //            break;
      //          case mdr.ValueTypes.Boolean:
      //            break;
      //          //case mdr.ValueTypes.Null:
      //          case mdr.ValueTypes.Object:
      //          case mdr.ValueTypes.Function:
      //          case mdr.ValueTypes.Array:
      //          case mdr.ValueTypes.Property:
      //            _ilGen.Callvirt(Types.DObject.ToInt32);
      //            break;
      //          case mdr.ValueTypes.DValueRef:
      //            _ilGen.Call(Types.DValue.AsInt32);
      //            break;
      //          default:
      //            Trace.Fail("Cannot convert type {0} to int", type);
      //            break;
      //        }
      //        _result.ValueType = mdr.ValueTypes.Int32;
      //      }

      //      void AsUInt32() { throw new NotImplementedException(); }
      //      protected override void AsUInt32()
      //      {
      //        LoadStackPop();
      //        _result.ValueType = mdr.ValueTypes.DValueRef;
      //        base.AsUInt32();
      //      }

      //      void AsUInt16() { throw new NotImplementedException(); }

      //      void AsDouble()
      //      {
      //        var type = _result.ValueType;
      //        switch (type)
      //        {
      //          //case mdr.ValueTypes.Undefined:
      //          case mdr.ValueTypes.String:
      //            _ilGen.Call(Types.Operations.Convert.ToDouble.Get(type));
      //            break;
      //          case mdr.ValueTypes.Double:
      //            break;
      //          case mdr.ValueTypes.Int32:
      //            _ilGen.Conv_R8();
      //            break;
      //          case mdr.ValueTypes.Boolean:
      //            _ilGen.Conv_R8();
      //            break;
      //          //case mdr.ValueTypes.Null:
      //          case mdr.ValueTypes.Object:
      //          case mdr.ValueTypes.Function:
      //          case mdr.ValueTypes.Array:
      //          case mdr.ValueTypes.Property:
      //            _ilGen.Callvirt(Types.DObject.ToDouble);
      //            break;
      //          case mdr.ValueTypes.DValueRef:
      //            _ilGen.Call(Types.DValue.AsDouble);
      //            break;
      //          default:
      //            Trace.Fail("Cannot convert type {0} to double", type);
      //            break;
      //        }
      //        _result.ValueType = mdr.ValueTypes.Double;
      //      }
      //      protected override void AsDouble()
      //      {
      //        LoadStackPop();
      //        _result.ValueType = mdr.ValueTypes.DValueRef;
      //        base.AsDouble();
      //      }

      //      void AsString()
      //      {
      //        var type = _result.ValueType;
      //        switch (type)
      //        {
      //          //case mdr.ValueTypes.Undefined:
      //          case mdr.ValueTypes.String:
      //            break;
      //          case mdr.ValueTypes.Double:
      //          case mdr.ValueTypes.Int32:
      //          case mdr.ValueTypes.Boolean:
      //            _ilGen.Call(Types.Operations.Convert.ToString.Get(type));
      //            break;
      //          //case mdr.ValueTypes.Null:
      //          case mdr.ValueTypes.Object:
      //          case mdr.ValueTypes.Function:
      //          case mdr.ValueTypes.Array:
      //          case mdr.ValueTypes.Property:
      //            _ilGen.Callvirt(Types.DObject.ToString);
      //            break;
      //          case mdr.ValueTypes.DValueRef:
      //            _ilGen.Call(Types.DValue.AsString);
      //            break;
      //          default:
      //            Trace.Fail("Cannot convert type {0} to string", type);
      //            break;
      //        }
      //        _result.ValueType = mdr.ValueTypes.String;
      //      }
      //      protected override void AsString()
      //      {
      //        LoadStackPop();
      //        _result.ValueType = mdr.ValueTypes.DValueRef;
      //        base.AsString();
      //      }

      //      void AsDObject()
      //      {
      //        var type = _result.ValueType;
      //        switch (type)
      //        {
      //          case mdr.ValueTypes.Object:
      //          case mdr.ValueTypes.Property:
      //          case mdr.ValueTypes.Function:
      //          case mdr.ValueTypes.Array:
      //            break;
      //          case mdr.ValueTypes.DValueRef:
      //            _ilGen.Call(Types.DValue.AsDObject);
      //            _result.ValueType = mdr.ValueTypes.Object;
      //            break;
      //          default:
      //            Trace.Fail("Cannot convert type {0} to DObject", type);
      //            break;
      //        }
      //      }
      //      protected override void AsDObject()
      //      {
      //        LoadStackPop();
      //        _result.ValueType = mdr.ValueTypes.DValueRef;
      //        base.AsDObject();
      //      }

      //      void AsDArray()
      //      {
      //        var type = _result.ValueType;
      //        switch (type)
      //        {
      //          case mdr.ValueTypes.Array:
      //            break;
      //          case mdr.ValueTypes.DValueRef:
      //            _ilGen.Call(Types.DValue.AsDArray);
      //            _result.ValueType = mdr.ValueTypes.Array;
      //            break;
      //          default:
      //            Trace.Fail("Cannot convert type {0} to DArray", type);
      //            break;
      //        }
      //      }

      //      void AsObject(Type t = null)
      //      {
      //        var type = _result.ValueType;
      //        switch (type)
      //        {
      //          case mdr.ValueTypes.Any:
      //            break;
      //          case mdr.ValueTypes.DValueRef:
      //            _ilGen.Call(Types.DValue.AsObject);
      //            break;
      //          default:
      //            Trace.Fail("Cannot convert type {0} to DObject", type);
      //            break;
      //        }
      //        if (t != null)
      //        {
      //          _ilGen.Castclass(t);
      //          _result.Type = t;
      //        }
      //        else
      //          _result.ValueType = mdr.ValueTypes.Object;
      //      }
      //      protected override void AsObject(Type t = null)
      //      {
      //        LoadStackPop();
      //        _result.ValueType = mdr.ValueTypes.DValueRef;
      //        base.AsObject(t);
      //      }
      #endregion

      #region Statements; ECMA 12. -------------------------------------------------------------------------------------

      public override void Visit(BlockStatement node)
      {
        var currSP = _stackModel.StackPointer;
        base.Visit(node);
        Debug.Assert(currSP == _stackModel.StackPointer, new JSSourceLocation(_currFuncMetadata, node), "Stack depth at begining ({0}) and end ({1}) of block do not match!", currSP, _stackModel.StackPointer);
      }

      public override void Visit(IfStatement node)
      {
        PushLocation(node);

        VisitNode(node.Condition);
        _stackModel.Pop(1);
        var conditionIndex = _stackModel.StackPointer;
        var gotoElseIndex = ReserveNewICIndex();
        VisitNode(node.Then);

        int elseIndex;
        if (node.Else != null)
        {
          var gotoEndIndex = ReserveNewICIndex();

          elseIndex = gotoEndIndex + 1;

          VisitNode(node.Else);

          var endIndex = GetCurrentICIndex() + 1;
          BeginICMethod("GotoEnd");
          Br(endIndex);
          EndICMethod(gotoEndIndex);
        }
        else
          elseIndex = GetCurrentICIndex() + 1;

        BeginICMethod("GotoElse");
        Brfalse(conditionIndex, elseIndex);
        EndICMethod(gotoElseIndex);

        PopLocation();
      }

      void UpdateJumps(int targetIndex, LinkedList<int> jumpIndexes)
      {
        foreach (var jumpIndex in jumpIndexes)
        {
          BeginICMethod("Goto_" + targetIndex.ToString());
          Br(targetIndex);
          EndICMethod(jumpIndex);
        }
      }

      protected override void Loop(LoopStatement loop, Statement initilization, Expression condition, Expression increment, Statement body, bool isDoWhile)
      {
        PushLocation(loop);

        VisitNode(initilization);

        ++_loopNestLevel;

        var breakIndexes = new LinkedList<int>();
        var continueIndexes = new LinkedList<int>();
        _labelInfos.PushLoop(loop, breakIndexes, continueIndexes);

        var gotoLoopCheckIndex = -1;
        if (!isDoWhile)
          gotoLoopCheckIndex = ReserveNewICIndex(); //Reserver a place and will generate the goto later

        var loopBeginIndex = GetCurrentICIndex() + 1;
        VisitNode(body);

        var loopIncrementIndex = GetCurrentICIndex() + 1;
        UpdateJumps(loopIncrementIndex, continueIndexes);
        if (increment != null)
        {
          VisitNode(increment);
          AsVoid();
        }

        if (gotoLoopCheckIndex != -1)
        {
          BeginICMethod("GotoLoopCheck");
          Br(GetCurrentICIndex() + 1);
          EndICMethod(gotoLoopCheckIndex);
        }

        if (condition != null)
        {
          VisitNode(condition);
          _stackModel.Pop(1);
          BeginICMethod("GotoLoopBegin");
          Brtrue(_stackModel.StackPointer, loopBeginIndex);
          EndICMethod();
        }
        else
        {
          BeginICMethod("GotoLoopBegin");
          Br(loopBeginIndex);
          EndICMethod();
        }

        UpdateJumps(GetCurrentICIndex() + 1, breakIndexes);

        --_loopNestLevel;

        _labelInfos.PopLoop(loop);

        PopLocation();
      }

      public override void Visit(LabelStatement node)
      {
        //Note: in JS, the label is for after the statements!
        var breakIndexes = new LinkedList<int>();
        _labelInfos.PushLabel(node, breakIndexes);
        VisitNode(node.Target);

        UpdateJumps(GetCurrentICIndex() + 1, breakIndexes);

        _labelInfos.PopLabel(node);
      }

      protected override void Jump(GotoStatement node, bool isContinue)
      {
        var labelInfo = _labelInfos.GetLabelInfo(node.Target);
        string error = null;
        if (labelInfo == null)
        {
          if (node.Target == null)
            if (isContinue)
              error = "SyntaxError: Illegal continue statement";
            else
              error = "SyntaxError: Illegal break statement";
          else
            error = string.Format("SyntaxError: Undefined label '{0}'", node.Target);
        }
        else
          if (isContinue && !labelInfo.HasContinueTarget)
            error = string.Format("SyntaxError: Undefined label '{0}'", node.Target);

        if (error != null)
        {
          var tmpException = new ThrowStatement(new StringLiteral(error));
          VisitNode(tmpException); //We call visit here to have the proper override of it get called.
          //TODO: var ErrorCall = new InternalCall(CodeGen.Types.Operations.Error.SemanticError, null);
        }
        else
        {
          //We reserve one slot in the _ics and later back fill these jumps. 
          (isContinue ? labelInfo.ContinueTarget : labelInfo.BreakTarget).AddLast(ReserveNewICIndex());
        }
      }

      public override void Visit(ReturnStatement node)
      {
        PushLocation(node);
        if (node.Expression != null)
        {
          VisitNode(node.Expression);
        }

        BeginICMethod("Return");
        if (node.Expression != null)
        {
          _stackModel.Pop(1);

          _ilGen.Ldarg_CallFrame();
          _ilGen.Ldc_I4(_stackModel.StackPointer);
          _ilGen.Call(Types.Operations.ICMethods.Return);
        }

        Br(int.MaxValue);
        EndICMethod();

        PopLocation();
      }

      public override void Visit(SwitchStatement node)
      {
        PushLocation(node);

        var breakIndexes = new LinkedList<int>();
        _labelInfos.PushLabel(null, breakIndexes);

        ///We know all case.Compare expressions should only generate one boolean value, except the the default: case
        var comparisionResultPointer = _stackModel.StackPointer;

        var caseCompareIndexes = new int[node.CaseClauses.Count]; // We will fill those slots after we know the begining index of blocks
        var defaultTargetIndex = -1;


        for (var caseIndex = 0; caseIndex < node.CaseClauses.Count; ++caseIndex)
        {
          var c = node.CaseClauses[caseIndex];

          if (!c.IsDefault)
          {
            VisitNode(c.Comparison);
            Debug.Assert(_stackModel.StackPointer == comparisionResultPointer + 1, "Invalid situation, stack state did not change correctly in switch statement");

            ///We will later generate the BrTrue IC here which will Pop the comparision result
            ///but now we should set the StackPointer correctly for the next CaseClause to visit
            _stackModel.Pop(1);
          }
          else
          {
            defaultTargetIndex = caseIndex;
          }
          caseCompareIndexes[caseIndex] = ReserveNewICIndex();
        }
        Debug.Assert(_stackModel.StackPointer == comparisionResultPointer, "Invalid stack pointer value! Expected {0} instead of {1}", comparisionResultPointer, _stackModel.StackPointer);


        for (var caseIndex = 0; caseIndex < node.CaseClauses.Count; ++caseIndex)
        {
          var c = node.CaseClauses[caseIndex];
          BeginICMethod(c);
          if (c.IsDefault)
          {
            Debug.Assert(caseIndex == defaultTargetIndex, "Invalid situation");
            Br(GetCurrentICIndex() + 1);
          }
          else
            Brtrue(comparisionResultPointer, GetCurrentICIndex() + 1);
          EndICMethod(caseCompareIndexes[caseIndex]);

          VisitNodes(c.Body.Statements);
        }

        UpdateJumps(GetCurrentICIndex() + 1, breakIndexes);

        _labelInfos.PopLabel(null);

        PopLocation();
      }

      public override void Visit(ThrowStatement node)
      {
        PushLocation(node);

        VisitNode(node.Expression);

        BeginICMethod(node);
        _ilGen.LoadValue(_stackModel.StackPointer - 1);
        _ilGen.Call(Types.JSException.Throw.Get(mdr.ValueTypes.DValueRef));
        EndICMethod();

        _stackModel.Pop(1);

        PopLocation();
      }

      public override void Visit(TryStatement node)
      {
        ///In case we ever use a StackPointer variable that is changed at runtime, we have to consider the following:
        ///we may have an exception anywhere in the try block
        ///so stack may be at its max when entering the catch block
        ///one option is to clear up the stack before starting (we chose this one)
        ///the other optios is to make sure there is enough room on the stack

        PushLocation(node);

        var tryICIndex = ReserveNewICIndex();

        var tryBeginIndex = GetCurrentICIndex() + 1;
        VisitNode(node.Statement);
        var tryEndIndex = GetCurrentICIndex();

        var exceptionVariableIndex = -1;
        var catchBeginIndex = int.MaxValue;
        var catchEndIndex = int.MinValue;
        if (node.Catch != null)
        {
          var symbol = node.Catch.Identifier.Symbol;
          Debug.Assert(symbol != null, new JSSourceLocation(_currFuncMetadata, node.Catch), string.Format("Cannot find symbol for identifier {0} in catch clause", node.Catch.Identifier));

          var originalSymbolType = symbol.SymbolType;
          symbol.SymbolType = JSSymbol.SymbolTypes.Local; //This symbols is defined only in the catch scope.

          exceptionVariableIndex = GetIndex(symbol);

          catchBeginIndex = GetCurrentICIndex() + 1;
          VisitNode(node.Catch);
          catchEndIndex = GetCurrentICIndex();

          symbol.SymbolType = originalSymbolType;
        }

        var finallyBeginIndex = int.MaxValue;
        var finallyEndIndex = int.MinValue;
        if (node.Finally != null)
        {
          finallyBeginIndex = GetCurrentICIndex() + 1;
          VisitNode(node.Finally);
          finallyEndIndex = GetCurrentICIndex();
        }

        BeginICMethod(node);
        _ilGen.Ldarg_CallFrame();
        _ilGen.Ldc_I4(tryBeginIndex);
        _ilGen.Ldc_I4(tryEndIndex);
        _ilGen.Ldc_I4(catchBeginIndex);
        _ilGen.Ldc_I4(catchEndIndex);
        _ilGen.Ldc_I4(finallyBeginIndex);
        _ilGen.Ldc_I4(finallyEndIndex);
        _ilGen.Ldc_I4(exceptionVariableIndex);
        _ilGen.Call(Types.Operations.ICMethods.TryCatchFinally);
        EndICMethod(tryICIndex, GetCurrentICIndex() + 1);

        PopLocation();
      }
     
      public override void Visit(CatchClause node)
      {
        VisitNode(node.Statement);
      }
      
      public override void Visit(FinallyClause node)
      {
        VisitNode(node.Statement);
      }

      #endregion

      #region Primary Expressions; ECMA 11.1 -------------------------------------------------------------------------------------

      public override void Visit(ThisLiteral node)
      {
        BeginICMethod(node);
        base.Visit(node);
        StackPush();
        EndICMethod();
      }

      public override void Visit(NullLiteral node)
      {
        BeginICMethod(node);
        base.Visit(node);
        StackPush();
        EndICMethod();
      }

      public override void Visit(BooleanLiteral node)
      {
        BeginICMethod(node);
        base.Visit(node);
        StackPush();
        EndICMethod();
      }

      public override void Visit(IntLiteral node)
      {
        BeginICMethod(node);
        base.Visit(node);
        StackPush();
        EndICMethod();
      }

      public override void Visit(DoubleLiteral node)
      {
        BeginICMethod(node);
        base.Visit(node);
        StackPush();
        EndICMethod();
      }

      public override void Visit(StringLiteral node)
      {
        BeginICMethod(node);
        base.Visit(node);
        StackPush();
        EndICMethod();
      }

      public override void Visit(RegexpLiteral node)
      {
        BeginICMethod(node);
        base.Visit(node);
        StackPush();
        EndICMethod();
      }

      public override void Visit(ArrayLiteral node)
      {
        PushLocation(node);
        VisitNodes(node.Items);

        BeginICMethod(node);
        var arraySize = node.Items.Count;
        _stackModel.Pop(arraySize);
        _ilGen.Ldarg_CallFrame();
        _ilGen.Ldc_I4(_stackModel.StackPointer);
        _ilGen.Ldc_I4(arraySize);
        _ilGen.Call(Types.Operations.ICMethods.CreateArray);
        EndICMethod();

        _result.ValueType = mdr.ValueTypes.DValueRef;
        _stackModel.Push(1);

        PopLocation();
      }

      public override void Visit(ObjectLiteral node)
      {
        PushLocation(node);

        var icMethod = BeginICMethod(node, updateIlGen: false);
        foreach (var v in node.Properties)
        {
          v.AssignFieldId();
          icMethod.Ldc_I4(v.FieldId);
          icMethod.StoreValue(_stackModel.StackPointer, mdr.ValueTypes.Int32);
          _stackModel.Push(1);

          VisitNode(v.Expression);
        }

        var itemsCount = node.Properties.Count;
        _stackModel.Pop(itemsCount * 2);
        icMethod.Ldarg_CallFrame();
        icMethod.Ldc_I4(_stackModel.StackPointer);
        icMethod.Ldc_I4(itemsCount);
        icMethod.Call(Types.Operations.ICMethods.CreateObject);
        EndICMethod(icMethod);

        _result.ValueType = mdr.ValueTypes.DValueRef;
        _stackModel.Push(1);

        PopLocation();
      }

      public override void Visit(PropertyAssignment node)
      {
        throw new NotImplementedException();
      }


      public override void Visit(ReadIdentifierExpression node)
      {
        PushLocation(node);
        var symbol = node.Symbol;

        BeginICMethod(node);
        switch (symbol.SymbolType)
        {
          case JSSymbol.SymbolTypes.Local:
          case JSSymbol.SymbolTypes.HiddenLocal:
          case JSSymbol.SymbolTypes.Arguments:
            if (symbol.IsParameter)
            {
              if (_currFuncMetadata.Scope.HasArgumentsSymbol)
              {
                _ilGen.Call(Types.Operations.ICMethods.GetArguments);
                _ilGen.Ldfld(Types.DArray.Elements);
                _ilGen.Ldc_I4(symbol.ParameterIndex);
                _ilGen.Ldelema(Types.DValue.TypeOf);
              }
              else
              {
                Ldarg_Parameter(symbol.ParameterIndex);
              }
            }
            else
            {
              _ilGen.LoadValue(GetIndex(symbol));
            }
            StackPush();
            break;
          case JSSymbol.SymbolTypes.ClosedOnLocal:
          case JSSymbol.SymbolTypes.ParentLocal:
          case JSSymbol.SymbolTypes.Global:
          case JSSymbol.SymbolTypes.Unknown:
            _ilGen.Ldarg_CallFrame();
            _ilGen.Ldc_I4(_stackModel.StackPointer);
            _ilGen.Ldc_I4(GetIndex(symbol));
            _ilGen.Ldc_I4(symbol.FieldId);
            _ilGen.Call(Types.Operations.ICMethods.ReadFromContext);

            _result.ValueType = mdr.ValueTypes.DValueRef;
            _stackModel.Push(1);
            break;
          default:
            Trace.Fail("cannot process symbol type {0} in {1}", symbol.SymbolType, _currFuncMetadata.FullName);
            break;
        }
        EndICMethod();

        PopLocation();
      }

      public override void Visit(ReadIndexerExpression node)
      {
        PushLocation(node);

        VisitNode(node.Container);
        VisitNode(node.Index);

        _stackModel.Pop(2);

        BeginICMethod(node);
        _ilGen.Ldarg_CallFrame();
        _ilGen.Ldc_I4(_stackModel.StackPointer);
        _ilGen.Call(Types.Operations.ICMethods.ReadIndexer);
        EndICMethod();

        _stackModel.Push(1);

        PopLocation();
      }

      public override void Visit(ReadPropertyExpression node)
      {
        PushLocation(node);

        VisitNode(node.Container);
        node.AssignFieldId();

        _stackModel.Pop(1);

        BeginICMethod(node);
        _ilGen.Ldarg_CallFrame();
        _ilGen.Ldc_I4(_stackModel.StackPointer);
        _ilGen.Ldc_I4(node.FieldId);
        _ilGen.Call(Types.Operations.ICMethods.ReadProperty);
        EndICMethod();

        _stackModel.Push(1);

        PopLocation();
      }

      #endregion

      protected override void Visit(UnaryExpression node)
      {
        PushLocation(node);

        VisitNode(node.Expression);
        var operandType = _result.ValueType;

        _stackModel.Pop(1);

        BeginICMethod(node);
        Operations.ICMethods.CreateUnaryOperationIC(_ilGen, node.NodeType, _stackModel.StackPointer, operandType, true);
        EndICMethod();

        _result.ValueType = mdr.ValueTypes.DValueRef;
        _stackModel.Push(1);

        PopLocation();
      }

      public override void Visit(DeleteExpression node)
      {
        throw new NotImplementedException();
      }

      protected override void Visit(BinaryExpression node)
      {
        PushLocation(node);

        VisitNode(node.Left);
        var leftType = _result.ValueType;
        VisitNode(node.Right);
        var rightType = _result.ValueType;

        _stackModel.Pop(2);

        BeginICMethod(node);
        Operations.ICMethods.CreateBinaryOperationIC(_ilGen, node.NodeType, _stackModel.StackPointer, leftType, rightType, true, true);
        EndICMethod();

        _result.ValueType = mdr.ValueTypes.DValueRef;
        _stackModel.Push(1);

        PopLocation();
      }

      #region Conditional Operators; ECMA 11.12 -------------------------------------------------------------------------------------

      public override void Visit(TernaryExpression node)
      {
        PushLocation(node);

        VisitNode(node.Left);
        _stackModel.Pop(1);
        var conditionIndex = _stackModel.StackPointer;

        var gotoRightIndex = ReserveNewICIndex();

        VisitNode(node.Middle);
        _stackModel.Pop(1);
        var gotoEndIndex = ReserveNewICIndex();

        BeginICMethod(node);
        Brfalse(conditionIndex, GetCurrentICIndex() + 1);
        EndICMethod(gotoRightIndex);

        VisitNode(node.Right);

        BeginICMethod(node);
        Br(GetCurrentICIndex() + 1);
        EndICMethod(gotoEndIndex);

        PopLocation();
      }
      
      #endregion 

      #region Assignment; ECMA 11.13 -------------------------------------------------------------------------------------

      public override void Visit(WriteTemporaryExpression node)
      {
        Debug.Assert(node.Users.Count > 1, "Invalid situation, temporary must have more than one user!");

        WriteTemporaryInfo info;
        if (_writeTemporaries.TryGetValue(node, out info))
        {
          //this was already visited
          info.Readers.Add(new WriteTemporaryInfo.ReaderInfo()
          {
            ReaderICIndex = ReserveNewICIndex(),
            DestinationValueIndex = _stackModel.StackPointer,
          });
          _stackModel.Push(1);
        }
        else
        {
          //First visit
          VisitNode(node.Value);
          info = new WriteTemporaryInfo()
          {
            WriterICIndex = ReserveNewICIndex(),
            SourceValueIndex = _stackModel.StackPointer - 1, //load the top item, but no pop or push
          };
          _writeTemporaries.Add(node, info);
        }
      }

      public override void Visit(WriteIdentifierExpression node)
      {
        PushLocation(node);
        var symbol = node.Symbol;

        VisitNode(node.Value);
        //We should not pop the results since there might be a user

        BeginICMethod(node);
        switch (symbol.SymbolType)
        {
          case JSSymbol.SymbolTypes.Local:
          case JSSymbol.SymbolTypes.HiddenLocal:
          case JSSymbol.SymbolTypes.Arguments:
            if (symbol.IsParameter)
            {
              if (_currFuncMetadata.Scope.HasArgumentsSymbol)
              {
                _ilGen.Call(Types.Operations.ICMethods.GetArguments);
                _ilGen.Ldfld(Types.DArray.Elements);
                _ilGen.Ldc_I4(symbol.ParameterIndex);
                _ilGen.Ldelema(Types.DValue.TypeOf);
              }
              else
              {
                Ldarg_Parameter(symbol.ParameterIndex);
              }
            }
            else
            {
              _ilGen.LoadValue(GetIndex(symbol));
            }
            StackTop();
            _ilGen.Call(Types.DValue.Set.Get(mdr.ValueTypes.DValueRef));
            break;
          case JSSymbol.SymbolTypes.ClosedOnLocal:
          case JSSymbol.SymbolTypes.ParentLocal:
          case JSSymbol.SymbolTypes.Global:
          case JSSymbol.SymbolTypes.Unknown:
            _ilGen.Ldarg_CallFrame();
            _ilGen.Ldc_I4(_stackModel.StackPointer - 1);
            _ilGen.Ldc_I4(GetIndex(symbol));
            _ilGen.Ldc_I4(symbol.FieldId);
            _ilGen.Call(Types.Operations.ICMethods.WriteToContext);
            break;
          default:
            Trace.Fail("cannot process symbol type {0} in {1}", symbol.SymbolType, _currFuncMetadata.FullName);
            break;
        }
        EndICMethod();

        PopLocation();
      }

      public override void Visit(WriteIndexerExpression node)
      {
        PushLocation(node);

        VisitNode(node.Container);
        VisitNode(node.Index);
        VisitNode(node.Value);

        _stackModel.Pop(3);

        BeginICMethod(node);
        _ilGen.Ldarg_CallFrame();
        _ilGen.Ldc_I4(_stackModel.StackPointer);
        _ilGen.Call(Types.Operations.ICMethods.WriteIndexer);
        EndICMethod();

        _stackModel.Push(1);

        PopLocation();
      }

      public override void Visit(WritePropertyExpression node)
      {
        PushLocation(node);

        VisitNode(node.Container);
        VisitNode(node.Value);
        node.AssignFieldId();

        _stackModel.Pop(2);

        BeginICMethod(node);
        _ilGen.Ldarg_CallFrame();
        _ilGen.Ldc_I4(_stackModel.StackPointer);
        _ilGen.Ldc_I4(node.FieldId);
        _ilGen.Call(Types.Operations.ICMethods.WriteProperty);
        EndICMethod();

        _stackModel.Push(1);

        PopLocation();
      }

      #endregion

      #region Function Calls; ECMA 11.2.2, 11.2.3 -------------------------------------------------------------------------------------

      public override void Visit(NewExpression node)
      {
        PushLocation(node);

        VisitNode(node.Function);
        VisitNodes(node.Arguments);

        _stackModel.Pop(1 + node.Arguments.Count);

        BeginICMethod(node);
        _ilGen.Ldarg_CallFrame();
        _ilGen.Ldc_I4(node.Arguments.Count);
        _ilGen.Ldc_I4(_stackModel.StackPointer);
        _ilGen.Call(Types.Operations.ICMethods.New);
        EndICMethod();

        _result.ValueType = mdr.ValueTypes.DValueRef;
        _stackModel.Push(1);

        PopLocation();
      }

      public override void Visit(CallExpression node)
      {
        PushLocation(node);

        VisitNode(node.Function);
        VisitNode(node.ThisArg);
        VisitNodes(node.Arguments);

        var hasThis = node.ThisArg != null;
        _stackModel.Pop(1 + (hasThis ? 1 : 0) + node.Arguments.Count);

        BeginICMethod(node);
        _ilGen.Ldarg_CallFrame();
        _ilGen.Ldc_I4(node.Arguments.Count);
        _ilGen.Ldc_I4(_stackModel.StackPointer);
        _ilGen.Ldc_I4(hasThis);
        _ilGen.Ldc_I4(node.IsDirectEvalCall);
        _ilGen.Call(Types.Operations.ICMethods.Call);
        EndICMethod();

        _result.ValueType = mdr.ValueTypes.DValueRef;
        _stackModel.Push(1);

        PopLocation();
      }

      #endregion

      #region Function Definition; ECMA 13 -------------------------------------------------------------------------------------

      public override void Visit(FunctionExpression node)
      {
        PushLocation(node);

        BeginICMethod(node);
        Ldarg_CallFrame();
        _ilGen.Ldc_I4(_stackModel.StackPointer);
        _ilGen.Ldc_I4(node.Metadata.FuncDefinitionIndex);
        _ilGen.Call(Types.Operations.ICMethods.CreateFunction);
        EndICMethod();

        _result.ValueType = mdr.ValueTypes.Function;
        _stackModel.Push(1);

        PopLocation();
      }

      #endregion

      #region Interanls

      void LoadArguments(InternalInvocation node, System.Reflection.MethodBase method, int resultIndex)
      {
        var parameters = method.GetParameters();

        var hasThisArg = !method.IsStatic && !method.IsConstructor;
        Debug.Assert(node.Arguments.Count == parameters.Length + (hasThisArg ? 1 : 0), "Arguments mismatch between node {0} and method {1} of {2}", node, method, method.DeclaringType);
        
        int i = 0;
        if (hasThisArg)
        {
          _ilGen.LoadValue(resultIndex++, Types.ValueTypeOf(method.DeclaringType));
          ++i;
        }

        for (; i < node.Arguments.Count; ++i)
          _ilGen.LoadValue(resultIndex++, Types.ValueTypeOf(parameters[i].ParameterType));
      }

      public override void Visit(InternalCall node)
      {
        PushLocation(node);

        var resultIndex = _stackModel.StackPointer;

        VisitNodes(node.Arguments);
        _stackModel.Pop(node.Arguments.Count);

        BeginICMethod(node);
        LoadArguments(node, node.Method, resultIndex);
        _ilGen.Call(node.Method);
        if (node.Method.ReturnType != null && node.Method.ReturnType != Types.ClrSys.Void)
        {
          _ilGen.StoreValue(resultIndex, Types.ValueTypeOf(node.Method.ReturnType));
          _stackModel.Push(1);
        }
        EndICMethod();

        PopLocation();
      }

      public override void Visit(InternalNew node)
      {
        PushLocation(node);

        var resultIndex = _stackModel.StackPointer;

        VisitNodes(node.Arguments);
        _stackModel.Pop(node.Arguments.Count);


        BeginICMethod(node);
        LoadArguments(node, node.Constructor, resultIndex);
        _ilGen.Newobj(node.Constructor);
        _ilGen.StoreValue(resultIndex, Types.ValueTypeOf(node.Constructor.DeclaringType));
        _stackModel.Push(1);
        EndICMethod();

        PopLocation();
      }
      #endregion

      public override void Visit(GuardedCast node)
      {
        //Update the index where the values need to be stored.
        node.ICIndex = GetCurrentICIndex() + 1;
        base.Visit(node);
      }
    }
  }
}

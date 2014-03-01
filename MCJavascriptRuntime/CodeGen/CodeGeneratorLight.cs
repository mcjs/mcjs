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
using System.Reflection.Emit;
using System.Reflection;

using mjr.IR;
using m.Util.Diagnose;

namespace mjr.CodeGen
{
  class CodeGeneratorLight : CodeGenerator
  {
    static AlgorithmImplementation _pool = new AlgorithmPool<CodeGeneratorLightImp>("JS/Jit/Light/", () => new CodeGeneratorLightImp(), () => JSRuntime.Instance.Configuration.ProfileJitTime);
    public new static void Execute(CodeGenerationInfo cgInfo) { _pool.Execute(cgInfo); }

    sealed class CodeGeneratorLightImp : CodeGeneratorImp
    {
      #region Stack related

      struct StackModel
      {
        int _maxStackDepth;
        public int MaxStackDepth { get { return _maxStackDepth; } }

        int _stackDepth;
        public int StackDepth { get { return _stackDepth; } }

        public void Push(int itemCount)
        {
          _stackDepth += itemCount;
          if (_stackDepth > _maxStackDepth)
            _maxStackDepth = _stackDepth;

        }
        public void Pop(int itemCount)
        {
          _stackDepth -= itemCount;
          Debug.Assert(_stackDepth >= 0, "Stack empty!");
        }
      }
      StackModel _stackModel;

      LocalBuilder _stack;

      void Call(MethodInfo mi, int popCount, int pushCount)
      {
        _ilGen.Ldloca(_stack);

#if DEBUG || DIAGNOSE
        if (JSRuntime.Instance.Configuration.EnableDiagOperation)
        {
          _ilGen.Ldstr(string.Format("Calling {0}.{1}", mi.DeclaringType.FullName, mi.Name));
          _ilGen.Ldc_I4_0();
          _ilGen.NewArr(Types.ClrSys.Object.TypeOf);
          _ilGen.Call(Types.Diagnose.Debug.WriteLine);
        }
#endif

        if (JSRuntime.Instance.Configuration.ProfileOpTime)
        {
          var callLengthTicks = _localVars.PushTemporary(Types.ClrSys.Int64);
          GenReadTicks(callLengthTicks);
          _ilGen.Call(mi);
          GenAccumulateTicks("op-time", mi.Name, callLengthTicks);
          _localVars.PopTemporary(callLengthTicks);
        }
        else
          _ilGen.Call(mi);
        if (JSRuntime.Instance.Configuration.ProfileOpFrequency)
          GenCounterIncrement("op-freq", mi.Name, 1);


        _stackModel.Pop(popCount);
        _stackModel.Push(pushCount);
      }
      void LoadStackPop()
      {
        _ilGen.Ldloca(_stack);
        _ilGen.Ldloca(_stack);
        _ilGen.Ldfld(Types.Operations.Stack.Sp);
        _ilGen.Ldc_I4_1();
        _ilGen.Sub();
        _ilGen.Stfld(Types.Operations.Stack.Sp);

        _ilGen.Ldloca(_stack);
        _ilGen.Ldfld(Types.Operations.Stack.Items);
        _ilGen.Ldloca(_stack);
        _ilGen.Ldfld(Types.Operations.Stack.Sp);
        _ilGen.Ldelema(Types.DValue.TypeOf);

        _stackModel.Pop(1);
      }

      /// <summary>
      /// Load an item in the stack indexed from top or bottom
      /// </summary>
      /// <param name="index">negative numbers are treated as index from top</param>
      void LoadStackItem(int index)
      {
        _ilGen.Ldloca(_stack);
        _ilGen.Ldfld(Types.Operations.Stack.Items);
        if (index < 0)
        {//Load from top
          _ilGen.Ldloca(_stack);
          _ilGen.Ldfld(Types.Operations.Stack.Sp);
          _ilGen.Ldc_I4(-index);
          _ilGen.Sub();
        }
        else
        {//Load from bottom
          _ilGen.Ldc_I4(index);
        }
        _ilGen.Ldelema(Types.DValue.TypeOf);
      }
      void LoadStackItem(LocalBuilder indexVar)
      {
        _ilGen.Ldloca(_stack);
        _ilGen.Ldfld(Types.Operations.Stack.Items);
        _ilGen.Ldloc(indexVar);
        _ilGen.Ldelema(Types.DValue.TypeOf);
      }
      void LoadStackSp()
      {
        _ilGen.Ldloca(_stack);
        _ilGen.Ldfld(Types.Operations.Stack.Sp);
      }
      void StoreStackSp(int sp)
      {
        _ilGen.Ldloca(_stack);
        _ilGen.Ldc_I4(sp);
        _ilGen.Stfld(Types.Operations.Stack.Sp);
      }
      #endregion
      protected override void ExecuteInitialize()
      {
        //if (JSRuntime.Instance.Configuration.ProfileFunctionTime)
        //  StartJitTimer("JS/Jit/Light/" + funcMetadata.Declaration);
        //else
        //  StartJitTimer("JS/Jit/Light");

        base.ExecuteInitialize();

        _stackModel = new StackModel();
        _stack = _localVars.Declare(Types.Operations.Stack.TypeOf);
      }
      protected override void ExecuteFinalize()
      {
        base.ExecuteFinalize();
      }

      //CodeGenerationInfo _cgInfo;
      Label _prologInit;
      Label _prologBegin;

      protected override void GenProlog()
      {
        _ilGen.WriteComment("Prolog");

        ///We will have some information known only after processing the body
        ///We use those for init generated after the body, but executed befoe the body!
        _prologInit = _ilGen.DefineLabel();
        _ilGen.Br(_prologInit);
        _prologBegin = _ilGen.DefineLabel();
        _ilGen.MarkLabel(_prologBegin);

        PrepareContext();
        DeclareParameterSymbols();
        DeclareSymbols();
      }

      protected override void DeclareParameterSymbols()
      {
        var scope = _currFuncMetadata.Scope;
        var parameters = _currFuncMetadata.FunctionIR.Parameters;

        //Extend missing arguments
        Ldarg_CallFrame();
        _ilGen.Ldc_I4(parameters.Count);
        _ilGen.Call(Types.CallFrame.SetExpectedArgsCount);

        if (scope.HasArgumentsSymbol)
        {
          ///In this case, we already read/write from the arguments.Elements[i] for the symbol. 
          ///however, we should always refer to the arguments variable itself since its .Elements 
          ///may be resized and changed
          ///we cannot rely on the argument symbol's variable for this since the progremmer
          ///may write the arguments symbol and we can lose the value, so we keep our own separately
          _arguments = _localVars.Declare(Types.DArray.TypeOf);

          Ldarg_CallFrame();
          _ilGen.Ldloc(_context);
          _ilGen.Call(Types.JSFunctionArguments.CreateArgumentsObject_CallFrameRef_DObject); //This function will also add getter/setters for arguments to context if needed.
          _ilGen.Stloc(_arguments);
        }
        else
        {
          /*var knownArgTypesCount = _currFuncCode.Signature.GetLastKnownArgTypeIndex() + 1;*/ // TODO: Unused.
          for (var i = parameters.Count - 1; i >= 0; --i)
          {
            var symbol = parameters[i].Symbol;
            Debug.Assert(symbol.ParameterIndex == i, "Invalid situation!, symbol {0} should be paramter with parameter index {1} instead of {2}", symbol.Name, symbol.ParameterIndex, symbol.ParameterIndex);

            if (symbol.SymbolType == JSSymbol.SymbolTypes.ClosedOnLocal)
            {
              ///In this case, we have to call the PD.Getter/PD.Setter to access the actual argument
              symbol.AssignFieldId();

              _ilGen.Ldloc(_context);
              _ilGen.Ldstr(symbol.Name);
              _ilGen.Ldc_I4(symbol.FieldId);
              Call(Types.Operations.Stack.DeclareVariable, 0, 0);

              Ldarg_CallFrame();
              _ilGen.Ldc_I4(symbol.ParameterIndex);
              Call(Types.Operations.Stack.LoadArg, 0, 1);

              _ilGen.Ldloc(_context);
              _ilGen.Ldc_I4(symbol.FieldId);
              //_ilGen.Ldc_I4(symbol.AncestorDistance);
              _ilGen.Ldc_I4(false);
              Call(Types.Operations.Stack.StoreVariable, 1, 0);
            }
          }
        }
      }

      private void DeclareSymbols()
      {
        var scope = _currFuncMetadata.Scope;
        foreach (var symbol in scope.Symbols)
        {
          if (symbol.IsParameter)
            continue; //This was already handled before!

          symbol.AssignFieldId();

          _ilGen.WriteComment("{0} : {1}", symbol.Name, symbol.SymbolType);
          switch (symbol.SymbolType)
          {
            case JSSymbol.SymbolTypes.Local:
            case JSSymbol.SymbolTypes.ClosedOnLocal:
              _ilGen.Ldloc(_context);
              _ilGen.Ldstr(symbol.Name);
              _ilGen.Ldc_I4(symbol.FieldId);
              Call(Types.Operations.Stack.DeclareVariable, 0, 0);
              break;
            case JSSymbol.SymbolTypes.Unknown:
            case JSSymbol.SymbolTypes.ParentLocal:
            case JSSymbol.SymbolTypes.Global:
              break;
            case JSSymbol.SymbolTypes.Arguments:
              _ilGen.Ldloc(_arguments);
              Call(Types.Operations.Stack.LoadDObject, 0, 1);
              WriteResults(symbol, false);
              break;
            case JSSymbol.SymbolTypes.HiddenLocal:
              _localVars.Declare(mdr.ValueTypes.DValue, symbol);
              break;
          }
        }
      }

      protected override void GenBody()
      {
        _ilGen.WriteComment("Body");
        base.GenBody();
        _ilGen.Br(_epilogLabel);

        _ilGen.MarkLabel(_prologInit);
        if (_stackModel.MaxStackDepth > 0)
        {
          //Initializing _stack.Items
          _ilGen.Ldloca(_stack);
          _ilGen.Ldc_I4(_stackModel.MaxStackDepth);
          _ilGen.Call(Types.JSFunctionArguments.Allocate);
          _ilGen.Stfld(Types.Operations.Stack.Items);
          //Initializing _stack.Sp
          _ilGen.Ldloca(_stack);
          _ilGen.Ldc_I4(0);
          _ilGen.Stfld(Types.Operations.Stack.Sp);
        }
        _ilGen.Br(_prologBegin);
      }

      protected override void GenEpilog()
      {
        _ilGen.WriteComment("Epilog");

        if (_stackModel.MaxStackDepth > 0)
        {
          _ilGen.Ldloca(_stack);
          _ilGen.Ldfld(Types.Operations.Stack.Items);
          _ilGen.Call(Types.JSFunctionArguments.Release);
        }
        base.GenEpilog();
      }

      #region AsX

      protected override void AsVoid()
      {
        Call(Types.Operations.Stack.Pop, 1, 0);
      }

      protected override void AsBoolean()
      {
        LoadStackPop();
        _result.ValueType = mdr.ValueTypes.DValueRef;
        base.AsBoolean();
      }

      protected override void AsUInt32()
      {
        LoadStackPop();
        _result.ValueType = mdr.ValueTypes.DValueRef;
        base.AsUInt32();
      }

      protected override void AsDouble()
      {
        LoadStackPop();
        _result.ValueType = mdr.ValueTypes.DValueRef;
        base.AsDouble();
      }

      protected override void AsString()
      {
        LoadStackPop();
        _result.ValueType = mdr.ValueTypes.DValueRef;
        base.AsString();
      }

      protected override void AsDObject()
      {
        LoadStackPop();
        _result.ValueType = mdr.ValueTypes.DValueRef;
        base.AsDObject();
      }

      protected override void AsObject(Type t = null)
      {
        LoadStackPop();
        _result.ValueType = mdr.ValueTypes.DValueRef;
        base.AsObject(t);
      }
      #endregion

      #region Statements; ECMA 12. -------------------------------------------------------------------------------------

      public override void Visit(BlockStatement node)
      {

        var oldStackDepth = _stackModel.StackDepth;

        //base.Visit(node); //For some reason this is not working. 
        PushLocation(node);
        VisitNodes(node.Statements);
        PopLocation();

        Debug.Assert(oldStackDepth == _stackModel.StackDepth, new JSSourceLocation(_currFuncMetadata, node), "Stack depth at begining ({0}) and end ({1}) of block do not match!", oldStackDepth, _stackModel.StackDepth);

      }

      public override void Visit(ReturnStatement node)
      {
        PushLocation(node);

        if (node.Expression != null)
        {
          VisitNode(node.Expression);
          Ldarg_CallFrame();
          Call(Types.Operations.Stack.Return, 1, 0);
        }

        PopLocation();

        if (_labelInfos.CurrProtectedRegion == null)
          _ilGen.Br(_epilogLabel);
        else
          _ilGen.Leave(_epilogLabel);
      }

      public override void Visit(ThrowStatement node)
      {
        PushLocation(node);

        VisitNode(node.Expression);
        Call(Types.Operations.Stack.Throw, 1, 0);

        PopLocation();
      }

      public override void Visit(TryStatement node)
      {
        ///we may have an exception anywhere in the try block
        ///so stack may be at its max when entering the catch block
        ///one option is to clear up the stack before starting (we chose this one)
        ///the other optios is to make sure there is enough room on the stack

        PushLocation(node);

        _ilGen.BeginExceptionBlock();
        _labelInfos.PushProtectedRegion(node);
        VisitNode(node.Statement);
        _labelInfos.PopProtectedRegion(node);
        VisitNode(node.Catch);
        VisitNode(node.Finally);
        _ilGen.EndExceptionBlock();

        PopLocation();
      }
      public override void Visit(CatchClause node)
      {
        _ilGen.BeginCatchBlock(Types.JSException.TypeOf);
        _labelInfos.PushProtectedRegion(node);

        StoreStackSp(0); //Empty the stack

        //At this point, the exception object is on the stack
        _ilGen.Ldflda(Types.JSException.Value);

        Call(Types.Operations.Stack.LoadDValue, 0, 1);

        var symbol = node.Identifier.Symbol;// _currFuncMetadata.GetSymbol(expression.Catch.Identifier);
        Debug.Assert(symbol != null, new JSSourceLocation(_currFuncMetadata, node.Statement), string.Format("Cannot find symbol for identifier {0} in catch clause", node.Identifier));
        WriteResults(symbol, false);

        VisitNode(node.Statement);
        _labelInfos.PopProtectedRegion(node);
      }
      public override void Visit(FinallyClause node)
      {
        _ilGen.BeginFinallyBlock();
        VisitNode(node.Statement);
      }
      #endregion

      #region Primary Expressions; ECMA 11.1 -------------------------------------------------------------------------------------

      public override void Visit(ThisLiteral node)
      {
        PushLocation(node);
        Ldarg_CallFrame();
        Call(Types.Operations.Stack.LoadThis, 0, 1);
        PopLocation();
      }

      public override void Visit(NullLiteral node)
      {
        PushLocation(node);
        Call(Types.Operations.Stack.LoadNull, 0, 1);
        PopLocation();
      }

      public override void Visit(BooleanLiteral node)
      {
        PushLocation(node);
        _ilGen.Ldc_I4(node.Value);
        Call(Types.Operations.Stack.LoadBoolean, 0, 1);
        PopLocation();
      }

      public override void Visit(IntLiteral node)
      {
        PushLocation(node);
        _ilGen.Ldc_I4((int)node.Value);
        Call(Types.Operations.Stack.LoadInt, 0, 1);
        PopLocation();
      }

      public override void Visit(DoubleLiteral node)
      {
        PushLocation(node);
        _ilGen.Ldc_R8(node.Value);
        Call(Types.Operations.Stack.LoadDouble, 0, 1);
        PopLocation();
      }

      public override void Visit(StringLiteral node)
      {
        PushLocation(node);
        _ilGen.Ldstr(node.Value);
        Call(Types.Operations.Stack.LoadString, 0, 1);
        PopLocation();
      }

      public override void Visit(RegexpLiteral node)
      {
        PushLocation(node);
        _ilGen.Ldstr(node.Regexp);
        _ilGen.Ldstr(node.Options);
        Call(Types.Operations.Stack.CreateRegexp, 0, 1);
        PopLocation();
      }

      public override void Visit(ArrayLiteral node)
      {
        PushLocation(node);
        VisitNodes(node.Items);

        var arraySize = node.Items.Count;
        _ilGen.Ldc_I4(arraySize);
        Call(Types.Operations.Stack.CreateArray, arraySize, 1);
        PopLocation();
      }

      public override void Visit(ObjectLiteral node)
      {
        PushLocation(node);
        foreach (var v in node.Properties)
        {
          //TODO: instead we can load the fieldId(v.key). 
          _ilGen.Ldstr(v.Name);
          Call(Types.Operations.Stack.LoadString, 0, 1);
          VisitNode(v.Expression);
        }
        var itemsCount = node.Properties.Count;
        _ilGen.Ldc_I4(itemsCount);
        Call(Types.Operations.Stack.CreateJson, 2 * itemsCount, 1);
        PopLocation();
      }

      public override void Visit(ReadIdentifierExpression node)
      {
        PushLocation(node);
        var symbol = node.Symbol;
        switch (symbol.SymbolType)
        {
          case JSSymbol.SymbolTypes.Unknown:
          case JSSymbol.SymbolTypes.Local:
          case JSSymbol.SymbolTypes.ClosedOnLocal:
          case JSSymbol.SymbolTypes.ParentLocal:
          case JSSymbol.SymbolTypes.Global:
          case JSSymbol.SymbolTypes.Arguments:
            if (symbol.IsParameter && _currFuncMetadata.Scope.HasArgumentsSymbol)
            {
              _ilGen.Ldloc(_arguments);
              _ilGen.Ldfld(Types.DArray.Elements);
              _ilGen.Ldc_I4(symbol.ParameterIndex);
              _ilGen.Ldelema(Types.DValue.TypeOf);
              Call(Types.Operations.Stack.LoadDValue, 0, 1);
            }
            else if (symbol.IsParameter && symbol.SymbolType != JSSymbol.SymbolTypes.ClosedOnLocal)
            {
              Ldarg_CallFrame();
              _ilGen.Ldc_I4(symbol.ParameterIndex);
              Call(Types.Operations.Stack.LoadArg, 0, 1);
            }
            else
            {
              _ilGen.Ldloc(_context);
              _ilGen.Ldc_I4(symbol.FieldId);
              //_ilGen.Ldc_I4(symbol.AncestorDistance);
              Call(Types.Operations.Stack.LoadVariable, 0, 1);
            }
            break;
          //case JSSymbol.SymbolTypes.Undefined:
          //  Call(Types.Operations.Stack.LoadUndefined, 0, 1);
          //  break;
          case JSSymbol.SymbolTypes.HiddenLocal:
            _ilGen.Ldloca(_localVars.Get(symbol));
            Call(Types.Operations.Stack.LoadDValue, 0, 1);
            break;
          default:
            Trace.Fail(new JSSourceLocation(_currFuncMetadata, node), "Cannot load symbol {0}:{1}", symbol.Name, symbol.SymbolType);
            break;
        }
        PopLocation();
      }

      public override void Visit(ReadIndexerExpression node)
      {
        PushLocation(node);
        //TODO: how should we add typed array acces to this? 
        //  If we know the actual type is DArray with some type data, we can potentially use that. But that should be done at run time
        //  So, we should generate code to do that. Or is it more complicated than that?

        ///Any read or write in this situation must consider that the results might a Prototype, and so getter/setter should be called as well

        ///In the case of Usages.ReadWrite:
        ///We should execute the read side then the write side. If object itself does not have field, it will
        ///use its prototype chain for read. If we do the reverse order, the field is first added to the object.
        ///Also, 
        ///  if the object does have the field, the result ref for read and write will be point to two different objects
        ///  if the object has the filed, both refs will point to the same thing. So, we should be ok in terms of dangling refs

        ///Note:
        ////GetField... does not have any side effects, so we don't need to call it!
        ///SetField... or GetOrAddField... may have side effects, so we should call it as well no matter what, but if we are in Write or ReadWrite mode, the User cannot be null

        VisitNode(node.Container);
        VisitNode(node.Index);

        // TODO: Commented since canPopOperand is unused.
        /*var canPopOperand = true;// !expression.UserIsFunction;*/
        var popOperandCount = 2;// expression.UserIsFunction ? 1 : 2; //remove index, but leave object for This if user is function
        _ilGen.Ldc_I4(popOperandCount);
        Call(Types.Operations.Stack.LoadField, popOperandCount, 1);
        PopLocation();
      }

      public override void Visit(ReadPropertyExpression node)
      {
        PushLocation(node);

        VisitNode(node.Container);

        node.AssignFieldId();
        _ilGen.Ldc_I4(node.FieldId);

        var popOperandCount = 1;// node.UserIsFunction ? 0 : 1; //Leave object for This if user is function
        _ilGen.Ldc_I4(popOperandCount);
        Call(Types.Operations.Stack.LoadFieldByFieldId, popOperandCount, 1);

        PopLocation();
      }

      #endregion

      #region Type Conversions; ECMA 9 -------------------------------------------------------------------------------------

      public override void Visit(ToPrimitive node)
      {
        PushLocation(node);
        VisitNode(node.Expression);
        Call(Types.Operations.Stack.ToPrimitive, 1, 1);
        PopLocation();
      }

      public override void Visit(ToBoolean node)
      {
        PushLocation(node);
        VisitNode(node.Expression);
        Call(Types.Operations.Stack.ToBoolean, 1, 1);
        PopLocation();
      }

      public override void Visit(ToNumber node)
      {
        PushLocation(node);
        VisitNode(node.Expression);
        Call(Types.Operations.Stack.ToNumber, 1, 1);
        PopLocation();
      }

      public override void Visit(ToDouble node)
      {
        PushLocation(node);
        VisitNode(node.Expression);
        Call(Types.Operations.Stack.ToDouble, 1, 1);
        PopLocation();
      }

      public override void Visit(ToInteger node)
      {
        PushLocation(node);
        VisitNode(node.Expression);
        Call(Types.Operations.Stack.ToInteger, 1, 1);
        PopLocation();
      }

      public override void Visit(ToInt32 node)
      {
        PushLocation(node);
        VisitNode(node.Expression);
        Call(Types.Operations.Stack.ToInt32, 1, 1);
        PopLocation();
      }

      public override void Visit(ToUInt32 node)
      {
        PushLocation(node);
        VisitNode(node.Expression);
        Call(Types.Operations.Stack.ToUInt32, 1, 1);
        PopLocation();
      }

      public override void Visit(ToUInt16 node)
      {
        PushLocation(node);
        VisitNode(node.Expression);
        Call(Types.Operations.Stack.ToUInt16, 1, 1);
        PopLocation();
      }

      public override void Visit(ToString node)
      {
        PushLocation(node);
        VisitNode(node.Expression);
        Call(Types.Operations.Stack.ToString_StackRef, 1, 1);
        PopLocation();
      }

      public override void Visit(ToObject node)
      {
        PushLocation(node);
        VisitNode(node.Expression);
        Call(Types.Operations.Stack.ToObject, 1, 1);
        PopLocation();
      }

      public override void Visit(ToFunction node)
      {
        PushLocation(node);
        VisitNode(node.Expression);
        Call(Types.Operations.Stack.ToFunction, 1, 1);
        PopLocation();
      }

      #endregion

      #region Unary Operators; ECMA 11.4 -------------------------------------------------------------------------------------

      public override void Visit(DeleteExpression node)
      {
        PushLocation(node);
        var expression = node.Expression;
        var parent = expression as ParenExpression;
        if (parent != null)
          expression = parent.Expression;
        var indexer = expression as ReadIndexerExpression;
        if (indexer != null)
        {
          VisitNode(indexer.Container);
          VisitNode(indexer.Index);
          Call(Types.Operations.Stack.DeleteProperty, 2, 1);
        }
        else
        {
          //we need to visit in case the expression has side effects, but then return false
          VisitNode(node.Expression);
          var readId = expression as ReadIdentifierExpression;
          if (readId != null)
          {
            Call(Types.Operations.Stack.Pop, 1, 0);
            _ilGen.Ldloc(_context);
            Call(Types.Operations.Stack.LoadDObject, 0, 1);
            _ilGen.Ldc_I4(readId.Symbol.FieldId);
            Call(Types.Operations.Stack.LoadInt, 0, 1);
            Call(Types.Operations.Stack.DeleteVariable, 2, 1);
          }
          else
            Call(Types.Operations.Stack.Delete, 1, 1);
        }
        PopLocation();
      }

      public override void Visit(VoidExpression node)
      {
        PushLocation(node);
        VisitNode(node.Expression);
        Call(Types.Operations.Stack.Void, 1, 1);
        PopLocation();
      }

      public override void Visit(TypeofExpression node)
      {
        PushLocation(node);
        VisitNode(node.Expression);
        Call(Types.Operations.Stack.TypeOfOp, 1, 1);
        PopLocation();
      }

      public override void Visit(PositiveExpression node)
      {
        PushLocation(node);
        VisitNode(node.Expression);
        Call(Types.Operations.Stack.Positive, 1, 1);
        PopLocation();
      }

      public override void Visit(NegativeExpression node)
      {
        PushLocation(node);
        VisitNode(node.Expression);
        Call(Types.Operations.Stack.Negate, 1, 1);
        PopLocation();
      }

      public override void Visit(BitwiseNotExpression node)
      {
        PushLocation(node);
        VisitNode(node.Expression);
        Call(Types.Operations.Stack.BitwiseNot, 1, 1);
        PopLocation();
      }

      public override void Visit(LogicalNotExpression node)
      {
        PushLocation(node);
        VisitNode(node.Expression);
        Call(Types.Operations.Stack.LogicalNot, 1, 1);
        PopLocation();
      }

      #endregion

      #region Binary Multiplicative Operators; ECMA 11.5 -------------------------------------------------------------------------------------

      public override void Visit(MultiplyExpression node)
      {
        PushLocation(node);
        VisitNode(node.Left);
        VisitNode(node.Right);
        Call(Types.Operations.Stack.Times, 2, 1);
        PopLocation();
      }

      public override void Visit(DivideExpression node)
      {
        PushLocation(node);
        VisitNode(node.Left);
        VisitNode(node.Right);
        Call(Types.Operations.Stack.Div, 2, 1);
        PopLocation();
      }

      public override void Visit(RemainderExpression node)
      {
        PushLocation(node);
        VisitNode(node.Left);
        VisitNode(node.Right);
        Call(Types.Operations.Stack.Modulo, 2, 1);
        PopLocation();
      }

      #endregion
      #region Binary Additive Operators; ECMA 11.6 -------------------------------------------------------------------------------------

      public override void Visit(AdditionExpression node)
      {
        PushLocation(node);
        VisitNode(node.Left);
        VisitNode(node.Right);
        Call(Types.Operations.Stack.Plus, 2, 1);
        PopLocation();
      }

      public override void Visit(SubtractionExpression node)
      {
        PushLocation(node);
        VisitNode(node.Left);
        VisitNode(node.Right);
        Call(Types.Operations.Stack.Minus, 2, 1);
        PopLocation();
      }

      #endregion
      #region Binary Bitwise Shift Operators; ECMA 11.7 -------------------------------------------------------------------------------------

      public override void Visit(LeftShiftExpression node)
      {
        PushLocation(node);
        VisitNode(node.Left);
        VisitNode(node.Right);
        Call(Types.Operations.Stack.LeftShift, 2, 1);
        PopLocation();
      }

      public override void Visit(RightShiftExpression node)
      {
        PushLocation(node);
        VisitNode(node.Left);
        VisitNode(node.Right);
        Call(Types.Operations.Stack.RightShift, 2, 1);
        PopLocation();
      }

      public override void Visit(UnsignedRightShiftExpression node)
      {
        PushLocation(node);
        VisitNode(node.Left);
        VisitNode(node.Right);
        Call(Types.Operations.Stack.UnsignedRightShift, 2, 1);
        PopLocation();
      }

      #endregion
      #region Binary Relational Operators; ECMA 11.8 -------------------------------------------------------------------------------------

      public override void Visit(LesserExpression node)
      {
        PushLocation(node);
        VisitNode(node.Left);
        VisitNode(node.Right);
        Call(Types.Operations.Stack.Lesser, 2, 1);
        PopLocation();
      }

      public override void Visit(GreaterExpression node)
      {
        PushLocation(node);
        VisitNode(node.Left);
        VisitNode(node.Right);
        Call(Types.Operations.Stack.Greater, 2, 1);
        PopLocation();
      }

      public override void Visit(LesserOrEqualExpression node)
      {
        PushLocation(node);
        VisitNode(node.Left);
        VisitNode(node.Right);
        Call(Types.Operations.Stack.LesserOrEqual, 2, 1);
        PopLocation();
      }

      public override void Visit(GreaterOrEqualExpression node)
      {
        PushLocation(node);
        VisitNode(node.Left);
        VisitNode(node.Right);
        Call(Types.Operations.Stack.GreaterOrEqual, 2, 1);
        PopLocation();
      }

      public override void Visit(InstanceOfExpression node)
      {
        PushLocation(node);
        VisitNode(node.Left);
        VisitNode(node.Right);
        Call(Types.Operations.Stack.InstanceOf, 2, 1);
        PopLocation();
      }

      public override void Visit(InExpression node)
      {
        PushLocation(node);
        VisitNode(node.Left);
        VisitNode(node.Right);
        Call(Types.Operations.Stack.In, 2, 1);
        PopLocation();
      }

      #endregion
      #region Binary Equality Operators; ECMA 11.9 -------------------------------------------------------------------------------------

      public override void Visit(EqualExpression node)
      {
        PushLocation(node);
        VisitNode(node.Left);
        VisitNode(node.Right);
        Call(Types.Operations.Stack.Equal, 2, 1);
        PopLocation();
      }

      public override void Visit(NotEqualExpression node)
      {
        PushLocation(node);
        VisitNode(node.Left);
        VisitNode(node.Right);
        Call(Types.Operations.Stack.NotEqual, 2, 1);
        PopLocation();
      }

      public override void Visit(SameExpression node)
      {
        PushLocation(node);
        VisitNode(node.Left);
        VisitNode(node.Right);
        Call(Types.Operations.Stack.Same, 2, 1);
        PopLocation();
      }

      public override void Visit(NotSameExpression node)
      {
        PushLocation(node);
        VisitNode(node.Left);
        VisitNode(node.Right);
        Call(Types.Operations.Stack.NotSame, 2, 1);
        PopLocation();
      }

      #endregion
      #region Binary Bitwise Operators; ECMA 11.10 -------------------------------------------------------------------------------------

      public override void Visit(BitwiseAndExpression node)
      {
        PushLocation(node);
        VisitNode(node.Left);
        VisitNode(node.Right);
        Call(Types.Operations.Stack.BitwiseAnd, 2, 1);
        PopLocation();
      }

      public override void Visit(BitwiseOrExpression node)
      {
        PushLocation(node);
        VisitNode(node.Left);
        VisitNode(node.Right);
        Call(Types.Operations.Stack.BitwiseOr, 2, 1);
        PopLocation();
      }

      public override void Visit(BitwiseXorExpression node)
      {
        PushLocation(node);
        VisitNode(node.Left);
        VisitNode(node.Right);
        Call(Types.Operations.Stack.BitwiseXOr, 2, 1);
        PopLocation();
      }

      #endregion

      #region Conditional Operators; ECMA 11.12 -------------------------------------------------------------------------------------

      public override void Visit(TernaryExpression node)
      {
        PushLocation(node);
        var secondChoice = _ilGen.DefineLabel();
        var done = _ilGen.DefineLabel();

        VisitNode(node.Left);
        AsBoolean();
        _ilGen.Brfalse(secondChoice);
        VisitNode(node.Middle);
        _ilGen.Br(done);
        _ilGen.MarkLabel(secondChoice);
        VisitNode(node.Right);
        _ilGen.MarkLabel(done);

        _stackModel.Pop(2);
        _stackModel.Push(1);
        PopLocation();
      }

      #endregion

      #region Assignment; ECMA 11.13 -------------------------------------------------------------------------------------

      public override void Visit(WriteTemporaryExpression node)
      {
        Debug.Assert(node.Users.Count > 1, "Invalid situation, temporary must have more than one user!");
        var local = _localVars.Get(node);
        if (local != null)
        {
          //this was already visited
          _ilGen.Ldloca(local);
          Call(Types.Operations.Stack.LoadDValue, 0, 1);
        }
        else
        {
          //First visit
          VisitNode(node.Value);
          local = _localVars.Declare(mdr.ValueTypes.DValue, node);
          LoadStackItem(-1); //load the top item, but no pop or push
          _ilGen.Ldloca(local);
          _ilGen.Call(Types.Operations.Assign.Get(mdr.ValueTypes.DValueRef));
        }
      }

      void WriteResults(JSSymbol symbol, bool pushBackResult)
      {
        switch (symbol.SymbolType)
        {
          case JSSymbol.SymbolTypes.Unknown:
          case JSSymbol.SymbolTypes.Local:
          case JSSymbol.SymbolTypes.ClosedOnLocal:
          case JSSymbol.SymbolTypes.ParentLocal:
          case JSSymbol.SymbolTypes.Global:
          case JSSymbol.SymbolTypes.Arguments:
            if (symbol.IsParameter && symbol.SymbolType != JSSymbol.SymbolTypes.ClosedOnLocal)
            {
              Ldarg_CallFrame();
              _ilGen.Ldc_I4(symbol.ParameterIndex);
              _ilGen.Ldc_I4(pushBackResult);
              Call(Types.Operations.Stack.StoreArg, 1, pushBackResult ? 1 : 0);
            }
            else
            {
              _ilGen.Ldloc(_context);
              _ilGen.Ldc_I4(symbol.FieldId);
              //_ilGen.Ldc_I4(symbol.AncestorDistance);
              _ilGen.Ldc_I4(pushBackResult);
              Call(Types.Operations.Stack.StoreVariable, 1, pushBackResult ? 1 : 0);
            }
            break;
          case JSSymbol.SymbolTypes.HiddenLocal:
            _ilGen.Ldloca(_localVars.Get(symbol));
            if (pushBackResult)
              LoadStackItem(-1);
            else
              LoadStackPop();
            _ilGen.Call(Types.DValue.Set.Get(mdr.ValueTypes.DValueRef));
            break;
          default:
            Trace.Fail("Cannot write to symbol {0}:{1}", symbol.Name, symbol.SymbolType);
            break;
        }
      }
      public override void Visit(WriteIdentifierExpression node)
      {
        PushLocation(node);
        VisitNode(node.Value);
        WriteResults(node.Symbol, true);
        PopLocation();
      }

      public override void Visit(WriteIndexerExpression node)
      {
        PushLocation(node);
        var pushBackResult = true;
        VisitNode(node.Container);
        VisitNode(node.Index);
        VisitNode(node.Value);

        _ilGen.Ldc_I4(pushBackResult);
        Call(Types.Operations.Stack.StoreField, 3, pushBackResult ? 1 : 0);
        PopLocation();
      }

      public override void Visit(WritePropertyExpression node)
      {
        PushLocation(node);

        var pushBackResult = true;
        VisitNode(node.Container);
        VisitNode(node.Value);

        node.AssignFieldId();
        _ilGen.Ldc_I4(node.FieldId);

        _ilGen.Ldc_I4(pushBackResult);
        Call(Types.Operations.Stack.StoreFieldByFieldId, 2, pushBackResult ? 1 : 0);

        PopLocation();
      }

      #endregion

      #region Function Calls; ECMA 11.2.2, 11.2.3 -------------------------------------------------------------------------------------

      public override void Visit(NewExpression node)
      {
        PushLocation(node);

        VisitNode(node.Function);
        VisitNodes(node.Arguments);

        //GenExecTimerStop(true);

        _ilGen.Ldc_I4(node.Arguments.Count);
        Call(Types.Operations.Stack.New, 1 + node.Arguments.Count, 1);

        //GenExecTimerStart(true);

        PopLocation();
      }

      public override void Visit(CallExpression node)
      {
        PushLocation(node);

        VisitNode(node.Function);
        VisitNode(node.ThisArg);
        VisitNodes(node.Arguments);

        GenExecTimerStop(true);

        Ldarg_CallFrame();
        _ilGen.Ldloc(_context);
        _ilGen.Ldc_I4(node.Arguments.Count);
        var hasThis = node.ThisArg != null;
        _ilGen.Ldc_I4(hasThis);
        _ilGen.Ldc_I4(node.IsDirectEvalCall);
        Call(Types.Operations.Stack.Call, (hasThis ? 1 : 0) + 1 + node.Arguments.Count, 1);

        GenExecTimerStart(true);

        PopLocation();
      }

      #endregion

      #region Function Definition; ECMA 13 -------------------------------------------------------------------------------------

      public override void Visit(FunctionExpression node)
      {
        PushLocation(node);
        var impIndex = node.Metadata.FuncDefinitionIndex;
        Ldarg_CallFrame();
        _ilGen.Ldc_I4(impIndex);
        _ilGen.Ldloc(_context);
        Call(Types.Operations.Stack.CreateFunction, 0, 1);

        PopLocation();
      }

      #endregion

      #region Interanls
      private void LoadFromStack(Expression node, Type type)
      {
        VisitNode(node);
        var vt = Types.ValueTypeOf(type);

        switch (vt)
        {
          case mdr.ValueTypes.Boolean: AsBoolean(); break;
          case mdr.ValueTypes.String: AsString(); break;
          case mdr.ValueTypes.Double: AsDouble(); break;
          case mdr.ValueTypes.Int32: AsInt32(); break;
          case mdr.ValueTypes.UInt16: AsUInt16(); break;
          case mdr.ValueTypes.UInt32: AsUInt32(); break;
          case mdr.ValueTypes.Object: AsDObject(); break;
          case mdr.ValueTypes.Any: AsObject(type); break;
          default: Trace.Fail("Cannot convert {0} to proper type stack for parameter type {1}", vt, type); break;
        }
      }
      void StoreToStack(Type type)
      {
        var vt = Types.ValueTypeOf(type);

        switch (vt)
        {
          case mdr.ValueTypes.Undefined:
          case mdr.ValueTypes.Unknown:
            Call(Types.Operations.Stack.LoadUndefined, 0, 1);
            break;
          case mdr.ValueTypes.Any:
            Call(Types.Operations.Stack.LoadAny, 0, 1);
            break;
          case mdr.ValueTypes.Null:
            Call(Types.Operations.Stack.LoadNull, 0, 1);
            break;
          case mdr.ValueTypes.DValue:
            Call(Types.Operations.Stack.LoadDValue, 0, 1);
            break;
          case mdr.ValueTypes.DValueRef:
            Call(Types.Operations.Stack.LoadArg, 0, 1); // ???
            break;
          case mdr.ValueTypes.Char:
          case mdr.ValueTypes.UInt8:
          case mdr.ValueTypes.Int8:
          case mdr.ValueTypes.Int16:
          case mdr.ValueTypes.UInt16:
          case mdr.ValueTypes.Int32:
          case mdr.ValueTypes.UInt32:
            Call(Types.Operations.Stack.LoadInt, 0, 1);
            break;
          case mdr.ValueTypes.Boolean:
            Call(Types.Operations.Stack.LoadBoolean, 0, 1);
            break;
          case mdr.ValueTypes.String:
            Call(Types.Operations.Stack.LoadString, 0, 1);
            break;
          case mdr.ValueTypes.Float:
          case mdr.ValueTypes.Double:
          case mdr.ValueTypes.Int64:
          case mdr.ValueTypes.UInt64:
            Call(Types.Operations.Stack.LoadDouble, 0, 1);
            break;
          case mdr.ValueTypes.Object:
          case mdr.ValueTypes.Array:
          case mdr.ValueTypes.Property:
          case mdr.ValueTypes.Function:
            Call(Types.Operations.Stack.LoadDObject, 0, 1);
            break;
        }
      }
      public override void Visit(InternalCall node)
      {
        PushLocation(node);

        var parameters = node.Method.GetParameters();
        Debug.Assert(node.Arguments.Count == parameters.Length + (node.Method.IsStatic ? 0 : 1), "Arguments mismatch between node {0} and method {1} of {2}", node, node.Method, node.Method.DeclaringType);
        int i = 0;
        if (!node.Method.IsStatic)
          LoadFromStack(node.Arguments[i++], node.Method.DeclaringType);
        for (; i < node.Arguments.Count; ++i)
          LoadFromStack(node.Arguments[i], parameters[i].ParameterType);
        _ilGen.Call(node.Method);
        StoreToStack(node.Method.ReturnType);

        PopLocation();
      }

      public override void Visit(InternalNew node)
      {
        PushLocation(node);

        var parameters = node.Constructor.GetParameters();
        Debug.Assert(node.Arguments.Count == parameters.Length, "Arguments mismatch between node {0} and constructor {1} of {2}", node, node.Constructor, node.Constructor.DeclaringType);
        for (int i = 0; i < node.Arguments.Count; ++i)
          LoadFromStack(node.Arguments[i], parameters[i].ParameterType);
        _ilGen.Newobj(node.Constructor);
        StoreToStack(node.Constructor.DeclaringType);

        PopLocation();
      }

      #endregion

    }
  }
}

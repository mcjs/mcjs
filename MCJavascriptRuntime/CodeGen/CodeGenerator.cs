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
using mjr.IR;
using m.Util.Diagnose;

namespace mjr.CodeGen
{
  class CodeGenerator : CodeGeneratorBase
  {
    static AlgorithmImplementation _pool = new AlgorithmPool<CodeGeneratorImp>("JS/Jit/Full/", () => new CodeGeneratorImp(), () => JSRuntime.Instance.Configuration.ProfileJitTime);
    public static void Execute(CodeGenerationInfo cgInfo) { _pool.Execute(cgInfo); }

    protected class CodeGeneratorImp : CodeGeneratorBaseImp, AlgorithmImplementation
    {
      //protected LocalBuilder _funcCode;
      protected LocalBuilder _context; //The current execution context
      protected LocalBuilder _arguments; //Assigned if function .HasArgumentsSymbol
      protected Label _epilogLabel;
      protected LabelInfoManager<Label> _labelInfos = new LabelInfoManager<Label>();

      protected override void ExecuteInitialize()
      {
        base.ExecuteInitialize();

        _ilGen.WriteComment("thread: {0}", System.Threading.Thread.CurrentThread.ManagedThreadId);
        _ilGen.WriteComment(string.Format("sig=0x{0:X}", _currFuncCode.Signature.Value));

        CheckSignature(); //Just make sure base.ExecuteInitialize is not generating any code
      }

      protected override void ExecuteFinalize()
      {
        _labelInfos.Clear();
        base.ExecuteFinalize();
      }

      protected override void GenProlog()
      {
        _epilogLabel = _ilGen.DefineLabel();

        if (JSRuntime.Instance.Configuration.ProfileFunctionFrequency)
          GenCounterIncrement("Frequency", _currFuncMetadata.Declaration, 1);

        ///We want to make sure no matter what, we eventually stop the timer. So we generate a try/finally pattern
        if (JSRuntime.Instance.Configuration.ProfileExecuteTime)
        {
          GenExecTimerInit();
          _ilGen.BeginExceptionBlock();
          GenExecTimerStart(false);
        }

        base.GenProlog();
      }
      protected override void GenEpilog()
      {
        _ilGen.MarkLabel(_epilogLabel);

        if (JSRuntime.Instance.Configuration.ProfileExecuteTime)
        {
          _ilGen.BeginFinallyBlock();
          GenExecTimerStop(false);
          _ilGen.EndExceptionBlock();
        }

        base.GenEpilog();

        ///we need to be careful about what is done in the epilog, perhaps it is better to have two labels: _epilogLabel & _returnLabel to avoid problems later!
        ///this depends on what is done in the epilog, it might be safer to just call the 
        _ilGen.Ret();
      }

      protected override void PrepareContext()
      {
        _context = _localVars.Declare(Types.DObject.TypeOf, "__context");
        base.PrepareContext();
        _ilGen.Stloc(_context);
      }

      mdr.ValueTypes GetType(JSSymbol symbol)
      {
        var symbolValueType = symbol.ValueType; //TODO: we actually need to read this from cgInfo
        return symbolValueType;
      }
      mdr.ValueTypes GetType(Expression node) { return node.ValueType; }

      private void CheckSignature()
      {
        ///This function must be called at the very begining of the function so that
        ///if we need to return no new timer, counter, location ... is modified. 
        ///if the signature does not match, we should not be here at all!

        //bool isTypeInferenceEnabled = (_currFuncMetadata.EnabledOptimizations & JSFunctionMetadata.Optimizations.TypeInference) != 0;
        if (_currFuncMetadata.FunctionIR.Parameters != null && _currFuncCode.Signature.Value != mdr.DFunctionSignature.EmptySignature.Value)
        {
          var signatureMatches = _ilGen.DefineLabel();
          Ldarg_CallFrame();
          _ilGen.Call(Types.Operations.Internals.CheckSignature);
          _ilGen.Brtrue(signatureMatches);
          _ilGen.Ret(); //Return immediately before any timer or counter is fired. 
          _ilGen.MarkLabel(signatureMatches);
        }
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
          var knownArgTypesCount = _currFuncCode.Signature.GetLastKnownArgTypeIndex() + 1;
          for (var i = parameters.Count - 1; i >= 0; --i)
          {
            var symbol = parameters[i].Symbol;
            var paramIndex = symbol.ParameterIndex;
            Debug.Assert(paramIndex == i, "Invalid situation!, symbol {0} should be paramter with parameter index {1} instead of {2}", symbol.Name, paramIndex, symbol.ParameterIndex);
            if (symbol.SymbolType == JSSymbol.SymbolTypes.ClosedOnLocal)
            {
              ///In this case, we have to call the PD.Getter/PD.Setter to access the actual argument
              symbol.AssignFieldId();
              var pd = _localVars.Declare(Types.PropertyDescriptor.TypeOf, symbol);
              _ilGen.Ldloc(_context);
              _ilGen.Ldc_I4(symbol.FieldId);
              _ilGen.Call(Types.DObject.GetPropertyDescriptorByFieldId);
              _ilGen.Stloc(pd);

              var argNotPassed = _ilGen.DefineLabel();
              Ldarg_CallFrame();
              _ilGen.Ldfld(Types.CallFrame.PassedArgsCount);
              _ilGen.Ldc_I4(paramIndex);
              _ilGen.Ble(argNotPassed);
              _ilGen.Ldloc(pd);
              _ilGen.Ldloc(_context);
              Ldarg_Parameter(paramIndex);
              _ilGen.Call(Types.PropertyDescriptor.Set.Get(mdr.ValueTypes.Object, mdr.ValueTypes.DValueRef));
              _ilGen.MarkLabel(argNotPassed);
            }
            else
            {
              Debug.Assert(symbol.SymbolType == JSSymbol.SymbolTypes.Local, "Invalid symbol type {0}", symbol.SymbolType);
              var symbolValueType = GetType(symbol);
              if (paramIndex < knownArgTypesCount)
              {
                var argType = _currFuncCode.Signature.GetArgType(paramIndex);
                if (mdr.ValueTypesHelper.IsDefined(argType))
                {
                  if (symbolValueType == mdr.ValueTypes.Unknown)
                  {
                    ///ArgType is specified in the signature and this DFuctionCode is picked only if the signature match
                    ///We are here most likely because TI was not executed, 
                    ///still, if there are no writers to the symbol, we can give it a fixed type and improve code gen
                    if (symbol.Writers.Count == 0)
                      symbolValueType = argType;
                  }
                  else
                    Debug.Assert(
                      argType == symbolValueType //Same types
                      || symbolValueType == TypeCalculator.ResolveType(argType, symbolValueType) //multiple types assigned to the symbol
                      , string.Format("incompatible assignment {0}:{1} = arg[{2}]:{3}", symbol.Name, symbolValueType, paramIndex, argType)
                    );

                  if (mdr.ValueTypesHelper.IsDefined(symbolValueType))
                  {
                    ///In this case, we read/write the argument via the typed variable on the stack
                    ///we could only be here, if the TI has proven the type of the symbols is ok to be fixed, i.e. no assignments etc. 

                    var localSym = _localVars.Declare(symbolValueType, symbol);
                    Ldarg_Parameter(paramIndex);
                    _ilGen.Call(Types.DValue.As(argType));
                    if (argType != symbolValueType && mdr.ValueTypesHelper.IsNumber(argType))
                    {
                      Debug.Assert(mdr.ValueTypesHelper.IsNumber(symbolValueType)
                        , string.Format("incompatible assignment {0}:{1} = arg[{2}]:{3}", symbol.Name, symbolValueType, paramIndex, argType)
                      );
                      _ilGen.Call(Types.ClrSys.Convert.Get(argType, symbolValueType));
                    }
                    _ilGen.Stloc(localSym);
                  }
                }
                else
                {
                  ///The only other option for a artType is undefined
                    Debug.Assert(argType == mdr.ValueTypes.Undefined || argType == mdr.ValueTypes.Null, "Invalid argument type {0} for argument {1} in function {2}", argType, symbol.Name, _currFuncMetadata.Declaration);
                  Debug.Assert(symbolValueType == mdr.ValueTypes.Unknown //TI was not executed
                    || symbolValueType == mdr.ValueTypes.DValueRef //TI was executed and DValueRef was assigned for "undefined" argtype
                    , "Invalid symbol type {0} for argument {1} in function {2}", symbolValueType, symbol.Name, _currFuncMetadata.Declaration);
                }
              }
              ///In all other cases, we just read/write the parameter by directly accessing the arguments in the call frame
            }
          }
        }
      }

      protected virtual void DeclareNonParamSymbol(JSSymbol symbol, mdr.ValueTypes symbolValueType, LocalBuilder contextMap)
      {
        Debug.Assert(!symbol.IsParameter, "This function must not be called for parameter symbols");

        var scope = symbol.ContainerScope;
        switch (symbol.SymbolType)
        {
          case JSSymbol.SymbolTypes.HiddenLocal:
          case JSSymbol.SymbolTypes.Local:
            {
              switch (symbolValueType)
              {
                case mdr.ValueTypes.Unknown:
                case mdr.ValueTypes.DValueRef:
                case mdr.ValueTypes.DValue:
                  _localVars.Declare(Types.DValue.TypeOf, symbol);
                  break;
                default:
                  _localVars.Declare(Types.TypeOf(symbolValueType), symbol);
                  break;
              }
              break;
            }
          case JSSymbol.SymbolTypes.ClosedOnLocal:
            {
              Debug.Assert(scope.IsFunction, "closed on symbols are only supported at the function level at this time");
              Debug.Assert(symbolValueType == mdr.ValueTypes.Unknown || symbolValueType == mdr.ValueTypes.DValueRef, "Invalid symbol type {0} for symbol {1}", symbolValueType, symbol.Name);
              symbol.AssignFieldId();
              ///It is guaranteed that we will have the property in the _context

              if (scope.HasEval || scope.IsEvalFunction)
              {
                ///In this case, the _context.Fields may change; so we cannot hold on to its refrences!
                _ilGen.Ldloc(contextMap);
                _ilGen.Ldc_I4(symbol.FieldId);
                _ilGen.Call(Types.PropertyMap.GetPropertyDescriptorByFieldId);
                _localVars.Declare(Types.PropertyDescriptor.TypeOf, symbol);
              }
              else
              {
                ///In this case, as an optmization, we can just store the ref to the actual field in the context
                _ilGen.Ldloc(_context);
                _ilGen.Ldfld(Types.DObject.Fields);
                _ilGen.Ldloc(contextMap);
                _ilGen.Ldc_I4(symbol.FieldId);
                _ilGen.Call(Types.PropertyMap.GetPropertyDescriptorByFieldId);
                _ilGen.Ldfld(Types.PropertyDescriptor.Index);
                _ilGen.Ldelema(Types.DValue.TypeOf);
                _localVars.Declare(Types.DValue.RefOf, symbol);
              }
              _ilGen.Stloc(_localVars.Get(symbol));

              break;
            }
          case JSSymbol.SymbolTypes.ParentLocal:
            {
              Debug.Assert(symbolValueType == mdr.ValueTypes.Unknown || symbolValueType == mdr.ValueTypes.DValueRef, "Invalid symbol type {0} for symbol {1}", symbolValueType, symbol.Name);
              symbol.AssignFieldId();
              _ilGen.Ldloc(contextMap);
              _ilGen.Ldc_I4(symbol.FieldId);
              _ilGen.Call(Types.PropertyMap.GetPropertyDescriptorByFieldId); //We know this will return an inherited PD

              var closedOnScope = symbol.ResolvedSymbol.ContainerScope.ContainerFunction.FunctionIR.Scope;
              if (true || ////TODO: this optimizations is disabled for now to find a better approach!
                closedOnScope.HasEval //eval can make any changs to the context, so no clever trick
                || closedOnScope.IsEvalFunction //eval depends on the caller function's context which is not known at compile time
              )
              {
                //In this case, the .Fields of the context may change, so we cannot rely on the references
                _localVars.Declare(Types.PropertyDescriptor.TypeOf, symbol);
                _ilGen.Stloc(_localVars.Get(symbol));
              }
              else
              {
                //In this case, we can do the lookup once, and then just use the reference to the symbol's storage
                var tmp = _localVars.PushTemporary(Types.PropertyDescriptor.TypeOf);
                _ilGen.Stloc(tmp);

                var loadContainer = _ilGen.DefineLabel();
                var loadedContainer = _ilGen.DefineLabel();

                _ilGen.Ldloc(tmp);
                _ilGen.Call(Types.PropertyDescriptor.IsInherited);
                _ilGen.Brtrue(loadContainer);

                _ilGen.Ldloc(_context);
                _ilGen.Br(loadedContainer);

                _ilGen.MarkLabel(loadContainer);
                _ilGen.Ldloc(tmp);
                _ilGen.Ldfld(Types.PropertyDescriptor.Container);

                _ilGen.MarkLabel(loadedContainer);
                _ilGen.Ldfld(Types.DObject.Fields);
                _ilGen.Ldloc(tmp);
                _ilGen.Ldfld(Types.PropertyDescriptor.Index);
                _ilGen.Ldelema(Types.DValue.TypeOf);
                _localVars.Declare(Types.DValue.RefOf, symbol);
                _localVars.PopTemporary(tmp);
                _ilGen.Stloc(_localVars.Get(symbol));
              }
              break;
            }
          case JSSymbol.SymbolTypes.Global:
            {
              ///We know it is in the global state already, but we cannot use the reference bacause:
              ///- .Fields array may change during execution
              ///- the actuall property have accessor in the global scope.
              Debug.Assert(symbolValueType == mdr.ValueTypes.Unknown || symbolValueType == mdr.ValueTypes.DValueRef, "Invalid symbol type {0} for symbol {1}", symbolValueType, symbol.Name);
              symbol.AssignFieldId();

              //The following case is ready taken care of with the OuterDuplicate case
              //var globalSymbol =
              //  (scope != _currFuncMetadata.Scope)
              //  ? _currFuncMetadata.Scope.GetSymbol(symbol.Name)
              //  : null;

              //if (globalSymbol != null)
              //{
              //  //It should be already initialized, and we don't need to do it. 
              //  var globalVar = _localVars.Get(globalSymbol);
              //  Debug.Assert(globalVar != null, "Invalid situation, global symbol {0} must already have been declared", globalSymbol.Name);
              //  _localVars.Declare(globalVar, symbol);
              //  break;
              //}
              //else
              _localVars.Declare(Types.PropertyDescriptor.TypeOf, symbol);
              _ilGen.LoadRuntimeInstance();
              _ilGen.Ldfld(Types.Runtime.GlobalContext);
              _ilGen.Ldc_I4(symbol.FieldId);
              _ilGen.Call(Types.DObject.GetPropertyDescriptorByFieldId);
              _ilGen.Stloc(_localVars.Get(symbol));
              break;
            }
          case JSSymbol.SymbolTypes.Arguments:
            {
              if (symbolValueType == mdr.ValueTypes.Array)
              {
                ///In this case, the arguments symbols must have been only read from 
                Debug.Assert(_arguments != null, "Invalid situation, the _arguments variable must be already assigned");
                var local = _localVars.Declare(Types.DArray.TypeOf, symbol);
                _ilGen.Ldloc(_arguments);
                _ilGen.Stloc(local);

              }
              else if (symbolValueType == mdr.ValueTypes.DValueRef || symbolValueType == mdr.ValueTypes.Unknown)
              {
                ///In this case, we either don't know the type, or there has been assignments to the arguments symbol
                var local = _localVars.Declare(Types.DValue.TypeOf, symbol);
                _ilGen.Ldloca(local);
                _ilGen.Ldloc(_arguments);
                _ilGen.Call(Types.DValue.Set.Get(mdr.ValueTypes.Object));
              }
              else
              {
                Debug.Fail("Invalid symbol type {0} for 'arguments'", symbolValueType);
              }
              break;
            }
          case JSSymbol.SymbolTypes.Unknown:
            {
              ///This symbol could not be resolved during analysis, and hence can be added to the context chain at runtime
              ///The context chain is: func.Context->func.Parent.Context->...GlobalContext->Object.prototype
              ///Therefore we should generate code to look it up at the access site
              Debug.Assert(symbolValueType == mdr.ValueTypes.Unknown || symbolValueType == mdr.ValueTypes.DValueRef, "Invalid symbol type");
              symbol.AssignFieldId();
              break;
            }
          case JSSymbol.SymbolTypes.OuterDuplicate:
            //we don't need to do anything since the outer symbols is going to be used. 
            break;
          default:
            Trace.Fail("cannot process symbol type {0} in {1}", symbol.SymbolType, _currFuncMetadata.FullName);
            break;
        }
      }

      protected override void DeclareSymbols(Scope scope)
      {
        LocalBuilder contextMap = null;
        if (scope.HasClosedOnSymbol || scope.HasParentLocalSymbol)
        {
          contextMap = _localVars.PushTemporary(Types.PropertyMap.TypeOf);
          _ilGen.Ldloc(_context);
          _ilGen.Call(Types.DObject.GetMap);
          _ilGen.Stloc(contextMap);
        }

        foreach (var symbol in scope.Symbols)
        {
          if (symbol.IsParameter)
            continue; //This was already handled before!

          var symbolValueType = GetType(symbol);
          _ilGen.WriteComment("{0} : {1}", symbol.Name, symbolValueType);

          DeclareNonParamSymbol(symbol, symbolValueType, contextMap);

        }

        if (contextMap != null)
          _localVars.PopTemporary(contextMap);
      }

      #region AsX

      protected override void AsVoid()
      {
        _ilGen.Pop();
        _result.ValueType = mdr.ValueTypes.Unknown;
      }

      //protected override void AsBoolean()
      //{
      //  Debug.Assert(_result.ValueType == mdr.ValueTypes.Boolean, "Value on the stack is not boolean!");
      //}

      #endregion

      #region Statements; ECMA 12. -------------------------------------------------------------------------------------
      public override void Visit(VariableDeclaration node)
      {
        var stackState = _localVars.GetTemporaryStackState();
        base.Visit(node);
        _localVars.PopTemporariesAfter(stackState);
      }

      public override void Visit(ExpressionStatement node)
      {
        var stackState = _localVars.GetTemporaryStackState();
        base.Visit(node);
        _localVars.PopTemporariesAfter(stackState);
      }

      public override void Visit(IfStatement node)
      {
        PushLocation(node);

        System.Reflection.Emit.Label endLabel;
        System.Reflection.Emit.Label elseLabel = _ilGen.DefineLabel();

        var stackState = _localVars.GetTemporaryStackState();
        VisitNode(node.Condition);
        AsBoolean();
        _ilGen.Brfalse(elseLabel);
        _localVars.PopTemporariesAfter(stackState);

        VisitNode(node.Then);

        if (node.Else != null)
        {
          endLabel = _ilGen.DefineLabel();
          _ilGen.Br(endLabel);
          _ilGen.MarkLabel(elseLabel);
          VisitNode(node.Else);
          _ilGen.MarkLabel(endLabel);
        }
        else
          _ilGen.MarkLabel(elseLabel);

        PopLocation();
      }

      protected override void Loop(LoopStatement loop, Statement initilization, Expression condition, Expression increment, Statement body, bool isDoWhile)
      {
        PushLocation(loop);

        var loopBegin = _ilGen.DefineLabel();
        var loopIncrement = _ilGen.DefineLabel();
        var loopCheck = _ilGen.DefineLabel();
        var loopEnd = _ilGen.DefineLabel();

        _labelInfos.PushLoop(loop, loopEnd, loopIncrement);

        VisitNode(initilization);

        ++_loopNestLevel;

        if (!isDoWhile)
          _ilGen.Br(loopCheck);

        _ilGen.MarkLabel(loopBegin);
        VisitNode(body);

        _ilGen.MarkLabel(loopIncrement);
        if (increment != null)
        {
          var stackState = _localVars.GetTemporaryStackState();
          VisitNode(increment);
          AsVoid();
          _localVars.PopTemporariesAfter(stackState);
        }

        _ilGen.MarkLabel(loopCheck);

        if (condition != null)
        {
          VisitNode(condition);
          AsBoolean();
          _ilGen.Brtrue(loopBegin);
        }
        else
          _ilGen.Br(loopBegin);

        _ilGen.MarkLabel(loopEnd);

        --_loopNestLevel;

        _labelInfos.PopLoop(loop);

        PopLocation();
      }

      public override void Visit(LabelStatement node)
      {
        //Note: in JS, the label is for after the statements!
        var label = _ilGen.DefineLabel();
        _labelInfos.PushLabel(node, label);
        VisitNode(node.Target);
        _ilGen.MarkLabel(label);
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
          var targetLabel = isContinue ? labelInfo.ContinueTarget : labelInfo.BreakTarget;
          bool isLeavingProtectedRegion = labelInfo.ProtectedRegion != _labelInfos.CurrProtectedRegion;
          if (isLeavingProtectedRegion)
            _ilGen.Leave(targetLabel);
          else
            _ilGen.Br(targetLabel);
        }
      }

      public override void Visit(ReturnStatement node)
      {
        PushLocation(node);

        if (node.Expression != null)
        {
          var stackState = _localVars.GetTemporaryStackState();
          Ldarg_CallFrame();
          _ilGen.Ldflda(Types.CallFrame.Return);
          VisitNode(node.Expression);
          _ilGen.Call(Types.DValue.Set.Get(_result.ValueType));
          _localVars.PopTemporariesAfter(stackState);
        }

        PopLocation();

        if (_labelInfos.CurrProtectedRegion == null)
          _ilGen.Br(_epilogLabel);
        else
          _ilGen.Leave(_epilogLabel);
      }

      public override void Visit(SwitchStatement node)
      {
        PushLocation(node);

        var switchDone = _ilGen.DefineLabel();
        _labelInfos.PushLabel(null, switchDone);

        var labels = new System.Reflection.Emit.Label[node.CaseClauses.Count];
        var defaultTargetIndex = -1;


        for (var caseIndex = 0; caseIndex < node.CaseClauses.Count; ++caseIndex)
        {
          var c = node.CaseClauses[caseIndex];
          var caseTarget = _ilGen.DefineLabel();
          labels[caseIndex] = caseTarget;
          if (!c.IsDefault)
          {
            var stackState = _localVars.GetTemporaryStackState();
            VisitNode(c.Comparison);
            AsBoolean();
            _ilGen.Brtrue(caseTarget);
            _localVars.PopTemporariesAfter(stackState);
          }
          else
            defaultTargetIndex = caseIndex;
        }
        if (defaultTargetIndex != -1)
          _ilGen.Br(labels[defaultTargetIndex]);

        for (var caseIndex = 0; caseIndex < node.CaseClauses.Count; ++caseIndex)
        {
          var c = node.CaseClauses[caseIndex];
          _ilGen.MarkLabel(labels[caseIndex]);
          VisitNodes(c.Body.Statements);
        }

        _ilGen.MarkLabel(switchDone);
        _labelInfos.PopLabel(null);

        PopLocation();
      }

      public override void Visit(ThrowStatement node)
      {
        PushLocation(node);

        var stackState = _localVars.GetTemporaryStackState();
        VisitNode(node.Expression);
        _ilGen.Call(Types.JSException.Throw.Get(_result.ValueType));
        _localVars.PopTemporariesAfter(stackState);

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

        ///At this point, the exception object is on the stack, however, the JSException is not a DObject
        ///that we can work with. So, we immediately load its .Value and use that one
        _ilGen.Ldflda(Types.JSException.Value);

        DeclareSymbols(node.Scope);

        var symbol = node.Identifier.Symbol;
        Debug.Assert(symbol != null, new JSSourceLocation(_currFuncMetadata, node.Statement), string.Format("Cannot find symbol for identifier {0} in catch clause", node.Identifier));

        ///It is important not to use symbol.Name since this is effectively a new scropt and there might be some other name clash!
        ///For example in the following case
        ///try { ... } catch(e) { try { ... } catch(e) { ... } }
        ///our current implementation is wrong specially if the catch identifier is closed on. But for now, we use a simple implementation
        ///for a good complicated example, see Tests/TryCatch9.js
        var local = _localVars.Get(symbol);
        Debug.Assert(local != null && local.LocalType == Types.DValue.TypeOf, "catch symbol must have a variable of type {0}", Types.DValue.TypeOf);
        //if (local == null)
        //  local = _localVars.Declare(_localVars.Declare(Types.DValue.RefOf), symbol);
        //_ilGen.Stloc(local);
        _ilGen.Ldloca(local);
        _ilGen.Call(Types.Operations.Assign.Get(mdr.ValueTypes.DValueRef));
        VisitNode(node.Statement);

        //_localVars.Release(symbol);
        _labelInfos.PopProtectedRegion(node);
      }

      public override void Visit(FinallyClause node)
      {
        _ilGen.BeginFinallyBlock();
        VisitNode(node.Statement);
      }
      #endregion

      #region Primary Expressions; ECMA 11.1 -------------------------------------------------------------------------------------

      public override void Visit(ArrayLiteral node)
      {
        PushLocation(node);

        var arraySize = node.Items.Count;
        _ilGen.Ldc_I4(arraySize);
        _ilGen.Newobj(Types.DArray.CONSTRUCTOR_Int32); //Here we are sure that the internal array has the correct size, so we can directly .Set stuff
        if (arraySize > 0)
        {
          _ilGen.Dup(); //The one on the stack is for storage, one more to get the InternalArray
          _ilGen.Ldfld(Types.DArray.Elements);

          var elements = _localVars.PushTemporary(Types.DValue.ArrayOf);
          _ilGen.Stloc(elements);

          for (var i = 0; i < arraySize; ++i)
          {
            var stackState = _localVars.GetTemporaryStackState();
            _ilGen.Ldloc(elements);
            _ilGen.Ldc_I4(i);
            _ilGen.Ldelema(Types.DValue.TypeOf);
            VisitNode(node.Items[i]);
            _ilGen.Call(Types.DValue.Set.Get(_result.ValueType));
            _localVars.PopTemporariesAfter(stackState);
          }

          _localVars.PopTemporary(elements);
        }
        _result.ValueType = mdr.ValueTypes.Array;
        PopLocation();
      }

      public override void Visit(ObjectLiteral node)
      {
        PushLocation(node);
        _ilGen.Newobj(Types.DObject.CONSTRUCTOR);
        VisitNodes(node.Properties);
        _result.ValueType = mdr.ValueTypes.Object;
        PopLocation();
      }

      public override void Visit(PropertyAssignment node)
      {
        //We start by having an instance of the object on the stack, we should leave one at the end
        _ilGen.Dup();
        //TODO: instead we can load the fieldId(v.key). 
        _ilGen.Ldstr(node.Name);
        Debug.Assert(node.Expression != null, "At this point only data fields for object literals is supported");
        VisitNode(node.Expression);
        _ilGen.Callvirt(Types.DObject.SetField(mdr.ValueTypes.String, _result.ValueType));

      }

      public override void Visit(InlinedInvocation node)
      {
        DeclareSymbols(node.Scope);
        base.Visit(node);
      }

      public override void Visit(ReadIdentifierExpression node)
      {
        ///In this function we have to be careful specially when we are accessing a pointer. 
        ///We cannot just load address of something since we may have a situation like (x + x++), where the value
        ///of x is changed in later expressions. therefore, we have to always be careful to load the VALUE on the stack
        PushLocation(node);
        var symbol = node.Symbol;
        
        if (symbol.SymbolType == JSSymbol.SymbolTypes.OuterDuplicate)
          symbol = symbol.ResolvedSymbol;

        var local = _localVars.Get(symbol);
        var value = _localVars.PushTemporary(Types.DValue.TypeOf);

        if (local == null)
        {

          if (symbol.IsParameter)
          {
            if (_currFuncMetadata.Scope.HasArgumentsSymbol)
            {
              _ilGen.Ldloc(_arguments);
              _ilGen.Ldfld(Types.DArray.Elements);
              _ilGen.Ldc_I4(symbol.ParameterIndex);
              _ilGen.Ldelema(Types.DValue.TypeOf);
            }
            else
            {
              Ldarg_Parameter(symbol.ParameterIndex);
            }
            _ilGen.Ldobj(Types.DValue.TypeOf);
            _ilGen.Stloc(value);
          }

          else
          {
            Debug.Assert(symbol.SymbolType == JSSymbol.SymbolTypes.Unknown, "symbol {0}:{1} must have a variable", symbol.Name, symbol.SymbolType);
            var pd = _localVars.PushTemporary(Types.PropertyDescriptor.TypeOf);
            _ilGen.Ldloc(_context);
            _ilGen.Ldc_I4(symbol.FieldId);
            _ilGen.Call(Types.DObject.GetPropertyDescriptorByFieldId);
            _ilGen.Stloc(pd);

            _ilGen.Ldloc(pd);
            _ilGen.Ldloc(_context);
            _ilGen.Ldloca(value);
            _ilGen.Call(Types.PropertyDescriptor.Get_DObject_DValueRef);

            _localVars.PopTemporary(pd);
          }

          _ilGen.Ldloca(value);
          _result.ValueType = mdr.ValueTypes.DValueRef;
        }
        else
        {
          if (local.LocalType == Types.PropertyDescriptor.TypeOf)
          {
            _ilGen.Ldloc(local);
            if (symbol.SymbolType == JSSymbol.SymbolTypes.Global)
            {
              _ilGen.LoadRuntimeInstance();
              _ilGen.Ldfld(Types.Runtime.GlobalContext);
            }
            else
            {
              _ilGen.Ldloc(_context);
            }
            _ilGen.Ldloca(value);
            _ilGen.Call(Types.PropertyDescriptor.Get_DObject_DValueRef);

            _ilGen.Ldloca(value);
            _result.ValueType = mdr.ValueTypes.DValueRef;
          }
          else if (local.LocalType == Types.DValue.TypeOf)
          {

            _ilGen.Ldloc(local);
            _ilGen.Stloc(value);

            _ilGen.Ldloca(value);
            _result.ValueType = mdr.ValueTypes.DValueRef;
          }
          else if (local.LocalType == Types.DValue.RefOf)
          {

            _ilGen.Ldloc(local);
            _ilGen.Ldobj(Types.DValue.TypeOf);
            _ilGen.Stloc(value);

            _ilGen.Ldloca(value);
            _result.ValueType = mdr.ValueTypes.DValueRef;
          }
          else
          {
            _ilGen.Ldloc(local);
            _result.ValueType = Types.ValueTypeOf(local.LocalType);
            Debug.Assert(_result.ValueType < mdr.ValueTypes.DValue, "Invalid result type {0} for reading identifier {1}", _result.ValueType, symbol.Name);
          }
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

        //We push this first since we don't want to pop it later, before pushing/poping more 
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

        PerformLookup(node, obj, objType, indexType, value);

        _localVars.PopTemporariesAfter(stackState);

        _ilGen.Ldloca(value);
        _result.ValueType = mdr.ValueTypes.DValueRef;

        PopLocation();
      }

      protected virtual void PerformLookup(ReadIndexerExpression node, LocalBuilder obj, mdr.ValueTypes objType, mdr.ValueTypes indexType, LocalBuilder value)
      {
        if (objType == mdr.ValueTypes.Array && indexType == mdr.ValueTypes.Int32)
        {
          var index = _localVars.PushTemporary(indexType);
          _ilGen.Stloc(index);

          var slowPath = _ilGen.DefineLabel();
          var exitPath = _ilGen.DefineLabel();
          _ilGen.Pop();
          _ilGen.Ldloc(index);
          _ilGen.Ldc_I4_0();
          _ilGen.Blt(slowPath);
          _ilGen.Ldloc(index);
          _ilGen.Ldloc(obj);
          _ilGen.Ldfld(Types.DArray.ElementsLength);
          _ilGen.Bge(slowPath);

          _ilGen.Ldloc(obj);
          _ilGen.Ldfld(Types.DArray.Elements);
          _ilGen.Ldloc(index);
          _ilGen.Ldelema(Types.DValue.TypeOf);
          _ilGen.Ldobj(Types.DValue.TypeOf);
          _ilGen.Stloc(value);
          _ilGen.Br(exitPath);

          _localVars.PopTemporary(index);

          _ilGen.MarkLabel(slowPath);
          _ilGen.Ldloc(obj);
          _ilGen.Ldloc(index);
          _ilGen.Callvirt(Types.DObject.GetPropertyDescriptor.Get(_result.ValueType));
          _ilGen.Ldloc(obj);
          _ilGen.Ldloca(value);
          _ilGen.Callvirt(Types.PropertyDescriptor.Get_DObject_DValueRef);

          _ilGen.MarkLabel(exitPath);
        }
        else
        {
          _ilGen.Callvirt(Types.DObject.GetPropertyDescriptor.Get(_result.ValueType));
          //_ilGen.Callvirt(Types.DObject.GetField(_result.ValueType));
          _ilGen.Ldloc(obj);
          _ilGen.Ldloca(value);
          _ilGen.Callvirt(Types.PropertyDescriptor.Get_DObject_DValueRef);
        }
      }

      public override void Visit(ReadPropertyExpression node)
      {
        PushLocation(node);

        var value = _localVars.PushTemporary(Types.DValue.TypeOf);
        var stackState = _localVars.GetTemporaryStackState();

        VisitNode(node.Container);
        AsDObject();
        node.AssignFieldId();
        _ilGen.Ldc_I4(node.FieldId);
        _ilGen.Ldloca(value);
        _ilGen.Call(Types.DObject.GetFieldByFieldId_Int32_DValueRef);

        _localVars.PopTemporariesAfter(stackState);

        _ilGen.Ldloca(value);
        _result.ValueType = mdr.ValueTypes.DValueRef;

        PopLocation();
      }

      #endregion

      void Visit(UnaryExpression node, Types.MethodCache1 opearation)
      {
        PushLocation(node);

        var stackState = _localVars.GetTemporaryStackState();
        VisitNode(node.Expression);
        _localVars.PopTemporariesAfter(stackState);

        var mi = opearation.Get(_result.ValueType);
        _result.ValueType = opearation.ReturnType(_result.ValueType);

        if (_result.ValueType == mdr.ValueTypes.DValueRef)
        {
          Debug.Assert(mi.GetParameters().Length == 2 && mi.GetParameters()[1].ParameterType == Types.TypeOf(mdr.ValueTypes.DValueRef), "Invalid situation, method {0} must get a second parameter of type 'ref DValue'", mi);
          var local = _localVars.PushTemporary(mdr.ValueTypes.DValue);
          _ilGen.Ldloca(local);
          _ilGen.Call(mi);
          _ilGen.Ldloca(local);
        }
        else
        {
          _ilGen.Call(mi);
        }
        PopLocation();
      }

      #region Type Conversions; ECMA 9 -------------------------------------------------------------------------------------

      public override void Visit(ToPrimitive node) { Visit(node, Types.Operations.Convert.ToPrimitive); }

      public override void Visit(ToBoolean node) { Visit(node, Types.Operations.Convert.ToBoolean); }

      public override void Visit(ToNumber node) { Visit(node, Types.Operations.Convert.ToNumber); }

      public override void Visit(ToDouble node) { Visit(node, Types.Operations.Convert.ToDouble); }

      public override void Visit(ToInteger node) { Visit(node, Types.Operations.Convert.ToInt32); }

      public override void Visit(ToInt32 node) { Visit(node, Types.Operations.Convert.ToInt32); }

      public override void Visit(ToUInt32 node) { Visit(node, Types.Operations.Convert.ToUInt32); }

      public override void Visit(ToUInt16 node) { throw new NotImplementedException(); }

      public override void Visit(ToString node) { Visit(node, Types.Operations.Convert.ToString); }

      public override void Visit(ToObject node) { Visit(node, Types.Operations.Convert.ToObject); }

      public override void Visit(ToFunction node) { Visit(node, Types.Operations.Convert.ToFunction); }
      //{
      //  Visit(node, Types.Operations.Convert.ToObject);
      //  _ilGen.Callvirt(Types.DObject.ToDFunction);
      //  _result.ValueType = mdr.ValueTypes.Function;
      //}

      #endregion

      #region Unary Operators; ECMA 11.4 -------------------------------------------------------------------------------------

      public override void Visit(DeleteExpression node)
      {
        var expression = node.Expression;
        var parent = expression as ParenExpression;
        if (parent != null)
          expression = parent.Expression;
        var indexer = expression as ReadIndexerExpression;
        if (indexer != null)
        {
          PushLocation(node);
          var stackState = _localVars.GetTemporaryStackState();
          VisitNode(indexer.Container);
          var containerType = _result.ValueType;
          VisitNode(indexer.Index);
          var indexType = _result.ValueType;
          _localVars.PopTemporariesAfter(stackState);
          _ilGen.Call(Types.Operations.Unary.DeleteProperty.Get(containerType, indexType));
          _result.ValueType = mdr.ValueTypes.Boolean;
          PopLocation();
        }
        else
        {
          //we need to visit in case the expression has side effects, but then throw away result
          var readId = expression as ReadIdentifierExpression;
          if (readId != null)
          {
            PushLocation(node);
            var stackState = _localVars.GetTemporaryStackState();
            VisitNode(readId);
            AsVoid();
            _localVars.PopTemporariesAfter(stackState);

            _ilGen.Ldloc(_context);
            _ilGen.Ldc_I4(readId.Symbol.FieldId);
            _ilGen.Call(Types.Operations.Unary.DeleteVariable.Get(mdr.ValueTypes.Object, mdr.ValueTypes.Int32));
            _result.ValueType = mdr.ValueTypes.Boolean;
            PopLocation();
          }
          else
            Visit(node, Types.Operations.Unary.Delete);
        }
      }

      public override void Visit(VoidExpression node) { Visit(node, Types.Operations.Unary.Void); }

      public override void Visit(TypeofExpression node) { Visit(node, Types.Operations.Unary.Typeof); }

      public override void Visit(PositiveExpression node) { Visit(node, Types.Operations.Unary.Positive); }

      public override void Visit(NegativeExpression node) { Visit(node, Types.Operations.Unary.Negative); }

      public override void Visit(BitwiseNotExpression node) { Visit(node, Types.Operations.Unary.BitwiseNot); }

      public override void Visit(LogicalNotExpression node) { Visit(node, Types.Operations.Unary.LogicalNot); }

      #endregion

      void Visit(BinaryExpression node, Types.MethodCache2 operation)
      {
        PushLocation(node);

        var stackState = _localVars.GetTemporaryStackState();
        VisitNode(node.Left);
        var leftType = _result.ValueType;
        VisitNode(node.Right);
        var rightType = _result.ValueType;
        _localVars.PopTemporariesAfter(stackState);

        var mi = operation.Get(leftType, rightType);
        _result.ValueType = operation.ReturnType(leftType, rightType);
        if (_result.ValueType == mdr.ValueTypes.DValueRef)
        {
          Debug.Assert(mi.GetParameters().Length == 3 && mi.GetParameters()[2].ParameterType == Types.TypeOf(mdr.ValueTypes.DValueRef), "Invalid situation, method {0} must get a third parameter of type 'ref DValue'", mi);
          var local = _localVars.PushTemporary(mdr.ValueTypes.DValue);
          _ilGen.Ldloca(local);
          _ilGen.Call(mi);
          _ilGen.Ldloca(local);
        }
        else
        {
          _ilGen.Call(mi);
        }
        PopLocation();
      }

      #region Binary Multiplicative Operators; ECMA 11.5 -------------------------------------------------------------------------------------

      public override void Visit(MultiplyExpression node) { Visit(node, Types.Operations.Binary.Multiply); }

      public override void Visit(DivideExpression node) { Visit(node, Types.Operations.Binary.Divide); }

      public override void Visit(RemainderExpression node) { Visit(node, Types.Operations.Binary.Remainder); }

      #endregion
      #region Binary Additive Operators; ECMA 11.6 -------------------------------------------------------------------------------------

      public override void Visit(AdditionExpression node) { Visit(node, Types.Operations.Binary.Addition); }

      public override void Visit(SubtractionExpression node) { Visit(node, Types.Operations.Binary.Subtraction); }

      #endregion
      #region Binary Bitwise Shift Operators; ECMA 11.7 -------------------------------------------------------------------------------------

      public override void Visit(LeftShiftExpression node) { Visit(node, Types.Operations.Binary.LeftShift); }

      public override void Visit(RightShiftExpression node) { Visit(node, Types.Operations.Binary.RightShift); }

      public override void Visit(UnsignedRightShiftExpression node) { Visit(node, Types.Operations.Binary.UnsignedRightShift); }

      #endregion
      #region Binary Relational Operators; ECMA 11.8 -------------------------------------------------------------------------------------

      public override void Visit(LesserExpression node) { Visit(node, Types.Operations.Binary.LessThan); }

      public override void Visit(GreaterExpression node) { Visit(node, Types.Operations.Binary.GreaterThan); }

      public override void Visit(LesserOrEqualExpression node) { Visit(node, Types.Operations.Binary.LessThanOrEqual); }

      public override void Visit(GreaterOrEqualExpression node) { Visit(node, Types.Operations.Binary.GreaterThanOrEqual); }

      public override void Visit(InstanceOfExpression node) { Visit(node, Types.Operations.Binary.InstanceOf); }

      public override void Visit(InExpression node) { Visit(node, Types.Operations.Binary.In); }

      #endregion
      #region Binary Equality Operators; ECMA 11.9 -------------------------------------------------------------------------------------

      public override void Visit(EqualExpression node) { Visit(node, Types.Operations.Binary.Equal); }

      public override void Visit(NotEqualExpression node) { Visit(node, Types.Operations.Binary.NotEqual); }

      public override void Visit(SameExpression node) { Visit(node, Types.Operations.Binary.Same); }

      public override void Visit(NotSameExpression node) { Visit(node, Types.Operations.Binary.NotSame); }

      #endregion
      #region Binary Bitwise Operators; ECMA 11.10 -------------------------------------------------------------------------------------

      public override void Visit(BitwiseAndExpression node) { Visit(node, Types.Operations.Binary.BitwiseAnd); }

      public override void Visit(BitwiseOrExpression node) { Visit(node, Types.Operations.Binary.BitwiseOr); }

      public override void Visit(BitwiseXorExpression node) { Visit(node, Types.Operations.Binary.BitwiseXor); }

      #endregion
      //#region Binary Logical Operators; ECMA 11.11 -------------------------------------------------------------------------------------

      //public override void Visit(LogicalAndExpression node) {}

      //public override void Visit(LogicalOrExpression node) {}

      //#endregion

      #region Conditional Operators; ECMA 11.12 -------------------------------------------------------------------------------------

      public override void Visit(TernaryExpression node)
      {
        PushLocation(node);

        var elseLabel = _ilGen.DefineLabel();
        var endLabel = _ilGen.DefineLabel();

        VisitNode(node.Left);
        AsBoolean();
        _ilGen.Brfalse(elseLabel);

        ///We don't know if the type of left & right are different or not, we start by assuming they are the same type
        ///and then if not, patch the code

        var stackState1 = _localVars.GetTemporaryStackState();
        VisitNode(node.Middle);
        _localVars.PopTemporariesAfter(stackState1);
        var middleValueType = _result.ValueType;
        var middleValue = _localVars.PushTemporary(middleValueType);
        _ilGen.Stloc(middleValue);
        _ilGen.Br(endLabel);

        _ilGen.MarkLabel(elseLabel);
        var stackState2 = _localVars.GetTemporaryStackState();
        VisitNode(node.Right);
        _localVars.PopTemporariesAfter(stackState2);
        var rightValueType = _result.ValueType;

        if (middleValueType == rightValueType)
        {
          ///Hopefully this is the common case with the least complication
          _ilGen.Stloc(middleValue);

          _ilGen.MarkLabel(endLabel);
          _ilGen.Ldloc(middleValue);
          _result.ValueType = middleValueType;
        }
        else
        {
          _localVars.PopTemporariesAfter(stackState1); //release middle middleValue temporary, it is ok if we reuse the variable here
          var resultValueType = TypeCalculator.ResolveType(middleValueType, rightValueType);
          if (resultValueType == mdr.ValueTypes.DValueRef)
          {
            var resultValue = _localVars.PushTemporary(Types.DValue.TypeOf);
            _ilGen.Ldloca(resultValue);
            _ilGen.Call(Types.Operations.Assign.Get(rightValueType));
            var endPatchLabel = _ilGen.DefineLabel();
            _ilGen.Br(endPatchLabel);

            _ilGen.MarkLabel(endLabel);
            _ilGen.Ldloc(middleValue);
            _ilGen.Ldloca(resultValue);
            _ilGen.Call(Types.Operations.Assign.Get(middleValueType));

            _ilGen.MarkLabel(endPatchLabel);
            _ilGen.Ldloca(resultValue);
            _result.ValueType = mdr.ValueTypes.DValueRef;
          }
          else
          {
            var needConvertion = false;
            if (resultValueType == mdr.ValueTypes.Object)
            {
              Debug.Assert(
                (mdr.ValueTypesHelper.IsObject(middleValueType) || middleValueType == mdr.ValueTypes.Null)
                && (mdr.ValueTypesHelper.IsObject(rightValueType) || rightValueType == mdr.ValueTypes.Null)
                , "Invalid situation! result type {0} does not match {1} and {2}", resultValueType, middleValueType, rightValueType);
            }
            else
            {
              Debug.Assert(
                mdr.ValueTypesHelper.IsNumber(resultValueType)
                && (mdr.ValueTypesHelper.IsNumber(middleValueType) || middleValueType == mdr.ValueTypes.Boolean)
                && (mdr.ValueTypesHelper.IsNumber(rightValueType) || rightValueType == mdr.ValueTypes.Boolean)
                , "Invalid situation! result type {0} does not match {1} and {2}", resultValueType, middleValueType, rightValueType);
              needConvertion = true;
            }

            var resultValue = _localVars.PushTemporary(resultValueType);
            if (needConvertion)
              _ilGen.Call(Types.ClrSys.Convert.Get(rightValueType, resultValueType));
            _ilGen.Stloc(resultValue);
            var endPatchLabel = _ilGen.DefineLabel();
            _ilGen.Br(endPatchLabel);

            _ilGen.MarkLabel(endLabel);
            _ilGen.Ldloc(middleValue);
            if (needConvertion)
              _ilGen.Call(Types.ClrSys.Convert.Get(middleValueType, resultValueType));
            _ilGen.Stloc(resultValue);

            _ilGen.MarkLabel(endPatchLabel);
            _ilGen.Ldloc(resultValue);
            _result.ValueType = resultValueType;
          }
        }

        //var trueValueType = node.Middle.ValueType;
        //var falseValueType = node.Right.ValueType;
        //if (trueValueType == falseValueType)
        //{
        //  //We can optioally just generate code in each BB and leave it on stack, but then
        //  //in debug mode, the source generator will think that stuff is left on the stack and generate wrong code
        //  //So, we just always store into a local variable

        //  var result = _localVars.PushTemporary(trueValueType);

        //  VisitNode(node.Middle);
        //  Debug.Assert(_resultType == trueValueType, "actual type {0} differ from expected type {1}", _resultType, trueValueType);
        //  _ilGen.Stloc(result);
        //  _ilGen.Br(endLabel);

        //  _ilGen.MarkLabel(elseLabel);
        //  VisitNode(node.Right);
        //  Debug.Assert(_resultType == falseValueType, "actual type {0} differ from expected type {1}", _resultType, falseValueType);
        //  _ilGen.Stloc(result);

        //  _ilGen.MarkLabel(endLabel);
        //  _ilGen.Ldloc(result);
        //  _resultType = trueValueType;
        //}
        //else
        //{
        //  var result = _localVars.PushTemporary(Types.DValue.TypeOf);

        //  var stackState = _localVars.GetTemporaryStackState();
        //  _ilGen.Ldloca(result);
        //  VisitNode(node.Middle);
        //  Debug.Assert(_resultType == trueValueType, "actual type {0} differ from expected type {1}", _resultType, trueValueType);
        //  _ilGen.Call(Types.DValue.Set(trueValueType));
        //  _localVars.PopTemorariesAfter(stackState);
        //  _ilGen.Br(endLabel);

        //  _ilGen.MarkLabel(elseLabel);
        //  stackState = _localVars.GetTemporaryStackState();
        //  _ilGen.Ldloca(result);
        //  VisitNode(node.Right);
        //  Debug.Assert(_resultType == falseValueType, "actual type {0} differ from expected type {1}", _resultType, falseValueType);
        //  _ilGen.Call(Types.DValue.Set(falseValueType));
        //  _localVars.PopTemorariesAfter(stackState);

        //  _ilGen.MarkLabel(endLabel);
        //  _ilGen.Ldloca(result);
        //  _resultType = mdr.ValueTypes.DValueRef;
        //}

        PopLocation();
      }

      #endregion

      #region Assignment; ECMA 11.13 -------------------------------------------------------------------------------------

      public override void Visit(WriteTemporaryExpression node)
      {
        var local = _localVars.Get(node);
        if (local == null)
        {
          //First visit
          Debug.Assert(node.Users != null && node.Users.Count > 1, "Invalid situation, temporary must have more than one user!");
          VisitNode(node.Value);
          _ilGen.Dup();
          if (_result.ValueType == mdr.ValueTypes.DValueRef)
          {
            local = _localVars.Declare(mdr.ValueTypes.DValue, node);
            _ilGen.Ldloca(local);
            _ilGen.Call(Types.Operations.Assign.Get(_result.ValueType));
          }
          else if (_result.ValueType == mdr.ValueTypes.Unknown || _result.ValueType == mdr.ValueTypes.Any)
          {
            local = _localVars.Declare(_result.Type, node);
            _ilGen.Stloc(local);
          }
          else
          {
            local = _localVars.Declare(_result.ValueType, node);
            _ilGen.Stloc(local);
          }
        }
        else
        {
          //this was already visited
          if (local.LocalType == Types.DValue.TypeOf)
          {
            _ilGen.Ldloca(local);
            _result.ValueType = mdr.ValueTypes.DValueRef;
          }
          else
          {
            _ilGen.Ldloc(local);
            _result.Type = local.LocalType;
          }
        }
      }

      public override void Visit(WriteIdentifierExpression node)
      {
        PushLocation(node);
        VisitNode(node.Value);
        var valueType = _result.ValueType;

        _ilGen.Dup();

        var symbol = node.Symbol;
        if (symbol.SymbolType == JSSymbol.SymbolTypes.OuterDuplicate)
          symbol = symbol.ResolvedSymbol;

        var local = _localVars.Get(symbol);
        if (local == null)
        {
          if (symbol.IsParameter)
          {
            if (_currFuncMetadata.Scope.HasArgumentsSymbol)
            {
              _ilGen.Ldloc(_arguments);
              _ilGen.Ldfld(Types.DArray.Elements);
              _ilGen.Ldc_I4(symbol.ParameterIndex);
              _ilGen.Ldelema(Types.DValue.TypeOf);
            }
            else
            {
              Ldarg_Parameter(symbol.ParameterIndex); //this function changes the _result
            }
            _ilGen.Call(Types.Operations.Assign.Get(valueType));
          }
          else
          {
            Debug.Assert(symbol.SymbolType == JSSymbol.SymbolTypes.Unknown, "symbol {0}:{1} must have a variable", symbol.Name, symbol.SymbolType);
            var value = _localVars.PushTemporary(_result.ValueType);
            _ilGen.Stloc(value);

            var pd = _localVars.PushTemporary(Types.PropertyDescriptor.TypeOf);
            _ilGen.Ldloc(_context);
            _ilGen.Ldc_I4(symbol.FieldId);
            _ilGen.Call(Types.DObject.GetPropertyDescriptorByFieldId);
            _ilGen.Stloc(pd);

            var undefined = _ilGen.DefineLabel();
            var done = _ilGen.DefineLabel();
            _ilGen.Ldloc(pd);
            _ilGen.Call(Types.PropertyDescriptor.IsUndefined);
            _ilGen.Brtrue(undefined);

            _ilGen.Ldloc(pd);
            _ilGen.Ldloc(_context);
            _ilGen.Ldloc(value);
            _ilGen.Call(Types.PropertyDescriptor.Set.Get(mdr.ValueTypes.Object, _result.ValueType));
            _ilGen.Br(done);

            _ilGen.MarkLabel(undefined);
            _ilGen.LoadRuntimeInstance();
            _ilGen.Ldfld(Types.Runtime.GlobalContext);
            _ilGen.Ldc_I4(symbol.FieldId);
            _ilGen.Ldloc(value);
            _ilGen.Call(Types.DObject.SetFieldByFieldId(_result.ValueType));

            _ilGen.MarkLabel(done);

            _localVars.PopTemporary(pd);
            _localVars.PopTemporary(value);
          }
        }
        else
        {
          if (local.LocalType == Types.PropertyDescriptor.TypeOf)
          {
            var value = _localVars.PushTemporary(_result.ValueType);
            _ilGen.Stloc(value);

            _ilGen.Ldloc(local);
            if (symbol.SymbolType == JSSymbol.SymbolTypes.Global)
            {
              _ilGen.LoadRuntimeInstance();
              _ilGen.Ldfld(Types.Runtime.GlobalContext);
            }
            else
            {
              _ilGen.Ldloc(_context);
            }
            _ilGen.Ldloc(value);
            _ilGen.Call(Types.PropertyDescriptor.Set.Get(mdr.ValueTypes.Object, _result.ValueType));
            _localVars.PopTemporary(value);
          }
          else if (local.LocalType == Types.DValue.TypeOf)
          {
            _ilGen.Ldloca(local);
            _ilGen.Call(Types.Operations.Assign.Get(_result.ValueType));
          }
          else if (local.LocalType == Types.DValue.RefOf)
          {
            _ilGen.Ldloc(local);
            _ilGen.Call(Types.Operations.Assign.Get(_result.ValueType));
          }
          else
          {
            if (local.LocalType != _result.Type)
            {
              if ((_result.ValueType == mdr.ValueTypes.Array ||
                   _result.ValueType == mdr.ValueTypes.Null ||
                   _result.ValueType == mdr.ValueTypes.Undefined) &&
                   Types.ValueTypeOf(local.LocalType) == mdr.ValueTypes.Object)
                _ilGen.Castclass(Types.TypeOf(mdr.ValueTypes.Object));
              else
                _ilGen.Call(Types.ClrSys.Convert.Get(_result.ValueType, Types.ValueTypeOf(local.LocalType)));
            }

            _ilGen.Stloc(local);
          }
        }
        _result.ValueType = valueType; //since we .Dup(), this ensures that final value type is correct
        PopLocation();
      }

      public override void Visit(WriteIndexerExpression node)
      {
        PushLocation(node);

        var stackState = _localVars.GetTemporaryStackState();

        VisitNode(node.Container);
        AsDObject();
        var objType = _result.ValueType;
        var obj = _localVars.PushTemporary(objType);
        _ilGen.Dup();
        _ilGen.Stloc(obj);

        VisitNode(node.Index);
        var indexType = _result.ValueType;
        var index = _localVars.PushTemporary(indexType);
        _ilGen.Dup();
        _ilGen.Stloc(index);

        VisitNode(node.Value);
        var valueType = _result.ValueType;
        var value = _localVars.PushTemporary(valueType);
        _ilGen.Stloc(value);
        _ilGen.Pop();
        _ilGen.Pop();

        if (objType == mdr.ValueTypes.Array &&
          indexType == mdr.ValueTypes.Int32)
        {
          var slowPath = _ilGen.DefineLabel();
          var exitPath = _ilGen.DefineLabel();
          _ilGen.Ldloc(index);
          _ilGen.Ldc_I4_0();
          _ilGen.Blt(slowPath);
          _ilGen.Ldloc(index);
          _ilGen.Ldloc(obj);
          _ilGen.Ldfld(Types.DArray.ElementsLength);
          _ilGen.Bge(slowPath);

          _ilGen.Ldloc(obj);
          _ilGen.Ldfld(Types.DArray.Elements);
          _ilGen.Ldloc(index);
          _ilGen.Ldelema(Types.DValue.TypeOf);
          _ilGen.Ldloc(value);
          _ilGen.Call(Types.DValue.Set.Get(valueType));
          _ilGen.Br(exitPath);

          _ilGen.MarkLabel(slowPath);
          _ilGen.Ldloc(obj);
          _ilGen.Ldloc(index);
          _ilGen.Callvirt(Types.DObject.AddPropertyDescriptor.Get(indexType));
          _ilGen.Ldloc(obj);
          _ilGen.Ldloc(value);
          //_ilGen.Call(Types.PropertyDescriptor.Set.Get(objType, valueType));
          Debug.Assert(mdr.ValueTypesHelper.IsObject(objType), "Invalid situation, expexted object value type here");
          _ilGen.Call(Types.PropertyDescriptor.Set.Get(mdr.ValueTypes.Object, valueType));

          _ilGen.MarkLabel(exitPath);
        }
        else
        {
          _ilGen.Ldloc(obj);
          _ilGen.Ldloc(index);
          _ilGen.Callvirt(Types.DObject.AddPropertyDescriptor.Get(indexType));
          _ilGen.Ldloc(obj);
          _ilGen.Ldloc(value);
          //_ilGen.Call(Types.PropertyDescriptor.Set.Get(objType, valueType));
          Debug.Assert(mdr.ValueTypesHelper.IsObject(objType), "Invalid situation, expexted object value type here");
          _ilGen.Call(Types.PropertyDescriptor.Set.Get(mdr.ValueTypes.Object, valueType));
          //_ilGen.Call(Types.DObject.SetField(indexType, valueType));
        }
        _localVars.PopTemporariesAfter(stackState);
        _localVars.PushTemporary(value); //We just release all vars, but need to make sure we put back the value variable for later use

        _ilGen.Ldloc(value);
        _result.ValueType = valueType;

        PopLocation();
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
        _ilGen.Ldloc(obj);
        node.AssignFieldId();
        _ilGen.Ldc_I4(node.FieldId);
        VisitNode(node.Value);
        var valueType = _result.ValueType;

        _localVars.PopTemporariesAfter(stackState);
        var value = _localVars.PushTemporary(valueType);
        _ilGen.Stloc(value);

        _ilGen.Ldloc(value);
        _ilGen.Call(Types.DObject.SetFieldByFieldId(valueType));

        _ilGen.Ldloc(value);
        _result.ValueType = valueType;

        PopLocation();
      }

      #endregion

      #region Function Calls; ECMA 11.2.2, 11.2.3 -------------------------------------------------------------------------------------

      protected virtual LocalBuilder CreateCallFrame(Invocation node)
      {
        var callFrame = _localVars.PushTemporary(Types.CallFrame.TypeOf);

        _ilGen.Ldloca(callFrame);
        _ilGen.Initobj(Types.CallFrame.TypeOf);

        _ilGen.Ldloca(callFrame);
        VisitNode(node.Function);
        Debug.Assert(_result.ValueType == mdr.ValueTypes.Function, "Invalid situation, {0} is not a function value type", node.Function);
        _ilGen.Stfld(Types.CallFrame.Function);

        _ilGen.Ldloca(callFrame);
        _ilGen.Ldflda(Types.CallFrame.Signature);
        _ilGen.Ldc_U8(0);
        _ilGen.Stfld(Types.DFunctionSignature.Value);


        _ilGen.Ldloca(callFrame);
        _ilGen.Ldc_I4(node.Arguments.Count);
        _ilGen.Stfld(Types.CallFrame.PassedArgsCount);

        var argsArraySize = node.Arguments.Count - mdr.CallFrame.InlineArgsCount;
        if (argsArraySize > 0)
        {
          _ilGen.Ldloca(callFrame);
          _ilGen.Ldc_I4(argsArraySize);
          _ilGen.NewArr(Types.DValue.TypeOf);
          _ilGen.Stfld(Types.CallFrame.Arguments);
        }

        if (JSRuntime.Instance.Configuration.ProfileStats)
          GenCounterIncrement("Args_" + node.Arguments.Count.ToString(), null, 1);

        return callFrame;
      }

      mdr.DFunctionCode LoadArguments(Invocation node, LocalBuilder callFrame)
      {
        var knownStaticSignature = mdr.DFunctionSignature.EmptySignature;
        var staticallyTypedArgsCount = 0;


        for (var argIndex = 0; argIndex < node.Arguments.Count; ++argIndex)
        {
          var passedArg = node.Arguments[argIndex];

          _ilGen.Ldloca(callFrame);
          Ldarg_Parameter(argIndex, true);
          VisitNode(passedArg);
          var argType = _result.ValueType;
          _ilGen.Call(Types.DValue.Set.Get(argType));

          if (argType < mdr.ValueTypes.DValue)
          {
            knownStaticSignature.InitArgType(argIndex, argType);
            ++staticallyTypedArgsCount;
          }
          else
          {
            _ilGen.Ldloca(callFrame);
            _ilGen.Ldflda(Types.CallFrame.Signature);
            _ilGen.Ldc_I4(argIndex);
            _ilGen.Ldloca(callFrame);
            Ldarg_Parameter(argIndex, true);
            _ilGen.Call(Types.DValue.GetValueType);
            _ilGen.Call(Types.DFunctionSignature.InitArgType);
          }
        }

        if (staticallyTypedArgsCount > 0)
        {
          //Merge static signature with dynamically calculated one.
          _ilGen.Ldloca(callFrame);
          _ilGen.Ldflda(Types.CallFrame.Signature);
#if DEBUG
          //This is to prevent error in our source code generators
          _ilGen.Ldloca(callFrame);
          _ilGen.Ldflda(Types.CallFrame.Signature);
#else
          _ilGen.Dup();
#endif
          _ilGen.Ldfld(Types.DFunctionSignature.Value);
          _ilGen.Ldc_U8(knownStaticSignature.Value);
          _ilGen.Or();
          _ilGen.Stfld(Types.DFunctionSignature.Value);
        }

        ///If parallel jit is enabled, we try to launch jitting for possible callee functions
        //if (JSRuntime.Instance.Configuration.EnableParallelJit)
        //{
        //  string lookupKey = null;
        //  var id = function as Identifier;
        //  if (id != null)
        //    lookupKey = id.Text;
        //  else
        //  {
        //    var indexer = function as Indexer;
        //    if (indexer != null && indexer.FieldId != mdr.Runtime.InvalidFieldId)
        //      lookupKey = mdr.Runtime.Instance.GetFieldName(indexer.FieldId);
        //  }
        //  if (lookupKey != null)
        //  {
        //    var funcs = JSRuntime.Instance.Speculator.Get(lookupKey);
        //    if (funcs != null)
        //      foreach (var f in funcs)
        //      {
        //        bool dummy;
        //        f.Jit(ref knownStaticSignature, out dummy);
        //      }
        //  }
        //  else
        //  {
        //    var funcExp = function as FunctionExpression;
        //    if (funcExp != null)
        //    {
        //      bool dummy;
        //      funcExp.Metadata.Jit(ref knownStaticSignature, out dummy);
        //    }
        //  }
        //}

        var targetFuncMetadata = node.TargetFunctionMetadata;
        bool canEmitDirectCalls =
          targetFuncMetadata != null
          && JSRuntime.Instance.Configuration.EnableDirectCalls
          && staticallyTypedArgsCount == node.Arguments.Count;
        if (
            canEmitDirectCalls
            && targetFuncMetadata.CurrentStatus != JSFunctionMetadata.Status.Jitting //FIXME: for now we disable recursive direct calls because CreateDelegate may throw if current method calls unfinised methods
            )
        {
          StopJitTimer();
          //knownStaticSignature.Value |= unknownStaticSignature.Value; //We now can messup with this signature.
          Debug.WriteLine("Looking for {0} with sig=0x{1:X}", targetFuncMetadata.Declaration, knownStaticSignature.Value);
          var targetCode = targetFuncMetadata.JitSpeculatively(ref knownStaticSignature);
          StartJitTimer();

          if (targetCode != null)
          {
            //We have everythihng ready on the stack and all params are statically typed
            //So we can directly call the target function
            //TODO: we can even use this situation for return type calculation.

            if (targetCode.Method == null)
            {
              //TODO: add targetcode to list to wait for it!
            }

            //var targetCode = targetFuncMetadata.UniqueInstance;
            //if (targetCode == null)
            //{
            //    targetCode = targetFuncMetadata.Cache.Get(staticSignature.Value);
            //    if (targetCode == null)
            //        targetCode = expression.TargetFunctionMetadata.Jit(ref staticSignature);
            //}
          }
          return targetCode;
        }
        return null;
      }

      public override void Visit(NewExpression node)
      {
        PushLocation(node);

        var stackState = _localVars.GetTemporaryStackState();

        var callFrame = CreateCallFrame(node);
        LoadArguments(node, callFrame);

        GenExecTimerStop(true);

        //TODO: technically, we should lookup ".constructor" field, and if it exists, call ".ToDFunction().Construct()" on it.
        _ilGen.Ldloca(callFrame);
        _ilGen.Ldfld(Types.CallFrame.Function);
        _ilGen.Ldloca(callFrame);
        _ilGen.Callvirt(Types.DFunction.Construct);

        GenExecTimerStart(true);

        _localVars.PopTemporariesAfter(stackState);
        //We need to immediately copy the return value on caller's stack before it gets destroyed
        var result = _localVars.PushTemporary(Types.DObject.TypeOf);
        _ilGen.Ldloca(callFrame);
        _ilGen.Ldfld(Types.CallFrame.This);
        _ilGen.Stloc(result);

        _ilGen.Ldloc(result);
        _result.ValueType = mdr.ValueTypes.Object;

        PopLocation();
      }

      public override void Visit(CallExpression node)
      {
        if (node.InlinedIR != null)
        {
          VisitNode(node.InlinedIR);
          return;
        }

        PushLocation(node);

        var stackState = _localVars.GetTemporaryStackState();

        var callFrame = CreateCallFrame(node);

        if (node.ThisArg != null)
        {
          _ilGen.Ldloca(callFrame);
          VisitNode(node.ThisArg);
          AsDObject();
          _ilGen.Stfld(Types.CallFrame.This);
        }
        else if (node.IsDirectEvalCall)
        {
          //This could be direct eval call
          _ilGen.Ldloca(callFrame);
          Ldarg_CallFrame();
          _ilGen.Ldfld(Types.CallFrame.Function);
          _ilGen.Stfld(Types.CallFrame.CallerFunction);

          _ilGen.Ldloca(callFrame);
          _ilGen.Ldloc(_context);
          _ilGen.Stfld(Types.CallFrame.CallerContext);

          _ilGen.Ldloca(callFrame);
          Ldarg_CallFrame();
          _ilGen.Ldfld(Types.CallFrame.This);
          _ilGen.Stfld(Types.CallFrame.This);
        }
        else //if (!node.IsDirectEvalCall)
        {
          //TODO: can move this to the callee prolog and only assig global obj in case .This is null; of course that requires we guarantee .This is always reset to null
          _ilGen.Ldloca(callFrame);
          _ilGen.LoadRuntimeInstance();
          _ilGen.Ldfld(Types.Runtime.GlobalContext);
          _ilGen.Stfld(Types.CallFrame.This);
        }

        var targetCode = LoadArguments(node, callFrame);

        GenExecTimerStop(true);
        if (targetCode != null)
        {
          //We have everythihng ready on the stack and all params are statically typed
          //So we can directly call the target function
          //TODO: we can even use this situation for return type calculation.
          _ilGen.Ldloca(callFrame);
          _ilGen.Call(targetCode.MethodHandle);
        }
        else
        {
          _ilGen.Ldloca(callFrame);
          _ilGen.Ldfld(Types.CallFrame.Function);
          _ilGen.Ldloca(callFrame);
          _ilGen.Call(Types.DFunction.Call);
        }
        GenExecTimerStart(true);

        _localVars.PopTemporariesAfter(stackState);
        //We need to immediately copy the return value on caller's stack before it gets destroyed
        var result = _localVars.PushTemporary(Types.DValue.TypeOf);

        _ilGen.Ldloca(callFrame);
        _ilGen.Ldflda(Types.CallFrame.Return);
        _ilGen.Ldobj(Types.DValue.TypeOf);
        _ilGen.Stloc(result);

        _ilGen.Ldloca(result);
        _result.ValueType = mdr.ValueTypes.DValueRef;

        PopLocation();
      }

      #endregion

      #region Function Definition; ECMA 13 -------------------------------------------------------------------------------------

      public override void Visit(FunctionExpression node)
      {
        PushLocation(node);

        var impIndex = node.Metadata.FuncDefinitionIndex;
        Ldarg_CallFrame();
        _ilGen.Ldfld(Types.CallFrame.Function);
        _ilGen.Call(Types.DFunction.GetMetadata);
        _ilGen.Castclass(Types.JSFunctionMetadata.TypeOf);
        _ilGen.Ldc_I4(impIndex);
        _ilGen.Call(Types.JSFunctionMetadata.GetSubFunction);
        _ilGen.Ldloc(_context);
        _ilGen.Newobj(Types.DFunction.CONSTRUCTOR_DFunctionMetadata_DObject);
        _result.ValueType = mdr.ValueTypes.Function;

        PopLocation();
      }

      #endregion

      #region Interanls

      public override void Visit(InternalCall node)
      {
        PushLocation(node);

        VisitNodes(node.Arguments);
        _ilGen.Call(node.Method);
        _result.Type = node.Method.ReturnType;

        PopLocation();
      }

      public override void Visit(InternalNew node)
      {
        PushLocation(node);

        VisitNodes(node.Arguments);
        _ilGen.Newobj(node.Constructor);
        _result.Type = node.Constructor.DeclaringType;

        PopLocation();
      }

      #endregion
    }

  }
}

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
  class CodeGeneratorBase
  {
    protected abstract class CodeGeneratorBaseImp : DepthFirstVisitor, AlgorithmImplementation
    {
      public void Execute(JSFunctionMetadata funcMetadata) 
      {
        _currFuncMetadata = funcMetadata;
        _currFuncCode = null;
        _ilGen = null;
        Execute();
      }
      public void Execute(CodeGenerationInfo cgInfo)
      {
        _currFuncMetadata = cgInfo.FuncMetadata;
        _currFuncCode = cgInfo.FuncCode;
        _ilGen = cgInfo.IlGen;
        Execute();
      }

      protected JSFunctionMetadata _currFuncMetadata;
      protected JSFunctionCode _currFuncCode;

      protected ILGen.BaseILGenerator _ilGen;

      protected int _loopNestLevel;
      protected LocalVarManager _localVars = new LocalVarManager();

      protected struct Result
      {
        Type _type;
        mdr.ValueTypes _valueType;

        public Type Type
        {
          get
          {
            if (_type == null)
              _type = Types.TypeOf(_valueType);
            return _type;
          }

          set
          {
            _type = value;
            _valueType = Types.ValueTypeOf(value, false);
          }
        }

        public mdr.ValueTypes ValueType
        {
          get { return _valueType; }
          set
          {
            _valueType = value;
            _type = null; //This is the common case, we just don't want to pay the overhead
          }
        }
      }
      protected Result _result = new Result();

      protected virtual void Execute()
      {
        ExecuteInitialize();

        PushLocation(_currFuncMetadata.FunctionIR.Statement);

        GenProlog();

        GenBody();

        PopLocation();

        GenEpilog();

        ExecuteFinalize();
      }
      protected virtual void ExecuteInitialize()
      {
        _loopNestLevel = 0;
        _localVars.Init(_ilGen);
      }
      protected virtual void ExecuteFinalize()
      {
        _localVars.Clear();
        //if (JSRuntime.Instance.Configuration.ProfileJitTime)
        //  StopJitTimer();
      }

      #region Timers & Counters
      protected void GenCounterIncrement(string name, string notes, int increment)
      {
        int counterId = JSRuntime.Instance.Counters.FindId(name, notes);

        _ilGen.LoadRuntimeInstance();
        _ilGen.Ldfld(Types.Runtime.Counters);
        _ilGen.Ldc_I4(counterId);
        _ilGen.Call(Types.Util.Counters.GetCounter_Int32);
        _ilGen.Dup();
        _ilGen.Ldfld(Types.Util.Counters.Counter.Count);
        _ilGen.Ldc_I8(increment);
        _ilGen.Add();
        _ilGen.Stfld(Types.Util.Counters.Counter.Count);
      }

      m.Util.Timers.Timer _jitTimeCounter;
      protected void StartJitTimer(string name)
      {
        //names[0] = string.Format("{0}.{1}", System.Threading.Thread.CurrentThread.ManagedThreadId, names[0]);
        _jitTimeCounter = JSRuntime.StartTimer(JSRuntime.Instance.Configuration.ProfileJitTime, name);
      }
      protected void StartJitTimer() { if (_jitTimeCounter != null) _jitTimeCounter.Start(); }
      protected void StopJitTimer() { if (_jitTimeCounter != null) _jitTimeCounter.Stop(); }

      protected LocalBuilder GenTimerInit(string name)
      {
        var timerVar = _localVars.Declare(Types.Util.Timers.Timer.TypeOf);
        var id = JSRuntime.Instance.Timers.GetTimerId(name);
        _ilGen.LoadRuntimeInstance();
        _ilGen.Ldfld(Types.Runtime.Timers);
        _ilGen.Ldc_I4(id);
        _ilGen.Call(Types.Util.Timers.GetTimer_Int32);
        _ilGen.Stloc(timerVar);

        //timerVar = _localVars.Get(Types.Util.TimeCounter.TypeOf);
        //var counterId = JSRuntime.Counters.FindId("Execute", _currFuncMetadata.Declaration, true);
        //_ilGen.Ldc_I4(counterId);
        //_ilGen.Call(Types.JSRuntime.GetCounters);
        //_ilGen.Newobj(Types.Util.TimeCounter.CONSTRUCTOR_Int32_Counters);
        //_ilGen.Stloc(timerVar);
        return timerVar;
      }
      protected void GenTimerStart(LocalBuilder timerVar)
      {
        _ilGen.Ldloc(timerVar);
        _ilGen.Call(Types.Util.Timers.Timer.Start);
        //_ilGen.Call(Types.Util.TimeCounter.Start);
      }
      protected void GenTimerStop(LocalBuilder timerVar)
      {
        _ilGen.Ldloc(timerVar);
        _ilGen.Call(Types.Util.Timers.Timer.Stop);
        //_ilGen.Call(Types.Util.TimeCounter.Stop);
      }

      protected void GenReadTicks(LocalBuilder timerTicksVariable)
      {
        _ilGen.Call(Types.Util.Timers.GetTicks);
        _ilGen.Stloc(timerTicksVariable);
      }
      protected void GenAccumulateTicks(string name, string notes, LocalBuilder timerTicksVariable)
      {
        var ticksDiff = _localVars.PushTemporary(Types.ClrSys.Int64);
        _ilGen.Call(Types.Util.Timers.GetTicks);
        _ilGen.Ldloc(timerTicksVariable);
        _ilGen.Sub();
        _ilGen.Stloc(ticksDiff);

        var counterId = JSRuntime.Instance.Counters.FindId(name, notes, true);

        _ilGen.LoadRuntimeInstance();
        _ilGen.Ldfld(Types.Runtime.Counters);
        _ilGen.Ldc_I4(counterId);
        _ilGen.Call(Types.Util.Counters.GetCounter_Int32);
        _ilGen.Dup();
        _ilGen.Ldfld(Types.Util.Counters.Counter.Count);
        _ilGen.Ldloc(ticksDiff);
        _ilGen.Add();
        _ilGen.Stfld(Types.Util.Counters.Counter.Count);

        _localVars.PopTemporary(ticksDiff);
      }

      //LocalBuilder _execTimerTicks = null;
      LocalBuilder _execTimerVar;
      protected void GenExecTimerInit()
      {
        if (JSRuntime.Instance.Configuration.ProfileExecuteTime)
          if (JSRuntime.Instance.Configuration.ProfileFunctionTime)
            _execTimerVar = GenTimerInit("JS/Execute/" + _currFuncMetadata.Declaration);
          else
            _execTimerVar = GenTimerInit("JS/Execute");

      }
      protected void GenExecTimerStart(bool generateFinally)
      {
        ///Only call and new pass true here. However, in some cases, there are stuff on the .NET stack
        ///and creating an exception block will not be possible. 
        ///for now we just disable the feature. If it is a major problem, for call/new when profiling is enabled
        ///we can use something like Operations.Internal.New/Call(timer, ref callFrame) to safely stop/start the timer
        generateFinally = false;

        if (JSRuntime.Instance.Configuration.ProfileExecuteTime)
        {
          if (generateFinally)
          {
            //In this case assumption is that Stop was wrapped in a try, we need to make sure start happens no matter what
            _ilGen.BeginFinallyBlock();
            GenTimerStart(_execTimerVar);
            _ilGen.EndExceptionBlock();
          }
          else
            GenTimerStart(_execTimerVar);
        }
        //GenReadTicks(_execTimerTicks);
      }
      protected void GenExecTimerStop(bool generateTry)
      {
        generateTry = false; //See the comments above
        if (JSRuntime.Instance.Configuration.ProfileExecuteTime)
        {
          if (generateTry)
            _ilGen.BeginExceptionBlock();
          GenTimerStop(_execTimerVar);
        }
        //GenAccumulateTicks("Execute", _currFuncMetadata.Declaration, _execTimerTicks);
      }

      #endregion

      protected void PushLocation(Node node)
      {
#if DEBUG || DIAGNOSE
        if (!JSRuntime.Instance.Configuration.EnableDiagLocation)
          return;
        //Errors may occur both during compilation or execution, so we push location now, and also generate code for execution time
        JSRuntime.PushLocation(_currFuncMetadata, node.SourceOffset);
        if (_ilGen != null)
        {
          Ldarg_func();
          _ilGen.Call(Types.DFunction.GetMetadata);
          _ilGen.Castclass(Types.JSFunctionMetadata.TypeOf);
          _ilGen.Ldc_I4(node.SourceOffset);
          _ilGen.Call(Types.JSRuntime.PushLocation);
        }
#endif
      }
      protected void PopLocation()
      {
#if DEBUG || DIAGNOSE
        if (!JSRuntime.Instance.Configuration.EnableDiagLocation)
          return;
        JSRuntime.PopLocation();
        if (_ilGen != null)
          _ilGen.Call(Types.JSRuntime.PopLocation);
#endif
      }

      protected virtual void GenProlog()
      {
        PrepareContext();
        DeclareParameterSymbols();
        DeclareSymbols(_currFuncMetadata.FunctionIR.Scope);
      }
      protected virtual void GenBody()
      {
        VisitNode(_currFuncMetadata.FunctionIR.Statement);
      }
      protected virtual void GenEpilog()
      { }

      protected virtual void PrepareContext()
      {
        var scope = _currFuncMetadata.Scope;

        Ldarg_CallFrame();
        if (scope.IsProgram)
          _ilGen.Call(Types.JSFunctionContext.CreateProgramContext); //This
        else if (scope.IsEvalFunction)
          _ilGen.Call(Types.JSFunctionContext.CreateEvalContext);
        else if (scope.IsConstContext)
          _ilGen.Call(Types.JSFunctionContext.CreateConstantContext);
        else
          _ilGen.Call(Types.JSFunctionContext.CreateFunctionContext);
      }

      protected virtual void DeclareParameterSymbols() { }
      protected virtual void DeclareSymbols(Scope scope) { }

      #region CallFrame
      protected void Ldarg_CallFrame() { _ilGen.Ldarg_0(); }
      protected void Ldarg_func()
      {
        Ldarg_CallFrame();
        _ilGen.Ldfld(Types.CallFrame.Function);
      }
      protected void Ldarg_signature()
      {
        Ldarg_CallFrame();
        _ilGen.Ldfld(Types.CallFrame.Signature);
      }
      protected void Ldarg_This()
      {
        Ldarg_CallFrame();
        _ilGen.Ldfld(Types.CallFrame.This);
      }
      protected void Ldarg_arguments()
      {
        Ldarg_CallFrame();
        _ilGen.Ldfld(Types.CallFrame.Arguments);
      }
      protected void Ldarg_Parameter(int parameterIndex, bool callFrameAlreadyLoaded = false)
      {
        if (!callFrameAlreadyLoaded)
          Ldarg_CallFrame();
        switch (parameterIndex)
        {
          case 0: _ilGen.Ldflda(Types.CallFrame.Arg0); break;
          case 1: _ilGen.Ldflda(Types.CallFrame.Arg1); break;
          case 2: _ilGen.Ldflda(Types.CallFrame.Arg2); break;
          case 3: _ilGen.Ldflda(Types.CallFrame.Arg3); break;
          default:
            Debug.Assert(parameterIndex >= mdr.CallFrame.InlineArgsCount, "Code gen must be updated to support inlined arguments");
            _ilGen.Ldfld(Types.CallFrame.Arguments);
            _ilGen.Ldc_I4(parameterIndex - mdr.CallFrame.InlineArgsCount);
            _ilGen.Ldelema(Types.DValue.TypeOf);
            break;
        }
        _result.ValueType = mdr.ValueTypes.DValueRef;
      }
      #endregion

      #region AsX
      /// <summary>
      /// Function in this region convert the top of the stack to the corresponding type. 
      /// This is purely for CIL code generation and independent of JS semantics. 
      /// </summary>

      protected virtual void AsX(mdr.ValueTypes type)
      {
        Debug.Assert(_result.ValueType == mdr.ValueTypes.DValueRef, "Make sure that the result type is DvalueRef before calling AsX.");
        _ilGen.Call(Types.DValue.As(type));
        _result.ValueType = type;
      }

      protected virtual void AsVoid()
      {
        _ilGen.Pop();
      }

      protected virtual void AsPrimitive()  //AsDValue()
      {
        //var type = _resultType;
        //if (type == mdr.ValueTypes.DValue)
        //  return;

        //_ilGen.Call(Types.DValue.Create(type));
        //_resultType = mdr.ValueTypes.DValue;
        throw new NotImplementedException();
      }

      protected virtual void AsBoolean()
      {
        var type = _result.ValueType;
        if (type == mdr.ValueTypes.Boolean)
          return;

        switch (type)
        {
          //case mdr.ValueTypes.Undefined:
          case mdr.ValueTypes.String:
            _ilGen.Call(Types.Operations.Convert.ToBoolean.Get(mdr.ValueTypes.String));
            break;
          case mdr.ValueTypes.Double:
            _ilGen.Conv_I4();
            break;
          case mdr.ValueTypes.Int32:
            break;
          case mdr.ValueTypes.Boolean:
            break;
          //case mdr.ValueTypes.Null:
          case mdr.ValueTypes.Object:
          case mdr.ValueTypes.Function:
          case mdr.ValueTypes.Array:
          case mdr.ValueTypes.Property:
            _ilGen.Callvirt(Types.DObject.ToBoolean);
            break;
          case mdr.ValueTypes.DValueRef:
            _ilGen.Call(Types.DValue.AsBoolean);
            break;
          default:
            Trace.Fail("Cannot convert type {0} to boolean", type);
            break;
        }
#if DEBUG
        //This is to help the CSharpILGenerator produce valid C# code
        if (type == mdr.ValueTypes.Double || type == mdr.ValueTypes.Int32)
        {
          _ilGen.Ldc_I4_0();
          _ilGen.Ceq();
          _ilGen.Ldc_I4(false);
          _ilGen.Ceq();
        }
#endif
        _result.ValueType = mdr.ValueTypes.Boolean;
      }

      protected virtual void AsInt32()
      {
        var type = _result.ValueType;
        switch (type)
        {
          //case mdr.ValueTypes.Undefined:
          case mdr.ValueTypes.String:
            _ilGen.Call(Types.Operations.Convert.ToInt32.Get(type));
            break;
          case mdr.ValueTypes.Double:
            _ilGen.Conv_I4();
            break;
          case mdr.ValueTypes.Int32:
            break;
          case mdr.ValueTypes.Boolean:
            break;
          //case mdr.ValueTypes.Null:
          case mdr.ValueTypes.Object:
          case mdr.ValueTypes.Function:
          case mdr.ValueTypes.Array:
          case mdr.ValueTypes.Property:
            _ilGen.Callvirt(Types.DObject.ToInt32);
            break;
          case mdr.ValueTypes.DValueRef:
            _ilGen.Call(Types.DValue.AsInt32);
            break;
          default:
            Trace.Fail("Cannot convert type {0} to int", type);
            break;
        }
        _result.ValueType = mdr.ValueTypes.Int32;
      }

      protected virtual void AsUInt32() { throw new NotImplementedException(); }

      protected virtual void AsUInt16() { throw new NotImplementedException(); }

      protected virtual void AsDouble()
      {
        var type = _result.ValueType;
        switch (type)
        {
          //case mdr.ValueTypes.Undefined:
          case mdr.ValueTypes.String:
            _ilGen.Call(Types.Operations.Convert.ToDouble.Get(type));
            break;
          case mdr.ValueTypes.Double:
            break;
          case mdr.ValueTypes.Int32:
            _ilGen.Conv_R8();
            break;
          case mdr.ValueTypes.Boolean:
            _ilGen.Conv_R8();
            break;
          //case mdr.ValueTypes.Null:
          case mdr.ValueTypes.Object:
          case mdr.ValueTypes.Function:
          case mdr.ValueTypes.Array:
          case mdr.ValueTypes.Property:
            _ilGen.Callvirt(Types.DObject.ToDouble);
            break;
          case mdr.ValueTypes.DValueRef:
            _ilGen.Call(Types.DValue.AsDouble);
            break;
          default:
            Trace.Fail("Cannot convert type {0} to double", type);
            break;
        }
        _result.ValueType = mdr.ValueTypes.Double;
      }

      protected virtual void AsString()
      {
        var type = _result.ValueType;
        switch (type)
        {
          //case mdr.ValueTypes.Undefined:
          case mdr.ValueTypes.String:
            break;
          case mdr.ValueTypes.Double:
          case mdr.ValueTypes.Int32:
          case mdr.ValueTypes.Boolean:
            _ilGen.Call(Types.Operations.Convert.ToString.Get(type));
            break;
          //case mdr.ValueTypes.Null:
          case mdr.ValueTypes.Object:
          case mdr.ValueTypes.Function:
          case mdr.ValueTypes.Array:
          case mdr.ValueTypes.Property:
            _ilGen.Callvirt(Types.DObject.ToString);
            break;
          case mdr.ValueTypes.DValueRef:
            _ilGen.Call(Types.DValue.AsString);
            break;
          default:
            Trace.Fail("Cannot convert type {0} to string", type);
            break;
        }
        _result.ValueType = mdr.ValueTypes.String;
      }

      protected virtual void AsDObject()
      {
        var type = _result.ValueType;
        switch (type)
        {
          case mdr.ValueTypes.Object:
          case mdr.ValueTypes.Property:
          case mdr.ValueTypes.Function:
          case mdr.ValueTypes.Array:
            break;
          case mdr.ValueTypes.DValueRef:
            _ilGen.Call(Types.DValue.AsDObject);
            _result.ValueType = mdr.ValueTypes.Object;
            break;
          default:
            Trace.Fail("Cannot convert type {0} to DObject", type);
            break;
        }
      }

      protected virtual void AsDArray()
      {
        var type = _result.ValueType;
        switch (type)
        {
          case mdr.ValueTypes.Array:
            break;
          case mdr.ValueTypes.DValueRef:
            _ilGen.Call(Types.DValue.AsDArray);
            _result.ValueType = mdr.ValueTypes.Array;
            break;
          default:
            Trace.Fail("Cannot convert type {0} to DArray", type);
            break;
        }
      }

      protected virtual void AsObject(Type t = null)
      {
        var type = _result.ValueType;
        switch (type)
        {
          case mdr.ValueTypes.Any:
            break;
          case mdr.ValueTypes.DValueRef:
            _ilGen.Call(Types.DValue.AsObject);
            break;
          default:
            Trace.Fail("Cannot convert type {0} to DObject", type);
            break;
        }
        if (t != null)
        {
          _ilGen.Castclass(t);
          _result.Type = t;
        }
        else
          _result.ValueType = mdr.ValueTypes.Object;
      }
      #endregion

      #region Statements; ECMA 12. -------------------------------------------------------------------------------------

      public override void Visit(BlockStatement node)
      {
        PushLocation(node);
        VisitNodes(node.Statements);
        PopLocation();
      }

      public override void Visit(VariableDeclarationStatement node)
      {
        PushLocation(node);
        VisitNodes(node.Declarations);
        PopLocation();
      }

      public override void Visit(VariableDeclaration node)
      {
        PushLocation(node);
        if (node.Initialization != null)
        {
          VisitNode(node.Initialization);
          AsVoid();
        }
        PopLocation();
      }
      public override void Visit(EmptyStatement node)
      {
        PushLocation(node);
        PopLocation();
      }

      public override void Visit(ExpressionStatement node)
      {
        PushLocation(node);
        VisitNode(node.Expression);
        AsVoid();
        PopLocation();
      }

      public override void Visit(IfStatement node)
      {
        throw new NotImplementedException();
      }

      protected virtual void Loop(LoopStatement loop, Statement initilization, Expression condition, Expression increment, Statement body, bool isDoWhile)
      {
        throw new NotImplementedException();
      }

      public override void Visit(DoWhileStatement node)
      {
        Loop(node, null, node.Condition, null, node.Body, true);
      }

      public override void Visit(WhileStatement node)
      {
        Loop(node, null, node.Condition, null, node.Body, false);
      }

      public override void Visit(ForStatement node)
      {
        Loop(node, node.Initialization, node.Condition, node.Increment, node.Body, false);
      }

      public override void Visit(ForEachInStatement node)
      {
        Loop(node, node.IteratorInitialization, node.Condition, null, node.Body, false);
      }

      public override void Visit(LabelStatement node)
      {
        throw new NotImplementedException();
      }

      protected virtual void Jump(GotoStatement node, bool isContinue)
      {
        throw new NotImplementedException();
      }

      public override void Visit(GotoStatement node)
      {
        Jump(node, false);
      }

      public override void Visit(ContinueStatement node)
      {
        Jump(node, true);
      }

      public override void Visit(BreakStatement node)
      {
        Jump(node, false);
      }

      public override void Visit(ReturnStatement node)
      {
        throw new NotImplementedException();
      }

      public override void Visit(WithStatement node)
      {
        PushLocation(node);
        Trace.Fail("Visitor for node type {0} is not implemented", node.GetType());
        PopLocation();
      }

      public override void Visit(SwitchStatement node)
      {
        throw new NotImplementedException();
      }

      public override void Visit(ThrowStatement node)
      {
        throw new NotImplementedException();
      }

      public override void Visit(TryStatement node)
      {
        throw new NotImplementedException();
      }

      public override void Visit(CatchClause node)
      {
        throw new NotImplementedException();
      }

      public override void Visit(FinallyClause node)
      {
        throw new NotImplementedException();
      }

      #endregion

      #region Primary Expressions; ECMA 11.1 -------------------------------------------------------------------------------------

      public override void Visit(ThisLiteral node)
      {
        PushLocation(node);
        Ldarg_CallFrame();
        _ilGen.Ldfld(Types.CallFrame.This);
        _result.ValueType = mdr.ValueTypes.Object;
        PopLocation();
      }

      public override void Visit(NullLiteral node)
      {
        PushLocation(node);
        _ilGen.LoadRuntimeInstance();
        _ilGen.Ldfld(Types.Runtime.DefaultDNull);
        _result.ValueType = mdr.ValueTypes.Null;
        PopLocation();
      }

      public override void Visit(BooleanLiteral node)
      {
        PushLocation(node);
        _ilGen.Ldc_I4(node.Value);
        _result.ValueType = mdr.ValueTypes.Boolean;
        PopLocation();
      }

      public override void Visit(IntLiteral node)
      {
        PushLocation(node);
        _ilGen.Ldc_I4((int)node.Value);
        _result.ValueType = mdr.ValueTypes.Int32;
        PopLocation();
      }

      public override void Visit(DoubleLiteral node)
      {
        PushLocation(node);
        _ilGen.Ldc_R8(node.Value);
        _result.ValueType = mdr.ValueTypes.Double;
        PopLocation();
      }

      public override void Visit(StringLiteral node)
      {
        PushLocation(node);
        _ilGen.Ldstr(node.Value);
        _result.ValueType = mdr.ValueTypes.String;
        PopLocation();
      }

      public override void Visit(RegexpLiteral node)
      {
        PushLocation(node);
        _ilGen.Ldstr(node.Regexp);
        _ilGen.Ldstr(node.Options);
        _ilGen.Newobj(Types.DRegExp.CONSTRUCTOR_String_String);
        _result.ValueType = mdr.ValueTypes.Object;
        PopLocation();
      }

      public override void Visit(ArrayLiteral node)
      {
        throw new NotImplementedException();
      }

      public override void Visit(ObjectLiteral node)
      {
        throw new NotImplementedException();
      }

      public override void Visit(PropertyAssignment node)
      {
        throw new NotImplementedException();
      }


      public override void Visit(ParenExpression node)
      {
        PushLocation(node);
        VisitNode(node.Expression);
        PopLocation();
      }

      public override void Visit(GuardedCast node)
      {
        PushLocation(node);
        VisitNode(node.Expression);
        PopLocation();
      }

      public override void Visit(InlinedInvocation node)
      {
        throw new NotImplementedException();
      }

      public override void Visit(ReadIdentifierExpression node)
      {
        throw new NotImplementedException();
      }

      public override void Visit(ReadIndexerExpression node)
      {
        throw new NotImplementedException();
      }

      public override void Visit(ReadPropertyExpression node)
      {
        throw new NotImplementedException();
      }
      #endregion

      #region Binary Logical Operators; ECMA 11.11 -------------------------------------------------------------------------------------

      public override void Visit(LogicalAndExpression node)
      {
        PushLocation(node);
        VisitNode(node.Implementation);
        PopLocation();
      }

      public override void Visit(LogicalOrExpression node)
      {
        PushLocation(node);
        VisitNode(node.Implementation);
        PopLocation();
      }

      #endregion

      #region Comma Operator; ECMA 11.14 -------------------------------------------------------------------------------------

      public override void Visit(CommaOperatorExpression node)
      {
        PushLocation(node);
        for (var i = 0; i < node.Expressions.Count - 1; ++i)
        {
          var stackState = _localVars.GetTemporaryStackState();
          VisitNode(node.Expressions[i]);
          AsVoid();
          _localVars.PopTemporariesAfter(stackState);
        }
        VisitNode(node.Expressions[node.Expressions.Count - 1]);

        PopLocation();
      }

      #endregion

      #region Function Definition; ECMA 13 -------------------------------------------------------------------------------------

      public override void Visit(FunctionDeclarationStatement node)
      {
        PushLocation(node);
        var stackState = _localVars.GetTemporaryStackState();
        VisitNode(node.Implementation);
        AsVoid();
        _localVars.PopTemporariesAfter(stackState);
        PopLocation();
      }

      #endregion

    }

  }
}

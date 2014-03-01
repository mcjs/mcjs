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
using mjr.IR;

namespace mjr.CodeGen
{
  public class Interpreter
  {
    public Interpreter()
      : base()
    {}

    JSFunctionMetadata _currFuncMetadata;
    Scope _currScope;
    Profiler _currProfiler;

    public mdr.DObject Context;
    public mdr.DArray Arguments;
    public mdr.DValue[] SymbolValues;
    //public Stack<Node> CurrLocations = new Stack<Node>();
    public m.Util.Timers.Timer Timer;

    #region Temporary handling

    public class Temporary
    {
      public WriteTemporaryExpression Node;
      public mdr.DValue Value;
    }
    public Temporary[] Temporaries;
    int TemporariesCount;
    public Temporary Declare(WriteTemporaryExpression node)
    {
      Temporary temp;
      for (var i = TemporariesCount - 1; i >= 0; --i)
      {
        temp = Temporaries[i];
        if (temp.Node == node)
          return temp;
      }
      //Did not find it, so need to declare it
      if (Temporaries == null)
        Temporaries = new Temporary[5];
      if (TemporariesCount >= Temporaries.Length)
        Array.Resize(ref Temporaries, TemporariesCount * 2);
      temp = Temporaries[TemporariesCount];
      if (temp == null)
        Temporaries[TemporariesCount] = temp = new Temporary();
      else
        temp.Node = null; //This is to signal the node it should recalculate
      ++TemporariesCount;
      return temp;
    }
    internal int GetTemporaryCount() { return TemporariesCount; }
    internal void ReleaseTemporariesAfter(int count) { TemporariesCount = count; }
    #endregion

    #region Labels and Completion
    List<Statement> Targets = new List<Statement>();
    public void PushTarget(Statement s)
    {
      Debug.Assert(s is LoopStatement || s is SwitchStatement || s is LabelStatement, "Invalid target statement type {0}", s.GetType());
      Targets.Add(s);
    }
    public void PopTarget(Statement s)
    {
      Debug.Assert(Targets.Count > 0 && Targets[Targets.Count - 1] == s, "Target statement {0} does not exist in the list", s);
      Targets.RemoveAt(Targets.Count - 1);
      if (CompletionType == Interpreter.CompletionTypes.Break && CompletionTargetStatement == s)
        SetCompletion(CompletionTypes.Normal, null);
    }

    /// <summary>
    /// ECMA 8.9
    /// </summary>
    public enum CompletionTypes
    {
      Normal,
      Break,
      Continue,
      Return,
      Throw,
    }
    public CompletionTypes CompletionType { get; private set; }
    public Statement CompletionTargetStatement { get; private set; }
    public void SetCompletion(CompletionTypes type, string targetName)
    {
      CompletionType = type;
      switch (type)
      {
        case CompletionTypes.Break:
          {
            for (var i = Targets.Count - 1; i >= 0; --i)
            {
              var label = Targets[i] as LabelStatement;
              if ((targetName == null && label == null)
                || (targetName != null && label != null && label.Name == targetName))
              {
                CompletionTargetStatement = Targets[i];
                return;
              }
            }
            Operations.Error.SyntaxError(targetName == null ? "Illegal break statement" : string.Format("Undefined label '{0}'", targetName));
            break;
          }
        case CompletionTypes.Continue:
          {
            LoopStatement loop = null;
            var i = Targets.Count - 1;
            do
            {
              for (loop = null; i >= 0; --i)
              {
                loop = Targets[i] as LoopStatement;
                if (loop != null)
                  break;
              }
              if (loop == null)
              {
                Operations.Error.SyntaxError(targetName == null ? "Illegal continue statement" : string.Format("Undefined label '{0}'", targetName));
                return;
              }
              CompletionTargetStatement = loop;
              if (targetName == null)
                break;
              else
              {
                ///This is the uncommon, but expensive one. 
                ///We are going to find label-set of the loop
                ///at this point Targets[i] is loop
                if (i == 0)
                  --i; //We did not want to do this in the loop, just for the corner case which eventually fails

                while (i > 0)
                {
                  var currStatement = Targets[i];
                  --i;
                  var label = Targets[i] as LabelStatement;
                  if (label == null || label.Target != currStatement)
                    break; //Restart search
                  if (label.Name == targetName)
                    return; //found, loop already set as target!
                }
              }
            } while (true);
            break;
          }
        default:
          CompletionTargetStatement = null;
          break;
      }
    }
    #endregion


    public void Execute(ref mdr.CallFrame callFrame)
    {
      _currFuncMetadata = (JSFunctionMetadata)callFrame.Function.Metadata;
      Timer =
        JSRuntime.Instance.Configuration.ProfileFunctionTime
        ? JSRuntime.StartTimer(JSRuntime.Instance.Configuration.ProfileExecuteTime, "JS/Interpret/" + _currFuncMetadata.Declaration)
        : JSRuntime.StartTimer(JSRuntime.Instance.Configuration.ProfileExecuteTime, "JS/Interpret");
      try
      {
        _currScope = _currFuncMetadata.Scope;
        _currProfiler = ((JSFunctionCode)callFrame.Function.Code).Profiler;

        Debug.WriteLine("Interpretting {0} with sig=0x{1:X}-->{2} for {3}th time"
          , _currFuncMetadata.Declaration
          , callFrame.Signature.Value
          , string.Join(",", callFrame.Signature.Types)
          , (_currProfiler!=null ? _currProfiler.ExecutionCount.ToString() : "n-"));

        RunProlog(ref callFrame);
        RunBody(ref callFrame);
        RunEilog(ref callFrame);
      }
      finally
      {
        JSRuntime.StopTimer(Timer);
      }
    }

    private void RunProlog(ref mdr.CallFrame callFrame)
    {
      PrepareContext(ref callFrame);
      SymbolValues = new mdr.DValue[_currFuncMetadata.TotalSymbolCount];
      DeclareParameterSymbols(ref callFrame);
      DeclareSymbols(_currScope);
    }

    private void PrepareContext(ref mdr.CallFrame callFrame)
    {
      if (_currScope.IsProgram)
        Context = JSFunctionContext.CreateProgramContext(ref callFrame);
      else if (_currScope.IsEvalFunction)
        Context = JSFunctionContext.CreateEvalContext(ref callFrame);
      else if (_currScope.IsConstContext)
        Context = JSFunctionContext.CreateConstantContext(ref callFrame);
      else
        Context = JSFunctionContext.CreateFunctionContext(ref callFrame);
    }

    private void DeclareParameterSymbols(ref mdr.CallFrame callFrame)
    {
      var parameters = _currFuncMetadata.FunctionIR.Parameters;

      //Extend missing arguments
      callFrame.SetExpectedArgsCount(parameters.Count);

      if (_currScope.HasArgumentsSymbol)
      {
        ///In this case, we already read/write from the arguments.Elements[i] for the symbol. 
        ///however, we should always refer to the arguments variable itself since its .Elements 
        ///may be resized and changed
        ///we cannot rely on the argument symbol's variable for this since the progremmer
        ///may write the arguments symbol and we can lose the value, so we keep our own separately

        Arguments = JSFunctionArguments.CreateArgumentsObject(ref callFrame, Context);
        var symbols = _currScope.Symbols;
        for (var i = parameters.Count; i < symbols.Count; ++i) //Hopefully "arguments" is the symbol almost immediately after the parameters
        {
          var symbol = symbols[i];
          if (symbol.SymbolType == JSSymbol.SymbolTypes.Arguments)
          {
            SymbolValues[symbol.ValueIndex].Set(Arguments);
            break;
          }
        }
      }
      else
      {
        Arguments = null;
        var i = parameters.Count;
        if (i > callFrame.PassedArgsCount)
          i = callFrame.PassedArgsCount;
        for (i = i - 1; i >= 0; --i)
        {
          var symbol = parameters[i].Symbol;
          Debug.Assert(symbol.ParameterIndex == i, "Invalid situation!, symbol {0} should be paramter with parameter index {1} instead of {2}", symbol.Name, i, symbol.ParameterIndex);
          if (symbol.SymbolType == JSSymbol.SymbolTypes.ClosedOnLocal)
          {
            var pd = Context.Map.GetPropertyDescriptorByFieldId(symbol.FieldId);
            Debug.Assert(
              pd != null
              && !pd.HasAttributes(mdr.PropertyDescriptor.Attributes.Undefined | mdr.PropertyDescriptor.Attributes.Inherited | mdr.PropertyDescriptor.Attributes.Accessor)
              , "Invalid situation, symbol {0} is not properly added to context", symbol.Name);
            Context.Fields[pd.Index] = callFrame.Arg(i);
            SymbolValues[symbol.ValueIndex].Set(pd);
          }
        }
      }
    }

    private void DeclareSymbols(Scope scope)
    {
      //We do these on demand!
    }

    private void RunBody(ref mdr.CallFrame callFrame)
    {
      //as tempting it might be, we cannot use the callFrame.Return here since function may not have a return statement at all.
      var result = new mdr.DValue();
      _currFuncMetadata.FunctionIR.Statement.Execute(ref result, ref callFrame, this);
    }

    private void RunEilog(ref mdr.CallFrame callFrame)
    {
      //TODO: if we plan to reuse the Interpreter object, we should release other stuff!
    }

    internal void PushLocation(Node node)
    {
      //CurrLocations.Push(node);
    }

    internal void PopLocation(Node node, ref mdr.DValue result)
    {
      //_currProfiler.UpdateProfile(node, result.ValueType);
      //Node n;
      //do
      //{
      //  Debug.Assert(CurrLocations.Count > 0, "invalid situation! location stack is empty, could not fine {0}", node);
      //  n = CurrLocations.Pop();
      //} while (n != node);
      ////Debug.Assert(node == n, "invalid situation! node {0} is different from node {1} on top of stack", node, n);
    }

    internal void PopLocation(GuardedCast node, ref mdr.DValue result)
    {
      if (_currProfiler != null)
      {
        _currProfiler.GetOrAddNodeProfile(node).UpdateNodeProfile(result.ValueType);
      }
    }

    internal void PopLocation(ReadIndexerExpression node, mdr.DObject obj, mdr.PropertyDescriptor pd)
    {
      if (_currProfiler != null)
      {

        if (mdr.Runtime.Instance.Configuration.ProfileStats)
        {
          mdr.Runtime.Instance.Counters.GetCounter("Prop lookup").Count++;
          if (pd.IsDataDescriptor && !pd.IsInherited)
          {
            mdr.Runtime.Instance.Counters.GetCounter("Prop lookup owndata").Count++;
          }
          if (pd.IsDataDescriptor && pd.IsInherited)
          {
            mdr.Runtime.Instance.Counters.GetCounter("Prop lookup inherited").Count++;
          }
          MapNodeProfile mapProfile = _currProfiler.GetOrAddNodeProfile(node);
          if (mapProfile != null)
          {
            if (mapProfile.Map == obj.Map) 
            {
              mdr.Runtime.Instance.Counters.GetCounter("Prop lookup map hit").Count++;
              if (mapProfile.PD == pd)
              {
                mdr.Runtime.Instance.Counters.GetCounter("Prop lookup map/pd hit").Count++;
                if (pd.HasAttributes(mdr.PropertyDescriptor.Attributes.Data) && !pd.HasAttributes(mdr.PropertyDescriptor.Attributes.Inherited))
                {
                  mdr.Runtime.Instance.Counters.GetCounter("Prop lookup map/pd owndata hit").Count++;
                }
                if (pd.HasAttributes(mdr.PropertyDescriptor.Attributes.Inherited))
                {
                  mdr.Runtime.Instance.Counters.GetCounter("Prop lookup map/pd inherited hit").Count++;
                }
              }
            }
          }
        }
        _currProfiler.GetOrAddNodeProfile(node).UpdateNodeProfile(obj.Map, pd);
        //NodeProfile nProfile = _currProfiler.GetNodeProfile(node);
        //if (nProfile != null)
        //{
        //  (nProfile as MapNodeProfile).UpdateNodeProfile(obj.Map, pd);
        //}
        //else
        //{
        //  _currProfiler.CreateNewProfile(node, obj.Map, pd);
        //}
      }
    }

    internal void PopLocation(Invocation node, mdr.DFunction callTarget)
    {
      if (_currProfiler != null)
      {
        _currProfiler.GetOrAddNodeProfile(node).UpdateNodeProfile(callTarget);
        //NodeProfile nProfile = _currProfiler.GetNodeProfile(functionExpr);
        //if (nProfile != null)
        //{
        //  (nProfile as CallNodeProfile).UpdateNodeProfile(callTarget);
        //}
        //else
        //{
        //  _currProfiler.CreateNewProfile(functionExpr, callTarget);
        //}
      }
    }

    internal void IncrementBackedgeCount(int count)
    {
      if (_currProfiler != null)
      {
        _currProfiler.BackedgeCount += count;
      }
    }

    private mdr.PropertyDescriptor GetPropertyDescriptor(JSSymbol symbol)
    {
      mdr.PropertyDescriptor pd;
      if (SymbolValues[symbol.ValueIndex].ValueType == mdr.ValueTypes.Undefined)
      {
        //first time visit
        pd = Context.GetPropertyDescriptorByFieldId(symbol.FieldId);
        SymbolValues[symbol.ValueIndex].Set(pd);
      }
      else
        pd = (mdr.PropertyDescriptor)SymbolValues[symbol.ValueIndex].AsObject();
      return pd;
    }
    internal void ReadSymbol(JSSymbol symbol, ref mdr.DValue result, ref mdr.CallFrame callFrame)
    {
      switch (symbol.SymbolType)
      {
        case JSSymbol.SymbolTypes.Local:
        case JSSymbol.SymbolTypes.HiddenLocal:
        case JSSymbol.SymbolTypes.Arguments:
          {
            if (symbol.IsParameter)
              if (Arguments != null)
                //result = Arguments.Elements[symbol.ParameterIndex];
                Arguments.GetField(symbol.ParameterIndex, ref result); //TODO: optimize this for faster access
              else
                result = callFrame.Arg(symbol.ParameterIndex);
            else
              result = SymbolValues[symbol.ValueIndex];
            break;
          }
        case JSSymbol.SymbolTypes.ClosedOnLocal:
        case JSSymbol.SymbolTypes.ParentLocal:
        case JSSymbol.SymbolTypes.Global:
          {
            var pd = GetPropertyDescriptor(symbol);
            pd.Get(Context, ref result);
            break;
          }
        case JSSymbol.SymbolTypes.Unknown:
          {
            var pd = Context.GetPropertyDescriptorByFieldId(symbol.FieldId);
            pd.Get(Context, ref result);
            break;
          }
        case JSSymbol.SymbolTypes.OuterDuplicate:
          ReadSymbol(symbol.ResolvedSymbol, ref result, ref callFrame);
          break;
        default:
          Trace.Fail("Could not interpret symbol {0} with type {1}", symbol.Name, symbol.SymbolType);
          break;
      }
    }
    internal void WriteSymbol(JSSymbol symbol, ref mdr.DValue result, ref mdr.CallFrame callFrame)
    {
      switch (symbol.SymbolType)
      {
        case JSSymbol.SymbolTypes.Local:
        case JSSymbol.SymbolTypes.HiddenLocal:
        case JSSymbol.SymbolTypes.Arguments:
          {
            if (symbol.IsParameter)
              if (Arguments != null)
                Arguments.Elements[symbol.ParameterIndex].Set(ref result);
              else
                callFrame.SetArg(symbol.ParameterIndex, ref result);
            else
              SymbolValues[symbol.ValueIndex] = result;
            break;
          }
        case JSSymbol.SymbolTypes.ClosedOnLocal:
        case JSSymbol.SymbolTypes.ParentLocal:
        case JSSymbol.SymbolTypes.Global:
          {
            var pd = GetPropertyDescriptor(symbol);
            pd.Set(Context, ref result);
            break;
          }
        case JSSymbol.SymbolTypes.Unknown:
          {
            var pd = Context.GetPropertyDescriptorByFieldId(symbol.FieldId);
            if (pd.IsUndefined)
              JSRuntime.Instance.GlobalContext.SetFieldByFieldId(symbol.FieldId, ref result);
            else
              pd.Set(Context, ref result);
            break;
          }
        case JSSymbol.SymbolTypes.OuterDuplicate:
          WriteSymbol(symbol.ResolvedSymbol, ref result, ref callFrame);
          break;
        default:
          Trace.Fail("Could not interpret symbol {0} with type {1}", symbol.Name, symbol.SymbolType);
          break;
      }
    }

    public void LoadArguments(List<Expression> arguments, ref mdr.CallFrame calleeFrame, ref mdr.CallFrame callFrame)
    {
      calleeFrame.PassedArgsCount = arguments.Count;
      switch (arguments.Count)
      {
        case 0: break;
        case 1:
          arguments[0].Execute(ref calleeFrame.Arg0, ref callFrame, this);
          break;
        case 2:
          arguments[0].Execute(ref calleeFrame.Arg0, ref callFrame, this);
          arguments[1].Execute(ref calleeFrame.Arg1, ref callFrame, this);
          break;
        case 3:
          arguments[0].Execute(ref calleeFrame.Arg0, ref callFrame, this);
          arguments[1].Execute(ref calleeFrame.Arg1, ref callFrame, this);
          arguments[2].Execute(ref calleeFrame.Arg2, ref callFrame, this);
          break;
        case 4:
          arguments[0].Execute(ref calleeFrame.Arg0, ref callFrame, this);
          arguments[1].Execute(ref calleeFrame.Arg1, ref callFrame, this);
          arguments[2].Execute(ref calleeFrame.Arg2, ref callFrame, this);
          arguments[3].Execute(ref calleeFrame.Arg3, ref callFrame, this);
          break;
        default:
          arguments[0].Execute(ref calleeFrame.Arg0, ref callFrame, this);
          arguments[1].Execute(ref calleeFrame.Arg1, ref callFrame, this);
          arguments[2].Execute(ref calleeFrame.Arg2, ref callFrame, this);
          arguments[3].Execute(ref calleeFrame.Arg3, ref callFrame, this);
          calleeFrame.Arguments = new mdr.DValue[arguments.Count - mdr.CallFrame.InlineArgsCount];
          for (var i = 4; i < arguments.Count; ++i)
            arguments[i].Execute(ref calleeFrame.Arguments[i - mdr.CallFrame.InlineArgsCount], ref callFrame, this);
          break;
      }
      //TODO: remove signature calculation and only do it if needed
      for (var i = calleeFrame.PassedArgsCount - 1; i >= 0; --i)
        calleeFrame.Signature.InitArgType(i, calleeFrame.Arg(i).ValueType);

    }
  }
}
namespace mjr.IR
{
  using mjr.CodeGen;
  public partial class Node { public virtual void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter) { throw new NotImplementedException(); } }
  #region Statements; ECMA 12. -------------------------------------------------------------------------------------
  public partial class BlockStatement
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      for (var i = 0; i < Statements.Count; ++i)
      {
        Statements[i].Execute(ref result, ref callFrame, interpreter);
        if (interpreter.CompletionType != Interpreter.CompletionTypes.Normal)
          break;
      }
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class VariableDeclarationStatement
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      for (var i = 0; i < Declarations.Count; ++i)
        Declarations[i].Execute(ref result, ref callFrame, interpreter);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class VariableDeclaration
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      if (Initialization != null)
        Initialization.Execute(ref result, ref callFrame, interpreter);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class EmptyStatement
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class ExpressionStatement
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      Expression.Execute(ref result, ref callFrame, interpreter);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class IfStatement
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);

      Condition.Execute(ref result, ref callFrame, interpreter);
      if (result.AsBoolean())
        Then.Execute(ref result, ref callFrame, interpreter);
      else if (Else != null)
        Else.Execute(ref result, ref callFrame, interpreter);

      interpreter.PopLocation(this, ref result);
    }
  }

  public partial class DoWhileStatement
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);

      interpreter.PushTarget(this);
      var tempCount = interpreter.GetTemporaryCount();
      //int backedgeCount = -1;
      do
      {
        //if (JSRuntime.Instance.Configuration.EnableProfiling)
          //backedgeCount++;
        Body.Execute(ref result, ref callFrame, interpreter);
        if (interpreter.CompletionType == Interpreter.CompletionTypes.Continue && interpreter.CompletionTargetStatement == this)
          interpreter.SetCompletion(Interpreter.CompletionTypes.Normal, null);
        else if (interpreter.CompletionType != Interpreter.CompletionTypes.Normal)
          break;

        Condition.Execute(ref result, ref callFrame, interpreter);
        interpreter.ReleaseTemporariesAfter(tempCount);
      } while (result.AsBoolean());
      interpreter.PopTarget(this);

      //interpreter.IncrementBackedgeCount(backedgeCount);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class WhileStatement
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);

      interpreter.PushTarget(this);
      var tempCount = interpreter.GetTemporaryCount();
      //int backedgeCount = 0;

      while (true)
      {
        Condition.Execute(ref result, ref callFrame, interpreter);
        if (!result.AsBoolean())
          break;

        //backedgeCount++;
        Body.Execute(ref result, ref callFrame, interpreter);
        if (interpreter.CompletionType == Interpreter.CompletionTypes.Continue && interpreter.CompletionTargetStatement == this)
          interpreter.SetCompletion(Interpreter.CompletionTypes.Normal, null);
        else if (interpreter.CompletionType != Interpreter.CompletionTypes.Normal)
          break;
        interpreter.ReleaseTemporariesAfter(tempCount);
      }
      interpreter.PopTarget(this);

      //interpreter.IncrementBackedgeCount(backedgeCount);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class ForStatement
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);

      interpreter.PushTarget(this);
      if (Initialization != null)
        Initialization.Execute(ref result, ref callFrame, interpreter);
      var tempCount = interpreter.GetTemporaryCount();
      //int backedgeCount = 0;

      while (true)
      {
        if (Condition != null)
        {
          Condition.Execute(ref result, ref callFrame, interpreter);
          if (!result.AsBoolean())
            break;
        }

        //backedgeCount++;
        Body.Execute(ref result, ref callFrame, interpreter);
        if (interpreter.CompletionType == Interpreter.CompletionTypes.Continue && interpreter.CompletionTargetStatement == this)
          interpreter.SetCompletion(Interpreter.CompletionTypes.Normal, null);
        else if (interpreter.CompletionType != Interpreter.CompletionTypes.Normal)
          break;

        if (Increment != null)
          Increment.Execute(ref result, ref callFrame, interpreter);
        interpreter.ReleaseTemporariesAfter(tempCount);
      }
      interpreter.PopTarget(this);

      //interpreter.IncrementBackedgeCount(backedgeCount);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class ForEachInStatement
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      ///We try to do as much here as possible to avoid reaching the InternalCall and InternalNew
      ///See NodeFactory.MakeForEachInStatement to understand the logic
      interpreter.PushLocation(this);
      Expression.Execute(ref result, ref callFrame, interpreter);
      var iterator = new JSPropertyNameEnumerator(result.AsDObject());
      Debug.Assert(
        ExtendedBody.Statements.Count == 2
        && ExtendedBody.Statements[0] is ExpressionStatement
        , "Invalid situation! We must have a ExpressionStatement here!");
      var assignToLeft = (ExtendedBody.Statements[0] as ExpressionStatement).Expression;

      interpreter.PushTarget(this);
      var tempCount = interpreter.GetTemporaryCount();
      //int backedgeCount = 0;

      while (iterator.MoveNext())
      {
        var writeId = assignToLeft as WriteIdentifierExpression;
        if (writeId != null)
        {
          result.Set(iterator.GetCurrent());
          interpreter.WriteSymbol(writeId.Symbol, ref result, ref callFrame);
        }
        else
        {
          //This is an unlikely case anyways!
          var writeIndex = assignToLeft as WriteIndexerExpression;
          Debug.Assert(writeIndex != null, "Invalid situation! We must have a WriteIndexerExpression here!");
          writeIndex.Container.Execute(ref result, ref callFrame, interpreter);
          var obj = result.AsDObject();
          writeIndex.Index.Execute(ref result, ref callFrame, interpreter);
          obj.SetField(ref result, iterator.GetCurrent());
        }

        //backedgeCount++;
        OriginalBody.Execute(ref result, ref callFrame, interpreter);
        if (interpreter.CompletionType == Interpreter.CompletionTypes.Continue && interpreter.CompletionTargetStatement == this)
          interpreter.SetCompletion(Interpreter.CompletionTypes.Normal, null);
        else if (interpreter.CompletionType != Interpreter.CompletionTypes.Normal)
          break;
        interpreter.ReleaseTemporariesAfter(tempCount);
      }
      interpreter.PopTarget(this);

      //interpreter.IncrementBackedgeCount(backedgeCount);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class LabelStatement
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);

      interpreter.PushTarget(this);
      Target.Execute(ref result, ref callFrame, interpreter);
      interpreter.PopTarget(this);

      interpreter.PopLocation(this, ref result);
    }
  }
  //public partial class GotoStatement
  //{
  //  public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
  //  {
  //    interpreter.PushLocation(this);
  //    interpreter.PopLocation(this, ref result);
  //  }
  //}
  public partial class ContinueStatement
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      interpreter.SetCompletion(Interpreter.CompletionTypes.Continue, Target);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class BreakStatement
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      interpreter.SetCompletion(Interpreter.CompletionTypes.Break, Target);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class ReturnStatement
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      if (Expression != null)
      {
        Expression.Execute(ref result, ref callFrame, interpreter);
        callFrame.Return = result;
      }
      interpreter.SetCompletion(Interpreter.CompletionTypes.Return, null);
      interpreter.PopLocation(this, ref result);
    }
  }
  //public partial class WithStatement
  //{
  //  public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
  //  {
  //    interpreter.PushLocation(this);
  //    interpreter.PopLocation(this, ref result);
  //  }
  //}
  public partial class SwitchStatement
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);

      interpreter.PushTarget(this);
      var i = 0;
      for (; i < CaseClauses.Count; ++i)
      {
        var caseClause = CaseClauses[i];
        if (caseClause.IsDefault)
          break;
        caseClause.Comparison.Execute(ref result, ref callFrame, interpreter);
        if (result.AsBoolean())
          break;
      }
      for (; i < CaseClauses.Count && interpreter.CompletionType == Interpreter.CompletionTypes.Normal; ++i)
      {
        var caseClause = CaseClauses[i];
        caseClause.Execute(ref result, ref callFrame, interpreter);
      }
      interpreter.PopTarget(this);

      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class CaseClause
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      Body.Execute(ref result, ref callFrame, interpreter);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class ThrowStatement
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      Expression.Execute(ref result, ref callFrame, interpreter);
      JSException.Throw(ref result);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class TryStatement
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      var count = interpreter.GetTemporaryCount();
      try
      {
        Statement.Execute(ref result, ref callFrame, interpreter);
      }
      catch (JSException e)
      {
        if (Catch != null)
          Catch.Execute(ref e.Value, ref callFrame, interpreter);
        else
          throw;
      }
      finally
      {
        if (Finally != null)
          Finally.Execute(ref result, ref callFrame, interpreter);
        interpreter.ReleaseTemporariesAfter(count);
      }
      //if (Catch != null)
      //{
      //  if (Finally != null)
      //  {
      //    try
      //    {
      //      Statement.Execute(ref result, ref callFrame, interpreter);
      //    }
      //    catch (JSException e)
      //    {
      //      Catch.Execute(ref e.Value, ref callFrame, interpreter);
      //    }
      //    finally
      //    {
      //      Finally.Execute(ref result, ref callFrame, interpreter);
      //    }
      //  }
      //  else
      //  {
      //    try
      //    {
      //      Statement.Execute(ref result, ref callFrame, interpreter);
      //    }
      //    catch (JSException e)
      //    {
      //      Catch.Execute(ref e.Value, ref callFrame, interpreter);
      //    }
      //  }
      //}
      //else
      //{
      //  try
      //  {
      //    Statement.Execute(ref result, ref callFrame, interpreter);
      //  }
      //  finally
      //  {
      //    Finally.Execute(ref result, ref callFrame, interpreter);
      //  }
      //}
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class CatchClause
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      var catchSymbol = Identifier.Symbol;
      Debug.Assert(catchSymbol.SymbolType == JSSymbol.SymbolTypes.Local, "catch symbol must be local, nothing else is supported at this time");
      interpreter.SymbolValues[catchSymbol.ValueIndex] = result;
      Statement.Execute(ref result, ref callFrame, interpreter);
      //catchSymbol.SymbolType = JSSymbol.SymbolTypes.Unknown;
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class FinallyClause
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      Statement.Execute(ref result, ref callFrame, interpreter);
      interpreter.PopLocation(this, ref result);
    }
  }
  #endregion

  #region Primary Expressions; ECMA 11.1 -------------------------------------------------------------------------------------
  public partial class ThisLiteral
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      result.Set(callFrame.This);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class NullLiteral
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      result.SetNull();
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class BooleanLiteral
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      result.Set(Value);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class IntLiteral
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      result.Set((int)Value);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class DoubleLiteral
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      result.Set(Value);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class StringLiteral
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      result.Set(Value);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class RegexpLiteral
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      result.Set(new mdr.DRegExp(Regexp, Options));
      interpreter.PopLocation(this, ref result);
    }
  }

  public partial class ArrayLiteral
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      var array = new mdr.DArray(Items.Count);
      for (var i = 0; i < Items.Count; ++i)
        Items[i].Execute(ref array.Elements[i], ref callFrame, interpreter);
      result.Set(array);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class ObjectLiteral
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      var obj = new mdr.DObject();
      for (var i = 0; i < Properties.Count; ++i)
      {
        var prop = Properties[i];
        prop.Execute(ref result, ref callFrame, interpreter);
        obj.DefineOwnProperty(prop.Name, ref result, mdr.PropertyDescriptor.Attributes.Data);
      }
      result.Set(obj);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class PropertyAssignment
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      Debug.Assert(Expression != null, "At this point only data fields for object literals is supported");
      Expression.Execute(ref result, ref callFrame, interpreter);
      interpreter.PopLocation(this, ref result);
    }
  }

  public partial class ParenExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      Expression.Execute(ref result, ref callFrame, interpreter);
      interpreter.PopLocation(this, ref result);
    }
  }

  public partial class ReadIdentifierExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      interpreter.ReadSymbol(Symbol, ref result, ref callFrame);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class ReadIndexerExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);

      Container.Execute(ref result, ref callFrame, interpreter);
      var obj = result.AsDObject();
      Index.Execute(ref result, ref callFrame, interpreter);
      var pd = obj.GetPropertyDescriptor(ref result);
      obj.GetFieldByPD(pd, ref result);

      interpreter.PopLocation(this, obj, pd);
    }
  }
  public partial class ReadPropertyExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);

      Container.Execute(ref result, ref callFrame, interpreter);
      var obj = result.AsDObject();
      AssignFieldId();
      var pd = obj.GetPropertyDescriptorByFieldId(FieldId);
      obj.GetFieldByPD(pd, ref result);

      interpreter.PopLocation(this, obj, pd);
    }
  }
  #endregion

  #region Type Conversions; ECMA 9 -------------------------------------------------------------------------------------
  public partial class ToPrimitive
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      Expression.Execute(ref result, ref callFrame, interpreter);
      Operations.Convert.ToPrimitive.Run(ref result, ref result);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class ToBoolean
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      Expression.Execute(ref result, ref callFrame, interpreter);
      result.Set(Operations.Convert.ToBoolean.Run(ref result));
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class ToNumber
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      Expression.Execute(ref result, ref callFrame, interpreter);
      Operations.Convert.ToNumber.Run(ref result, ref result);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class ToDouble
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      Expression.Execute(ref result, ref callFrame, interpreter);
      result.Set(Operations.Convert.ToDouble.Run(ref result));
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class ToInteger
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      Expression.Execute(ref result, ref callFrame, interpreter);
      result.Set(Operations.Convert.ToInt32.Run(ref result));
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class ToInt32
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      Expression.Execute(ref result, ref callFrame, interpreter);
      result.Set(Operations.Convert.ToInt32.Run(ref result));
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class ToUInt32
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      Expression.Execute(ref result, ref callFrame, interpreter);
      result.Set(Operations.Convert.ToUInt32.Run(ref result));
      interpreter.PopLocation(this, ref result);
    }
  }
  //public partial class ToUInt16
  //{
  //  public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
  //  {
  //    interpreter.PushLocation(this);
  //    Expression.Execute(ref result, ref callFrame, interpreter);
  //    result.Set(Operations.Convert.ToInt16.Run(ref result));
  //    interpreter.PopLocation(this, ref result);
  //  }
  //}
  public partial class ToString
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      Expression.Execute(ref result, ref callFrame, interpreter);
      result.Set(Operations.Convert.ToString.Run(ref result));
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class ToObject
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      Expression.Execute(ref result, ref callFrame, interpreter);
      if (mdr.Runtime.Instance.Configuration.ProfileStats)
      {
        mdr.Runtime.Instance.Counters.GetCounter("ToObject").Count++;
        string toObjectCounterName = "ToObject_" + result.ValueType;
        mdr.Runtime.Instance.Counters.GetCounter(toObjectCounterName).Count++;
      }
      result.Set(Operations.Convert.ToObject.Run(ref result));
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class ToFunction
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      Expression.Execute(ref result, ref callFrame, interpreter);
      var obj = Operations.Convert.ToObject.Run(ref result);
      result.Set(obj.ToDFunction());
      interpreter.PopLocation(this, ref result);
    }
  }
  #endregion

  #region Unary Operators; ECMA 11.4 -------------------------------------------------------------------------------------
  public partial class DeleteExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      var expression = Expression;
      var parent = expression as ParenExpression;
      if (parent != null)
        expression = parent.Expression;
      var indexer = expression as ReadIndexerExpression;
      if (indexer != null)
      {
        indexer.Container.Execute(ref result, ref callFrame, interpreter);
        var obj = result.AsDObject();
        indexer.Index.Execute(ref result, ref callFrame, interpreter);
        result.Set(Operations.Unary.DeleteProperty.Run(obj, ref result));
      }
      else
      {
        //we need to visit in case the expression has side effects, but then throw away result
        expression.Execute(ref result, ref callFrame, interpreter);

        var readId = expression as ReadIdentifierExpression;
        if (readId != null)
          result.Set(Operations.Unary.DeleteVariable.Run(interpreter.Context, readId.Symbol.FieldId));
        else
          result.Set(Operations.Unary.Delete.Run(ref result));
      }
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class VoidExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      Expression.Execute(ref result, ref callFrame, interpreter);
      result.SetUndefined();
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class TypeofExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      Expression.Execute(ref result, ref callFrame, interpreter);
      result.Set(Operations.Unary.Typeof.Run(ref result));
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class PositiveExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      Expression.Execute(ref result, ref callFrame, interpreter);
      Operations.Unary.Positive.Run(ref result, ref result);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class NegativeExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      Expression.Execute(ref result, ref callFrame, interpreter);
      Operations.Unary.Negative.Run(ref result, ref result);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class BitwiseNotExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      Expression.Execute(ref result, ref callFrame, interpreter);
      result.Set(Operations.Unary.BitwiseNot.Run(ref result));
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class LogicalNotExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      Expression.Execute(ref result, ref callFrame, interpreter);
      result.Set(Operations.Unary.LogicalNot.Run(ref result));
      interpreter.PopLocation(this, ref result);
    }
  }
  #endregion

  #region Binary Multiplicative Operators; ECMA 11.5 -------------------------------------------------------------------------------------
  public partial class MultiplyExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      var tmp = new mdr.DValue();
      Left.Execute(ref result, ref callFrame, interpreter);
      Right.Execute(ref tmp, ref callFrame, interpreter);
      Operations.Binary.Multiply.Run(ref result, ref tmp, ref result);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class DivideExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      var tmp = new mdr.DValue();
      Left.Execute(ref result, ref callFrame, interpreter);
      Right.Execute(ref tmp, ref callFrame, interpreter);
      result.Set(Operations.Binary.Divide.Run(ref result, ref tmp));
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class RemainderExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      var tmp = new mdr.DValue();
      Left.Execute(ref result, ref callFrame, interpreter);
      Right.Execute(ref tmp, ref callFrame, interpreter);
      result.Set(Operations.Binary.Remainder.Run(ref result, ref tmp));
      interpreter.PopLocation(this, ref result);
    }
  }
  #endregion
  #region Binary Additive Operators; ECMA 11.6 -------------------------------------------------------------------------------------
  public partial class AdditionExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      var tmp = new mdr.DValue();
      Left.Execute(ref result, ref callFrame, interpreter);
      Right.Execute(ref tmp, ref callFrame, interpreter);
      Operations.Binary.Addition.Run(ref result, ref tmp, ref result);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class SubtractionExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      var tmp = new mdr.DValue();
      Left.Execute(ref result, ref callFrame, interpreter);
      Right.Execute(ref tmp, ref callFrame, interpreter);
      Operations.Binary.Subtraction.Run(ref result, ref tmp, ref result);
      interpreter.PopLocation(this, ref result);
    }
  }
  #endregion
  #region Binary Bitwise Shift Operators; ECMA 11.7 -------------------------------------------------------------------------------------
  public partial class LeftShiftExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      var tmp = new mdr.DValue();
      Left.Execute(ref result, ref callFrame, interpreter);
      Right.Execute(ref tmp, ref callFrame, interpreter);
      result.Set(Operations.Binary.LeftShift.Run(ref result, ref tmp));
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class RightShiftExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      var tmp = new mdr.DValue();
      Left.Execute(ref result, ref callFrame, interpreter);
      Right.Execute(ref tmp, ref callFrame, interpreter);
      result.Set(Operations.Binary.RightShift.Run(ref result, ref tmp));
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class UnsignedRightShiftExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      var tmp = new mdr.DValue();
      Left.Execute(ref result, ref callFrame, interpreter);
      Right.Execute(ref tmp, ref callFrame, interpreter);
      result.Set(Operations.Binary.UnsignedRightShift.Run(ref result, ref tmp));
      interpreter.PopLocation(this, ref result);
    }
  }
  #endregion
  #region Binary Relational Operators; ECMA 11.8 -------------------------------------------------------------------------------------
  public partial class LesserExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      var tmp = new mdr.DValue();
      Left.Execute(ref result, ref callFrame, interpreter);
      Right.Execute(ref tmp, ref callFrame, interpreter);
      result.Set(Operations.Binary.LessThan.Run(ref result, ref tmp));
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class GreaterExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      var tmp = new mdr.DValue();
      Left.Execute(ref result, ref callFrame, interpreter);
      Right.Execute(ref tmp, ref callFrame, interpreter);
      result.Set(Operations.Binary.GreaterThan.Run(ref result, ref tmp));
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class LesserOrEqualExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      var tmp = new mdr.DValue();
      Left.Execute(ref result, ref callFrame, interpreter);
      Right.Execute(ref tmp, ref callFrame, interpreter);
      result.Set(Operations.Binary.LessThanOrEqual.Run(ref result, ref tmp));
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class GreaterOrEqualExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      var tmp = new mdr.DValue();
      Left.Execute(ref result, ref callFrame, interpreter);
      Right.Execute(ref tmp, ref callFrame, interpreter);
      result.Set(Operations.Binary.GreaterThanOrEqual.Run(ref result, ref tmp));
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class InstanceOfExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      var tmp = new mdr.DValue();
      Left.Execute(ref result, ref callFrame, interpreter);
      Right.Execute(ref tmp, ref callFrame, interpreter);
      result.Set(Operations.Binary.InstanceOf.Run(ref result, ref tmp));
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class InExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      var tmp = new mdr.DValue();
      Left.Execute(ref result, ref callFrame, interpreter);
      Right.Execute(ref tmp, ref callFrame, interpreter);
      result.Set(Operations.Binary.In.Run(ref result, ref tmp));
      interpreter.PopLocation(this, ref result);
    }
  }
  #endregion
  #region Binary Equality Operators; ECMA 11.9 -------------------------------------------------------------------------------------
  public partial class EqualExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      var tmp = new mdr.DValue();
      Left.Execute(ref result, ref callFrame, interpreter);
      Right.Execute(ref tmp, ref callFrame, interpreter);
      result.Set(Operations.Binary.Equal.Run(ref result, ref tmp));
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class NotEqualExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      var tmp = new mdr.DValue();
      Left.Execute(ref result, ref callFrame, interpreter);
      Right.Execute(ref tmp, ref callFrame, interpreter);
      result.Set(Operations.Binary.NotEqual.Run(ref result, ref tmp));
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class SameExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      var tmp = new mdr.DValue();
      Left.Execute(ref result, ref callFrame, interpreter);
      Right.Execute(ref tmp, ref callFrame, interpreter);
      result.Set(Operations.Binary.Same.Run(ref result, ref tmp));
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class NotSameExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      var tmp = new mdr.DValue();
      Left.Execute(ref result, ref callFrame, interpreter);
      Right.Execute(ref tmp, ref callFrame, interpreter);
      result.Set(Operations.Binary.NotSame.Run(ref result, ref tmp));
      interpreter.PopLocation(this, ref result);
    }
  }
  #endregion
  #region Binary Bitwise Operators; ECMA 11.10 -------------------------------------------------------------------------------------
  public partial class BitwiseAndExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      var tmp = new mdr.DValue();
      Left.Execute(ref result, ref callFrame, interpreter);
      Right.Execute(ref tmp, ref callFrame, interpreter);
      result.Set(Operations.Binary.BitwiseAnd.Run(ref result, ref tmp));
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class BitwiseOrExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      var tmp = new mdr.DValue();
      Left.Execute(ref result, ref callFrame, interpreter);
      Right.Execute(ref tmp, ref callFrame, interpreter);
      result.Set(Operations.Binary.BitwiseOr.Run(ref result, ref tmp));
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class BitwiseXorExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      var tmp = new mdr.DValue();
      Left.Execute(ref result, ref callFrame, interpreter);
      Right.Execute(ref tmp, ref callFrame, interpreter);
      result.Set(Operations.Binary.BitwiseXor.Run(ref result, ref tmp));
      interpreter.PopLocation(this, ref result);
    }
  }
  #endregion
  #region Binary Logical Operators; ECMA 11.11 -------------------------------------------------------------------------------------
  public partial class LogicalAndExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      Implementation.Execute(ref result, ref callFrame, interpreter);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class LogicalOrExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      Implementation.Execute(ref result, ref callFrame, interpreter);
      interpreter.PopLocation(this, ref result);
    }
  }
  #endregion

  #region Conditional Operators; ECMA 11.12 -------------------------------------------------------------------------------------
  public partial class TernaryExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      Left.Execute(ref result, ref callFrame, interpreter);
      if (result.AsBoolean())
        Middle.Execute(ref result, ref callFrame, interpreter);
      else
        Right.Execute(ref result, ref callFrame, interpreter);
      interpreter.PopLocation(this, ref result);
    }
  }
  #endregion

  #region Assignment; ECMA 11.13 -------------------------------------------------------------------------------------
  public partial class WriteTemporaryExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      var temp = interpreter.Declare(this);
      if (temp.Node == null)
      {
        //This is the very first visit
        //We need to regenerate results
        temp.Node = this;

        interpreter.PushLocation(this);
        Value.Execute(ref result, ref callFrame, interpreter);
        temp.Value = result;
        interpreter.PopLocation(this, ref result);
      }
      else
        result = temp.Value;
    }
  }
  public partial class WriteIdentifierExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      Value.Execute(ref result, ref callFrame, interpreter);
      interpreter.WriteSymbol(Symbol, ref result, ref callFrame);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class WriteIndexerExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);

      Container.Execute(ref result, ref callFrame, interpreter);
      var obj = result.AsDObject();
      var index = new mdr.DValue();
      Index.Execute(ref index, ref callFrame, interpreter);
      Value.Execute(ref result, ref callFrame, interpreter);
      obj.SetField(ref index, ref result);

      //interpreter.PopLocation(this, ref result, obj);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class WritePropertyExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);

      Container.Execute(ref result, ref callFrame, interpreter);
      var obj = result.AsDObject();
      AssignFieldId();
      Value.Execute(ref result, ref callFrame, interpreter);
      obj.SetFieldByFieldId(FieldId, ref result);

      //interpreter.PopLocation(this, ref result, obj);
      interpreter.PopLocation(this, ref result);
    }
  }
  #endregion

  #region Comma Operator; ECMA 11.14 -------------------------------------------------------------------------------------
  public partial class CommaOperatorExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      for (var i = 0; i < Expressions.Count; ++i)
        Expressions[i].Execute(ref result, ref callFrame, interpreter);
      interpreter.PopLocation(this, ref result);
    }
  }
  #endregion

  #region Function Calls; ECMA 11.2.2, 11.2.3 -------------------------------------------------------------------------------------
  public partial class NewExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);

      var tmpCallFrame = new mdr.CallFrame();

      Function.Execute(ref result, ref callFrame, interpreter);

      interpreter.LoadArguments(this.Arguments, ref tmpCallFrame, ref callFrame);

      tmpCallFrame.Function = result.AsDFunction();
      
      JSRuntime.StopTimer(interpreter.Timer);
      tmpCallFrame.Function.Construct(ref tmpCallFrame);
      JSRuntime.StartTimer(interpreter.Timer);
      result.Set(tmpCallFrame.This);

      interpreter.PopLocation(this, tmpCallFrame.Function);
    }
  }
  public partial class CallExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);

      var tmpCallFrame = new mdr.CallFrame();

      Function.Execute(ref result, ref callFrame, interpreter);

      if (ThisArg != null)
      {
        var thisValue = new mdr.DValue();
        ThisArg.Execute(ref thisValue, ref callFrame, interpreter);
        tmpCallFrame.This = thisValue.AsDObject();
      }
      else if (IsDirectEvalCall)
      {
        tmpCallFrame.CallerFunction = callFrame.Function;
        tmpCallFrame.CallerContext = interpreter.Context;
        tmpCallFrame.This = callFrame.This;
      }
      else
      {
        tmpCallFrame.This = mdr.Runtime.Instance.GlobalContext;
      }

      interpreter.LoadArguments(this.Arguments, ref tmpCallFrame, ref callFrame);

      tmpCallFrame.Function = result.AsDFunction();
      JSRuntime.StopTimer(interpreter.Timer);
      tmpCallFrame.Function.Call(ref tmpCallFrame);
      JSRuntime.StartTimer(interpreter.Timer);
      result = tmpCallFrame.Return;

      interpreter.PopLocation(this, tmpCallFrame.Function);
    }


  }
  #endregion

  #region Guards
  public partial class GuardedCast
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);

      this.Expression.Execute(ref result, ref callFrame, interpreter);

      interpreter.PopLocation(this, ref result);
    }
  }

  #endregion

  #region Function Definition; ECMA 13 -------------------------------------------------------------------------------------
  public partial class FunctionExpression
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      var func = new mdr.DFunction(
        ((JSFunctionMetadata)callFrame.Function.Metadata).SubFunctions[Metadata.FuncDefinitionIndex]
        , interpreter.Context);
      result.Set(func);
      interpreter.PopLocation(this, ref result);
    }
  }
  public partial class FunctionDeclarationStatement
  {
    public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
    {
      interpreter.PushLocation(this);
      Implementation.Execute(ref result, ref callFrame, interpreter);
      interpreter.PopLocation(this, ref result);
    }
  }
  #endregion

  #region Program; ECMA 14 -------------------------------------------------------------------------------------
  //public partial class Program
  //{
  //  public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
  //  {
  //    interpreter.PushLocation(this);
  //    interpreter.PopLocation(this, ref result);
  //  }
  //}
  #endregion

  #region Interanls
  //public partial class InternalCall
  //{
  //  public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
  //  {
  //    interpreter.PushLocation(this);
  //    interpreter.PopLocation(this, ref result);
  //  }
  //}
  //public partial class InternalNew
  //{
  //  public override void Execute(ref mdr.DValue result, ref mdr.CallFrame callFrame, Interpreter interpreter)
  //  {
  //    interpreter.PushLocation(this);
  //    interpreter.PopLocation(this, ref result);
  //  }
  //}
  #endregion
}
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
using System.Text;
using System.Collections.Generic;

using m.Util.Diagnose;
using mjr.IR;

namespace mjr.CodeGen
{
  static class FunctionInliner
  {
    static AlgorithmImplementation _pool = new AlgorithmPool<FunctionInlinerImp>("JS/Inline/", () => new FunctionInlinerImp(), () => JSRuntime.Instance.Configuration.ProfileJitTime);
    public static void Execute(CodeGenerationInfo cgInfo) { _pool.Execute(cgInfo); }

    sealed class FunctionInlinerImp : IRCloner, AlgorithmImplementation
    {
      /// <summary>
      /// call node in caller responsible for the call
      /// </summary>
      CallExpression _call;

      /// <summary>
      /// callee function which is being inlined
      /// </summary>
      JSFunctionMetadata _targetFuncMetadata;

      /// <summary>
      /// The current scope in the target function 
      /// </summary>
      Scope _currentTargetScope;

      /// <summary>
      /// Since we may speculatively inline function, we should not polute the maing scope
      /// with renamed symbols. We have to keep everything contained.
      /// During the JIT we may be able to do more optimization, e.g. for globals
      /// </summary>
      Scope _newScope;

      /// <summary>
      /// If callee has return value, it will be written to this symbol
      /// </summary>
      JSSymbol _returnValueSymbol;

      /// <summary>
      /// every return is turned into a an assignment and jump to this label
      /// </summary>
      string _returnLabelName;

      static int _round = 0; //This is incremented everytime that inliner runs and its value is used in renaming symbols
      const int MaximumAstCostToInline = 150;

      LinkedList<JSFunctionMetadata> _functionsBeingInlined = new LinkedList<JSFunctionMetadata>();

      public void Execute(JSFunctionMetadata funcMetadata) { throw new NotImplementedException(); }
      public void Execute(CodeGenerationInfo cgInfo)
      {
        _functionsBeingInlined.Clear();
        _functionsBeingInlined.AddLast(cgInfo.FuncMetadata);

        InlineScope(cgInfo.FuncMetadata.Scope, cgInfo.FuncCode.Profiler);

        _functionsBeingInlined.RemoveLast();
        Debug.Assert(_functionsBeingInlined.Count == 0, "Invalid situation");
      }

      void InlineScope(Scope scope, Profiler scopeProfile)
      {
        foreach (Invocation ScopeInvocation in scope.Invocations)
        {
          CallExpression invocation = ScopeInvocation as CallExpression;
          if (invocation == null)
            continue; //For now we only support calls

          mdr.DFunction target = null;
          Profiler targetProfile = null;

          if (scopeProfile != null && JSRuntime.Instance.Configuration.EnableSpeculativeJIT)
          {
            var nodeProfile = scopeProfile.GetNodeProfile(invocation);
            if (nodeProfile != null)
            {
              target = nodeProfile.Target;
              if (target != null)
              {
                if (target.Code != null)
                  targetProfile = (target.Code as JSFunctionCode).Profiler;
              }
            }
          }

          var targetFuncMD = invocation.TargetFunctionMetadata;

          if (targetFuncMD != null)
          {
            if (invocation.InlinedIR != null)
            {
              ///We might be re-jiting a function and hence calling inliner again
              ///for invocation that were statically resolved and inline, we don't 
              ///have to repease the process again and can reuse the old inlined IR
              ///we can still use the targetProfile to help the rest of algorithms

              invocation.InlinedIR.TargetProfile = targetProfile;
              continue; //skip this call
            }
          }
          else
          {
            ///We need to clear the old inlined IR, 
            ///TODO: we should walk over the arguments of the call and remove users
            invocation.InlinedIR = null; //clear the old results if any

            if (target != null)
            {
              targetFuncMD = target.Metadata as JSFunctionMetadata;
            }
          }
          if (targetFuncMD != null)
          {
            InlineInvocation(invocation, targetFuncMD);
            if (invocation.InlinedIR != null)
            {
              invocation.InlinedIR.TargetProfile = targetProfile;
              InlineScope(invocation.InlinedIR.Scope, targetProfile);
            }
          }

        }
      }

      void InlineInvocation(CallExpression invocation, JSFunctionMetadata targetFuncMetadata)
      {
        _targetFuncMetadata = targetFuncMetadata;
        _call = invocation;
        Debug.WriteLine("Trying to inline function {0}", _targetFuncMetadata.Declaration);

        _targetFuncMetadata.Analyze(); //Just to make sure it is analyzed
        
        if (JSRuntime.Instance.Configuration.ProfileStats)
          JSRuntime.Instance.Counters.GetCounter("Attempted Inline").Count++;

        if (!CanInline(_targetFuncMetadata))
          return;

        if (JSRuntime.Instance.Configuration.ProfileStats)
          JSRuntime.Instance.Counters.GetCounter("Succeeded Inline").Count++;

        _functionsBeingInlined.AddLast(_targetFuncMetadata);

        _round++;

        throw new NotImplementedException(); //TODO: we need to update this algorithm based on the recent changes to the scope
        _newScope = new Scope(_currentTargetScope);
        _returnValueSymbol = _newScope.AddSymbol(RenameSymbol("retVal"));
        _returnValueSymbol.SymbolType = JSSymbol.SymbolTypes.HiddenLocal;


        _call.InlinedIR = new InlinedInvocation(
          _targetFuncMetadata
          , _newScope
          , BuildInlinedBody()
          , new ReadIdentifierExpression(_returnValueSymbol)
        );
        _call.InlinedIR.AddUser(_call);

        Debug.WriteLine("Inlined function {0}", _targetFuncMetadata.Declaration);

        _functionsBeingInlined.RemoveLast();
      }

      /// <summary>
      /// Checks requirements and heuristics and determines whether we should inline or not
      /// </summary>
      /// <returns></returns>
      private bool CanInline(JSFunctionMetadata funcMetadata)
      {
        var scope = funcMetadata.Scope;

        if (funcMetadata.SubFunctions.Count > 0 //don't inline if the target has subfunctions //TODO:unless none of subfunction (tree) has ParenLocal symbols
            || scope.AstCost > MaximumAstCostToInline //TODO: we don't have a good cost function
            || scope.HasClosedOnSymbol //TODO: we can inline some functions that have closed on variables
            || scope.HasThisSymbol //TODO: for now we don't inline if there is "this" keyword in the function, but can be done
            || scope.HasEval //we don't know what direct eval can do 
            || scope.HasArgumentsSymbol //TODO: we can actually do this as well, with some care at call site
            || _functionsBeingInlined.Contains(funcMetadata)//recursive call
            )
        {
          Debug.WriteLine("Cannot inline function {0}", funcMetadata.Declaration);
          return false;
        }
        return true;
      }

      private BlockStatement BuildInlinedBody()
      {
        var body = new BlockStatement(new List<Statement>());
        AddVarDeclarationsForParams(body);
        AddRenamedSymbols();
        _returnLabelName = RenameSymbol("#ReturnLabel:");
        var clonedBody = GetCloneOf(_targetFuncMetadata.FunctionIR.Statement);
        Debug.Assert(clonedBody != null, "Invalid situation! Cloning failed!");
        var returnLabel = new LabelStatement(_returnLabelName, clonedBody);
        body.Statements.Add(returnLabel);
        return body;
      }

      private string RenameSymbol(string name)
      {
        return string.Format("{0}÷{1}${2}", _targetFuncMetadata.FullName, _round, name);
      }

      private void AddVarDeclarationsForParams(BlockStatement body)
      {
        foreach (var p in _targetFuncMetadata.FunctionIR.Parameters)
        {
          var s = p.Symbol;
          Debug.Assert(s.IsParameter, "Invalid situation, symbol {0} in function {1} must be a Parameter", s.Name, _targetFuncMetadata.Declaration);
          var renamedSymbol = GetRenamedSymbolOf(s);
          WriteIdentifierExpression initialization = null;
          if (s.ParameterIndex < _call.Arguments.Count)
          {
            //TODO: in the following, we have not removed the user of the argument, so it will introduce a WriteTemporary. 
            initialization = new WriteIdentifierExpression(renamedSymbol, _call.Arguments[s.ParameterIndex]);
          }
          var declaration = new VariableDeclarationStatement(new List<VariableDeclaration>() { new VariableDeclaration(renamedSymbol, initialization) });
          body.Statements.Add(declaration);
        }
      }

      private JSSymbol GetRenamedSymbolOf(JSSymbol s)
      {
        JSSymbol newSymbol = null;
        switch (s.SymbolType)
        {
          case JSSymbol.SymbolTypes.Local:
            newSymbol = _newScope.GetOrAddSymbol(RenameSymbol(s.Name));
            newSymbol.SymbolType = JSSymbol.SymbolTypes.Local;
            break;
          case JSSymbol.SymbolTypes.Global:
            newSymbol = _newScope.GetOrAddSymbol(s.Name);
            newSymbol.SymbolType = JSSymbol.SymbolTypes.Global;
            break;
          default:
            Trace.Fail("Cannot support symbol type {0}", s.SymbolType);
            break;
        }
        return newSymbol;
      }

      private void AddRenamedSymbols()
      {
        //Technically, we don't need this, as we walk the IR, these will be gradually added!
        foreach (var s in _targetFuncMetadata.Scope.Symbols)
          GetRenamedSymbolOf(s);
      }

      public override void Visit(VariableDeclaration node)
      {
        result = new VariableDeclaration(GetRenamedSymbolOf(node.Symbol), GetCloneOf(node.Initialization));
      }

      public override void Visit(ReturnStatement node)
      {
        var gotoEnd = new GotoStatement(_returnLabelName);
        if (node.Expression != null)
        {
          //We have a return value as well
          var cloned = new BlockStatement(new List<Statement>());

          var returnValue = GetCloneOf(node.Expression);

          var retAssign = new WriteIdentifierExpression(_returnValueSymbol, returnValue);
          cloned.Statements.Add(gotoEnd);
          cloned.Statements.Add(new ExpressionStatement(retAssign));
          result = cloned;
        }
        else
        {
          result = gotoEnd;
        }
      }

      public override void Visit(ReadIdentifierExpression node)
      {
        unfinishedClone = new ReadIdentifierExpression(GetRenamedSymbolOf(node.Symbol));
        Visit((Identifier)node);
      }

      public override void Visit(WriteIdentifierExpression node)
      {
        var renamedSymbol = GetRenamedSymbolOf(node.Symbol);
        unfinishedClone = new WriteIdentifierExpression(renamedSymbol, GetCloneOf(node.Value));
        Visit((Identifier)node);
      }

      //TODO: if we want to keep inlining "new" operations, we should uncomment the following as well

      public override void Visit(CallExpression node)
      {
        base.Visit(node);
        _newScope.Invocations.Add((CallExpression)result);
      }

      public override void Visit(FunctionDeclarationStatement expression)
      {
        Trace.Fail("Invalid situation, should not get here");
      }

      public override void Visit(FunctionExpression expression)
      {
        Trace.Fail("Invalid situation, should not be here");
      }
    }

  }
}

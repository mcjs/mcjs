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
using System.Linq;
using System.Collections.Generic;

using mjr.IR;
using m.Util.Diagnose;

namespace mjr.CodeGen
{
  /// <summary>
  /// <Mehrdad>
  /// This class performs all analysis that is needed for running a function. Since we want to perform these analysis 
  /// only if we are going to run the function, we collect them all here. In some cases, it might be tempting to 
  /// change the behavior of setters/getters on IR nodes to do some of these tasks on the fly. But the problem is that
  /// many of those codes will run in the context of parser, for all functions even if they are not executed. 
  /// So, don't move code out of here, unless if you're absolutely sure it is always needed irrespective of function being executed 
  /// or not; and if that code is used in many places and help the code to be more readable
  /// </Mehrdad>
  /// </summary>
  static class Analyzer
  {
    static AlgorithmImplementation _pool = new AlgorithmPool<AnalyzerImp>("JS/Analyze/", () => new AnalyzerImp(), () => JSRuntime.Instance.Configuration.ProfileAnalyzeTime);
    public static void Execute(JSFunctionMetadata funcMetadata) { _pool.Execute(funcMetadata); }

    sealed class AnalyzerImp : AlgorithmImplementation
    {
      //static class AstCost
      //{
      //  public static int Cost(Program expression) { return 0; }
      //  public static int Cost(BlockStatement expression) { return 0; }
      //  public static int Cost(Statement expression) { return 0; }
      //  public static int Cost(EmptyStatement expression) { return 0; }
      //  public static int Cost(ExpressionStatement expression) { return 2; }

      //  public static int Cost(WithStatement expression) { return 0; }
      //  public static int Cost(IfStatement expression) { return 2; }
      //  public static int Cost(SwitchStatement expression) { return 2; }
      //  public static int Cost(WhileStatement expression) { return 10; }
      //  public static int Cost(DoWhileStatement expression) { return 10; }
      //  public static int Cost(ForStatement expression) { return 10; }
      //  public static int Cost(ForEachInStatement expression) { return 10; }

      //  public static int Cost(TryStatement expression) { return 4; }
      //  public static int Cost(ThrowStatement expression) { return 4; }

      //  public static int Cost(LabelStatement expression) { return 1; }
      //  public static int Cost(GotoStatement expression) { return 2; }
      //  public static int Cost(BreakStatement expression) { return 1; }
      //  public static int Cost(ContinueStatement expression) { return 1; }
      //  public static int Cost(ReturnStatement expression) { return 2; }

      //  public static int Cost(VariableDeclarationStatement expression) { return 1; }

      //  public static int Cost(CommaOperatorStatement expression) { return 2; }
      //  public static int Cost(AssignmentExpression expression) { return 2; }
      //  public static int Cost(FunctionDeclarationStatement expression) { return 1; }
      //  public static int Cost(FunctionExpression expression) { return 0; }
      //  public static int Cost(ArrayDeclaration expression) { return 4; }
      //  public static int Cost(JsonExpression expression) { return 4; }
      //  public static int Cost(PropertyDeclarationExpression expression) { return 2; }
      //  public static int Cost(RegexpExpression expression) { return 6; }
      //  public static int Cost(NewExpression expression) { return 4; }
      //  public static int Cost(MethodCall expression) { return 6; }

      //  public static int Cost(TernaryExpression expression) { return 3; }
      //  public static int Cost(BinaryExpression expression) { return 2; }
      //  public static int Cost(UnaryExpression expression) { return 1; }
      //  public static int Cost(PropertyExpression expression) { return 4; }
      //  public static int Cost(Indexer expression) { return 2; }
      //  public static int Cost(Identifier expression) { return 0; }
      //  public static int Cost(ValueExpression expression) { return 1; }
      //}

      JSFunctionMetadata _currFuncMetadata;

      public void Execute(CodeGenerationInfo cgInfo) { throw new NotImplementedException(); }
      public void Execute(JSFunctionMetadata funcMetadata)
      {
        Debug.Assert(!funcMetadata.IsAnalyzed, "Function {0} is already analysed", funcMetadata.Declaration);
        _currFuncMetadata = funcMetadata;

        var scope = _currFuncMetadata.FunctionIR.Scope;
        Debug.Assert(scope.IsFunction, "Scope of function {0} must be a function type", funcMetadata.Declaration);

        if (scope.IsProgram)
        {
          ///By default, there might be multiple scripts loaded, and hence we don't have full visibility over all program.
          ///therefore we should be conservative and assume that there will be unknown sub-functions with all sorts of possible side effects
          ///unless there is a switch for a special mode in the engine that says we can be more aggressive 
          ///and assume for example there is only going to be on top level program
          ///in generatl, one other option is also to assume all symbols in the main program (top level script) are marked global, however, knowing they are local may help later
          scope.HasUnknownSubFunction = true;

          //TODO: we may have to artificially add the "arguments" to the global scope, and later in the program set it to []
          AnalyzeScope(scope);

          scope.HasLocalSymbol = false; //We just converted everything to global!
        }
        else
        {
          if (scope.IsEvalFunction)
            ImplicitReturnInserter.Execute(funcMetadata);

          for (var i = _currFuncMetadata.SubFunctions.Count - 1; i >= 0; --i)
          {
            var f = _currFuncMetadata.SubFunctions[i];
            f.Analyze();
            scope.HasUnknownSubFunction = f.Scope.HasEval || f.Scope.HasUnknownSubFunction;
          }

          ///<Mehrdad>
          ///NOTE: we used to inline functions here, but in the new model, we only do it when the function is hot. 
          ///Also, we have separate scope for the inlined functions, so we will resolve them on demand later. 
          ///</Mehrdad>
          AnalyzeScope(scope);
        }
      }

      /// <summary>
      /// This function will only analyze the scope of one function
      /// </summary>
      private void AnalyzeScope(Scope scope)
      {
        Debug.WriteLine("Analyzing scope {0}", scope);

        if (scope.IsFunction)
        {
          CollectPropertiesFromInnerScopes(scope);
          if (scope.HasEval)
          {
            ///When we have eval, we should be very conservative and assume everything is possible. 
            ///this is the place to do that after all symbols are already processed.

            if (!scope.HasArgumentsSymbol)
            {
              var arguments = scope.GetOrAddSymbol(JSFunctionArguments.Name);
              Debug.Assert(
                 arguments.SymbolType == JSSymbol.SymbolTypes.Local
                 || arguments.SymbolType == JSSymbol.SymbolTypes.Unknown
                 , "invalide arguments type {0}", arguments.SymbolType);
              arguments.SymbolType = JSSymbol.SymbolTypes.Arguments;
              scope.HasArgumentsSymbol = true;
            }
          }
        }

        foreach (var s in scope.Symbols)
        {
          s.ValueIndex = _currFuncMetadata.TotalSymbolCount + s.Index;
          AnalyzeSymbol(s);
        }
        _currFuncMetadata.TotalSymbolCount += scope.Symbols.Count;

        foreach (var s in scope.InnerScopes)
        {
          if (!s.IsFunction)
          {
            AnalyzeScope(s);
            //TODO: should we have this code : scope.HasParentLocalSymbol = scope.HasParentLocalSymbol || s.HasParentLocalSymbol;
          }
          else
          {
            Debug.Assert(
              scope.ContainerFunction.FunctionIR.Scope.IsProgram //We are in program and sub-functions are not analyzed
              || s.ContainerFunction.CurrentStatus >= JSFunctionMetadata.Status.Analyzed //we are not in program and sub-function must be done
              , "A function must be analyzed only after all of its sub-functions are analyzed.");
          }
        }


      }

      /// <summary>
      /// Some important flags must be collected at the top function scope before we can 
      /// correctly analyze all symbols in the function.
      /// </summary>
      private void CollectPropertiesFromInnerScopes(Scope scope)
      {
        foreach (var s in scope.InnerScopes)
        {
          if (!s.IsFunction)
          {
            CollectPropertiesFromInnerScopes(s);
            scope.HasEval = scope.HasEval || s.HasEval;
            scope.HasUnknownSubFunction = scope.HasUnknownSubFunction || scope.HasEval || s.HasUnknownSubFunction || s.HasEval;
          }
        }
      }

      public void AnalyzeSymbol(JSSymbol symbol)
      {
        ///Symbol resolution is very critical and can be complicated considering parallel analyze and inlining. 
        ///Therefore, we have to be conservative and assert every assumption we make
        ///We should consider:
        /// - In normal case, symbols of a function are resolved AFTER its sub-functions are analyzed. Therefore, some cases should not occure. However, if lazy parsing kick in
        ///     the sub-function is parsed-analyzed way after the parent function was even executed. So, this will create a new set of assumptions. 
        /// - Processing the top leve program script may have special cases which we will go through before ending the analysis of the program

        var symbolScope = symbol.ContainerScope;

        ///<Mehrdad>
        ///A function may just reference the "arguments" symbol, or even define its own. 
        ///in any case, such a symbol is first initialized with the arctual arguments value, 
        ///and may be then overwritten in the function. 
        ///In anycase, existance of such a symbol will be costly. 
        ///</Mehrdad>
        if (symbol.Name == JSFunctionArguments.Name)
        {
          var argumentsSymbol = symbol;
          var functionScope = symbolScope;

          if (!symbolScope.IsFunction)
          {
            functionScope = symbolScope.ContainerFunction.FunctionIR.Scope;
            argumentsSymbol = functionScope.GetOrAddSymbol(JSFunctionArguments.Name);
          }

          functionScope.HasArgumentsSymbol = true;
          Debug.Assert(
            argumentsSymbol.SymbolType == JSSymbol.SymbolTypes.Arguments
            || argumentsSymbol.SymbolType == JSSymbol.SymbolTypes.Local
            || argumentsSymbol.SymbolType == JSSymbol.SymbolTypes.Unknown
            , "invalide arguments type {0}", argumentsSymbol.SymbolType);

          argumentsSymbol.SymbolType = JSSymbol.SymbolTypes.Arguments;
          if (symbol != argumentsSymbol)
          {
            Debug.Assert(functionScope != symbolScope, "Invalid situation!");
            symbol.SymbolType = JSSymbol.SymbolTypes.OuterDuplicate;
            symbol.ResolvedSymbol = argumentsSymbol;
          }
        }

        switch (symbol.SymbolType)
        {
          case JSSymbol.SymbolTypes.Local:
            if (
                symbolScope.HasUnknownSubFunction
                || symbolScope.HasEval
                || symbolScope.IsEvalFunction //We don't see the full picture in eval function, the local may be defined outside as well, besides all locals are added to parent scope
                )
            {
              //In these cases we have uncertainty in access sites, so we should threat all as potential ClosedOnLocals
              symbol.SymbolType = JSSymbol.SymbolTypes.ClosedOnLocal;
              goto case JSSymbol.SymbolTypes.ClosedOnLocal;
            }
            else
            {
              symbolScope.HasLocalSymbol = true;
            }
            break;
          case JSSymbol.SymbolTypes.ClosedOnLocal:
            Trace.Assert(symbolScope.IsFunction, "At this point closure on non function level variables is not  supported");
            symbolScope.HasClosedOnSymbol = true;
            if (symbolScope.IsProgram)
            {
              ///This is in fact a global variable declataion:
              ///- We know this code is certainly executed.
              ///- We look for this symbol in the global context when analyzing subfunctions
              ///- If we add this to global context after creating subfunction objects (during execution), the heavy propagate algorithm in the Propertymap will be executed
              ///So we add the symbol to the global object to ensure it exsits there
              //symbol.SymbolType = JSSymbol.SymbolTypes.Global;
              symbol.AssignFieldId();
              var prop = mdr.Runtime.Instance.GlobalContext.Map.GetPropertyDescriptorByFieldId(symbol.FieldId);
              if (prop == null || prop.IsUndefined)
              {
                mdr.Runtime.Instance.GlobalContext.AddOwnPropertyDescriptorByFieldId(
                  symbol.FieldId
                  , mdr.PropertyDescriptor.Attributes.Data 
//                  | mdr.PropertyDescriptor.Attributes.NotConfigurable //TODO: why do we need this? 
                );
              }
            }
            break;
          case JSSymbol.SymbolTypes.HiddenLocal:
            //Nothing to do for these types
            break;
          case JSSymbol.SymbolTypes.OuterDuplicate:
            //this can happen since NodeFactory may have already hoisted some locals to outer function scop
            break;
          case JSSymbol.SymbolTypes.ParentLocal:
            Trace.Fail("Since we are resolving functions bottom-up, and scopes top-down we should not see any symbol type of {0} here", symbol.SymbolType);
            break;
          case JSSymbol.SymbolTypes.Arguments:
            Debug.Assert(symbolScope.HasArgumentsSymbol, "{0} symbol exists, but scope does have the correct attributes", JSFunctionArguments.Name);
            break;
          case JSSymbol.SymbolTypes.Global:
            Debug.Assert(
              symbolScope.IsProgram
              && mdr.Runtime.Instance.GlobalContext.HasOwnProperty(symbol.Name)
              , "we should only see this after resolving symbols and symbol {0} should be global but is not in the global context", symbol.Name);
            break;
          case JSSymbol.SymbolTypes.Unknown:
            //NOTE: we should never try to resolve "undefined" since it can be easily assigned to at runtime!!!!
            if (symbolScope.HasEval && symbolScope.IsFunction)
            {
              break; //The symbol may be added later by the eval!
            }
            ResolveMethod(symbol);
            break;
          default:
            throw new InvalidOperationException(string.Format("{0} has unexpected symbol type {1} in {2}. This case should not have happened.", symbol.Name, symbol.SymbolType, _currFuncMetadata.FullName));
        }
      }

      private static void ResolveMethod(JSSymbol symbol)
      {
        var containerFunction = symbol.ContainerScope.ContainerFunction;

        bool shouldResolveAtRuntime = false;
        for (var outerScope = symbol.ContainerScope.OuterScope; outerScope != null; outerScope = outerScope.OuterScope)
        {
          var resolvedSymbol = outerScope.GetSymbol(symbol.Name);
          if (resolvedSymbol != null)
          {
            if (outerScope.ContainerFunction == containerFunction)
            {
              ///We just need to know everywhere instead of symbol, we should refer to resolved symbol
              ///the rest will be taken care of as we keep analyzing the upper scopes
              symbol.SymbolType = JSSymbol.SymbolTypes.OuterDuplicate;
              if (resolvedSymbol.SymbolType == JSSymbol.SymbolTypes.OuterDuplicate)
              {
                Debug.Assert(resolvedSymbol.ResolvedSymbol != null, "symbol {0} must have already been resolved!");
                symbol.ResolvedSymbol = resolvedSymbol.ResolvedSymbol;
              }
              else
                symbol.ResolvedSymbol = resolvedSymbol;
            }
            else
            {
              //here we are sure the symbol is found in a parent function scope
              switch (resolvedSymbol.SymbolType)
              {
                case JSSymbol.SymbolTypes.Local:
                case JSSymbol.SymbolTypes.ClosedOnLocal:
                  symbol.ResolvedSymbol = resolvedSymbol;
                  if (outerScope.IsProgram)
                  {
                    symbol.SymbolType = JSSymbol.SymbolTypes.Global;
                  }
                  else
                  {
                    symbol.SymbolType = JSSymbol.SymbolTypes.ParentLocal;
                  }
                  symbol.ContainerScope.HasParentLocalSymbol = true; //We need this anyways since we want to know if a context is needed at runtime at all
                  lock (resolvedSymbol) //In case sub-functions are processed in parallel
                  {
                    resolvedSymbol.SymbolType = JSSymbol.SymbolTypes.ClosedOnLocal;
                    resolvedSymbol.NonLocalWritersCount += symbol.Writers.Count;
                    resolvedSymbol.AssignFieldId();
                  }
                  symbol.AssignFieldId();
                  break;
                case JSSymbol.SymbolTypes.Unknown:
                  if (outerScope.HasEval)
                  {
                    shouldResolveAtRuntime = true;
                    outerScope = null; //to stop the loop
                  }
                  //we will take care of this after this loop
                  break;
                case JSSymbol.SymbolTypes.ParentLocal:
                  ///We could have made a shortcut here and setup the symbol info, but we would still need to walk up until we find the actual symbol declaration
                  ///So, the optimization here would be just not calling GetSymbol int some parent functions. 
                  ///To simplify code, for now, we just ignore this until we reach the declared symbol 
                  Debug.Assert(
                    !outerScope.HasEval
                    && !outerScope.IsProgram
                    && resolvedSymbol.ResolvedSymbol != null
                    , "symbol {0} in {1} must resolve at runtime", resolvedSymbol.Name, resolvedSymbol.ContainerScope.ContainerFunction.Declaration);
                  Debug.Assert(resolvedSymbol.ResolvedSymbol != null, "symbol {0} in {1} must have been already resolved", resolvedSymbol.Name, resolvedSymbol.ContainerScope.ContainerFunction.Declaration);
                  symbol.ResolvedSymbol = resolvedSymbol.ResolvedSymbol;
                  symbol.SymbolType = JSSymbol.SymbolTypes.ParentLocal;
                  symbol.ContainerScope.HasParentLocalSymbol = true;
                  symbol.AssignFieldId();
                  outerScope = null; //stop the loop
                  break;
                case JSSymbol.SymbolTypes.Global:
                  Debug.Assert(
                    outerScope.IsProgram
                    || !outerScope.HasEval
                    , "symbol {0} in {1} must resolve at runtime", resolvedSymbol.Name, resolvedSymbol.ContainerScope.ContainerFunction.Declaration);
                  //symbol is a global that ParentFunction is also using!
                  symbol.SymbolType = JSSymbol.SymbolTypes.Global;
                  if (!outerScope.IsProgram)
                  {
                    symbol.ResolvedSymbol = resolvedSymbol.ResolvedSymbol; //may or maynot be null!
                  }
                  symbol.AssignFieldId();
                  outerScope = null; //end the loop
                  break;
                default:
                  throw new InvalidOperationException(string.Format("{0} has unexpected symbol type {1} in {2}. This case should not have happened.", resolvedSymbol.Name, resolvedSymbol.SymbolType, resolvedSymbol.ContainerScope.ContainerFunction.Declaration));
              }
            }
            break;
          }
          else if (outerScope.HasEval && outerScope.IsFunction)
          {
            ///We did not find this symbol in the parent scopes all the way to the function scope and this scope has eval, so should not search any more
            ///note that if any of the inner scopes of a function scope has eval, then the function scope itself will also has eval
            shouldResolveAtRuntime = true;
            break; //The symbol may be added later by the eval!, end the loop
          }
        }
        if (
            symbol.SymbolType == JSSymbol.SymbolTypes.Unknown
            && !shouldResolveAtRuntime
            )
        {
          //This could be a builtin name
          if (JSRuntime.Instance.GlobalContext.HasOwnProperty(symbol.Name))
          {
            symbol.SymbolType = JSSymbol.SymbolTypes.Global;
            symbol.AssignFieldId();
          }
        }
      }
    }
  }

}

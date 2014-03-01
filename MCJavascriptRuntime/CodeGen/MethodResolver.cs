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
  static class MethodResolver
  {
    static AlgorithmImplementation _pool = new AlgorithmPool<Implementation>("JS/MR/", () => new Implementation(), () => JSRuntime.Instance.Configuration.ProfileJitTime);
    public static void Execute(JSFunctionMetadata funcMetada) { _pool.Execute(funcMetada); }

    class Implementation : AlgorithmImplementation
    {
      public void Execute(CodeGenerationInfo cgInfo) { throw new NotImplementedException(); }
      public void Execute(JSFunctionMetadata functionMetadata)
      {
        // functions declared in the global scope can be overwritten by other functions or 
        // other scripts in the page, so do not resolve!
        foreach (Invocation invocation in functionMetadata.Scope.Invocations)
        {
          DetectTargetFuncMetadata(functionMetadata, invocation, invocation.Function);
        }
      }

      void DetectTargetFuncMetadata(JSFunctionMetadata functionMetadata, Invocation expression, Expression function)
      {
        // functions declared in the global scope can be overwritten by other functions or 
        // other scripts in the page, so do not resolve!

        var parenExp = function as ParenExpression;
        if (parenExp != null)
        {
          DetectTargetFuncMetadata(functionMetadata, expression, parenExp.Expression);
          return;
        }

        var toFunction = function as ToFunction;
        if (toFunction != null)
        {
          DetectTargetFuncMetadata(functionMetadata, expression, toFunction.Expression);
          return;
        }

        var funcExp = function as FunctionExpression;
        if (funcExp != null)
        {
          Debug.WriteLine(new JSSourceLocation(functionMetadata, expression), "  function expression detected.");
          expression.TargetFunctionMetadata = funcExp.Metadata;
          return;
        }
        else
        {
          var id = function as Identifier;
          if (id != null)
          {
            var symbol = id.Symbol;// _currFuncMetadata.GetSymbol(id.Text);
            Debug.WriteLine(new JSSourceLocation(functionMetadata, id), "Method call to {0}:", symbol.Name);
            Debug.Assert(symbol != null, new JSSourceLocation(functionMetadata, id), "Symbol cannot be null here!");
            
            if (symbol.ResolvedSymbol != null)
              symbol = symbol.ResolvedSymbol;
            var jsfunc = symbol.ContainerScope.ContainerFunction;
            //JSFunctionMetadata jsfunc = functionMetadata;
            //for (int i = 0; i < symbol.AncestorDistance; i++)
            //  jsfunc = jsfunc.ParentFunction;

            //if (!jsfunc.Scope.IsProgram) //do not resolve global function declaration targets
            if (!symbol.ContainerScope.IsProgram)
            {
              //symbol = jsfunc.Scope.GetSymbol(id.Symbol.Name);
              if (symbol.SubFunctionIndex != mdr.Runtime.InvalidIndex && symbol.NonLocalWritersCount == 0)
              {
                expression.TargetFunctionMetadata = jsfunc.SubFunctions[symbol.SubFunctionIndex];
                // we have found the corresponding JSFunctionImp instance
                Debug.WriteLine(new JSSourceLocation(jsfunc, expression), string.Format("function declaration found. JSFunctionImp name: {0}", jsfunc.SubFunctions[symbol.SubFunctionIndex].FullName));
              }
              else if (symbol.Writers.Count == 1)
              {
                // if a symbol is assigned once and the assigned expression is a function, we can find
                // the corresponding JSFunctionImp statically when the variable is called. For example: var x = function() {}; x();
                var wfunc = symbol.Writers[0].Value as FunctionExpression;
                if (wfunc != null)
                {
                  Debug.Write(new JSSourceLocation(functionMetadata, wfunc), "  function expression detected.");
                  expression.TargetFunctionMetadata = wfunc.Metadata;
                }
              }
            }
            else
              Debug.WriteLine(new JSSourceLocation(jsfunc, expression), "function {0} was not resolved because it was declared globally", jsfunc.Declaration);
          }
        }
        Debug.WriteLine("");
        return;
      }
    }
  }
}
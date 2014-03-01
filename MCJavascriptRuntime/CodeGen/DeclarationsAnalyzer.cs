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
using System.Diagnostics;
using System.Linq;
using System.Text;
using MCJavascript.Expressions;
using MCJavascript.QParser;
using MCJavascript.Util;

namespace MCJavascript
{
    class DeclarationsAnalyzer : DepthFirstVisitor
    {
        protected JSFunctionImp _currFuncImp;

        public void Execute(JSFunctionImp funcImp)
        {
            var oldFuncImp = _currFuncImp;
            _currFuncImp = funcImp;

            Visit(funcImp.AST);

            //TODO: here we can decide whether to go further down the hierarchy
            foreach (var f in _currFuncImp.SubFunctions)
            {
                // Only analyze functions which have already been parsed. Laziness won't work without this; otherwise, when we analyze the
                // top-level function, we parse and analyze everything!
                if (f.IsParsed)
                    f.Analyze();
            }

            //if (_currFuncImp.ParentFunction == null) //This is either the program or function declaration
            //    CreateFieldForClosedVars(_currFuncImp);

            _currFuncImp = oldFuncImp;
        }

        /*
        void CreateFieldForClosedVars(JSFunctionImp funcImp)
        {
            foreach (var symbol in funcImp.Symbols)
            {
                System.Diagnostics.Debug.Assert(symbol.SymbolType != JSFunctionImp.Symbol.SymbolTypes.Unknown, string.Format("Symbol {0} has unknown type", symbol.Name));
                System.Diagnostics.Debug.Assert(symbol.SymbolType != JSFunctionImp.Symbol.SymbolTypes.Global || symbol.Index != mdr.DType.InvalidIndex, string.Format("Global symbol {0} has no index", symbol.Name));
                if (symbol.IsClosedOn)
                    funcImp.SetField(symbol.Name, mdr.DObject.Undefined); //Only declarations that are closed on will become a filed.
                if (symbol.SymbolType == JSFunctionImp.Symbol.SymbolTypes.ClosedOnLocal || symbol.SymbolType == JSFunctionImp.Symbol.SymbolTypes.ParentLocal)
                    symbol.Index = funcImp.DType.IndexOf(symbol.Name);
            }
            foreach (var f in funcImp.SubFunctions)
            {
                f.SetType(funcImp.DType);
                CreateFieldForClosedVars(f);
            }
        }
        */

        // The below two cases are all that require us to keep DeclarationsAnalyzer around. The problem is the strange interaction between the visitor pattern,
        // DeclarationsAnalyzer and TypeAnalyzer, and the fact that PropertyExpressions are also Identifiers. This usage of the visitor pattern is as bad
        // as 'goto' ever was; the behavior is extremely hard to understand from reading the code. My recommendation is to change PropertyExpression so
        // that it is no longer an Identifier, but this will require changes elsewhere in the codebase where this assumption is made (the parser, for one). - SF

        public override void Visit(Identifier expression)
        {
            var symbol = _currFuncImp.GetSymbol(expression.Text);

            //We may see the declaration of this later! For now we speculate on its type.
            if (symbol.SymbolType == JSFunctionImp.Symbol.SymbolTypes.Unknown)
            {
                for (var funcImp = _currFuncImp.ParentFunction; funcImp != null; funcImp = funcImp.ParentFunction)
                {
                    var psymbol = funcImp.GetSymbol(symbol.Name);
                    if (psymbol != null)
                    {
                        symbol.SymbolType = JSFunctionImp.Symbol.SymbolTypes.ParentLocal;
                        if (psymbol.SymbolType == JSFunctionImp.Symbol.SymbolTypes.Local)
                            psymbol.SymbolType = JSFunctionImp.Symbol.SymbolTypes.ClosedOnLocal;
                        return;
                    }
                }
            }
        }

        public override void Visit(PropertyExpression expression)
        {
            //This code is here to prevent the Visit(Identifier) to be called. Works fine in VS wo/ but not in mono
            base.Visit(expression);
        }
    }
}

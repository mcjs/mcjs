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

namespace MCJavascript
{
    class ClosureVarAnalyzer : DepthFirstVisitor
    {
        JSFunctionImp _currFuncImp;
        public void Execute(JSFunctionImp funcImp)
        {
            var oldFuncImp = _currFuncImp;
            _currFuncImp = funcImp;

            Visit(funcImp.Ast);
            foreach (var f in _currFuncImp.SubFunctions)
                Execute(f);

            if (_currFuncImp.ParentFunction == null) //This is either the program or function declaration
                CreateFieldForClosedVars(_currFuncImp);

            _currFuncImp = oldFuncImp;
            _currFuncImp = funcImp;
        }

        void CreateFieldForClosedVars(JSFunctionImp funcImp)
        {
            foreach (var decl in funcImp.Declarations)
                if (decl.IsClosedOn)
                    funcImp.SetField(decl.Name, new mdr.DVar()); //Only declarations that are closed on will become a filed.
            foreach (var f in funcImp.SubFunctions)
            {
                f.SetType(funcImp.DType);
                CreateFieldForClosedVars(f);
            }
        }

        public override void Visit(Jint.Expressions.Identifier expression)
        {
            if (_currFuncImp.GetDeclaration(expression.Text) != null)
                return; //It is a local var, so nothing to do!
            var funcImp = _currFuncImp.ParentFunction;
            while (funcImp != null)
            {
                var decl = funcImp.GetDeclaration(expression.Text);
                if (decl != null)
                {
                    decl.IsClosedOn = true;
                    return;
                }
            }
        }
        public override void Visit(Jint.Expressions.FunctionExpression expression)
        {
            //Look for identifiers
            Visit(expression.Statement);
        }
        public override void Visit(Jint.Expressions.FunctionDeclarationStatement expression)
        {
            //all global vars are in fact closed on, so we don't care if this func refers to them
        }
        public override void Visit(Jint.Expressions.Program expression)
        {
            //all global vars are in fact closed on
        }

    }
}

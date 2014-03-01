// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
﻿using mjr.Expressions;

namespace mjr.CodeGen
{
    public class JSIntrumentor : mjr.AstWriter
    {
        const string printFuncName = "__mcjs__.PrintVar";
        int tempCounter;
        int funcCounter;
        //const string tempVarName = "mctempvar";

        string TempName
        {
            get { return string.Format("{0}_{1}", JSRuntime.Instance.Configuration.InstJSPrefix, tempCounter++); }
            set { }
        }

        public JSIntrumentor(System.IO.TextWriter outStrem) :
            base(outStrem)
        {
            tempCounter = 0;
            funcCounter = 0;
        }

        public override void Visit(JSFunctionMetadata funcImp)
        {
            outStream.Write("{0}(\"calling {1}\", \"{2}_func{3}\");\n", printFuncName, funcImp.FullName, JSRuntime.Instance.Configuration.InstJSPrefix, funcCounter++);
            outStream.Write("++__mcjs__.PrintIndent;\n");
            base.Visit(funcImp);
            outStream.Write("--__mcjs__.PrintIndent;\n");
        }

        public override void Visit(AssignmentExpression expression)
        {
            outStream.Write(printFuncName + "(");
            expression.Left.Accept(this);            

            switch (expression.AssignmentOperator)
            {
                case AssignmentOperator.Add: outStream.Write(" += "); break;
                case AssignmentOperator.And: outStream.Write(" &= "); break;
                case AssignmentOperator.Assign: outStream.Write(" = "); break;
                case AssignmentOperator.Divide: outStream.Write(" /= "); break;
                case AssignmentOperator.Modulo: outStream.Write(" %= "); break;
                case AssignmentOperator.Multiply: outStream.Write(" *= "); break;
                case AssignmentOperator.Or: outStream.Write(" |= "); break;
                case AssignmentOperator.ShiftLeft: outStream.Write(" <<= "); break;
                case AssignmentOperator.ShiftRight: outStream.Write(" >>= "); break;
                case AssignmentOperator.Substract: outStream.Write(" -= "); break;
                case AssignmentOperator.UnsignedRightShift: outStream.Write(" >>>= "); break;
                case AssignmentOperator.XOr: outStream.Write(" ^= "); break;
            }

            expression.Right.Accept(this);
            outStream.Write(", \"{0}\")", TempName);

            //int currentTempCounter = tempCounter++;
            //outStream.Write("{0}{1} = ", tempVarName, currentTempCounter);
            //expression.Right.Accept(this);
            //outStream.WriteLine(";");

            //outStream.WriteLine("{0}{1};\nprint({0}{1});", tempVarName, currentTempCounter, tempVarName, currentTempCounter);            
        }

        public override void Visit(ReturnStatement expression)
        {
            outStream.Write("--__mcjs__.PrintIndent;\n");
            base.Visit(expression);
        }

        public override void Visit(VariableDeclarationStatement expression)
        {
            if (expression.Expression != null)
            {
                outStream.Write("var {0} = {1}(", expression.Identifier, printFuncName);
                expression.Expression.Accept(this);
                outStream.Write(", \"{0}\")", TempName);
            }
            else
                outStream.Write("var {0}", expression.Identifier);
        }
    }
}

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
using System.IO;
using mjr.Expressions;

namespace mjr
{
    // <summary>
    // Takes a parsed JavaScript AST and serializes it back into a string, adding instrumentation that is useful to compare our speculative parsing
    // implementation to work done by a "normal" JavaScript parser. Could be extended to add arbitrary other instrumentation.
    //
    // Note that for ease of comparing the instrumented output to that of the speculative parse, when DEBUG is defined there is code in the
    // SemanticActions class that will output the same debugging information that the instrumented JavaScript application will output. All
    // that is required is to compare the two sets of debugging information to see what's different.
    // </summary>

    public class AstWriter : IAstVisitor
    {
        private class FunctionInfo
        {
            public JSFunctionMetadata Func { get; set; }
            public bool IsMethod { get; set; }

            public FunctionInfo(JSFunctionMetadata func_, bool isMethod_)
            {
                Func = func_;
                IsMethod = isMethod_;
            }
        }

        //private Stack<FunctionInfo> functionStack;
        protected TextWriter outStream;
        //private int functionCounter;
        //private int methodCounter
        private bool enteringMethodDefinition;

        public AstWriter(TextWriter outStream_)
        {
            outStream = outStream_;

            Reset();
        }

        private void Reset()
        {
            enteringMethodDefinition = false;
        }

        public void Execute(JSFunctionMetadata funcImp)
        {
            Reset();
            // Process AST
            Visit(funcImp);
            // Emit epilogue
            //outStream.WriteLine();
        }

        public void Execute(Statement statement)
        {
            Reset();
            statement.Accept(this);
        }

        public virtual void Visit(JSFunctionMetadata funcImp)
        {
            // Ensure AST is available
            funcImp.Parse();

            // Process AST
            enteringMethodDefinition = false;
            funcImp.AST.Accept(this);
        }

        public virtual void VisitStatements(IEnumerable<Statement> statements)
        {
            foreach (var s in statements)
            {
                s.Accept(this);
                outStream.WriteLine(";");
            }
        }

        #region Implementation of IAstVisitor

        public virtual void Visit(Expressions.Program expression)
        {
            Visit((BlockStatement)expression);
        }
        public void Visit(BlockStatement expression)
        {
            VisitStatements(expression.Statements);
        }

        public virtual void Visit(AssignmentExpression expression)
        {
            //outStream.Write("(");
            expression.Left.Accept(this);
            //outStream.Write(")");

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

            if ((expression.Left is PropertyExpression || expression.Left is Indexer) && (expression.Right is FunctionExpression))
                enteringMethodDefinition = true;

            //outStream.Write("(");
            expression.Right.Accept(this);
            //outStream.Write(")");
        }


        public void Visit(BreakStatement expression)
        {
            if (expression.TargetLabel == null)
                outStream.Write("break");
            else
                outStream.Write("break {0}", expression.TargetLabel.Name);
        }

        public void Visit(ContinueStatement expression)
        {
            if (expression.TargetLabel == null)
                outStream.Write("continue");
            else
                outStream.Write("continue {0}", expression.TargetLabel.Name);
        }

        public void Visit(DoWhileStatement expression)
        {
            outStream.WriteLine("do {");
            expression.Statement.Accept(this);
            outStream.WriteLine();
            outStream.Write("} while (");
            expression.Condition.Accept(this);
            outStream.Write(")");
        }

        public void Visit(EmptyStatement expression)
        {
        }

        public void Visit(ExpressionStatement expression)
        {
            expression.Expression.Accept(this);
        }

        public void Visit(ForEachInStatement expression)
        {
            outStream.Write("for (");
            expression.InitializationStatement.Accept(this);
            outStream.Write(" in ");
            expression.Expression.Accept(this);
            outStream.WriteLine(") {");
            expression.Statement.Accept(this);
            outStream.WriteLine();
            outStream.Write("}");
        }

        public void Visit(ForStatement expression)
        {
            outStream.Write("for (");
            expression.InitialisationStatement.Accept(this);
            outStream.Write(" ; ");
            expression.ConditionExpression.Accept(this);
            outStream.Write(" ; ");
            expression.IncrementExpression.Accept(this);
            outStream.WriteLine(") {");
            expression.Statement.Accept(this);
            outStream.WriteLine();
            outStream.Write("}");
        }

        public void Visit(FunctionDeclarationStatement expression)
        {
            Visit(expression.Expression);
        }

        public void Visit(IfStatement expression)
        {
            outStream.Write("if (");
            expression.Expression.Accept(this);
            outStream.WriteLine(") {");
            expression.Then.Accept(this);

            if (expression.Else != null)
            {
                outStream.WriteLine("} else {");
                expression.Else.Accept(this);
            }

            outStream.Write("}");
        }

        public virtual void Visit(ReturnStatement expression)
        {
            if (expression.Expression != null)
            {
                outStream.Write("return ");
                expression.Expression.Accept(this);
            }
            else
                outStream.Write("return");
        }

        public void Visit(SwitchStatement expression)
        {
            outStream.Write("switch (");
            expression.Expression.Accept(this);
            outStream.WriteLine(") {");

            foreach (var cc in expression.CaseClauses)
            {
                if (cc.IsDefault)
                    outStream.WriteLine("default:");
                else
                {
                    outStream.Write("case ");
                    cc.Expression.Accept(this);
                    outStream.WriteLine(":");
                }

                VisitStatements(cc.Statements);
            }

            outStream.Write("}");
        }

        public void Visit(WithStatement expression)
        {
            outStream.Write("with (");
            expression.Expression.Accept(this);
            outStream.WriteLine(") {");
            expression.Statement.Accept(this);
            outStream.Write("}");
        }

        public void Visit(ThrowStatement expression)
        {
            outStream.Write("throw ");
            expression.Expression.Accept(this);
        }

        public void Visit(TryStatement expression)
        {
            outStream.WriteLine("try {");
            expression.Statement.Accept(this);            
            if (expression.Catch != null)
            {
                outStream.WriteLine("}} catch ({0}) {{", expression.Catch.Identifier);
                expression.Catch.Statement.Accept(this);
            }

            if (expression.Finally != null)
            {
                outStream.WriteLine("} finally {");
                expression.Finally.Statement.Accept(this);
            }

            outStream.WriteLine("}");
        }

        public virtual void Visit(VariableDeclarationStatement expression)
        {
            if (expression.Expression != null)
            {
                outStream.Write("var {0} = ", expression.Identifier);
                expression.Expression.Accept(this);
            }
            else
                outStream.Write("var {0}", expression.Identifier);
        }

        public void Visit(WhileStatement expression)
        {
            outStream.Write("while (");
            expression.Condition.Accept(this);
            outStream.WriteLine(") {");
            expression.Statement.Accept(this);
            outStream.Write("}");
        }

        public void Visit(ArrayDeclaration expression)
        {
            outStream.Write("[");

            foreach (var p in expression.Parameters)
            {
                p.Accept(this);
                outStream.Write(", ");
            }

            outStream.Write("]");
        }

        public void Visit(CommaOperatorStatement expression)
        {
            if (expression.Statements.Count > 0)
            {
                if (expression.Statements.First() is VariableDeclarationStatement)
                {
                    // This is a compound declaration
                    var vds = (VariableDeclarationStatement)expression.Statements.First();

                    if (vds.Expression != null)
                    {
                        outStream.Write("var {0} = (", vds.Identifier);
                        vds.Expression.Accept(this);
                        outStream.Write(")");
                    }
                    else
                        outStream.Write("var {0}", vds.Identifier);

                    foreach (var p in expression.Statements.Skip(1))
                    {
                        var nextVDS = (VariableDeclarationStatement)p;

                        if (nextVDS.Expression != null)
                        {
                            outStream.Write(", {0} = (", nextVDS.Identifier);
                            nextVDS.Expression.Accept(this);
                            outStream.Write(")");
                        }
                        else
                            outStream.Write(", {0}", nextVDS.Identifier);
                    }
                }
                else
                {
                    // Any other kind of CommaOperatorStatement
                    expression.Statements.First().Accept(this);

                    foreach (var p in expression.Statements.Skip(1))
                    {
                        outStream.Write(", ");
                        p.Accept(this);
                    }
                }
            }
        }

        public void Visit(FunctionExpression expression)
        {
            if (expression.Name == null || !expression.Metadata.HasName)
                outStream.Write("function (");
            else
                outStream.Write("function {0} (", expression.Name);

            if (expression.Parameters != null && expression.Parameters.Count > 0)
            {
                outStream.Write(expression.Parameters.First());

                foreach (var p in expression.Parameters.Skip(1))
                {
                    outStream.Write(", ");
                    outStream.Write(p);
                }
            }

            outStream.WriteLine(") {");
            Visit(expression.Metadata);
            outStream.Write("}");
        }

        public void Visit(MethodCall expression)
        {
            if (expression.Function is FunctionExpression)
            {
                outStream.Write("(");
                expression.Function.Accept(this);
                outStream.Write(")");
                outStream.Write("(");
            }
            else
            {
                expression.Function.Accept(this);
                outStream.Write("(");
            }

            if (expression.Arguments.Count > 0)
            {
                expression.Arguments.First().Accept(this);

                foreach (var a in expression.Arguments.Skip(1))
                {
                    outStream.Write(", ");
                    a.Accept(this);
                }
            }

            outStream.Write(")");
        }

        public void Visit(Indexer expression)
        {
            //outStream.Write("(");
            expression.Container.Accept(this);
            //outStream.Write(")[");
            outStream.Write("[");
            expression.Index.Accept(this);
            outStream.Write("]");
        }

        public void Visit(PropertyExpression expression)
        {
            //outStream.Write("(");
            expression.Container.Accept(this);
            //outStream.Write(").{0}", expression.Text);
            outStream.Write(".{0}", expression.Text);
        }

        public void Visit(PropertyDeclarationExpression expression)
        {
            outStream.Write("\"{0}\": ", EscapeString(expression.Name));
            switch (expression.Mode)
            {
                case PropertyExpressionType.Data:
                    if (expression.Expression is FunctionExpression)
                        enteringMethodDefinition = true;
                    expression.Expression.Accept(this);
                    break;
                case PropertyExpressionType.Get:
                    enteringMethodDefinition = true;
                    Visit(expression.GetFunction);
                    break;
                case PropertyExpressionType.Set:
                    enteringMethodDefinition = true;
                    Visit(expression.SetFunction);
                    break;
            }
        }

        public void Visit(Identifier expression)
        {
            outStream.Write(expression.Text);
        }

        public void Visit(JsonExpression expression)
        {
            outStream.WriteLine("{");

            foreach (var v in expression.Values.Values)
            {
                v.Accept(this);
                outStream.WriteLine(",");
            }

            outStream.Write("}");
        }

        public void Visit(NewExpression expression)
        {
            outStream.Write("new ");
            expression.Function.Accept(this);
            outStream.Write("(");

            if (expression.Arguments.Count > 0)
            {
                expression.Arguments.First().Accept(this);

                foreach (var a in expression.Arguments.Skip(1))
                {
                    outStream.Write(", ");
                    a.Accept(this);
                }
            }

            outStream.Write(")");
        }

        public void Visit(BinaryExpression expression)
        {
            outStream.Write("(");
            expression.LeftExpression.Accept(this);
            outStream.Write(")");

            switch (expression.Type)
            {
                case BinaryExpressionType.And: outStream.Write(" && "); break;
                case BinaryExpressionType.BitwiseAnd: outStream.Write(" & "); break;
                case BinaryExpressionType.BitwiseOr: outStream.Write(" | "); break;
                case BinaryExpressionType.BitwiseXOr: outStream.Write(" ^ "); break;
                case BinaryExpressionType.Div: outStream.Write(" / "); break;
                case BinaryExpressionType.Equal: outStream.Write(" == "); break;
                case BinaryExpressionType.Greater: outStream.Write(" > "); break;
                case BinaryExpressionType.GreaterOrEqual: outStream.Write(" >= "); break;
                case BinaryExpressionType.In: outStream.Write(" in "); break;
                case BinaryExpressionType.InstanceOf: outStream.Write(" instanceof "); break;
                case BinaryExpressionType.LeftShift: outStream.Write(" << "); break;
                case BinaryExpressionType.Lesser: outStream.Write(" < "); break;
                case BinaryExpressionType.LesserOrEqual: outStream.Write(" <= "); break;
                case BinaryExpressionType.Minus: outStream.Write(" - "); break;
                case BinaryExpressionType.Modulo: outStream.Write(" % "); break;
                case BinaryExpressionType.NotEqual: outStream.Write(" != "); break;
                case BinaryExpressionType.NotSame: outStream.Write(" !== "); break;
                case BinaryExpressionType.Or: outStream.Write(" || "); break;
                case BinaryExpressionType.Plus: outStream.Write(" + "); break;
                case BinaryExpressionType.RightShift: outStream.Write(" >> "); break;
                case BinaryExpressionType.Same: outStream.Write(" === "); break;
                case BinaryExpressionType.Times: outStream.Write(" * "); break;
                case BinaryExpressionType.UnsignedRightShift: outStream.Write(" >>> "); break;
            }

            outStream.Write("(");
            expression.RightExpression.Accept(this);
            outStream.Write(")");
        }

        public void Visit(TernaryExpression expression)
        {
            outStream.Write("(");
            expression.LeftExpression.Accept(this);
            outStream.Write(") ? (");
            expression.MiddleExpression.Accept(this);
            outStream.Write(") : (");
            expression.RightExpression.Accept(this);
            outStream.Write(")");
        }

        public void Visit(UnaryExpression expression)
        {
            switch (expression.Type)
            {
                case UnaryExpressionType.Delete: outStream.Write("delete ("); expression.Expression.Accept(this); outStream.Write(")"); break;
                case UnaryExpressionType.BitwiseNot: outStream.Write("~("); expression.Expression.Accept(this); outStream.Write(")"); break;
                case UnaryExpressionType.Negate: outStream.Write("-("); expression.Expression.Accept(this); outStream.Write(")"); break;
                case UnaryExpressionType.LogicalNot: outStream.Write("!("); expression.Expression.Accept(this); outStream.Write(")"); break;
                case UnaryExpressionType.Positive: outStream.Write("+("); expression.Expression.Accept(this); outStream.Write(")"); break;
                case UnaryExpressionType.PostfixMinusMinus: outStream.Write("("); expression.Expression.Accept(this); outStream.Write(")--"); break;
                case UnaryExpressionType.PostfixPlusPlus: outStream.Write("("); expression.Expression.Accept(this); outStream.Write(")++"); break;
                case UnaryExpressionType.PrefixMinusMinus: outStream.Write("--("); expression.Expression.Accept(this); outStream.Write(")"); break;
                case UnaryExpressionType.PrefixPlusPlus: outStream.Write("++("); expression.Expression.Accept(this); outStream.Write(")"); break;
                case UnaryExpressionType.TypeOf: outStream.Write("typeof ("); expression.Expression.Accept(this); outStream.Write(")"); break;
                case UnaryExpressionType.Void: outStream.Write("void ("); expression.Expression.Accept(this); outStream.Write(")"); break;
            }
        }

        public void Visit(GotoStatement expression)
        {
            outStream.WriteLine("goto {0}", expression.TargetLabel.Name);
        }

        public void Visit(LabelStatement expression)
        {
            if (!string.IsNullOrEmpty(expression.Name))
                outStream.Write("{0}: ", expression.Name);
            Visit(expression.Target);
        }

        public void Visit(ValueExpression expression)
        {
            switch (expression.TypeCode)
            {
                case TypeCode.Boolean: outStream.Write(FormatBool((Boolean)expression.Value)); break;
                case TypeCode.Double: outStream.Write((Double)expression.Value); break;
                case TypeCode.String: outStream.Write("\"{0}\"", EscapeString((string)expression.Value)); break;
            }
        }

        public void Visit(RegexpExpression expression)
        {
            outStream.Write("/{0}/{1}", expression.Regexp, expression.Options);
        }

        public void Visit(Statement expression)
        {
            if (expression != null)
                expression.Accept(this);
        }

        #endregion

        private static string EscapeString(string s)
        {
            s = s.Replace(@"\", @"\\");
            s = s.Replace(@"""", @"\""");
            s = s.Replace("\n", @"\n");
            s = s.Replace("\r", @"\r");
            s = s.Replace("\t", @"\t");
            s = s.Replace("\b", @"\b");
            s = s.Replace("\f", @"\f");
            s = s.Replace("\v", @"\v");
            return s;
        }

        private static string FormatBool(bool b)
        {
            return (b ? "true" : "false");
        }
    }
}

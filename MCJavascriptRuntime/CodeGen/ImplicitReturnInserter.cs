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

using mjr.IR;
using m.Util.Diagnose;

namespace mjr.CodeGen
{
  class ImplicitReturnInserter
  {
    static AlgorithmImplementation _pool = new AlgorithmPool<ImplicitReturnInserterImp>("JS/RetInserter/", () => new ImplicitReturnInserterImp(), () => JSRuntime.Instance.Configuration.ProfileJitTime);
    public static void Execute(JSFunctionMetadata funcMetadata) { _pool.Execute(funcMetadata); }

    sealed class ImplicitReturnInserterImp : INodeVisitor, AlgorithmImplementation
    {
      JSFunctionMetadata _currFuncMetadata;
      JSSymbol _implicitReturn;

      BlockStatement _currBlock;
      int _currStatementIndex;
      ExpressionStatement _currExpressionStatement;

      public void Execute(CodeGenerationInfo cgInfo) { throw new NotImplementedException(); }
      public void Execute(JSFunctionMetadata funcMetadata)
      {
        if (funcMetadata.IsAnalyzed)
          return;

        if (!funcMetadata.Scope.IsEvalFunction)
          return; //This is not the body in an eval()
        _currFuncMetadata = funcMetadata;
        _implicitReturn = _currFuncMetadata.Scope.GetOrAddSymbol("_implicit_eval_return");
        _implicitReturn.SymbolType = JSSymbol.SymbolTypes.HiddenLocal;

        _currBlock = null;
        _currStatementIndex = -1;
        _currExpressionStatement = null;

        VisitNode(_currFuncMetadata.FunctionIR.Statement);

        _currFuncMetadata.FunctionIR.Statement.Statements.Add(
            new ReturnStatement(
                new ReadIdentifierExpression(_implicitReturn)
            )
        );
      }

      private void TerminateCurrBlock()
      {
        _currStatementIndex = -1;
      }

      private void AssignToImplicitReturn(Expression expression)
      {
        Debug.Assert(expression == _currExpressionStatement.Expression, "Invalid situation!");
        expression.RemoveUser(_currExpressionStatement); //to avoid user complications!
        var assign = new WriteIdentifierExpression(_implicitReturn, expression);
        //_currExpressionStatement.Expression = assign;
        _currExpressionStatement.Replace(_currExpressionStatement.Expression, assign);
        TerminateCurrBlock();
      }

      #region Statements; ECMA 12. -------------------------------------------------------------------------------------
      public override void Visit(BlockStatement node)
      {
        var prevCurrBlock = _currBlock;
        var prevCurrStatement = _currStatementIndex;

        _currBlock = node;
        for (_currStatementIndex = node.Statements.Count - 1; _currStatementIndex >= 0; --_currStatementIndex)
        {
          VisitNode(node.Statements[_currStatementIndex]);
        }

        _currStatementIndex = prevCurrStatement;
        _currBlock = prevCurrBlock;
      }

      public override void Visit(VariableDeclarationStatement node) { }
      public override void Visit(VariableDeclaration node) { }
      public override void Visit(EmptyStatement node) { }
      public override void Visit(ExpressionStatement node)
      {
        Debug.Assert(_currExpressionStatement == null, "Invalid situation");
        _currExpressionStatement = node;
        VisitNode(node.Expression);
        _currExpressionStatement = null;
      }
      public override void Visit(IfStatement node)
      {
        VisitNode(node.Then);
        VisitNode(node.Else);
      }
      public override void Visit(DoWhileStatement node) { VisitNode(node.Body); }
      public override void Visit(WhileStatement node) { VisitNode(node.Body); }
      public override void Visit(ForStatement node) { VisitNode(node.Body); }
      public override void Visit(ForEachInStatement node) { VisitNode(node.Body); }
      public override void Visit(LabelStatement node) { VisitNode(node.Target); }
      public override void Visit(GotoStatement node) { }
      public override void Visit(ContinueStatement node) { }
      public override void Visit(BreakStatement node) { }
      public override void Visit(ReturnStatement node) { }
      public override void Visit(WithStatement node) { throw new NotImplementedException(); }
      public override void Visit(SwitchStatement node) { VisitNodes(node.CaseClauses); }
      public override void Visit(CaseClause node) { VisitNode(node.Body); }
      public override void Visit(ThrowStatement node) { }
      public override void Visit(TryStatement node)
      {
        VisitNode(node.Statement);
        VisitNode(node.Catch);
        VisitNode(node.Finally);
      }
      public override void Visit(CatchClause node) { VisitNode(node.Statement); }
      public override void Visit(FinallyClause node) { VisitNode(node.Statement); }
      #endregion

      #region Primary Expressions; ECMA 11.1 -------------------------------------------------------------------------------------
      public override void Visit(ThisLiteral node) { AssignToImplicitReturn(node); }
      public override void Visit(NullLiteral node) { AssignToImplicitReturn(node); }
      public override void Visit(BooleanLiteral node) { AssignToImplicitReturn(node); }
      public override void Visit(IntLiteral node) { AssignToImplicitReturn(node); }
      public override void Visit(DoubleLiteral node) { AssignToImplicitReturn(node); }
      public override void Visit(StringLiteral node) { AssignToImplicitReturn(node); }
      public override void Visit(RegexpLiteral node) { AssignToImplicitReturn(node); }

      public override void Visit(ArrayLiteral node) { AssignToImplicitReturn(node); }
      public override void Visit(ObjectLiteral node) { AssignToImplicitReturn(node); }
      public override void Visit(PropertyAssignment node) { throw new InvalidOperationException(); }

      public override void Visit(ParenExpression node) { AssignToImplicitReturn(node); }
      public override void Visit(GuardedCast node) { AssignToImplicitReturn(node); }
      public override void Visit(InlinedInvocation node) { Trace.Fail("Not yet supported"); }

      public override void Visit(ReadIdentifierExpression node) { AssignToImplicitReturn(node); }
      public override void Visit(ReadIndexerExpression node) { AssignToImplicitReturn(node); }
      public override void Visit(ReadPropertyExpression node) { AssignToImplicitReturn(node); }
      #endregion

      #region Type Conversions; ECMA 9 -------------------------------------------------------------------------------------
      public override void Visit(ToPrimitive node) { AssignToImplicitReturn(node); }
      public override void Visit(ToBoolean node) { AssignToImplicitReturn(node); }
      public override void Visit(ToNumber node) { AssignToImplicitReturn(node); }
      public override void Visit(ToDouble node) { AssignToImplicitReturn(node); }
      public override void Visit(ToInteger node) { AssignToImplicitReturn(node); }
      public override void Visit(ToInt32 node) { AssignToImplicitReturn(node); }
      public override void Visit(ToUInt32 node) { AssignToImplicitReturn(node); }
      public override void Visit(ToUInt16 node) { AssignToImplicitReturn(node); }
      public override void Visit(ToString node) { AssignToImplicitReturn(node); }
      public override void Visit(ToObject node) { AssignToImplicitReturn(node); }
      public override void Visit(ToFunction node) { AssignToImplicitReturn(node); }
      #endregion

      #region Unary Operators; ECMA 11.4 -------------------------------------------------------------------------------------
      public override void Visit(DeleteExpression node) { AssignToImplicitReturn(node); }
      public override void Visit(VoidExpression node) { AssignToImplicitReturn(node); }
      public override void Visit(TypeofExpression node) { AssignToImplicitReturn(node); }
      public override void Visit(PositiveExpression node) { AssignToImplicitReturn(node); }
      public override void Visit(NegativeExpression node) { AssignToImplicitReturn(node); }
      public override void Visit(BitwiseNotExpression node) { AssignToImplicitReturn(node); }
      public override void Visit(LogicalNotExpression node) { AssignToImplicitReturn(node); }
      #endregion

      #region Binary Multiplicative Operators; ECMA 11.5 -------------------------------------------------------------------------------------
      public override void Visit(MultiplyExpression node) { AssignToImplicitReturn(node); }
      public override void Visit(DivideExpression node) { AssignToImplicitReturn(node); }
      public override void Visit(RemainderExpression node) { AssignToImplicitReturn(node); }
      #endregion
      #region Binary Additive Operators; ECMA 11.6 -------------------------------------------------------------------------------------
      public override void Visit(AdditionExpression node) { AssignToImplicitReturn(node); }
      public override void Visit(SubtractionExpression node) { AssignToImplicitReturn(node); }
      #endregion
      #region Binary Bitwise Shift Operators; ECMA 11.7 -------------------------------------------------------------------------------------
      public override void Visit(LeftShiftExpression node) { AssignToImplicitReturn(node); }
      public override void Visit(RightShiftExpression node) { AssignToImplicitReturn(node); }
      public override void Visit(UnsignedRightShiftExpression node) { AssignToImplicitReturn(node); }
      #endregion
      #region Binary Relational Operators; ECMA 11.8 -------------------------------------------------------------------------------------
      public override void Visit(LesserExpression node) { AssignToImplicitReturn(node); }
      public override void Visit(GreaterExpression node) { AssignToImplicitReturn(node); }
      public override void Visit(LesserOrEqualExpression node) { AssignToImplicitReturn(node); }
      public override void Visit(GreaterOrEqualExpression node) { AssignToImplicitReturn(node); }
      public override void Visit(InstanceOfExpression node) { AssignToImplicitReturn(node); }
      public override void Visit(InExpression node) { AssignToImplicitReturn(node); }
      #endregion
      #region Binary Equality Operators; ECMA 11.9 -------------------------------------------------------------------------------------
      public override void Visit(EqualExpression node) { AssignToImplicitReturn(node); }
      public override void Visit(NotEqualExpression node) { AssignToImplicitReturn(node); }
      public override void Visit(SameExpression node) { AssignToImplicitReturn(node); }
      public override void Visit(NotSameExpression node) { AssignToImplicitReturn(node); }
      #endregion
      #region Binary Bitwise Operators; ECMA 11.10 -------------------------------------------------------------------------------------
      public override void Visit(BitwiseAndExpression node) { AssignToImplicitReturn(node); }
      public override void Visit(BitwiseOrExpression node) { AssignToImplicitReturn(node); }
      public override void Visit(BitwiseXorExpression node) { AssignToImplicitReturn(node); }
      #endregion
      #region Binary Logical Operators; ECMA 11.11 -------------------------------------------------------------------------------------
      public override void Visit(LogicalAndExpression node) { VisitNode(node.Implementation); }
      public override void Visit(LogicalOrExpression node) { VisitNode(node.Implementation); }
      #endregion

      #region Conditional Operators; ECMA 11.12 -------------------------------------------------------------------------------------
      public override void Visit(TernaryExpression node) { AssignToImplicitReturn(node); }
      #endregion

      #region Assignment; ECMA 11.13 -------------------------------------------------------------------------------------
      public override void Visit(WriteTemporaryExpression node) { AssignToImplicitReturn(node); }
      public override void Visit(WriteIdentifierExpression node) { AssignToImplicitReturn(node); }
      public override void Visit(WriteIndexerExpression node) { AssignToImplicitReturn(node); }
      public override void Visit(WritePropertyExpression node) { AssignToImplicitReturn(node); }
      #endregion

      #region Comma Operator; ECMA 11.14 -------------------------------------------------------------------------------------
      public override void Visit(CommaOperatorExpression node) { AssignToImplicitReturn(node); }
      #endregion

      #region Function Calls; ECMA 11.2.2, 11.2.3 -------------------------------------------------------------------------------------
      public override void Visit(NewExpression node) { AssignToImplicitReturn(node); }
      public override void Visit(CallExpression node) { AssignToImplicitReturn(node); }
      #endregion

      #region Function Definition; ECMA 13 -------------------------------------------------------------------------------------
      public override void Visit(FunctionExpression node) { AssignToImplicitReturn(node); }
      public override void Visit(FunctionDeclarationStatement node) { throw new InvalidOperationException(); }
      #endregion

      #region Program; ECMA 14 -------------------------------------------------------------------------------------
      public override void Visit(Program node) { }
      #endregion

      #region Interanls
      public override void Visit(InternalCall node) { throw new InvalidOperationException(); }
      public override void Visit(InternalNew node) { throw new InvalidOperationException(); }
      #endregion
    }
  }
}

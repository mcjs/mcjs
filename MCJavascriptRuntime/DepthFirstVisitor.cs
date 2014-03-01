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

namespace mjr
{
  public abstract class DepthFirstVisitor : INodeVisitor
  {
    #region abstract classes
    protected override void Visit(Node node) { }
    protected override void Visit(Statement node) { Visit((Node)node); }
    protected override void Visit(LoopStatement node) { Visit((Statement)node); }
    protected override void Visit(Expression node) { Visit((Node)node); }
    protected override void Visit(Literal node) { Visit((Expression)node); }
    protected override void Visit(Reference node) { Visit((Expression)node); }
    protected override void Visit(Identifier node) { Visit((Reference)node); }
    protected override void Visit(Indexer node)
    {
      VisitNode(node.Container);
      VisitNode(node.Index);
      Visit((Reference)node);
    }
    protected override void Visit(UnaryExpression node)
    {
      VisitNode(node.Expression);
      Visit((Expression)node);
    }
    protected override void Visit(ConversionExpression node) { Visit((UnaryExpression)node); }
    protected override void Visit(BinaryExpression node)
    {
      VisitNode(node.Left);
      VisitNode(node.Right);
      Visit((Expression)node);
    }
    protected override void Visit(Invocation node)
    {
      VisitNode(node.Function);
      VisitNodes(node.Arguments);
      Visit((Expression)node);
    }
    protected override void Visit(InternalExpression node) { Visit((Expression)node); }
    protected override void Visit(InternalInvocation node)
    {
      VisitNodes(node.Arguments);
      Visit((InternalExpression)node);
    }
    #endregion

    #region Statements; ECMA 12. -------------------------------------------------------------------------------------
    public override void Visit(BlockStatement node)
    {
      VisitNodes(node.Statements);
      Visit((Statement)node);
    }

    public override void Visit(VariableDeclarationStatement node)
    {
      VisitNodes(node.Declarations);
      Visit((Statement)node);
    }

    public override void Visit(VariableDeclaration node)
    {
      VisitNode(node.Initialization);
      Visit((Node)node);
    }

    public override void Visit(EmptyStatement node)
    {
      Visit((Statement)node);
    }

    public override void Visit(ExpressionStatement node)
    {
      VisitNode(node.Expression);
      Visit((Statement)node);
    }

    public override void Visit(IfStatement node)
    {
      VisitNode(node.Condition);
      VisitNode(node.Then);
      VisitNode(node.Else);
      Visit((Statement)node);
    }

    public override void Visit(DoWhileStatement node)
    {
      VisitNode(node.Body);
      VisitNode(node.Condition);
      Visit((LoopStatement)node);
    }

    public override void Visit(WhileStatement node)
    {
      VisitNode(node.Condition);
      VisitNode(node.Body);
      Visit((LoopStatement)node);
    }

    public override void Visit(ForStatement node)
    {
      VisitNode(node.Initialization);
      VisitNode(node.Condition);
      VisitNode(node.Increment);
      VisitNode(node.Body);
      Visit((LoopStatement)node);
    }

    public override void Visit(ForEachInStatement node)
    {
      VisitNode(node.Initialization);
      VisitNode(node.Expression);
      VisitNode(node.Body);
      Visit((LoopStatement)node);
    }

    public override void Visit(LabelStatement node)
    {
      //Trace.Fail("Visitor for node type {0} is not implemented", node.GetType());
      VisitNode(node.Target);
      Visit((Statement)node);
    }

    public override void Visit(GotoStatement node)
    {
      Visit((Statement)node);
    }

    public override void Visit(ContinueStatement node)
    {
      Visit((GotoStatement)node);
    }

    public override void Visit(BreakStatement node)
    {
      Visit((GotoStatement)node);
    }

    public override void Visit(ReturnStatement node)
    {
      VisitNode(node.Expression);
      Visit((Statement)node);
    }

    public override void Visit(WithStatement node)
    {
      Trace.Fail("Visitor for node type {0} is not implemented", node.GetType());
      Visit((Statement)node);
    }

    public override void Visit(SwitchStatement node)
    {
      VisitNode(node.Expression);
      VisitNodes(node.CaseClauses);
      Visit((Statement)node);
    }
    public override void Visit(CaseClause node)
    {
      VisitNode(node.Comparison);
      VisitNode(node.Expression);
      VisitNode(node.Body);
      Visit((Node)node);
    }
    public override void Visit(ThrowStatement node)
    {
      VisitNode(node.Expression);
      Visit((Statement)node);
    }

    public override void Visit(TryStatement node)
    {
      VisitNode(node.Statement);
      VisitNode(node.Catch);
      VisitNode(node.Finally);
      Visit((Statement)node);
    }
    public override void Visit(CatchClause node)
    {
      VisitNode(node.Statement);
      Visit((Node)node);
    }
    public override void Visit(FinallyClause node)
    {
      VisitNode(node.Statement);
      Visit((Node)node);
    }

    #endregion

    #region Primary Expressions; ECMA 11.1 -------------------------------------------------------------------------------------

    public override void Visit(ThisLiteral node) { Visit((Literal)node); }

    public override void Visit(NullLiteral node) { Visit((Literal)node); }

    public override void Visit(BooleanLiteral node) { Visit((Literal)node); }

    public override void Visit(IntLiteral node) { Visit((Literal)node); }

    public override void Visit(DoubleLiteral node) { Visit((Literal)node); }

    public override void Visit(StringLiteral node) { Visit((Literal)node); }

    public override void Visit(RegexpLiteral node) { Visit((Literal)node); }

    public override void Visit(ArrayLiteral node)
    {
      VisitNodes(node.Items);
      Visit((Literal)node);
    }

    public override void Visit(ObjectLiteral node)
    {
      VisitNodes(node.Properties);
      Visit((Literal)node);
    }
    public override void Visit(PropertyAssignment node)
    {
      VisitNode(node.Expression);
      Visit((Node)node);
    }

    public override void Visit(ParenExpression node)
    {
      VisitNode(node.Expression);
      Visit((Expression)node);
    }

    public override void Visit(GuardedCast node)
    {
      Visit((ParenExpression)node);
    }

    public override void Visit(InlinedInvocation node)
    {
      VisitNode(node.Statement);
      //VisitNode(node.ReturnedValue);
      Visit((ParenExpression)node);
    }


    public override void Visit(ReadIdentifierExpression node)
    {
      Visit((Identifier)node);
    }

    public override void Visit(ReadIndexerExpression node) { Visit((Indexer)node); }

    public override void Visit(ReadPropertyExpression node) { Visit((ReadIndexerExpression)node); }

    #endregion

    #region Type Conversions; ECMA 9 -------------------------------------------------------------------------------------
    public override void Visit(ToPrimitive node) { Visit((ConversionExpression)node); }
    public override void Visit(ToBoolean node) { Visit((ConversionExpression)node); }
    public override void Visit(ToNumber node) { Visit((ConversionExpression)node); }
    public override void Visit(ToDouble node) { Visit((ConversionExpression)node); }
    public override void Visit(ToInteger node) { Visit((ConversionExpression)node); }
    public override void Visit(ToInt32 node) { Visit((ConversionExpression)node); }
    public override void Visit(ToUInt32 node) { Visit((ConversionExpression)node); }
    public override void Visit(ToUInt16 node) { Visit((ConversionExpression)node); }
    public override void Visit(ToString node) { Visit((ConversionExpression)node); }
    public override void Visit(ToObject node) { Visit((ConversionExpression)node); }
    public override void Visit(ToFunction node) { Visit((ConversionExpression)node); }
    #endregion

    #region Unary Operators; ECMA 11.4 -------------------------------------------------------------------------------------
    public override void Visit(DeleteExpression node) { Visit((UnaryExpression)node); }
    public override void Visit(VoidExpression node) { Visit((UnaryExpression)node); }
    public override void Visit(TypeofExpression node) { Visit((UnaryExpression)node); }
    public override void Visit(PositiveExpression node) { Visit((UnaryExpression)node); }
    public override void Visit(NegativeExpression node) { Visit((UnaryExpression)node); }
    public override void Visit(BitwiseNotExpression node) { Visit((UnaryExpression)node); }
    public override void Visit(LogicalNotExpression node) { Visit((UnaryExpression)node); }
    #endregion

    #region Binary Multiplicative Operators; ECMA 11.5 -------------------------------------------------------------------------------------
    public override void Visit(MultiplyExpression node) { Visit((BinaryExpression)node); }
    public override void Visit(DivideExpression node) { Visit((BinaryExpression)node); }
    public override void Visit(RemainderExpression node) { Visit((BinaryExpression)node); }
    #endregion
    #region Binary Additive Operators; ECMA 11.6 -------------------------------------------------------------------------------------
    public override void Visit(AdditionExpression node) { Visit((BinaryExpression)node); }
    public override void Visit(SubtractionExpression node) { Visit((BinaryExpression)node); }
    #endregion
    #region Binary Bitwise Shift Operators; ECMA 11.7 -------------------------------------------------------------------------------------
    public override void Visit(LeftShiftExpression node) { Visit((BinaryExpression)node); }
    public override void Visit(RightShiftExpression node) { Visit((BinaryExpression)node); }
    public override void Visit(UnsignedRightShiftExpression node) { Visit((BinaryExpression)node); }
    #endregion
    #region Binary Relational Operators; ECMA 11.8 -------------------------------------------------------------------------------------
    public override void Visit(LesserExpression node) { Visit((BinaryExpression)node); }
    public override void Visit(GreaterExpression node) { Visit((BinaryExpression)node); }
    public override void Visit(LesserOrEqualExpression node) { Visit((BinaryExpression)node); }
    public override void Visit(GreaterOrEqualExpression node) { Visit((BinaryExpression)node); }
    public override void Visit(InstanceOfExpression node) { Visit((BinaryExpression)node); }
    public override void Visit(InExpression node) { Visit((BinaryExpression)node); }
    #endregion
    #region Binary Equality Operators; ECMA 11.9 -------------------------------------------------------------------------------------
    public override void Visit(EqualExpression node) { Visit((BinaryExpression)node); }
    public override void Visit(NotEqualExpression node) { Visit((BinaryExpression)node); }
    public override void Visit(SameExpression node) { Visit((BinaryExpression)node); }
    public override void Visit(NotSameExpression node) { Visit((BinaryExpression)node); }
    #endregion
    #region Binary Bitwise Operators; ECMA 11.10 -------------------------------------------------------------------------------------
    public override void Visit(BitwiseAndExpression node) { Visit((BinaryExpression)node); }
    public override void Visit(BitwiseOrExpression node) { Visit((BinaryExpression)node); }
    public override void Visit(BitwiseXorExpression node) { Visit((BinaryExpression)node); }
    #endregion
    #region Binary Logical Operators; ECMA 11.11 -------------------------------------------------------------------------------------
    public override void Visit(LogicalAndExpression node) { Visit((BinaryExpression)node); }
    public override void Visit(LogicalOrExpression node) { Visit((BinaryExpression)node); }
    #endregion

    #region Conditional Operators; ECMA 11.12 -------------------------------------------------------------------------------------
    public override void Visit(TernaryExpression node)
    {
      VisitNode(node.Left);
      VisitNode(node.Middle);
      VisitNode(node.Right);
      Visit((Expression)node);
    }
    #endregion

    #region Assignment; ECMA 11.13 -------------------------------------------------------------------------------------
    public override void Visit(WriteTemporaryExpression node)
    {
      Visit((Reference)node);
      VisitNode(node.Value);
    }
    public override void Visit(WriteIdentifierExpression node)
    {
      Visit((Identifier)node);
      VisitNode(node.Value);
    }
    public override void Visit(WriteIndexerExpression node)
    {
      Visit((Indexer)node);
      VisitNode(node.Value);
    }
    public override void Visit(WritePropertyExpression node) { Visit((WriteIndexerExpression)node); }
    #endregion

    #region Comma Operator; ECMA 11.14 -------------------------------------------------------------------------------------
    public override void Visit(CommaOperatorExpression node)
    {
      VisitNodes(node.Expressions);
      Visit((Expression)node);
    }
    #endregion

    #region Function Calls; ECMA 11.2.2, 11.2.3 -------------------------------------------------------------------------------------
    public override void Visit(NewExpression node) { Visit((Invocation)node); }
    public override void Visit(CallExpression node)
    {
      if (node.InlinedIR != null)
        VisitNode(node.InlinedIR);
      else
        Visit((Invocation)node);
    }
    #endregion

    #region Function Definition; ECMA 13 -------------------------------------------------------------------------------------
    public override void Visit(FunctionExpression node) { Visit((Expression)node); }
    public override void Visit(FunctionDeclarationStatement node)
    {
      VisitNode(node.Implementation);
      Visit((Statement)node);
    }
    #endregion

    #region Program; ECMA 14 -------------------------------------------------------------------------------------
    public override void Visit(Program node)
    {
      throw new InvalidProgramException("Program node should be visited! How did you get here?!");
    }
    #endregion


    #region Interanls
    public override void Visit(InternalCall node) { Visit((InternalInvocation)node); }
    public override void Visit(InternalNew node) { Visit((InternalInvocation)node); }
    #endregion

  }
}

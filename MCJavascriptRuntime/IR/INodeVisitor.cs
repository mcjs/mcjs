// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
using System.Collections.Generic;

namespace mjr.IR
{
  /// <summary>
  /// We use virtual methods instead of interface since it is going to be slightly faster
  /// </summary>
  public abstract class INodeVisitor
  {
    #region Helper methods -------------------------------------------------------------------------------------
    [System.Diagnostics.DebuggerStepThrough]
    protected void VisitNode(Node node)
    {
      if (node != null)
        node.Accept(this);
    }

    [System.Diagnostics.DebuggerStepThrough]
    protected void VisitNodes<T>(IEnumerable<T> collection) where T : Node
    {
      if (collection != null)
        foreach (var n in collection)
          n.Accept(this);
    }

    //The following is slightly faster since it is not using foreach which creates objects
    [System.Diagnostics.DebuggerStepThrough]
    protected void VisitNodes<T>(List<T> collection) where T : Node
    {
      if (collection != null)
        for (var i = 0; i < collection.Count; ++i)
          collection[i].Accept(this);
    }

    #endregion

    #region abstract classes
    protected virtual void Visit(Node node) { }
    protected virtual void Visit(Statement node) { }
    protected virtual void Visit(LoopStatement node) { }
    protected virtual void Visit(Expression node) { }
    protected virtual void Visit(Literal node) { }
    protected virtual void Visit(Reference node) { }
    protected virtual void Visit(Identifier node) { }
    protected virtual void Visit(Indexer node) { }
    protected virtual void Visit(UnaryExpression node) { }
    protected virtual void Visit(ConversionExpression node) { }
    protected virtual void Visit(BinaryExpression node) { }
    protected virtual void Visit(Invocation node) { }
    protected virtual void Visit(InternalExpression node) { }
    protected virtual void Visit(InternalInvocation node) { }
    #endregion


    #region Statements; ECMA 12. -------------------------------------------------------------------------------------
    public abstract void Visit(BlockStatement node);
    public abstract void Visit(VariableDeclarationStatement node);
    public abstract void Visit(VariableDeclaration node);
    public abstract void Visit(EmptyStatement node);
    public abstract void Visit(ExpressionStatement node);
    public abstract void Visit(IfStatement node);
    public abstract void Visit(DoWhileStatement node);
    public abstract void Visit(WhileStatement node);
    public abstract void Visit(ForStatement node);
    public abstract void Visit(ForEachInStatement node);
    public abstract void Visit(LabelStatement node);
    public abstract void Visit(GotoStatement node);
    public abstract void Visit(ContinueStatement node);
    public abstract void Visit(BreakStatement node);
    public abstract void Visit(ReturnStatement node);
    public abstract void Visit(WithStatement node);
    public abstract void Visit(SwitchStatement node);
    public abstract void Visit(CaseClause node);
    public abstract void Visit(ThrowStatement node);
    public abstract void Visit(TryStatement node);
    public abstract void Visit(CatchClause node);
    public abstract void Visit(FinallyClause node);
    #endregion

    #region Primary Expressions; ECMA 11.1 -------------------------------------------------------------------------------------
    public abstract void Visit(ThisLiteral node);
    public abstract void Visit(NullLiteral node);
    public abstract void Visit(BooleanLiteral node);
    public abstract void Visit(IntLiteral node);
    public abstract void Visit(DoubleLiteral node);
    public abstract void Visit(StringLiteral node);
    public abstract void Visit(RegexpLiteral node);

    public abstract void Visit(ArrayLiteral node);
    public abstract void Visit(ObjectLiteral node);
    public abstract void Visit(PropertyAssignment node);

    public abstract void Visit(ParenExpression node);
    public abstract void Visit(GuardedCast node);
    public abstract void Visit(InlinedInvocation node);

    public abstract void Visit(ReadIdentifierExpression node);
    public abstract void Visit(ReadIndexerExpression node);
    public abstract void Visit(ReadPropertyExpression node);
    #endregion

    #region Type Conversions; ECMA 9 -------------------------------------------------------------------------------------
    public abstract void Visit(ToPrimitive node);
    public abstract void Visit(ToBoolean node);
    public abstract void Visit(ToNumber node);
    public abstract void Visit(ToDouble node);
    public abstract void Visit(ToInteger node);
    public abstract void Visit(ToInt32 node);
    public abstract void Visit(ToUInt32 node);
    public abstract void Visit(ToUInt16 node);
    public abstract void Visit(ToString node);
    public abstract void Visit(ToObject node);
    public abstract void Visit(ToFunction node);
    #endregion

    #region Unary Operators; ECMA 11.4 -------------------------------------------------------------------------------------
    public abstract void Visit(DeleteExpression node);
    public abstract void Visit(VoidExpression node);
    public abstract void Visit(TypeofExpression node);
    public abstract void Visit(PositiveExpression node);
    public abstract void Visit(NegativeExpression node);
    public abstract void Visit(BitwiseNotExpression node);
    public abstract void Visit(LogicalNotExpression node);
    #endregion

    #region Binary Multiplicative Operators; ECMA 11.5 -------------------------------------------------------------------------------------
    public abstract void Visit(MultiplyExpression node);
    public abstract void Visit(DivideExpression node);
    public abstract void Visit(RemainderExpression node);
    #endregion
    #region Binary Additive Operators; ECMA 11.6 -------------------------------------------------------------------------------------
    public abstract void Visit(AdditionExpression node);
    public abstract void Visit(SubtractionExpression node);
    #endregion
    #region Binary Bitwise Shift Operators; ECMA 11.7 -------------------------------------------------------------------------------------
    public abstract void Visit(LeftShiftExpression node);
    public abstract void Visit(RightShiftExpression node);
    public abstract void Visit(UnsignedRightShiftExpression node);
    #endregion
    #region Binary Relational Operators; ECMA 11.8 -------------------------------------------------------------------------------------
    public abstract void Visit(LesserExpression node);
    public abstract void Visit(GreaterExpression node);
    public abstract void Visit(LesserOrEqualExpression node);
    public abstract void Visit(GreaterOrEqualExpression node);
    public abstract void Visit(InstanceOfExpression node);
    public abstract void Visit(InExpression node);
    #endregion
    #region Binary Equality Operators; ECMA 11.9 -------------------------------------------------------------------------------------
    public abstract void Visit(EqualExpression node);
    public abstract void Visit(NotEqualExpression node);
    public abstract void Visit(SameExpression node);
    public abstract void Visit(NotSameExpression node);
    #endregion
    #region Binary Bitwise Operators; ECMA 11.10 -------------------------------------------------------------------------------------
    public abstract void Visit(BitwiseAndExpression node);
    public abstract void Visit(BitwiseOrExpression node);
    public abstract void Visit(BitwiseXorExpression node);
    #endregion
    #region Binary Logical Operators; ECMA 11.11 -------------------------------------------------------------------------------------
    public abstract void Visit(LogicalAndExpression node);
    public abstract void Visit(LogicalOrExpression node);
    #endregion

    #region Conditional Operators; ECMA 11.12 -------------------------------------------------------------------------------------
    public abstract void Visit(TernaryExpression node);
    #endregion

    #region Assignment; ECMA 11.13 -------------------------------------------------------------------------------------
    public abstract void Visit(WriteTemporaryExpression node);
    public abstract void Visit(WriteIdentifierExpression node);
    public abstract void Visit(WriteIndexerExpression node);
    public abstract void Visit(WritePropertyExpression node);
    #endregion

    #region Comma Operator; ECMA 11.14 -------------------------------------------------------------------------------------
    public abstract void Visit(CommaOperatorExpression node);
    #endregion

    #region Function Calls; ECMA 11.2.2, 11.2.3 -------------------------------------------------------------------------------------
    public abstract void Visit(NewExpression node);
    public abstract void Visit(CallExpression node);
    #endregion

    #region Function Definition; ECMA 13 -------------------------------------------------------------------------------------
    public abstract void Visit(FunctionExpression node);
    public abstract void Visit(FunctionDeclarationStatement node);
    #endregion

    #region Program; ECMA 14 -------------------------------------------------------------------------------------
    public abstract void Visit(Program node);
    #endregion

    #region Interanls
    public abstract void Visit(InternalCall node);
    public abstract void Visit(InternalNew node);
    #endregion
  }
}

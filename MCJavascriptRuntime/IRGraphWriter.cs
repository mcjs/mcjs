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
  public class IRGraphWriter : DepthFirstVisitor
  {
    public static void Execute(JSFunctionMetadata funcMetadata)
    {
      new IRGraphWriter().Visit(funcMetadata);
    }

    protected void WriteEdges<T>(object container, List<T> collection, string rel) where T : Node
    {
      if (collection != null)
        for (var i = 0; i < collection.Count; ++i)
          WriteEdge(container, collection[i], rel + i.ToString());
    }
    protected void WriteNodes<T>(List<T> collection) where T : Node
    {
      if (collection != null)
        for (var i = 0; i < collection.Count; ++i)
          WriteNode(collection[i], collection[i].GetType().ToString());
    }
    System.IO.TextWriter _output;

    JSFunctionMetadata _currFuncMetadata;
    int _clusterName = 0;

    void OpenOutput(string name)
    {
      if (name == "")
        name = "main";
      var filename = System.IO.Path.Combine(JSRuntime.Instance.Configuration.OutputDir, name + ".dotty");
      _output = new System.IO.StreamWriter(filename);
      _output.WriteLine("digraph {0}", name);
      _output.WriteLine("{");
      //Write the graph header etc. 
    }
    void CloseOutput()
    {
      //write graph end
      _output.WriteLine("}");
      _output.Close();
      _output = null;
    }
    void StartSubgraph(string subgraph)
    {
      _output.WriteLine("   subgraph cluster_{0}", _clusterName);
      _output.WriteLine("   {");
      _output.WriteLine("\tstyle = filled", subgraph);
      _output.WriteLine("\tlabel = {0}", subgraph);
      if (subgraph == "Symbols")
        _output.WriteLine("\tcolor = green");
      else
        _output.WriteLine("\tcolor = grey");

      _clusterName++;
    }
    void EndSubgraph()
    {
      _output.WriteLine("   }");
    }
    int NodeNumber = 0;

    Dictionary<object, string> VisitedNodesNames = new Dictionary<object, string>();
    HashSet<object> VisitedNodes = new HashSet<object>();
    bool _writingTopStatements = false;

    bool IsNodeVisited(object node)
    {
      string nodeName;
      if (!VisitedNodesNames.TryGetValue(node, out nodeName))
      {
        return true;
      }
      return false;
    }

    protected new void VisitNode(Node node)
    {
      if (!VisitedNodes.Contains(node))
      {
        base.VisitNode(node);
      }
    }

    protected void VisitNodes<T>(List<T> collection, bool withSubgraphs = false) where T : Node
    {
      if (collection != null)
        for (var i = 0; i < collection.Count; ++i)
          if (!VisitedNodes.Contains(collection[i]))
          {
            VisitedNodes.Add(collection[i]);
            if (withSubgraphs)
            {
              StartSubgraph("Statement" + i.ToString());
            }
            collection[i].Accept(this);
            if (withSubgraphs)
            {
              EndSubgraph();
            }
          }
    }

    string GetNodeName(object node)
    {
      string nodeName;
      if (!VisitedNodesNames.TryGetValue(node, out nodeName))
      {
        nodeName = string.Format("N{0}", NodeNumber++);
        VisitedNodesNames[node] = nodeName;
      }
      return nodeName;
    }

    void WriteNode(object node, string name)//, bool symbol = false, bool literal = false)
    {
      if (node == null)
        return;

      _output.Write("\t{0}", GetNodeName(node));

      var symbol = node as JSSymbol;
      if (symbol != null)
      {
        _output.WriteLine(" [label=\"{0}:{1}\" shape=rectangle color=red]", name, symbol.SymbolType.ToString());
      }
      else
      {
        var literal = node as Literal;
        if (literal != null)
        {
          _output.WriteLine(" [label=\"{0}\" shape=circle color=green]", name);
        }
        else
          _output.WriteLine(" [label=\"{0}\"]", name);
      }
    }

    void WriteEdge(object source, object dest, string label, string style = "solid", string color = "black", int weight = 0)
    {
      if (source != null && dest != null)
      {
        if (weight == 0)
        {
          if (source is Statement)
          {
            if (dest is Statement)
              weight = 10000;
            else if (dest is Expression)
              weight = 100;
            else
              weight = 10;
          }
          else if (source is Expression)
          {
            if (dest is WriteTemporaryExpression)
              weight = 2;
            else if (dest is Expression)
              weight = 10;
          }
          else
            weight = 1;
        }

        _output.WriteLine(
          "\t{0} -> {1} [style={2} color={3} weight={4} label=\"{5}\"];"
          , GetNodeName(source), GetNodeName(dest), style, color, weight, label
        );
      }
    }


    private void Visit(JSFunctionMetadata node)
    {
      OpenOutput(node.FullName);
      _writingTopStatements = true;
      _currFuncMetadata = node;
      _output.WriteLine("  label = \"GRAPH FOR FUNCTION: {0}\"", node.Declaration);

      StartSubgraph("Symbols");
      foreach (var symbol in node.Scope.Symbols)
      {
        WriteNode(symbol, symbol.Name);
      }
      EndSubgraph();
      foreach (var symbol in node.Scope.Symbols)
      {
        VisitNodes(symbol.Readers);
        WriteNodes(symbol.Readers);
        WriteEdges(symbol, symbol.Readers, "Reader");
      }

      VisitNode(node.FunctionIR);


      CloseOutput();
    }

    #region abstract classes
    protected override void Visit(Node node) { WriteNode(node, node.GetType().ToString()); }
    protected override void Visit(Statement node) { Visit((Node)node); }
    protected override void Visit(LoopStatement node) { Visit((Statement)node); }
    protected override void Visit(Expression node)
    {
      Visit((Node)node);

      //node.User should be visited at some point, we avoid writing the name to detect zombi nodes. 
      //string nodeName;
      //if (node.User != null && !VisitedNodes.TryGetValue(node.User, out nodeName))
      //  WriteNode(node.User, node.User.GetType().ToString());

      WriteEdge(node, node.User, "user", "dotted", "yellow", 1);
    }
    protected override void Visit(Literal node) { WriteNode(node, node.ToString()); }
    protected override void Visit(Reference node) { Visit((Expression)node); }
    protected override void Visit(Identifier node)
    {
      Visit((Reference)node);
      //      WriteNode(node.Symbol, node.Symbol.Name, true);
      WriteEdge(node, node.Symbol, "symbol");
    }


    protected override void Visit(Indexer node)
    {
      VisitNode(node.Container);
      VisitNode(node.Index);
      Visit((Reference)node);
      WriteEdge(node, node.Container, "container");
      WriteEdge(node, node.Index, "index");
    }
    protected override void Visit(UnaryExpression node)
    {
      VisitNode(node.Expression);
      Visit((Expression)node);
      WriteEdge(node, node.Expression, "expression");
    }
    protected override void Visit(ConversionExpression node) { Visit((UnaryExpression)node); }
    protected override void Visit(BinaryExpression node)
    {
      VisitNode(node.Left);
      VisitNode(node.Right);
      Visit((Expression)node);
      WriteEdge(node, node.Left, "left");
      WriteEdge(node, node.Right, "right");
    }
    protected override void Visit(Invocation node)
    {
      VisitNode(node.Function);
      VisitNodes(node.Arguments);
      Visit((Expression)node);
      WriteEdge(node, node.Function, "function");
      WriteEdges(node, node.Arguments, "argument");
    }
    protected override void Visit(InternalExpression node) { Visit((Expression)node); }
    protected override void Visit(InternalInvocation node)
    {
      VisitNodes(node.Arguments);
      Visit((InternalExpression)node);
      WriteEdges(node, node.Arguments, "argument");
    }
    #endregion

    #region Statements; ECMA 12. -------------------------------------------------------------------------------------
    public override void Visit(BlockStatement node)
    {
      bool writingTopStatements = false;
      if (_writingTopStatements)
      {
        writingTopStatements = true;
        _writingTopStatements = false;
      }
      VisitNodes(node.Statements, writingTopStatements);
      Visit((Statement)node);
      WriteEdges(node, node.Statements, "statement");
    }

    public override void Visit(VariableDeclarationStatement node)
    {
      VisitNodes(node.Declarations);
      Visit((Statement)node);
      WriteEdges(node, node.Declarations, "declaration");
    }

    public override void Visit(VariableDeclaration node)
    {
      VisitNode(node.Initialization);
      Visit((Node)node);
      WriteEdge(node, node.Initialization, "initialization");
    }

    public override void Visit(EmptyStatement node)
    {
      Visit((Statement)node);
    }

    public override void Visit(ExpressionStatement node)
    {
      VisitNode(node.Expression);
      Visit((Statement)node);
      WriteEdge(node, node.Expression, "expression");
    }

    public override void Visit(IfStatement node)
    {
      VisitNode(node.Condition);
      VisitNode(node.Then);
      VisitNode(node.Else);
      Visit((Statement)node);
      WriteEdge(node, node.Condition, "condition");
      WriteEdge(node, node.Then, "then");
      WriteEdge(node, node.Else, "else");
    }

    public override void Visit(DoWhileStatement node)
    {
      VisitNode(node.Body);
      VisitNode(node.Condition);
      Visit((LoopStatement)node);
      WriteEdge(node, node.Body, "body");
      WriteEdge(node, node.Condition, "condition");
    }

    public override void Visit(WhileStatement node)
    {
      VisitNode(node.Condition);
      VisitNode(node.Body);
      Visit((LoopStatement)node);
      WriteEdge(node, node.Body, "body");
      WriteEdge(node, node.Condition, "condition");
    }

    public override void Visit(ForStatement node)
    {
      VisitNode(node.Initialization);
      VisitNode(node.Condition);
      VisitNode(node.Increment);
      VisitNode(node.Body);
      Visit((LoopStatement)node);
      WriteEdge(node, node.Initialization, "initialization");
      WriteEdge(node, node.Condition, "condition");
      WriteEdge(node, node.Increment, "increment");
      WriteEdge(node, node.Body, "body");
    }

    public override void Visit(ForEachInStatement node)
    {
      VisitNode(node.Initialization);
      VisitNode(node.Expression);
      VisitNode(node.Body);
      Visit((LoopStatement)node);
      WriteEdge(node, node.Initialization, "initialization");
      WriteEdge(node, node.Expression, "expression");
      WriteEdge(node, node.Body, "body");
    }

    public override void Visit(LabelStatement node)
    {
      //Trace.Fail("Visitor for node type {0} is not implemented", node.GetType());
      VisitNode(node.Target);
      Visit((Statement)node);
      WriteEdge(node, node.Target, "target");
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
      WriteEdge(node, node.Expression, "expression");
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
      WriteEdge(node, node.Expression, "expression");
      WriteEdges(node, node.CaseClauses, "caseclause");
    }
    public override void Visit(CaseClause node)
    {
      VisitNode(node.Comparison);
      VisitNode(node.Expression);
      VisitNode(node.Body);
      Visit((Node)node);
      WriteEdge(node, node.Comparison, "comparison");
      WriteEdge(node, node.Expression, "expression");
      WriteEdge(node, node.Body, "body");
    }
    public override void Visit(ThrowStatement node)
    {
      VisitNode(node.Expression);
      Visit((Statement)node);
      WriteEdge(node, node.Expression, "expression");
    }

    public override void Visit(TryStatement node)
    {
      VisitNode(node.Statement);
      VisitNode(node.Catch);
      VisitNode(node.Finally);
      Visit((Statement)node);
      WriteEdge(node, node.Statement, "statement");
      WriteEdge(node, node.Catch, "catch");
      WriteEdge(node, node.Finally, "finally");
    }
    public override void Visit(CatchClause node)
    {
      VisitNode(node.Statement);
      Visit((Node)node);
      WriteEdge(node, node.Statement, "statement");
    }
    public override void Visit(FinallyClause node)
    {
      VisitNode(node.Statement);
      Visit((Node)node);
      WriteEdge(node, node.Statement, "statement");
    }

    #endregion

    #region Primary Expressions; ECMA 11.1 -------------------------------------------------------------------------------------

    public override void Visit(ThisLiteral node) { WriteNode(node, "This"); }

    public override void Visit(NullLiteral node) { WriteNode(node, "Null"); }

    public override void Visit(BooleanLiteral node) { WriteNode(node, node.Value.ToString()); }

    public override void Visit(IntLiteral node) { WriteNode(node, node.Value.ToString()); }

    public override void Visit(DoubleLiteral node) { WriteNode(node, node.Value.ToString()); }

    public override void Visit(StringLiteral node) { WriteNode(node, node.Value); }

    public override void Visit(RegexpLiteral node) { WriteNode(node, "/" + node.Regexp + "/" + node.Options); }

    public override void Visit(ArrayLiteral node)
    {
      VisitNodes(node.Items);
      Visit((Literal)node);
      WriteEdges(node, node.Items, "item");
    }

    public override void Visit(ObjectLiteral node)
    {
      VisitNodes(node.Properties);
      Visit((Literal)node);
      WriteEdges(node, node.Properties, "property");
    }
    public override void Visit(PropertyAssignment node)
    {
      VisitNode(node.Expression);
      Visit((Node)node);
      WriteEdge(node, node.Expression, "expression");
    }
    public override void Visit(ParenExpression node)
    {
      VisitNode(node.Expression);
      Visit((Expression)node);
      WriteEdge(node, node.Expression, "expression");
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
      WriteEdge(node, node.Left, "left");
      WriteEdge(node, node.Middle, "middle");
      WriteEdge(node, node.Right, "right");
    }
    #endregion

    #region Assignment; ECMA 11.13 -------------------------------------------------------------------------------------
    public override void Visit(WriteTemporaryExpression node)
    {
      Visit((Reference)node);
      WriteEdges(node, node.Users, "user");
      VisitNode(node.Value);
      WriteEdge(node, node.Value, "value");
    }
    public override void Visit(WriteIdentifierExpression node)
    {
      Visit((Identifier)node);
      VisitNode(node.Value);
      WriteEdge(node, node.Value, "value");
    }
    public override void Visit(WriteIndexerExpression node)
    {
      Visit((Indexer)node);
      VisitNode(node.Value);
      WriteEdge(node, node.Value, "value");
    }
    #endregion

    #region Comma Operator; ECMA 11.14 -------------------------------------------------------------------------------------
    public override void Visit(CommaOperatorExpression node)
    {
      VisitNodes(node.Expressions);
      Visit((Expression)node);
      WriteEdges(node, node.Expressions, "expression");
    }
    #endregion

    #region Function Calls; ECMA 11.2.2, 11.2.3 -------------------------------------------------------------------------------------
    public override void Visit(NewExpression node) { Visit((Invocation)node); }
    public override void Visit(CallExpression node) { Visit((Invocation)node); }
    #endregion

    #region Function Definition; ECMA 13 -------------------------------------------------------------------------------------
    public override void Visit(FunctionExpression node)
    {
      WriteNode((Node)node, node.Metadata.Declaration);
      if (node.Metadata == _currFuncMetadata)
      {
        //This if the function declaration, so we generate the body
        VisitNode(node.Statement);
        WriteEdge(node, node.Statement, "statement");
      }
    }
    public override void Visit(FunctionDeclarationStatement node)
    {
      VisitNode(node.Implementation);
      //      VisitNode(node.Expression);
      Visit((Statement)node);
      //      WriteEdge(node, node.Expression, "expression");
      WriteEdge(node, node.Implementation, "implementation");
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

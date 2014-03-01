// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
using System;
using System.IO;
using System.Collections.Generic;

using mjr.IR;

namespace mjr
{
  class IRXMLSerializer : DepthFirstVisitor
  {
    private TextWriter Out { get; set; }
    private int Indent { get; set; }

    public IRXMLSerializer(TextWriter destination = null)
    {
      Out = destination ?? Console.Out;
      Indent = 0;
    }

    private void InsideTag(string tagName, Action action)
    {
      Out.Write("<" + tagName + ">");

      action();

      Out.Write("</" + tagName + ">");
    }

    protected override void Visit(Node node)
    {
      InsideTag("Node", () => {});
    }

    protected override void Visit(Statement node)
    {
      InsideTag("Statement", () => {});
    }

    protected override void Visit(LoopStatement node)
    {
      InsideTag("LoopStatement", () => {});
    }

    protected override void Visit(Expression node)
    {
      InsideTag("Expression", () => {});
    }

    protected override void Visit(Literal node)
    {
      InsideTag("Literal", () => { Out.WriteLine(node); });
    }

    protected override void Visit(Reference node)
    {
      InsideTag("Reference", () => {});
    }

    protected override void Visit(Identifier node)
    {
      InsideTag("Identifier", () => { Out.WriteLine(node); });
    }

    protected override void Visit(Indexer node)
    {
      InsideTag("Indexer", () =>
          {
            this.InsideTag("Container", () => { this.VisitNode(node.Container); });
            this.InsideTag("Index", () => { this.VisitNode(node.Index); });
          });
    }

    protected override void Visit(UnaryExpression node)
    {
      InsideTag("UnaryExpression", () => { this.VisitNode(node.Expression); });
    }

    protected override void Visit(ConversionExpression node)
    {
      Visit((UnaryExpression)node);
    }

    protected override void Visit(BinaryExpression node)
    {
      InsideTag("BinaryExpression", () =>
          {
            this.InsideTag("Left", () => { this.VisitNode(node.Left); });
            this.InsideTag("Op", () => { Out.WriteLine(node.GetType().ToString()); });
            this.InsideTag("Right", () => { this.VisitNode(node.Right); });
          });
    }

    protected override void Visit(Invocation node)
    {
      InsideTag("Invocation", () =>
          {
            this.InsideTag("Function", () => { this.VisitNode(node.Function); });
            this.InsideTag("Arguments", () => { this.VisitNodes(node.Arguments); });
          });
    }

    protected override void Visit(InternalExpression node)
    {
      Visit((Expression)node);
    }

    protected override void Visit(InternalInvocation node)
    {
      VisitNodes(node.Arguments);
      Visit((InternalExpression)node);
    }
  }
}

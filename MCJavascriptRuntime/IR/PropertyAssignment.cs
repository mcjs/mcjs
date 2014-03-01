// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
﻿namespace mjr.IR
{
  //TODO: double check if we need 3 different "value" members. It seems one is enough!
  public partial class PropertyAssignment : Node, Syntax.IPropertyAssignment
  {
    /// <summary>
    /// Nema of the propery in the JSON object
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// The FieldId of the this.Name
    /// </summary>
    public int FieldId { get; private set; }

    /// <summary>
    /// To avoid unnecessary overhead, call this function as late as possible, and only when FieldId is going to be used 
    /// </summary>
    public void AssignFieldId()
    {
      if (FieldId == mdr.Runtime.InvalidFieldId)
        FieldId = mdr.Runtime.Instance.GetFieldId(Name);
    }

    public Expression Expression { get; private set; }

    public FunctionExpression GetFunction { get; private set; }
    public FunctionExpression SetFunction { get; private set; }

    public PropertyAssignment(string name, Expression expression)
    {
      Name = name;
      FieldId = mdr.Runtime.InvalidFieldId;
      Expression = expression;
      SourceOffset = expression.SourceOffset;

      Use(Expression);
    }

    public PropertyAssignment(string name, FunctionExpression getFunction, FunctionExpression setFunction)
    {
      Name = name;
      GetFunction = getFunction;
      SetFunction = setFunction;

      if (getFunction != null)
        SourceOffset = getFunction.SourceOffset;
      else
        SourceOffset = setFunction.SourceOffset;

      Use(GetFunction);
      Use(setFunction);
    }

    public override bool Replace(Node oldValue, Node newValue)
    {
      return
        Replace(Expression, oldValue, newValue, n => Expression = n)
        ||
        Replace(GetFunction, oldValue, newValue, n => GetFunction = n)
        ||
        Replace(SetFunction, oldValue, newValue, n => SetFunction = n)
        ||
        base.Replace(oldValue, newValue);
    }

    public override string ToString()
    {
      return string.Format("{0}:{1}", Name, Expression.ToString());
    }

    [System.Diagnostics.DebuggerStepThrough]
    public override void Accept(INodeVisitor visitor)
    {
      visitor.Visit(this);
    }

  }
}

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
  public partial class CallExpression : Invocation
  {
    /// <summary>
    /// If this is set, then Funcsion is PropertyAccess and the call should load ThisArg
    /// </summary>
    internal Expression ThisArg { get; private set; }

    public bool IsDirectEvalCall { get; private set; }

    //internal JSIntrinsicImp IntrinsicImp { get; set; }

    /// <summary>
    /// after inlining, InlinedIR will hold the cloned IR of the callee
    /// </summary>
    public InlinedInvocation InlinedIR { get; set; }


    public CallExpression(ToFunction function, Expression thisArg, List<Expression> arguments, bool isDirectEvalCall)
      : base(function, arguments)
    {
      ThisArg = thisArg;
      Use(ThisArg);

      IsDirectEvalCall = isDirectEvalCall;

      m.Util.Diagnose.Debug.Assert(
        !IsDirectEvalCall
        || ThisArg == null
        , "Invalid situation! DirectEvalCall cannot have a this argument");
      
    }

    public override bool Replace(Node oldValue, Node newValue)
    {
      return
        Replace(ThisArg, oldValue, newValue, n => ThisArg = n)
        ||
        base.Replace(oldValue, newValue);
    }

    public override string ToString()
    {
      return Function.ToString() + "(" + Arguments.ToString() + ")";
    }

    [System.Diagnostics.DebuggerStepThrough]
    public override void Accept(INodeVisitor visitor)
    {
      visitor.Visit(this);
    }
  }
}

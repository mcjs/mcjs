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

using m.Util.Diagnose;

namespace mjr.IR
{
  /// <summary>
  /// This class is the root of all data nodes (expressions for DFG) in the IR
  /// </summary>
  public abstract partial class Expression : Node, Syntax.IExpression
  {
    /// <summary>
    /// By default, and expression can have upto only one user. For multiple users, we will need to introduce temporary WriteIndentifier expressions.
    /// We might have statements (e.g. if) that use the result, so type should be Node and not Expression
    /// </summary>
    public override void AddUser(Node newUser)
    {
      if (User == null)
        base.AddUser(newUser);
      else
      {
        var currUser = User;

        var writeTemp = currUser as WriteTemporaryExpression;
        if (writeTemp == null)
        {
          base.RemoveUser(currUser); //next line will call this.AddUser again, so should null it beforehand
          writeTemp = new WriteTemporaryExpression(this);
          Debug.Assert(User == writeTemp, "Invalid situation!");

          currUser.Replace(this, writeTemp);
          writeTemp.AddUser(currUser);
        }
        else
        {
          //already a temporary is assigned
        }

        newUser.Replace(this, writeTemp);
        writeTemp.AddUser(newUser);
      }
    }

    /// <summary>
    /// static value type usually detected by TypeInference
    /// </summary>
    public mdr.ValueTypes ValueType { get; set; }

    protected Expression()
      : base()
    {
      ///NOTE: constructors must call the following after all class paramters are assigned. 
      /// ValueType = CodeGen.TypeCalculator.GetType(this);
      /// It is important not to rely on virutal function here since we are in constructor!
      ValueType = mdr.ValueTypes.Unknown;
    }

  }
}

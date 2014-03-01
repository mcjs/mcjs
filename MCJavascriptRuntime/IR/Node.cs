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
using System.Text;

using m.Util.Diagnose;

namespace mjr.IR
{
  public abstract partial class Node : Syntax.ISyntaxNode
  {
#if DIAGNOSE || DEBUG
    private int sourceOffset = mdr.Runtime.InvalidOffset;
    public int SourceOffset { get { return sourceOffset; } set { sourceOffset = value; } }
#else
    public int SourceOffset { get { return mdr.Runtime.InvalidOffset; } set { } }
#endif

    /// <summary>
    /// This can be used in an algorith to check if it has already visited this node. 
    /// </summary>
    public int AlgPassNumber { get; set; }

    public virtual NodeType NodeType { get { throw new NotImplementedException(); } }

    protected Node() { }

    public Node User { get; private set; }
    public virtual bool HasUser { get { return User != null; } }
    public virtual bool IsUsedBy(Node node) { return User == node; }
    public virtual void AddUser(Node newUser)
    {
      Trace.Assert(User == null || newUser == null, "Invalid situation, node already has a user");
      User = newUser;
    }
    public virtual void RemoveUser(Node currUser)
    {
      Debug.Assert(User == currUser, "Invalid situation");
      //TODO: Assert(User == null || User.IsUsing(this)
      User = null;
    }

    protected void Use(Node expression)
    {
      if (expression != null)
        expression.AddUser(this);
    }
    protected void Use(IEnumerable<Node> nodes)
    {
      foreach (var e in nodes)
        Use(e);
    }
    protected void Use(List<Node> nodes) //Slightly faster version
    {
      for (var i = 0; i < nodes.Count; ++i)
        Use(nodes[i]);
    }
    protected void Use(List<Expression> expressions) //Slightly faster version
    {
      for (var i = 0; i < expressions.Count; ++i)
        Use(expressions[i]);
    }

    protected bool Replace<T>(IList<T> collection, Node oldValue, Node newValue) where T : class
    {
      Debug.Assert(oldValue != null, "Cannot safely perform field replacement with null value");
      for (var i = 0; i < collection.Count; ++i)
      {
        if (collection[i] == oldValue)
        {
          var newItemValue = newValue as T;
          Debug.Assert(newItemValue != null, "{0} is not an {1}", newValue, typeof(T));
          collection[i] = newItemValue;
          return true;
        }
      }
      return false;
    }
    protected bool Replace<T>(ref T field, Node oldValue, Node newValue) where T : class
    {
      Debug.Assert(oldValue != null, "Cannot safely perform field replacement with null value");
      if (field == oldValue)
      {
        var newFieldValue = newValue as T;
        Debug.Assert(newFieldValue != null, "{0} is not an {1}", newValue, typeof(T));
        field = newFieldValue;
        return true;
      }
      return false;
    }
    protected bool Replace<T>(T fieldValue, Node oldValue, Node newValue, Action<T> action) where T : class
    {
      Debug.Assert(oldValue != null, "Cannot safely perform field replacement with null value");
      if (fieldValue == oldValue)
      {
        var newFieldValue = newValue as T;
        Debug.Assert(newFieldValue != null, "{0} is not an {1}", newValue, typeof(T));
        action(newFieldValue);
        return true;
      }
      return false;
    }
    public virtual bool Replace(Node oldValue, Node newValue)
    {
      Trace.Fail("Cannot find {0} in {1}", oldValue, this);
      return false;
    }

    [System.Diagnostics.DebuggerStepThrough]
    public abstract void Accept(INodeVisitor visitor);
  }
}

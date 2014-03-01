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

using m.Util.Diagnose;

namespace mdr
{
  /// <summary>
  /// We want this class to be as light as possible, we can use this class for builtins as well so that we can have a name for them as well
  /// </summary>
  public class DFunctionMetadata
  {
    public DFunctionMetadata() { }

    public string FullName { get; protected set; }
    public virtual string Declaration
    {
      get
      {

        Trace.Fail(new NotImplementedException());
        return null;
      }
    }
    public override string ToString()
    {
      return null;
    }

    /// <summary>
    /// Everytime a function object is called, it will need to create a new context to store its environment. 
    /// The map of the context depends on the structure of the, captured by its .Metadata (i.e. this class)
    /// Since the structure is fixed, we can avoid recreating the Map for the context, and cache the Map after
    /// first creation and use it in the following invocations. 
    /// </summary>
    public PropertyMap ContextMap;


    /// <summary>
    /// We use a delegate rather than a virtual function for two reasons:
    /// 1- DFunction objects won't need to create a delegate everytime to point their JittedMethod to .Execute of this class
    /// 2- Actual implementation of .Execute might need to change as status of the metadata changes. So, we can avoid complex checks
    /// </summary>
    public DFunctionCode.JittedMethod Execute;

    #region InlineCache
    /// <summary>
    /// Every method gets call frame and index in the InlineCache[] and then returns the index of the next method to be called
    /// </summary>
    public delegate int InlineCachedMethod(ref CallFrame callFrame, int index);

    /// <summary>
    /// List of methods that implement the generic behavior of this function
    /// </summary>
    public InlineCachedMethod[] InlineCache;

    /// <summary>
    /// Total depth of the CallFrame.Values[] calculated during population of the InlineCache to minimize the resizes
    /// </summary>
    public int MaxStackLengh;

    #endregion
    /// <summary>
    /// 
    /// </summary>
    public int TypicalConstructedFieldsLength;

    public virtual void BlackList(DFunction func)
    {

    }
    
    [System.Diagnostics.DebuggerStepThrough]
    public virtual void Accept(IMdrVisitor visitor)
    {
      visitor.Visit(this);
    }

  }
}

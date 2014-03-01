// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
﻿using m.Util.Diagnose;

namespace mdr
{
  /// <summary>
  /// This structure is used to pass arguments to function calls. 
  /// </summary>
  public struct CallFrame
  {
    /// <summary>
    /// the executing function
    /// </summary>
    public DFunction Function;  //TODO: Rename this to callee to match the spec of arguments.callee
    
    /// <summary>
    /// The caller function, might be neede in cases such as eval
    /// </summary>
    public DFunction CallerFunction;

    /// <summary>
    /// The caller execution context. might be needed in cases such as eval
    /// </summary>
    public DObject CallerContext;

    /// <summary>
    /// Return value from this call
    /// </summary>
    public DValue Return;

    /// <summary>
    /// This argument
    /// </summary>
    public DObject This;

    /// <summary>
    /// This should be built based on the types of the arguments. This detemines the overloading of functions
    /// </summary>
    public DFunctionSignature Signature;

    /// <summary>
    /// Total number of arguments passed to the function
    /// </summary>
    public int PassedArgsCount;

    /// <summary>
    /// Callee can set this to say how many 
    /// </summary>
    public int ExpectedArgsCount { get; private set; } 

    public void SetExpectedArgsCount(int expectedArgsCount)
    {
      //Trace.Assert(
      //  expectedArgsCount <= PassedArgsCount
      //  || expectedArgsCount <= InlineArgsCount
      //  , "We still don't handle this situatoin");
      if (expectedArgsCount > PassedArgsCount &&  expectedArgsCount > InlineArgsCount)
        System.Array.Resize<DValue>(ref Arguments, expectedArgsCount - InlineArgsCount);

      ExpectedArgsCount = expectedArgsCount;
    }

    public const int InlineArgsCount = 4;
    public DValue Arg0;
    public DValue Arg1;
    public DValue Arg2;
    public DValue Arg3;
    public DValue[] Arguments; //Arg[InlineArgsCount .. ArgsCount]
    public DValue Arg(int i)
    {
      switch (i)
      {
        case 0: return Arg0;
        case 1: return Arg1;
        case 2: return Arg2;
        case 3: return Arg3;
        default: return Arguments[i - InlineArgsCount];
      }
    }

    void UpdateSignature(int i)
    {
      switch (i)
      {
        case 0: Signature.InitArgType(0, Arg0.ValueType); break;
        case 1: Signature.InitArgType(1, Arg1.ValueType); break;
        case 2: Signature.InitArgType(2, Arg2.ValueType); break;
        case 3: Signature.InitArgType(3, Arg3.ValueType); break;
        default: Signature.InitArgType(i, Arguments[i - InlineArgsCount].ValueType); break;
      }
    }
    public void SetArg(int i, string v)
    {
      switch (i)
      {
        case 0: Arg0.Set(v); break;
        case 1: Arg1.Set(v); break;
        case 2: Arg2.Set(v); break;
        case 3: Arg3.Set(v); break;
        default: Arguments[i - InlineArgsCount].Set(v); break;
      }
      //UpdateSignature(i);
    }

    public void SetArg(int i, double v)
    {
      switch (i)
      {
        case 0: Arg0.Set(v); break;
        case 1: Arg1.Set(v); break;
        case 2: Arg2.Set(v); break;
        case 3: Arg3.Set(v); break;
        default: Arguments[i - InlineArgsCount].Set(v); break;
      }
      //UpdateSignature(i);
    }

    public void SetArg(int i, int v)
    {
      switch (i)
      {
        case 0: Arg0.Set(v); break;
        case 1: Arg1.Set(v); break;
        case 2: Arg2.Set(v); break;
        case 3: Arg3.Set(v); break;
        default: Arguments[i - InlineArgsCount].Set(v); break;
      }
      //UpdateSignature(i);
    }

    public void SetArg(int i, DObject v)
    {
      switch (i)
      {
        case 0: Arg0.Set(v); break;
        case 1: Arg1.Set(v); break;
        case 2: Arg2.Set(v); break;
        case 3: Arg3.Set(v); break;
        default: Arguments[i - InlineArgsCount].Set(v); break;
      }
      //UpdateSignature(i);
    }

    public void SetArg(int i, ref DValue v)
    {
      switch (i)
      {
        case 0: Arg0.Set(ref v); break;
        case 1: Arg1.Set(ref v); break;
        case 2: Arg2.Set(ref v); break;
        case 3: Arg3.Set(ref v); break;
        default: Arguments[i - InlineArgsCount].Set(ref v); break;
      }
      //UpdateSignature(i);
    }

    #region Stack info
    public DValue[] Values;
    public int StackPointer;
    #endregion
  }
}

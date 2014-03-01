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

namespace mdr
{
  public class DFunctionCode
  {
    public delegate void JittedMethod(ref CallFrame callFrame);

    public System.Reflection.MethodInfo MethodHandle { get; set; }
    //public System.Reflection.MethodInfo SpecializedMethodHandle { get; set; }

    public JittedMethod Method { get; set; }

    ///This is the signature for which this function was generated
    ///For unknown types, we use 0 (i.e. undefined)
    public DFunctionSignature Signature;//{ get; set; }

    ///In some cases, we may not know the type of some args, the following captures that. 
    ///The mask has a 0x0 for every unknown arg, and 0xF for every known one
    ///MATCH = (SignatureMask & callFrame.Signature == Signature)
     DFunctionSignature SignatureMask;

    public bool MatchSignature(ref DFunctionSignature signature)
    {
      return (signature.Value & SignatureMask.Value) == Signature.Value;
    }

    public DFunctionCode(ref DFunctionSignature signature)
    {
      SignatureMask.Value = DFunctionSignature.GetMask(ref signature);
      Signature.Value = SignatureMask.Value & signature.Value;
    }

    //This is used to setup callee signature within the body of the current function;
    //public DFunctionSignature CalleeSignature;

    ///We keep a pool of right sized Arguments for each call site here. 
    ///As long as multiple DFunctionCode instances of a DFunctionMetadata don't run in parallel, we can share this pool with them.
    //public DValue[][] ArgumentsPool;

    public virtual void Execute(ref CallFrame callFrame)
    {
      Method(ref callFrame);
    }
  }
}

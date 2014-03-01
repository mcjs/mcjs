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
  public class DFunction : DObject
  {
    public override ValueTypes ValueType { get { return ValueTypes.Function; } }
    public override string GetTypeOf() { return "function"; }

    /// <summary>
    /// Metadata has all the information about functions tokens, AST, code cache, ...
    /// For builtin (CLR) functions, Metadata may be null, and we directly call the JittedCode
    /// </summary>
    public DFunctionMetadata Metadata { get; private set; }

    /// <summary>
    /// This is the last code instance that was used for running this function. In each call, the Code itself 
    /// checks to see if it is the correct one, otherwise, it asks Metadata to get/jit a new one and update DFunction
    /// In any case, this.Code is the current running code too.
    /// </summary>
    public DFunctionCode Code;// { get; set; }

    /// <summary>
    /// This is the trampoline mechansim for:
    ///     Asking Metadata to jit the function
    ///     Running builtin CLR functions
    ///     Running directly a jitted code to bypass/reduce overhead of TypeInference, Cache lookup, etc.
    /// </summary>
    public DFunctionCode.JittedMethod JittedCode { get; set; }

    /// <summary>
    /// If this.JittedCode is not the Generic Metadata.Execute, then it might be called directly from the code
    /// Therefore, we should force Code.JittedCode to check the signature and trampoline back to Metadata if necessary
    /// Otherwise, the Code.JitteCode can assume Metadata has already performed proper proper signature checks
    /// </summary>
    public bool EnableSignature;

    /// <summary>
    /// points to the upper scope to accessed closed on variables.
    /// The code gen may choose to reuse the this context or create a new one in each invocation
    /// </summary>
    public readonly DObject OuterContext;

    /// <summary>
    /// Holds the local variable values that are closed on by sub-functions
    /// The code gen may choose to reuse the context or create a new one in each invocation
    /// </summary>
    //public DObject Context;

    /// <summary>
    /// This is used to cache the result of "prototype" lookup in construct
    /// The initial DFunctionPrototype.prototype sets the value of this member
    /// </summary>
    internal PropertyDescriptor _prototypePropertyDescriptor;
    public PropertyDescriptor PrototypePropertyDescriptor
    {
      get
      {
        if (_prototypePropertyDescriptor == null)
        {
          ///This case happens if we access prototype from withing the runtime before JS code access it
          ///Otherwise, the DFunctionPrototype will take care of assigning a value to this field

          var pd = Map.GetPropertyDescriptorByFieldId(Runtime.Instance.PrototypeFieldId);
          var tmp = new DValue();
          pd.Get(this, ref tmp); //This call will indirectly set this field
          Debug.Assert(_prototypePropertyDescriptor != null, "Invalid situation!");
        }
        return _prototypePropertyDescriptor;
      }
      internal set
      {
        Debug.Assert(_prototypePropertyDescriptor == null, "Function already has a prototype descriptor!");
        _prototypePropertyDescriptor = value;
      }
    }

    /// <summary>
    /// This is used to cache the prototype map information
    /// </summary>
    PropertyMapMetadata _prototypeMapMetadata;


    public DFunction(DFunctionMetadata funcMetadata, DObject outerContext)
      : base(Runtime.Instance.DFunctionMap)
    {
      Metadata = funcMetadata;
      if (funcMetadata != null)
        JittedCode = Metadata.Execute;
      OuterContext = outerContext;
    }
    public DFunction(DFunctionCode.JittedMethod method)
      : this(null, null)
    {
      JittedCode = method;
    }

    public override string ToString()
    {
      if (Metadata != null)
        return Metadata.ToString();
      else
        return "function () {[native code]}";
    }
    public override DFunction ToDFunction() { return this; }

    public void Call(ref CallFrame callFrame)
    {
      JittedCode(ref callFrame);
    }

    public void BlackList()
    {
        if (Metadata != null)
        {
            Metadata.BlackList(this);
        }
    }

    public virtual void Construct(ref CallFrame callFrame)
    {

      DValue proto = new DValue();
      PrototypePropertyDescriptor.Get(this, ref proto);
      var protoObj = proto.AsDObject();
      if (_prototypeMapMetadata == null || _prototypeMapMetadata.Prototype != protoObj)
        _prototypeMapMetadata = Runtime.Instance.GetMapMetadataOfPrototype(protoObj);
      callFrame.Function = this;
      if (Metadata != null)
      {
        callFrame.This = (new DObject(Metadata.TypicalConstructedFieldsLength, _prototypeMapMetadata.Root));
      }
      else
      {
        callFrame.This = (new DObject(0, _prototypeMapMetadata.Root));
      }
      JittedCode(ref callFrame);
      if (Metadata != null && Metadata.TypicalConstructedFieldsLength < callFrame.This.Fields.Length)
        Metadata.TypicalConstructedFieldsLength = callFrame.This.Fields.Length;
      if (ValueTypesHelper.IsObject(callFrame.Return.ValueType))
        callFrame.This = callFrame.Return.AsDObject();
    }

    [System.Diagnostics.DebuggerStepThrough]
    public override void Accept(IMdrVisitor visitor)
    {
      visitor.Visit(this);
    }

  }
}

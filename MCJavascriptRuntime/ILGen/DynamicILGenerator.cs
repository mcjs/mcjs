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
using System.Reflection;
using System.Reflection.Emit;

using m.Util.Diagnose;

namespace mjr.ILGen
{
  class DynamicILGenerator : BaseILGenerator
  {
    DynamicMethod dynamicMethod;
    ILGenerator _msilGen;
    const int _monooptThreshod = 200000;
    protected ILGenerator MsilGen
    {
      get { return _msilGen; }
      set { _msilGen = value; }
    }

    public DynamicILGenerator()
    {
    }

    public override MethodInfo BeginMethod(string methodName, Type returnType, Type[] paramTypes, string[] paramNames)
    {
      dynamicMethod = new System.Reflection.Emit.DynamicMethod(methodName, returnType, paramTypes, true);

      //dynamicMethod = new System.Reflection.Emit.DynamicMethod(
      //    methodName
      //    , MethodAttributes.Public | MethodAttributes.Static
      //    , CallingConventions.Standard
      //    , returnType
      //    , paramTypes
      //    , CodeGen.Types.JSRuntime.TypeOf.Module
      //    , true
      //    );

      MsilGen = dynamicMethod.GetILGenerator();
      return dynamicMethod;
    }
    public override MethodInfo EndMethod()
    {
      Debug.WriteLine("IL size = {0} for function {1}", MsilGen.ILOffset, dynamicMethod);
#if __MonoCS__
//          if (
//            !JSRuntime.Instance.Configuration.EnableMonoOptimizations
//            && 
//            MsilGen.ILOffset < _monooptThreshod //For large IL sizes we need these optimizations otherwise we get "Method too complex" error
//          )
//            dynamicMethod.SetImplementationFlags(dynamicMethod.GetMethodImplementationFlags()
//                                                  | MethodImplAttributes.NoOptimization
//                                                  | MethodImplAttributes.NoOptimizationAggressive
//                                                  | MethodImplAttributes.MDRGenerated);
#endif
      return dynamicMethod;
    }
    public override Delegate EndMethod(Type methodType)
    {
      EndMethod(); //This is in case any of the child classes depend on this.
      return dynamicMethod.CreateDelegate(methodType);
    }

    public override LocalBuilder DeclareLocal(Type localType, string localName)
    {
      return _msilGen.DeclareLocal(localType);
    }
    public override LocalBuilder DeclareLocal(Type localType)
    {
      return _msilGen.DeclareLocal(localType);
    }

    public override Label DefineLabel()
    {
      return _msilGen.DefineLabel();
    }
    public override void MarkLabel(Label loc)
    {
      _msilGen.MarkLabel(loc);
    }

    protected override void Emit(OpCode opcode)
    {
      _msilGen.Emit(opcode);
    }
    protected override void Emit(OpCode opcode, ConstructorInfo con)
    {
      _msilGen.Emit(opcode, con);
    }
    protected override void Emit(OpCode opcode, FieldInfo field)
    {
      _msilGen.Emit(opcode, field);
    }
    protected override void Emit(OpCode opcode, Label label)
    {
      _msilGen.Emit(opcode, label);
    }
    protected override void Emit(OpCode opcode, Label[] labels)
    {
      _msilGen.Emit(opcode, labels);
    }
    protected override void Emit(OpCode opcode, LocalBuilder local)
    {
      _msilGen.Emit(opcode, local);
    }
    protected override void Emit(OpCode opcode, MethodInfo meth)
    {
      _msilGen.Emit(opcode, meth);
    }
    protected override void Emit(OpCode opcode, SignatureHelper signature)
    {
      _msilGen.Emit(opcode, signature);
    }
    protected override void Emit(OpCode opcode, String str)
    {
      _msilGen.Emit(opcode, str);
    }
    protected override void Emit(OpCode opcode, Type cls)
    {
      _msilGen.Emit(opcode, cls);
    }
    protected override void Emit(OpCode opcode, byte arg)
    {
      _msilGen.Emit(opcode, arg);
    }
    protected override void Emit(OpCode opcode, int arg)
    {
      _msilGen.Emit(opcode, arg);
    }
    protected override void Emit(OpCode opcode, bool arg) //This function is here to make source generation easier
    {
      Debug.Assert(opcode == OpCodes.Ldc_I4, string.Format("Invalid operation {0} with boolean argument", opcode));
      _msilGen.Emit((arg) ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
    }
    protected override void Emit(OpCode opcode, long arg)
    {
      _msilGen.Emit(opcode, arg);
    }
    protected override void Emit(OpCode opcode, short arg)
    {
      _msilGen.Emit(opcode, arg);
    }
    protected override void Emit(OpCode opcode, sbyte arg)
    {
      _msilGen.Emit(opcode, arg);
    }
    protected override void Emit(OpCode opcode, double arg)
    {
      _msilGen.Emit(opcode, arg);
    }
    protected override void Emit(OpCode opcode, float arg)
    {
      _msilGen.Emit(opcode, arg);
    }
    protected override void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[] optionalParameterTypes)
    {
      _msilGen.EmitCall(opcode, methodInfo, optionalParameterTypes);
    }
    //protected override void EmitCalli(OpCode opcode, CallingConvention unmanagedCallConv, Type returnType, Type[] parameterTypes)
    //{
    //    _msilGen.EmitCalli(opcode, unmanagedCallConv, returnType, parameterTypes);
    //}
    //protected override void EmitCalli(OpCode opcode, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, Type[] optionalParameterTypes)
    //{
    //    _msilGen.EmitCalli(opcode, callingConvention, returnType, parameterTypes, optionalParameterTypes);
    //}

    public override Label BeginExceptionBlock()
    {
      return _msilGen.BeginExceptionBlock();
    }
    public override void ThrowException(Type excType)
    {
      _msilGen.ThrowException(excType);
    }
    public override void BeginCatchBlock(Type exceptionType)
    {
      _msilGen.BeginCatchBlock(exceptionType);
    }
    public override void BeginFinallyBlock()
    {
      _msilGen.BeginFinallyBlock();
    }
    public override void EndExceptionBlock()
    {
      _msilGen.EndExceptionBlock();
    }
  }
}

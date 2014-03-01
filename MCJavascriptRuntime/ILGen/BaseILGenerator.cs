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
  abstract class BaseILGenerator
  {
    //protected BaseILGenerator(){}

    public abstract MethodInfo BeginMethod(string methodName, Type returnType, Type[] paramTypes, string[] paramNames);
    public abstract MethodInfo EndMethod();
    public abstract Delegate EndMethod(Type methodType);

    public abstract LocalBuilder DeclareLocal(Type localType);
    public abstract LocalBuilder DeclareLocal(Type localType, string localName); //this is mostly used for source code generation

    public abstract Label DefineLabel();
    public abstract void MarkLabel(Label loc);

    public abstract Label BeginExceptionBlock();
    public abstract void BeginCatchBlock(Type exceptionType);
    public abstract void BeginFinallyBlock();
    public abstract void EndExceptionBlock();
    public abstract void ThrowException(Type excType);

    protected abstract void Emit(OpCode opcode);
    protected abstract void Emit(OpCode opcode, bool arg); //This function is here to make source generation easier
    protected abstract void Emit(OpCode opcode, byte arg);
    protected abstract void Emit(OpCode opcode, double arg);
    protected abstract void Emit(OpCode opcode, float arg);
    protected abstract void Emit(OpCode opcode, int arg);
    protected abstract void Emit(OpCode opcode, long arg);
    protected abstract void Emit(OpCode opcode, sbyte arg);
    protected abstract void Emit(OpCode opcode, short arg);
    protected abstract void Emit(OpCode opcode, String str);
    protected abstract void Emit(OpCode opcode, LocalBuilder local);
    protected abstract void Emit(OpCode opcode, Label label);
    protected abstract void Emit(OpCode opcode, Label[] labels);
    protected abstract void Emit(OpCode opcode, Type cls);
    protected abstract void Emit(OpCode opcode, ConstructorInfo con);
    protected abstract void Emit(OpCode opcode, MethodInfo meth);
    protected abstract void Emit(OpCode opcode, FieldInfo field);
    protected abstract void Emit(OpCode opcode, SignatureHelper signature);
    protected abstract void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[] optionalParameterTypes);

    public virtual void Add() { Emit(OpCodes.Add); }
    public virtual void And() { Emit(OpCodes.And); }
    public virtual void Ceq() { Emit(OpCodes.Ceq); }
    public virtual void Cgt() { Emit(OpCodes.Cgt); }
    public virtual void Clt() { Emit(OpCodes.Clt); }
    public virtual void Conv_I4() { Emit(OpCodes.Conv_I4); }
    public virtual void Conv_R8() { Emit(OpCodes.Conv_R8); }
    public virtual void Conv_U4() { Emit(OpCodes.Conv_U4); }
    public virtual void Div() { Emit(OpCodes.Div); }
    public virtual void Dup() { Emit(OpCodes.Dup); }
    public virtual void Mul() { Emit(OpCodes.Mul); }
    public virtual void Neg() { Emit(OpCodes.Neg); }
    public virtual void Not() { Emit(OpCodes.Not); }
    public virtual void Or() { Emit(OpCodes.Or); }
    public virtual void Pop() { Emit(OpCodes.Pop); }
    public virtual void Rem() { Emit(OpCodes.Rem); }
    public virtual void Ret() { Emit(OpCodes.Ret); }
    public virtual void Shl() { Emit(OpCodes.Shl); }
    public virtual void Shr() { Emit(OpCodes.Shr); }
    public virtual void Shr_Un() { Emit(OpCodes.Shr_Un); }
    public virtual void Sub() { Emit(OpCodes.Sub); }
    public virtual void Xor() { Emit(OpCodes.Xor); }

    public virtual void Ldarg_0() { Emit(OpCodes.Ldarg_0); }
    public virtual void Ldarg_1() { Emit(OpCodes.Ldarg_1); }
    public virtual void Ldarg_2() { Emit(OpCodes.Ldarg_2); }
    public virtual void Ldarg_3() { Emit(OpCodes.Ldarg_3); }
    public virtual void Ldarg(int index) { Emit(OpCodes.Ldarg, index); }
    public virtual void Ldc_I4_0() { Emit(OpCodes.Ldc_I4_0); }
    public virtual void Ldc_I4_1() { Emit(OpCodes.Ldc_I4_1); }
    public virtual void Ldnull() { Emit(OpCodes.Ldnull); }

    public virtual void Ldc_I4_S(byte arg) { Emit(OpCodes.Ldc_I4_S, arg); }
    public virtual void Ldc_I4(int arg) { Emit(OpCodes.Ldc_I4, arg); }
    public virtual void Ldc_I4(bool arg) { Emit(OpCodes.Ldc_I4, arg); }
    public virtual void Ldc_I8(long arg) { Emit(OpCodes.Ldc_I8, arg); }
    public virtual void Ldc_U8(ulong arg) { Emit(OpCodes.Ldc_I8, (long)arg); } //This function is here to make validation and source generation easier
    public virtual void Ldc_R8(double arg) { Emit(OpCodes.Ldc_R8, arg); }
    public virtual void Ldstr(String str) { Emit(OpCodes.Ldstr, str); }

    public virtual void Ldloca(LocalBuilder local) { Emit(OpCodes.Ldloca, local); }
    public virtual void Ldloc(LocalBuilder local) { Emit(OpCodes.Ldloc, local); }
    public virtual void Stloc(LocalBuilder local) { Emit(OpCodes.Stloc, local); }

    public virtual void Leave(Label label) { Emit(OpCodes.Leave, label); }
    public virtual void Br(Label label) { Emit(OpCodes.Br, label); }
    public virtual void Brfalse(Label label) { Emit(OpCodes.Brfalse, label); }
    public virtual void Brtrue(Label label) { Emit(OpCodes.Brtrue, label); }
    public virtual void Beq(Label label) { Emit(OpCodes.Beq, label); }
    public virtual void Bne_Un(Label label) { Emit(OpCodes.Bne_Un, label); }
    public virtual void Bgt(Label label) { Emit(OpCodes.Bgt, label); }
    public virtual void Bge(Label label) { Emit(OpCodes.Bge, label); }
    public virtual void Blt(Label label) { Emit(OpCodes.Blt, label); }
    public virtual void Ble(Label label) { Emit(OpCodes.Ble, label); }
    public virtual void Switch(Label[] labels) { Emit(OpCodes.Switch, labels); }

    public virtual void Castclass(Type cls) { Emit(OpCodes.Castclass, cls); }
    public virtual void Initobj(Type cls) { Emit(OpCodes.Initobj, cls); }
    public virtual void Ldobj(Type cls) { Emit(OpCodes.Ldobj, cls); }
    public virtual void Stobj(Type cls) { Emit(OpCodes.Stobj, cls); }
    public virtual void Cpobj(Type cls) { Emit(OpCodes.Cpobj, cls); }

    public virtual void NewArr(Type cls) { Emit(OpCodes.Newarr, cls); }
    public virtual void Newobj(ConstructorInfo con) { Emit(OpCodes.Newobj, con); }

    public virtual void Call(MethodInfo meth)
    {
      if (meth.IsVirtual)
        Debug.Warning("Using Call for virtual method {0}", meth);
      Emit(OpCodes.Call, meth);
    }
    public virtual void Callvirt(MethodInfo meth)
    {
      if (meth.IsVirtual)
        Emit(OpCodes.Callvirt, meth);
      else
        Emit(OpCodes.Call, meth);
    }

    public virtual void Ldflda(FieldInfo field) { Emit(OpCodes.Ldflda, field); }
    public virtual void Ldfld(FieldInfo field) { Emit(OpCodes.Ldfld, field); }
    public virtual void Stfld(FieldInfo field) { Emit(OpCodes.Stfld, field); }

    public virtual void Ldsflda(FieldInfo field) { Emit(OpCodes.Ldsflda, field); }
    public virtual void Ldsfld(FieldInfo field) { Emit(OpCodes.Ldsfld, field); }
    public virtual void Stsfld(FieldInfo field) { Emit(OpCodes.Stsfld, field); }

    public virtual void Ldlen() { Emit(OpCodes.Ldlen); }
    public virtual void Ldelem_Ref() { Emit(OpCodes.Ldelem_Ref); }
    public virtual void Ldelema(Type cls) { Emit(OpCodes.Ldelema, cls); }
    public virtual void Stelem(Type cls) { Emit(OpCodes.Stelem, cls); }

    public virtual void Ldind_Ref() { Emit(OpCodes.Ldind_Ref); }
    public virtual void Stind_Ref() { Emit(OpCodes.Stind_Ref); }

    [System.Diagnostics.Conditional("DEBUG")]
    public virtual void WriteComment(string comment) { }

    [System.Diagnostics.Conditional("DEBUG")]
    public virtual void WriteComment(string format, params object[] arg) { WriteComment(string.Format(format, arg)); }


    #region Special Methods

    public virtual MethodInfo BeginJittedMethod(string methodName)
    {
      return BeginMethod(
        methodName,
        null,
        new Type[] { CodeGen.Types.CallFrame.RefOf },
        new string[] { "callFrame" }
       );
    }

    public virtual mdr.DFunctionCode.JittedMethod EndJittedMethod(JSFunctionMetadata funcMetadata, mdr.DFunctionCode funcInst)
    {
      var timer =
        JSRuntime.Instance.Configuration.ProfileFunctionTime
        ? JSRuntime.StartTimer(JSRuntime.Instance.Configuration.ProfileJitTime, "JS/Jit/Clr" + funcMetadata.Declaration)
        : JSRuntime.StartTimer(JSRuntime.Instance.Configuration.ProfileJitTime, "JS/Jit/Clr");
      Delegate md;
      try
      {
        md = EndMethod(typeof(mdr.DFunctionCode.JittedMethod));
      }
      finally
      {
        JSRuntime.StopTimer(timer);
      }

      return (mdr.DFunctionCode.JittedMethod)md;
    }

    public virtual MethodInfo BeginICMethod(string methodName)
    {
      return BeginMethod(
        methodName
        , CodeGen.Types.ClrSys.Int32
        , new Type[] { CodeGen.Types.CallFrame.RefOf, CodeGen.Types.ClrSys.Int32 }
        , new string[] { "callFrame", "index" }
      );
    }

    public mdr.DFunctionMetadata.InlineCachedMethod EndICMethod(JSFunctionMetadata funcMetadata)
    {
      var timer =
        JSRuntime.Instance.Configuration.ProfileFunctionTime
        ? JSRuntime.StartTimer(JSRuntime.Instance.Configuration.ProfileJitTime, "JS/Jit/ICClr" + funcMetadata.Declaration)
        : JSRuntime.StartTimer(JSRuntime.Instance.Configuration.ProfileJitTime, "JS/Jit/ICClr");
      Delegate md;
      try
      {
        md = EndMethod(typeof(mdr.DFunctionMetadata.InlineCachedMethod));
      }
      finally
      {
        JSRuntime.StopTimer(timer);
      }
      return (mdr.DFunctionMetadata.InlineCachedMethod)md;
    }

    public void Ldarg_CallFrame() { Ldarg_0(); }

    /// <summary>
    /// Loads the reference of the callFrame.Value[index] and if necessary calls the .AsT() on it
    /// </summary>
    public void LoadValue(int index, mdr.ValueTypes valueType = mdr.ValueTypes.DValueRef)
    {
      Ldarg_CallFrame();
      Ldfld(CodeGen.Types.CallFrame.Values);
      Ldc_I4(index);
      Ldelema(CodeGen.Types.DValue.TypeOf);
      if (valueType != mdr.ValueTypes.DValueRef)
        Call(CodeGen.Types.DValue.As(valueType));
    }

    /// <summary>
    /// To simplify code gen, we first put the value on the stack and then call this
    /// function which in turn uses Assign.Run functions to write to the callFrame.Value
    /// </summary>
    public void StoreValue(int index, mdr.ValueTypes valueType)
    {
      LoadValue(index, mdr.ValueTypes.DValueRef);
      //Console.WriteLine("{0}", valueType);
      if (valueType == mdr.ValueTypes.Known) {
        throw new JSDeoptFailedException();
      }
      Call(CodeGen.Types.Operations.Assign.Get(valueType));
    }

    /// <summary>
    /// We use the following to give ourselves the flexibility of load the runtime instance anyways we like
    /// </summary>
    public void LoadRuntimeInstance()
    {
      //Ldsfld(CodeGen.Types.Runtime.Instance);
      Call(CodeGen.Types.Runtime.get_Instance);
    }

    /// <summary>
    /// Function used to return the types of values in the valueStack during deoptimization.
    /// </summary>
    public virtual Type[] GetValueTypes()
    {
        return null;
    }
    #endregion
  }
}

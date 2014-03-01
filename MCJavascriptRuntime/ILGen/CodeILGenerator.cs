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
using System.Reflection.Emit;
using System.Reflection;

using m.Util.Diagnose;

namespace mjr.ILGen
{
  class CodeILGenerator : ValidatingILGenerator // DynamicILGenerator // DllILGenerator
  {
    CodeAsmGenerator _asm;

    public CodeILGenerator(CodeAsmGenerator asm, System.IO.TextWriter output)
      : base()
    {
      _asm = asm;
      _output = output;
    }
    public CodeILGenerator(CodeAsmGenerator asm)
      : this(asm, new System.IO.StringWriter())
    { }

    protected string MethodName { get; private set; }
    protected Type ReturnType { get; private set; }
    protected Type[] ParamTypes { get; private set; }
    protected string[] ParamNames { get; private set; }
    public override MethodInfo BeginMethod(string methodName, Type returnType, Type[] paramTypes, string[] paramNames)
    {
      var mi = base.BeginMethod(methodName, returnType, paramTypes, paramNames);

      MethodName = methodName;
      ReturnType = returnType;
      ParamTypes = paramTypes;
      ParamNames = paramNames;

      _labelNames.Clear();
      _varNames.Clear();

      WriteComment(MethodDeclaration(mi));
      Indent();

      return mi;
    }
    public override MethodInfo EndMethod()
    {
      Debug.Assert(_codeStack.Count == 0, string.Format("Stuff left on the stack in function {0}", MethodName));

      if (_output != Console.Out)
      {
        _asm.WriteOutput(_output.ToString());
        _output.Close();
      }

      return base.EndMethod();
    }

    //internal const string Separator = "Ɵ";
    internal const string Separator = "$";
    const string TempVarPrefix = Separator + "t";
    const string LocalVarPrefix = "l";
    const string LabelPrefix = "L";
    internal const string ByRefPrefix = Separator + "ref" + Separator;

    protected virtual string TypeName(Type t)
    {
      if (t == null)
        return "void";
      else
        return t.ToString();
    }

    protected string MethodDeclaration(MethodInfo mi)
    {
      var sb = new System.Text.StringBuilder();
      //if (mi.IsPublic) sb.Append("public ");
      if (mi.IsStatic) sb.Append("static ");
      sb.AppendFormat("{0} ", TypeName(ReturnType));
      sb.AppendFormat("{0}(", MethodName);
      for (var i = 0; i < ParamTypes.Length; ++i)
      {
        sb.AppendFormat("{0} {1}", TypeName(ParamTypes[i]), ParamNames[i]);
        if (i != ParamTypes.Length - 1)
          sb.Append(", ");
      }
      sb.Append(")");
      return sb.ToString();
    }


    //int _opcodeOffset = 0;
    string ToString(OpCode opcode) { return string.Format("IL_{0:x4}: {1}", MsilGen.ILOffset, opcode); }
    //{
    //        var currOffset = _opcodeOffset;
    //        var operandSize = 0;
    //        switch (opcode.OperandType)
    //        {
    //            case OperandType.InlineNone:
    //                operandSize = 0;
    //                break;
    //            case OperandType.ShortInlineBrTarget:
    //            case OperandType.ShortInlineI:
    //            case OperandType.ShortInlineVar:
    //                operandSize = 1;
    //                break;
    //            case OperandType.InlineVar:
    //                operandSize = 2;
    //                break;
    //            case OperandType.InlineBrTarget:
    //            case OperandType.InlineField:
    //            case OperandType.InlineI:
    //            case OperandType.InlineMethod:
    //            case OperandType.InlineSig:
    //            case OperandType.InlineString:
    //            case OperandType.InlineSwitch:
    //            case OperandType.InlineType:
    //            case OperandType.ShortInlineR:
    //                operandSize = 4;
    //                break;
    //            case OperandType.InlineI8:
    //            case OperandType.InlineR:
    //                operandSize = 8;
    //                break;
    //            case OperandType.InlineTok:// The operand is a FieldRef, MethodRef, or TypeRef token.  
    //            default:
    //                Trace.Fail("Operand type {0} not handled", opcode.OperandType);
    //                break;

    //        }
    //        _opcodeOffset += (opcode.Size + operandSize);
    //        Debug.Assert(_opcodeOffset == MsilGen.ILOffset, "Invalid ILOffset");

    //        return string.Format("IL_{0:x4}: {1}", currOffset, opcode);
    //    }
    #region Output

    System.IO.TextWriter _output;
    protected void OpenOutput(string outputFilename) { _output = new System.IO.StreamWriter(outputFilename); }
    protected void WriteRawOutput(string value)
    {
      if (JSRuntime.Instance.Configuration.EnableDiagIL && _output != Console.Out)
        Debug.WriteLine("{0}", value);

      _output.WriteLine("{0}", value);
    }
    protected void WriteRawOutput(string format, params object[] arg) { WriteRawOutput(string.Format(format, arg)); }

    string _indent = "\t\t";
    protected void Indent() { _indent += "\t"; }
    protected void UnIndent() { _indent = _indent.Remove(_indent.Length - 1); }

    protected void WriteOutput(string value) { WriteRawOutput(_indent + value); }
    protected void WriteOutput(string format, params object[] arg) { WriteOutput(string.Format(format, arg)); }
    public override void WriteComment(string comment) { WriteOutput(comment); }

    void WriteIL(string format, params object[] arg)
    {
      if (JSRuntime.Instance.Configuration.EnableDiagIL)
        WriteComment(format, arg);
    }
    #endregion

    #region CodeStack
    protected class CodeStack : Stack<string>
    {
      void DumpState(string prefix, string postfix)
      {
        if (!JSRuntime.Instance.Configuration.EnableDiagIL)
          return;

        var items = ToArray();
        Array.Reverse(items);
        Debug.WriteLine("{0}{1}{2}", prefix, string.Join(",", items), postfix);
      }

      public new void Push(string item)
      {
        base.Push(item);
        DumpState(">>", null);
      }
      public void Push(string format, params object[] arg) { this.Push(string.Format(format, arg)); }

      public new string Pop()
      {
        var item = base.Pop();
        DumpState("<<", null);
        return item;
      }
    }
    protected CodeStack _codeStack = new CodeStack();
    //protected int StackSize { get { return _stack.Count; } }
    #endregion

    #region Variables

    Dictionary<LocalBuilder, string> _varNames = new Dictionary<LocalBuilder, string>();

    int _varIndex = 0;
    protected string GetVar() { return string.Format("{0}{1}", TempVarPrefix, _varIndex++); }
    protected bool IsVar(string localName) { return _varNames.ContainsValue(localName); }
    public string GetVar(LocalBuilder localVar)
    {
      string localName = null;
      _varNames.TryGetValue(localVar, out localName);
      if (localName == null) //This should never happen, but still just in case!
        localName = string.Format("{0}{1}", LocalVarPrefix, localVar.LocalIndex);
      return localName;
    }

    public override LocalBuilder DeclareLocal(Type localType, string localName)
    {
      var localVar = base.DeclareLocal(localType);

      if (localType.IsByRef)
        localName = ByRefPrefix + localName;

      while (_varNames.ContainsValue(localName))
        localName += Separator; //To make sure we don't have redundant variable names
      _varNames[localVar] = localName;
      //if (localType != Types.DObject.RefOf)
      if (!localType.IsByRef)
        //    WriteOutput("{0} {1} = mdr::DObject::Undefined;", TypeName(localType), localName); //TODO: is there a better way? This is just a hack since we know this is the only type!
        //else
        WriteOutput("{0} {1};", TypeName(localType), localName);
      return localVar;
    }
    public override LocalBuilder DeclareLocal(Type localType)
    {
      return DeclareLocal(localType, GetVar());
    }

    #endregion

    #region Label

    Dictionary<Label, string> _labelNames = new Dictionary<Label, string>();
    protected string GetLabelName(Label label)
    {
      string labelName = null;
      _labelNames.TryGetValue(label, out labelName);
      return labelName;
    }
    public override Label DefineLabel()
    {
      var label = base.DefineLabel();
      _labelNames[label] = string.Format("{0}{1}", LabelPrefix, _labelNames.Count);
      return label;
    }
    public override void MarkLabel(Label loc)
    {
      base.MarkLabel(loc);
      var labelName = _labelNames[loc];
      UnIndent();
      WriteOutput("{0}:;", labelName);
      Indent();
    }

    #endregion

    #region Opcode visitors
    protected override void Emit(OpCode opcode)
    {
      WriteIL("{0}", ToString(opcode));
      base.Emit(opcode);
    }
    protected override void Emit(OpCode opcode, ConstructorInfo con)
    {
      WriteIL("{0} {1} from {2}", ToString(opcode), con, con.DeclaringType);
      base.Emit(opcode, con);
    }
    protected override void Emit(OpCode opcode, FieldInfo field)
    {
      WriteIL("{0} {1}", ToString(opcode), field);
      base.Emit(opcode, field);
    }
    protected override void Emit(OpCode opcode, Label label)
    {
      WriteIL("{0} {1}", ToString(opcode), GetLabelName(label));
      base.Emit(opcode, label);
    }
    protected override void Emit(OpCode opcode, Label[] labels)
    {
      WriteIL("{0} {1}", ToString(opcode), labels);
      base.Emit(opcode, labels);
    }
    protected override void Emit(OpCode opcode, LocalBuilder local)
    {
      WriteIL("{0} {1} : {2}", ToString(opcode), GetVar(local), local.LocalType);
      base.Emit(opcode, local);
    }
    protected override void Emit(OpCode opcode, MethodInfo meth)
    {
      WriteIL("{0} {1} of {2}", ToString(opcode), meth, meth.DeclaringType);
      base.Emit(opcode, meth);
    }
    protected override void Emit(OpCode opcode, SignatureHelper signature)
    {
      WriteIL("{0} {1}", ToString(opcode), signature);
      base.Emit(opcode, signature);
    }
    protected override void Emit(OpCode opcode, String str)
    {
      WriteIL("{0} {1}", ToString(opcode), str);
      base.Emit(opcode, str);
    }
    protected override void Emit(OpCode opcode, Type cls)
    {
      WriteIL("{0} {1}", ToString(opcode), cls);
      base.Emit(opcode, cls);
    }
    protected override void Emit(OpCode opcode, byte arg)
    {
      WriteIL("{0} {1}", ToString(opcode), arg);
      base.Emit(opcode, arg);
    }
    protected override void Emit(OpCode opcode, int arg)
    {
      WriteIL("{0} {1}", ToString(opcode), arg);
      base.Emit(opcode, arg);
    }
    protected override void Emit(OpCode opcode, bool arg) //This function is here to make source generation easier
    {
      WriteIL("{0} {1}", ToString(opcode), arg);
      base.Emit(opcode, arg);
    }
    protected override void Emit(OpCode opcode, long arg)
    {
      WriteIL("{0} {1}", ToString(opcode), arg);
      base.Emit(opcode, arg);
    }
    protected override void Emit(OpCode opcode, short arg)
    {
      WriteIL("{0} {1}", ToString(opcode), arg);
      base.Emit(opcode, arg);
    }
    protected override void Emit(OpCode opcode, sbyte arg)
    {
      WriteIL("{0} {1}", ToString(opcode), arg);
      base.Emit(opcode, arg);
    }
    protected override void Emit(OpCode opcode, double arg)
    {
      WriteIL("{0} {1}", ToString(opcode), arg);
      base.Emit(opcode, arg);
    }
    protected override void Emit(OpCode opcode, float arg)
    {
      WriteIL("{0} {1}", ToString(opcode), arg);
      base.Emit(opcode, arg);
    }
    protected override void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[] optionalParameterTypes)
    {
      WriteIL("{0} {1}, ", ToString(opcode), methodInfo, optionalParameterTypes);
      base.EmitCall(opcode, methodInfo, optionalParameterTypes);
    }
    #endregion
  }
}

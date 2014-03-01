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
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using m.Util.Diagnose;
using System.Text.RegularExpressions;

namespace mjr.ILGen
{
    class CppILGenerator : CodeILGenerator
    {
        CppAsmGenerator _asm;
        public CppILGenerator(CppAsmGenerator asm)
            : base(asm)
        {
            _asm = asm;
        }

        public CppILGenerator(CppAsmGenerator asm, System.IO.TextWriter output)
            : base(asm, output)
        {
            _asm = asm;
        }

        HashSet<LocalBuilder> _declaredVars = new HashSet<LocalBuilder>();

        protected override string TypeName(Type t) { return TypeName(t, true); }
        string TypeName(Type t, bool addCaret)
        {
            if (t == null)
                return "void";

            var name = t.FullName;
            switch (name[name.Length - 1])
            {
                case '&':
                    {
                        Debug.Assert(t.IsByRef, string.Format("None ref type '{0}' cannot end with '&'", name));
                        var targetName = name.Substring(0, name.Length - 1);
                        var targetType = Type.GetType(t.AssemblyQualifiedName.Replace(name, targetName));
                        Debug.Assert(targetType != null, string.Format("Could not find type for string '{0}'", targetName));
                        var resolvedName = TypeName(targetType, addCaret);
                        name = string.Format("{0}%", resolvedName);
                        break;
                    }
                case ']':
                    {
                        Debug.Assert(name[name.Length - 2] == '[', string.Format("type '{0}' end with ']' and not with '[]'", name));
                        Debug.Assert(t.IsArray, string.Format("None array type '{0}' cannot end with '[]'", name));
                        Debug.Assert(!t.IsValueType, string.Format("type '{0}' cannot be both array and value type", name));
                        var targetName = name.Substring(0, name.Length - 2);
                        var targetType = Type.GetType(t.AssemblyQualifiedName.Replace(name, targetName));
                        Debug.Assert(targetType != null, string.Format("Could not find type for string '{0}'", targetName));
                        var resolvedName = TypeName(targetType, addCaret);
                        name = string.Format("array<{0}>", resolvedName);
                        if (addCaret)
                            name += "^";
                        break;
                    }
                default:
                    {
                        if (!t.IsValueType && addCaret)
                            name += "^";
                        name = name.Replace(".", "::").Replace("+", "::");
                        break;
                    }
            }
            return name;
        }
        string TypeName(mdr.ValueTypes t, bool addCaret) { return TypeName(CodeGen.Types.TypeOf(t), addCaret); }

        public override MethodInfo BeginMethod(string methodName, Type returnType, Type[] paramTypes, string[] paramNames)
        {
            var mi = base.BeginMethod(methodName, returnType, paramTypes, paramNames);

            WriteRawOutput("\t\t{0}{{", MethodDeclaration(mi));
            //WriteOutput("static void {0}(mdr::DFunction^ func, mdr::DValue% This, mdr::DFunctionCode^ inst){{", methodName);
            _declaredVars.Clear();
            return mi;
        }

        public override MethodInfo EndMethod()
        {
            WriteRawOutput("\t\t}");
            return base.EndMethod();
        }
#if NONE
        #region Emit
        Dictionary<short, string> _opcodeSymbols = new Dictionary<short, string>()
        {
            {OpCodes.Add.Value, "+"},
            {OpCodes.And.Value, "&"},
            {OpCodes.Or.Value, "|"},
            {OpCodes.Sub.Value, "-"},
            {OpCodes.Mul.Value, "*"},
            {OpCodes.Div.Value, "/"},
            {OpCodes.Rem.Value, "%"},
            {OpCodes.Shl.Value, "<<"},
            {OpCodes.Shr.Value, ">>"},
            {OpCodes.Shr_Un.Value, ">>"},
            {OpCodes.Ceq.Value, "=="},
            {OpCodes.Cgt.Value, ">"},
            {OpCodes.Clt.Value, "<"},
            {OpCodes.Neg.Value, "-"},
            {OpCodes.Not.Value, "~"},
            {OpCodes.Xor.Value, "^"},
            {OpCodes.Conv_I4.Value, "(int)"},
            {OpCodes.Conv_R4.Value, "(float)"},
            {OpCodes.Conv_R8.Value, "(double)"},
            {OpCodes.Conv_U4.Value, "(unsigned int)"},


            {OpCodes.Ldc_I4_0.Value, "0"},
            {OpCodes.Ldc_I4_1.Value, "1"},

            {OpCodes.Ldnull.Value, "nullptr"},
            {OpCodes.Ret.Value, "return"},

            {OpCodes.Ldarg_0.Value, "func"},
            {OpCodes.Ldarg_1.Value, "This"},
            {OpCodes.Ldarg_2.Value, "inst"},
            {OpCodes.Ldelem.Value, ""},
            {OpCodes.Ldelem_I4.Value, ""},
            {OpCodes.Ldelem_Ref.Value, ""},
            {OpCodes.Ldelema.Value, ""},
            {OpCodes.Stelem.Value, ""},
            {OpCodes.Stelem_I4.Value, ""},
            {OpCodes.Stelem_Ref.Value, ""},
            {OpCodes.Stind_Ref.Value, "="},
            {OpCodes.Pop.Value, ""},

            {OpCodes.Nop.Value, ""}
        };
        protected override void Emit(OpCode opcode)
        {

            base.Emit(opcode);

            if (opcode == OpCodes.Dup)
            {
                var value = _stack.Pop();
                string variable;
                if (IsVar(value) && value.StartsWith("$ref$"))
                    variable = value; //FIXME: This can cause bug, if the variable is duplicated to preserve its value, then we really need to duplicate, but, this causes problem with refrences
                else
                {
                    variable = GetVar();
                    var type = "auto";
                    if (value.StartsWith("(mdr::DValue%)"))
                        type = "mdr::DValue^%";
                    WriteOutput("{0} {1} = {2}; //Dup {3}", type, variable, value, IsVar(value) ? value : "");
                }
                _stack.Push(variable);
                _stack.Push(variable);
                return;
            }
            if (opcode == OpCodes.Pop)
            {
                var v = GetVar();
                WriteOutput("auto {0} = {1}; //Pop and disgard", v, _stack.Pop());
                return;
            }
            if (opcode == OpCodes.Ldind_Ref)
            {
                _stack.Push(string.Format("/*Ldind_Ref*/{0}", _stack.Pop()));
                return;
            }
            if (opcode == OpCodes.Rem)
            {
                _stack.Push(string.Format("((int){1} % (int) {0})", _stack.Pop(), _stack.Pop()));
                return;
            }

            string opName = null;
            _opcodeSymbols.TryGetValue(opcode.Value, out opName);
            Debug.Assert(opName != null, string.Format("Cannot convert operation {0}", opcode.Name));
            string opr0 = "";
            string opr1 = "";
            string opr2 = "";
            string res = "";
            switch (opcode.StackBehaviourPop)
            {
                case StackBehaviour.Pop0:
                    res = opName;
                    break;
                case StackBehaviour.Pop1:
                case StackBehaviour.Popi:
                    opr1 = _stack.Pop();
                    res = string.Format("{0} {1}", opName, opr1);
                    break;
                case StackBehaviour.Varpop:
                    if (opcode == OpCodes.Ret)
                    {
                        //base.Emit(OpCodes.Ldnull);
                        base.Emit(OpCodes.Ret);
                    }

                    if (_stack.Count > 0)
                    {
                        throw new InvalidOperationException("In MCJS, functions cannot return value! There are items left on the stack!");
                        //opr1 = Pop();
                        //res = string.Format("{0} {1}", opName, opr1);
                    }
                    break;
                case StackBehaviour.Pop1_pop1:
                case StackBehaviour.Popi_pop1:
                case StackBehaviour.Popi_popi:
                case StackBehaviour.Popi_popi8:
                case StackBehaviour.Popi_popr4:
                case StackBehaviour.Popi_popr8:
                    opr1 = _stack.Pop();
                    opr0 = _stack.Pop();
                    if (opcode == OpCodes.Shr_Un)
                        res = string.Format("((int)(((unsigned int){0}) {1} {2}))", opr0, opName, opr1);
                    else
                        res = string.Format("({0} {1} {2})", opr0, opName, opr1);
                    break;
                case StackBehaviour.Popref_pop1:
                case StackBehaviour.Popref_popi:
                    opr1 = _stack.Pop();
                    opr0 = _stack.Pop();
                    res = string.Format("{0}[{1}]", opr0, opr1);
                    break;
                case StackBehaviour.Popref_popi_pop1:
                case StackBehaviour.Popref_popi_popi:
                case StackBehaviour.Popref_popi_popi8:
                case StackBehaviour.Popref_popi_popr4:
                case StackBehaviour.Popref_popi_popr8:
                case StackBehaviour.Popref_popi_popref:
                    opr2 = _stack.Pop();
                    opr1 = _stack.Pop();
                    opr0 = _stack.Pop();
                    res = string.Format("{0}[{1}]={2}", opr0, opr1, opr2);
                    break;
                default:
                    throw new NotImplementedException();
            }
            switch (opcode.StackBehaviourPush)
            {
                case StackBehaviour.Push0:
                    WriteOutput("{0};", res);
                    break;
                case StackBehaviour.Push1:
                case StackBehaviour.Pushi:
                case StackBehaviour.Pushi8:
                case StackBehaviour.Pushr4:
                case StackBehaviour.Pushr8:
                case StackBehaviour.Pushref:
                    _stack.Push(res);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        protected override void Emit(OpCode opcode, int arg)
        {
            base.Emit(opcode, arg);

            if (opcode == OpCodes.Ldc_I4 || opcode == OpCodes.Ldc_I8)
                _stack.Push(arg.ToString());
            else
                throw new NotImplementedException();
        }
        protected override void Emit(OpCode opcode, bool arg)
        {
            base.Emit(opcode, arg);

            if (opcode == OpCodes.Ldc_I4)
                _stack.Push(arg.ToString().ToLower());
            else
                throw new NotImplementedException();
        }
        protected override void Emit(OpCode opcode, double arg)
        {
            base.Emit(opcode, arg);

            if (opcode == OpCodes.Ldc_R8)
                _stack.Push(arg.ToString());
            else
                throw new NotImplementedException();
        }
        protected override void Emit(OpCode opcode, String str)
        {
            base.Emit(opcode, str);

            if (opcode == OpCodes.Ldstr)
                _stack.Push(string.Format("L\"{0}\"", str));
            else
                throw new NotImplementedException();
        }
        protected override void Emit(OpCode opcode, Type cls)
        {
            base.Emit(opcode, cls);

            if (opcode == OpCodes.Ldelema)
            {
                var opr1 = _stack.Pop();
                var opr0 = _stack.Pop();
                var res = string.Format("(({0})({1}[{2}]))", TypeName(cls.MakeByRefType()), opr0, opr1);
                _stack.Push(res);
            }
            else if (opcode == OpCodes.Castclass)
            {
                var opr0 = _stack.Pop();
                var res = string.Format("(({0}){1})", TypeName(cls), opr0);
                _stack.Push(res);
            }
            else if (opcode == OpCodes.Ldobj)
            {
                var opr0 = _stack.Pop();
                var res = string.Format("(/*{0}*/{1})", TypeName(cls), opr0);
                _stack.Push(res);
            }
            else if (opcode == OpCodes.Stobj)
            {
                var opr1 = _stack.Pop();
                var opr0 = _stack.Pop();
                var res = string.Format("(/*{0}*/{1} = {2})", TypeName(cls), opr0, opr1);
                _stack.Push(res);
            }
            else
                throw new NotImplementedException();
        }
        protected override void Emit(OpCode opcode, byte arg)
        {
            base.Emit(opcode, arg);
            throw new NotImplementedException();
        }
        protected override void Emit(OpCode opcode, long arg)
        {
            base.Emit(opcode, arg);
            throw new NotImplementedException();
        }
        protected override void Emit(OpCode opcode, short arg)
        {
            base.Emit(opcode, arg);
            throw new NotImplementedException();
        }
        protected override void Emit(OpCode opcode, sbyte arg)
        {
            base.Emit(opcode, arg);
            throw new NotImplementedException();
        }
        protected override void Emit(OpCode opcode, float arg)
        {
            base.Emit(opcode, arg);
            throw new NotImplementedException();
        }

        protected override void Emit(OpCode opcode, FieldInfo field)
        {
            base.Emit(opcode, field);

            if (opcode == OpCodes.Ldfld)
            {
                var obj = _stack.Pop();
                var res = string.Format("{0}{1}{2}", obj, field.DeclaringType.IsValueType ? "." : "->", field.Name);
                _stack.Push(res);
            }
            else if (opcode == OpCodes.Stfld)
            {
                var value = _stack.Pop();
                var obj = _stack.Pop();
                var res = string.Format("{0}{1}{2} = {3};", obj, field.DeclaringType.IsValueType ? "." : "->", field.Name, value);
                WriteOutput(res);
            }
            else if (opcode == OpCodes.Ldsfld)
            {
                var res = string.Format("{0}::{1}", TypeName(field.DeclaringType, false), field.Name);
                _stack.Push(res);
            }
            else if (opcode == OpCodes.Stsfld)
            {
                var value = _stack.Pop();
                var res = string.Format("{0}::{1} = {2};", TypeName(field.DeclaringType, false), field.Name, value);
                WriteOutput(res);
            }
            else if (opcode == OpCodes.Ldsflda)
            {
                var res = string.Format("/*&*/({0}::{1})", TypeName(field.DeclaringType, false), field.Name);
                _stack.Push(res);
            }
            else if (opcode == OpCodes.Ldflda)
            {
                var obj = _stack.Pop();
                var res = string.Format("/*&*/{0}{1}{2}", obj, field.DeclaringType.IsValueType ? "." : "->", field.Name);
                _stack.Push(res);
            }
            else
                throw new NotImplementedException();
        }
        protected override void Emit(OpCode opcode, LocalBuilder local)
        {
            base.Emit(opcode, local);

            if (opcode == OpCodes.Ldloc)
            {
                _stack.Push(GetVar(local));
            }
            else if (opcode == OpCodes.Stloc)
            {
                var declared = _declaredVars.Contains(local);
                if (declared || !local.LocalType.IsByRef)
                    WriteOutput("{0} = {1};", GetVar(local), _stack.Pop());
                else
                {
                    WriteOutput("{0} {1} = {2};", TypeName(local.LocalType), GetVar(local), _stack.Pop());
                    _declaredVars.Add(local);
                }
            }
            else if (opcode == OpCodes.Ldloca)
            {
                _stack.Push(string.Format("/*Ldloca*/{0}", GetVar(local)));
            }
            else
                throw new NotImplementedException();
        }

        protected override void Emit(OpCode opcode, Label label)
        {
            base.Emit(opcode, label);

            var labelName = GetLabelName(label);
            if (opcode == OpCodes.Br || opcode == OpCodes.Br_S)
                WriteOutput("goto {0};", labelName);
            else if (opcode == OpCodes.Brfalse || opcode == OpCodes.Brfalse_S)
            {
                //var v = GetVar();
                //WriteOutput("var {0} = {1};", v, Pop());
                //Push(v);
                WriteOutput("if (!({0})) goto {1};", _stack.Pop(), labelName);
            }
            else if (opcode == OpCodes.Brtrue || opcode == OpCodes.Brtrue_S)
                WriteOutput("if ({0}) goto {1};", _stack.Pop(), labelName);
            else
                throw new NotImplementedException();
        }
        protected override void Emit(OpCode opcode, Label[] labels)
        {
            base.Emit(opcode, labels);
            if (opcode == OpCodes.Switch)
            {
                var value = _stack.Pop();
                WriteOutput("switch({0}){{");
                for (var i = 0; i < labels.Length; ++i)
                {
                    var labelName = GetLabelName(labels[i]);
                    WriteOutput("case {0}: goto {1};", i, labelName);
                }
                WriteOutput("}");
            }
            else
                throw new NotImplementedException();
        }
        protected override void Emit(OpCode opcode, MethodInfo meth)
        {
            base.Emit(opcode, meth);

            if (opcode == OpCodes.Call || opcode == OpCodes.Callvirt)
            {
                var parameters = meth.GetParameters();
                var args = new string[parameters.Length];
                var i = args.Length - 1;
                while (i >= 0)
                {
                    var att = "";
                    //if (parameters[i].Attributes == ParameterAttributes.Out)
                    //    att = "out ";
                    att = string.Format("/*{0}*/ ", parameters[i].ParameterType);
                    args[i--] = att + _stack.Pop();
                }
                var prefix = "";
                if (meth.IsStatic)
                    prefix = string.Format("{0}::", TypeName(meth.DeclaringType, false));
                else
                {
                    if (meth.DeclaringType.IsValueType)
                        prefix = string.Format("{0}.", _stack.Pop());
                    else
                        prefix = string.Format("{0}->", _stack.Pop());
                }

                var result = "";
                if (meth.IsSpecialName)
                {
                    if (meth.Name.StartsWith("get_"))
                    {
                        var propertyName = meth.Name.Replace("get_", "");
                        result = string.Format("{0}{1}", prefix, propertyName);
                    }
                    if (meth.Name.StartsWith("set_"))
                    {
                        var propertyName = meth.Name.Replace("set_", "");
                        result = string.Format("{0}{1} = {2}", prefix, propertyName, string.Join(", ", args));
                    }
                }
                else
                    result = string.Format("{0}{1}({2})", prefix, meth.Name, string.Join(", ", args));
                if (meth.ReturnType != typeof(void))
                    _stack.Push(result);
                else
                    WriteOutput("{0};", result);
            }
            else
                throw new NotImplementedException();
        }
        protected override void Emit(OpCode opcode, ConstructorInfo con)
        {
            base.Emit(opcode, con);

            if (opcode == OpCodes.Newobj)
            {
                var args = new string[con.GetParameters().Length];
                var i = args.Length - 1;
                while (i >= 0)
                    args[i--] = _stack.Pop();
                var prefix = "";
                //if (con.IsStatic)
                prefix = TypeName(con.DeclaringType, false);
                var res = string.Format("(gcnew {0}({1}))", prefix, string.Join(", ", args));
                _stack.Push(res);
            }
            else
                throw new NotImplementedException();
        }
        protected override void Emit(OpCode opcode, SignatureHelper signature)
        {
            base.Emit(opcode, signature);
            throw new NotImplementedException();
        }
        protected override void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[] optionalParameterTypes)
        {
            base.EmitCall(opcode, methodInfo, optionalParameterTypes);
            throw new NotImplementedException();
        }
        //protected override void EmitCalli(OpCode opcode, CallingConvention unmanagedCallConv, Type returnType, Type[] parameterTypes)
        //{
        //    throw new NotImplementedException();
        //    base.Emit(opcode, unmanagedCallConv, returnType, parameterTypes);
        //}
        //protected override void EmitCalli(OpCode opcode, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, Type[] optionalParameterTypes)
        //{
        //    throw new NotImplementedException();
        //    base.Emit(opcode, callingConvention, returnType, parameterTypes, optionalParameterTypes);
        //}
        #endregion
#endif
        #region Exception handling
        public override Label BeginExceptionBlock()
        {
            WriteOutput("try {");
            return base.BeginExceptionBlock();
        }
        public override void ThrowException(Type excType)
        {
            //WriteOutput("throw new {0}();", excType);
            WriteOutput("throw {0};", _codeStack.Pop());
            base.ThrowException(excType);
        }
        public override void BeginCatchBlock(Type exceptionType)
        {
          var exceptionVar = GetVar();
          WriteOutput("}} catch ({0} {1}) {{", TypeName(exceptionType), exceptionVar);
          _codeStack.Push(exceptionVar);
          base.BeginCatchBlock(exceptionType);
        }
        public override void BeginFinallyBlock()
        {
            WriteOutput("} finally {");
            base.BeginFinallyBlock();
        }
        public override void EndExceptionBlock()
        {
            WriteOutput("}");
            base.EndExceptionBlock();
        }
        #endregion

        #region Operations
        public override void Add() { base.Add(); _codeStack.Push("({1} + {0})", _codeStack.Pop(), _codeStack.Pop()); }
        public override void And() { base.And(); _codeStack.Push("({1} & {0})", _codeStack.Pop(), _codeStack.Pop()); }
        public override void Ceq() { base.Ceq(); _codeStack.Push("({1} == {0})", _codeStack.Pop(), _codeStack.Pop()); }
        public override void Cgt() { base.Cgt(); _codeStack.Push("({1} > {0})", _codeStack.Pop(), _codeStack.Pop()); }
        public override void Clt() { base.Clt(); _codeStack.Push("({1} < {0})", _codeStack.Pop(), _codeStack.Pop()); }
        public override void Conv_I4() { base.Conv_I4(); _codeStack.Push("((int) {0})", _codeStack.Pop()); }
        public override void Conv_R8() { base.Conv_R8(); _codeStack.Push("((double) {0})", _codeStack.Pop()); }
        public override void Conv_U4() { base.Conv_U4(); _codeStack.Push("((unsigned int) {0})", _codeStack.Pop()); }
        public override void Div() { base.Div(); _codeStack.Push("({1} / {0})", _codeStack.Pop(), _codeStack.Pop()); }
        public override void Dup()
        {
            base.Dup();
            var value = _codeStack.Pop();
            string variable;
            if (IsVar(value) && value.StartsWith(ByRefPrefix))
                variable = value; //FIXME: This can cause bug, if the variable is duplicated to preserve its value, then we really need to duplicate, but, this causes problem with refrences
            else
            {
                variable = GetVar();
                var type = "auto";
                if (value.StartsWith("(mdr::DValue%)"))
                    type = "mdr::DValue^%";
                WriteOutput("{0} {1} = {2}; //Dup {3}", type, variable, value, IsVar(value) ? value : "");
            }
            _codeStack.Push(variable);
            _codeStack.Push(variable);
        }
        public override void Mul() { base.Mul(); _codeStack.Push("({1} * {0})", _codeStack.Pop(), _codeStack.Pop()); }
        public override void Neg() { base.Neg(); _codeStack.Push("(- {0})", _codeStack.Pop()); }
        public override void Not() { base.Not(); _codeStack.Push("(~ {0})", _codeStack.Pop()); }
        public override void Or() { base.Or(); _codeStack.Push("({1} | {0})", _codeStack.Pop(), _codeStack.Pop()); }
        public override void Pop() { base.Pop(); WriteOutput("auto {0} = {1}; //Pop and disgard", GetVar(), _codeStack.Pop()); }
        public override void Rem() { base.Rem(); _codeStack.Push("((int){1} % (int) {0})", _codeStack.Pop(), _codeStack.Pop()); }
        public override void Ret()
        {
            base.Ret();
            if (ReturnType == null || ReturnType == typeof(void))
            {
                if (_codeStack.Count > 0)
                    Trace.Fail("void function '{0}' cannot return value! There are items left on the stack!", MethodName);
                WriteOutput("return;");
            }
            else
            {
                if (_codeStack.Count != 1)
                    Trace.Fail("function '{0}' must return a value! There are no items left on the stack!", MethodName);
                WriteOutput("return {0};", _codeStack.Pop());
            }
        }
        public override void Shl() { base.Shl(); _codeStack.Push("({1} << {0})", _codeStack.Pop(), _codeStack.Pop()); }
        public override void Shr() { base.Shr(); _codeStack.Push("({1} >> {0})", _codeStack.Pop(), _codeStack.Pop()); }
        public override void Shr_Un() { base.Shr_Un(); _codeStack.Push("((int)(((unsigned int){1}) >> {0}))", _codeStack.Pop(), _codeStack.Pop()); }
        public override void Sub() { base.Sub(); _codeStack.Push("({1} - {0})", _codeStack.Pop(), _codeStack.Pop()); }
        public override void Xor() { base.Xor(); _codeStack.Push("({1} ^ {0})", _codeStack.Pop(), _codeStack.Pop()); }

        public override void Ldarg_0() { base.Ldarg_0(); _codeStack.Push(ParamNames[0]); }
        public override void Ldarg_1() { base.Ldarg_1(); _codeStack.Push(ParamNames[1]); }
        public override void Ldarg_2() { base.Ldarg_2(); _codeStack.Push(ParamNames[2]); }
        public override void Ldarg_3() { base.Ldarg_3(); _codeStack.Push(ParamNames[3]); }
        public override void Ldarg(int index) { base.Ldarg(index); _codeStack.Push(ParamNames[index]); }
        public override void Ldc_I4_0() { base.Ldc_I4_0(); _codeStack.Push("0"); }
        public override void Ldc_I4_1() { base.Ldc_I4_1(); _codeStack.Push("1"); }
        public override void Ldnull() { base.Ldnull(); _codeStack.Push("nullptr"); }

        public override void Ldc_I4(int arg) { base.Ldc_I4(arg); _codeStack.Push(arg.ToString()); }
        public override void Ldc_I4(bool arg) { base.Ldc_I4(arg); _codeStack.Push(arg.ToString().ToLower()); }
        public override void Ldc_I8(long arg) { base.Ldc_I8(arg); _codeStack.Push(arg.ToString().ToLower()); }
        public override void Ldc_U8(ulong arg) { base.Ldc_U8(arg); _codeStack.Push(arg.ToString().ToLower()); }
        public override void Ldc_R8(double arg) { base.Ldc_R8(arg); _codeStack.Push(arg.ToString()); }

        static class StringHelpers
        {
          static Dictionary<string, string> escapeMapping = new Dictionary<string, string>() 
          { 
            {"\"", "\\\""}, 
            {"\\\\", @"\\"}, 
            {"\a", @"\a"}, 
            {"\b", @"\b"}, 
            {"\f", @"\f"}, 
            {"\n", @"\n"}, 
            {"\r", @"\r"}, 
            {"\t", @"\t"}, 
            {"\v", @"\v"}, 
            {"\0", @"\0"}, 
          };

          static Regex escapeRegex = new Regex(string.Join("|", escapeMapping.Keys.ToArray()));

          static string EscapeMatchEval(Match m)
          {
            if (escapeMapping.ContainsKey(m.Value))
            {
              return escapeMapping[m.Value];
            }
            return escapeMapping[Regex.Escape(m.Value)];
          }

          public static string Escape(string s)
          {
            return escapeRegex.Replace(s, EscapeMatchEval);
          }
        }

        public override void Ldstr(String str) 
        { 
          base.Ldstr(str);
          //_codeStack.Push("gcnew System::String(L\"{0}\")", str);
          _codeStack.Push("gcnew System::String(L\"{0}\")", StringHelpers.Escape(str)); 
        }

        public override void Ldloca(LocalBuilder local) { base.Ldloca(local); _codeStack.Push("/*Ldloca*/{0}", GetVar(local)); }
        public override void Ldloc(LocalBuilder local) { base.Ldloc(local); _codeStack.Push(GetVar(local)); }
        public override void Stloc(LocalBuilder local)
        {
            base.Stloc(local);
            var declared = _declaredVars.Contains(local);
            if (declared || !local.LocalType.IsByRef)
                WriteOutput("{0} = {1};", GetVar(local), _codeStack.Pop());
            else
            {
                WriteOutput("{0} {1} = {2};", TypeName(local.LocalType), GetVar(local), _codeStack.Pop());
                _declaredVars.Add(local);
            }
        }

        public override void Leave(Label label) { base.Leave(label); WriteOutput("goto {0};", GetLabelName(label)); }
        public override void Br(Label label) { base.Br(label); WriteOutput("goto {0};", GetLabelName(label)); }
        public override void Brfalse(Label label) { base.Brfalse(label); WriteOutput("if (!({0})) goto {1};", _codeStack.Pop(), GetLabelName(label)); }
        public override void Brtrue(Label label) { base.Brtrue(label); WriteOutput("if ({0}) goto {1};", _codeStack.Pop(), GetLabelName(label)); }
        public override void Beq(Label label) { base.Beq(label); WriteOutput("if ({1} == {0}) goto {2};", _codeStack.Pop(), _codeStack.Pop(), GetLabelName(label)); }
        public override void Bne_Un(Label label) { base.Bne_Un(label); WriteOutput("if ({1} != {0}) goto {2};", _codeStack.Pop(), _codeStack.Pop(), GetLabelName(label)); }
        public override void Bgt(Label label) { base.Bgt(label); WriteOutput("if ({1} > {0}) goto {2};", _codeStack.Pop(), _codeStack.Pop(), GetLabelName(label)); }
        public override void Bge(Label label) { base.Bge(label); WriteOutput("if ({1} >= {0}) goto {2};", _codeStack.Pop(), _codeStack.Pop(), GetLabelName(label)); }
        public override void Blt(Label label) { base.Blt(label); WriteOutput("if ({1} < {0}) goto {2};", _codeStack.Pop(), _codeStack.Pop(), GetLabelName(label)); }
        public override void Ble(Label label) { base.Ble(label); WriteOutput("if ({1} <= {0}) goto {2};", _codeStack.Pop(), _codeStack.Pop(), GetLabelName(label)); }

        public override void Switch(Label[] labels)
        {
            base.Switch(labels);
            var value = _codeStack.Pop();
            WriteOutput("switch({0}){{", value);
            for (var i = 0; i < labels.Length; ++i)
            {
                var labelName = GetLabelName(labels[i]);
                WriteOutput("case {0}: goto {1};", i, labelName);
            }
            WriteOutput("}");
        }

        public override void Castclass(Type cls) { base.Castclass(cls); _codeStack.Push("(({0}){1})", TypeName(cls), _codeStack.Pop()); }
        public override void Initobj(Type cls) { base.Initobj(cls); WriteOutput("{1} = {0}();", TypeName(cls), _codeStack.Pop()); }
        public override void Ldobj(Type cls) { base.Ldobj(cls); _codeStack.Push("(/*{0}*/{1})", TypeName(cls), _codeStack.Pop()); }
        public override void Stobj(Type cls) { base.Stobj(cls); WriteOutput("(/*{0}*/{2} = {1});", TypeName(cls), _codeStack.Pop(), _codeStack.Pop()); }
        public override void Cpobj(Type cls) { base.Cpobj(cls); WriteOutput("(/*{0}*/{2} = {1});", TypeName(cls), _codeStack.Pop(), _codeStack.Pop()); }

        public override void NewArr(Type cls) { base.NewArr(cls); _codeStack.Push("gcnew {0}[{1}]", TypeName(cls, false), _codeStack.Pop()); }
        public override void Newobj(ConstructorInfo con)
        {
            base.Newobj(con);
            var args = new string[con.GetParameters().Length];
            var i = args.Length - 1;
            while (i >= 0)
                args[i--] = _codeStack.Pop();
            var prefix = "";
            //if (con.IsStatic)
            prefix = TypeName(con.DeclaringType, false);
            var res = string.Format("(gcnew {0}({1}))", prefix, string.Join(", ", args));
            _codeStack.Push(res);
        }

        private void WriteMethod(MethodInfo meth)
        {
            var parameters = meth.GetParameters();
            var args = new string[parameters.Length];
            var i = args.Length - 1;
            while (i >= 0)
            {
                var att = "";
                //if (parameters[i].Attributes == ParameterAttributes.Out)
                //    att = "out ";
                if (parameters[i].ParameterType.IsEnum)
                    att = string.Format("({0}) ", TypeName(parameters[i].ParameterType));
                else
                    att = string.Format("/*{0}*/ ", parameters[i].ParameterType);
                args[i--] = att + _codeStack.Pop();
            }
            var prefix = "";
            if (meth.DeclaringType != null)
            {
                if (meth.IsStatic)
                    prefix = string.Format("{0}::", TypeName(meth.DeclaringType, false));
                else
                {
                    if (meth.DeclaringType.IsValueType)
                        prefix = string.Format("{0}.", _codeStack.Pop());
                    else
                        prefix = string.Format("{0}->", _codeStack.Pop());
                }
            }

            var result = "";
            if (meth.IsSpecialName)
            {
                if (meth.Name.StartsWith("get_"))
                {
                    var propertyName = meth.Name.Replace("get_", "");
                    result = string.Format("{0}{1}", prefix, propertyName);
                }
                if (meth.Name.StartsWith("set_"))
                {
                    var propertyName = meth.Name.Replace("set_", "");
                    result = string.Format("{0}{1} = {2}", prefix, propertyName, string.Join(", ", args));
                }
            }
            else
                result = string.Format("{0}{1}({2})", prefix, meth.Name, string.Join(", ", args));

            //This is just a hack for now to get the code generation work correctly and match the type of function generated in the BeginMethod
            if (meth.IsStatic && meth.Name == "R")
            {
                var nameIndex = result.IndexOf("::R");
                Debug.Assert(nameIndex != -1, "This should never happen!");
                result = result.Remove(nameIndex, 3);
            }

            if (meth.ReturnType != null && meth.ReturnType != typeof(void))
                _codeStack.Push(result);
            else
                WriteOutput("{0};", result);
        }
        public override void Call(MethodInfo meth) { base.Call(meth); WriteMethod(meth); }
        public override void Callvirt(MethodInfo meth) { base.Callvirt(meth); WriteMethod(meth); }

        public override void Ldflda(FieldInfo field) { base.Ldflda(field); _codeStack.Push("/*&*/{0}{1}{2}", _codeStack.Pop(), field.DeclaringType.IsValueType ? "." : "->", field.Name); }
        public override void Ldfld(FieldInfo field) { base.Ldfld(field); _codeStack.Push("{0}{1}{2}", _codeStack.Pop(), field.DeclaringType.IsValueType ? "." : "->", field.Name); }
        public override void Stfld(FieldInfo field)
        {
            base.Stfld(field);
            var fieldValue = "";
            if (field.FieldType.IsEnum)
                fieldValue = string.Format("({0})", TypeName(field.FieldType));
            fieldValue += _codeStack.Pop();
            WriteOutput("{0}{1}{2} = {3};", _codeStack.Pop(), field.DeclaringType.IsValueType ? "." : "->", field.Name, fieldValue);
        }

        public override void Ldsflda(FieldInfo field) { base.Ldsflda(field); _codeStack.Push("/*&*/({0}::{1})", TypeName(field.DeclaringType, false), field.Name); }
        public override void Ldsfld(FieldInfo field) { base.Ldsfld(field); _codeStack.Push("{0}::{1}", TypeName(field.DeclaringType, false), field.Name); }
        public override void Stsfld(FieldInfo field) { base.Stsfld(field); WriteOutput("{0}::{1} = {2};", TypeName(field.DeclaringType, false), field.Name, _codeStack.Pop()); }

        public override void Ldlen() { base.Ldlen(); _codeStack.Push("{0}.Length", _codeStack.Pop()); }
        public override void Ldelem_Ref() { base.Ldelem_Ref(); _codeStack.Push("{1}[{0}]", _codeStack.Pop(), _codeStack.Pop()); }
        public override void Ldelema(Type cls) { base.Ldelema(cls); _codeStack.Push("(({0})({2}[{1}]))", TypeName(cls.MakeByRefType()), _codeStack.Pop(), _codeStack.Pop()); }
        public override void Stelem(Type cls) { base.Stelem(cls); WriteOutput("{2}[{1}]={0};", _codeStack.Pop(), _codeStack.Pop(), _codeStack.Pop()); }
        public override void Ldind_Ref() { base.Ldind_Ref(); _codeStack.Push("/*Ldind_Ref*/{0}", _codeStack.Pop()); }
        //public override void Stind_Ref() { base.Stind_Ref(); throw new NotImplementedException(); }
        public override void Stind_Ref() { base.Stind_Ref(); WriteOutput("{1} = {0};", _codeStack.Pop(), _codeStack.Pop()); }

        public override void WriteComment(string comment) { WriteOutput("/*{0}*/", comment); }
        #endregion

        #region Special Methods

        string FuncDefPath(JSFunctionMetadata funcMetadata)
        {
          if (funcMetadata.ParentFunction == null)
          {
            var key = JSRuntime.Instance.Scripts.GetScriptKey(funcMetadata);
            if (key != null)
              key = key.Replace('\\', '/');
            return string.Format(@"runtime->Scripts->GetMetadata(L""{0}"")", key);
          }
          return string.Format("{0}->SubFunctions[{1}]", FuncDefPath(funcMetadata.ParentFunction), funcMetadata.FuncDefinitionIndex);
        }

        public override mdr.DFunctionCode.JittedMethod EndJittedMethod(JSFunctionMetadata funcMetadata, mdr.DFunctionCode funcInst)
        {
          if (funcMetadata.Scope.IsEvalFunction)
            _asm.Init.WriteLine("/***** eval result *****"); //We still want to see them in the output

          var instMethodName = string.Format("{0}_inst", MethodName);
          _asm.Init.WriteLine("\t\t\t{");
          _asm.Init.WriteLine("\t\t\t\tauto funcMD = {0}; //{1}", FuncDefPath(funcMetadata), funcMetadata.FullName);
          _asm.Init.WriteLine("\t\t\t\tauto funcSignature = mdr::DFunctionSignature(gcnew array<mdr::ValueTypes> {{ {1} }});", instMethodName, string.Join(", ", funcInst.Signature.Types.Select(t => string.Format("mdr::ValueTypes::{0}", t)).ToArray()));
          _asm.Init.WriteLine("\t\t\t\tauto {0} = gcnew mjr::JSFunctionCode(funcMD, funcSignature);", instMethodName);
          _asm.Init.WriteLine("\t\t\t\t{0}->Method = gcnew mdr::DFunctionCode::JittedMethod(&{1});", instMethodName, MethodName);
          //_asm.InitCache.WriteLine("\t\t\t\t{0}->SignatureMask.Value = {1};", instMethodName, funcInst.SignatureMask.Value);
          //_asm.InitCache.WriteLine("\t\t\t\t{0}->Signature = mdr::DFunctionSignature(gcnew array<mdr::ValueTypes> {{ {1} }});", instMethodName, string.Join(", ", funcInst.Signature.Types.Select(t => string.Format("mdr::ValueTypes::{0}", t)).ToArray()));
          _asm.Init.WriteLine("\t\t\t\tfuncMD->Cache->Add({0});", instMethodName);
          _asm.Init.WriteLine("\t\t\t\tfor (auto i = 0; i < funcMD->Scope->Symbols->Count; ++i) funcMD->Scope->Symbols[i]->AssignFieldId();");
          _asm.Init.WriteLine("\t\t\t}");

          if (funcMetadata.Scope.IsEvalFunction)
            _asm.Init.WriteLine("***** eval result *****/");

          return base.EndJittedMethod(funcMetadata, funcInst);
        }

        #endregion
    }
}

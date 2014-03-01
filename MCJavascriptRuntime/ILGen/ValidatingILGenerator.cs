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
using mjr.CodeGen;

namespace mjr.ILGen
{
    class ValidatingILGenerator : DynamicILGenerator
    {
        MethodInfo _mi;
        #region ValueTypeStack
        class ValueTypeStack : Stack<Type>
        {
            public bool IgnoreFlag { get; set; }

            public ValueTypeStack()
            {
                IgnoreFlag = false;
            }

            void DumpState(string prefix, string postfix)
            {
                if (!JSRuntime.Instance.Configuration.EnableDiagIL)
                    return;

                var items = ToArray();
                Array.Reverse(items);
                Debug.WriteLine("{0}{1}{2}", prefix, string.Join<Type>(",", items), postfix);
            }

            public new void Push(Type item)
            {
                if (!IgnoreFlag)
                {
                    base.Push(item);
                    DumpState("}}", null);
                }
            }

            public new Type Pop()
            {
                if (IgnoreFlag) return typeof(void);

                var item = base.Pop();
                DumpState("{{", null);
                return item;
            }
        }
        ValueTypeStack _valueTypeStack = new ValueTypeStack();
        bool IgnoreValidationFlag { get; set; }

        public override Type[] GetValueTypes()
        {
            return _valueTypeStack.ToArray();
        }

        #endregion

        public override MethodInfo BeginMethod(string methodName, Type returnType, Type[] paramTypes, string[] paramNames)
        {
            _mi = base.BeginMethod(methodName, returnType, paramTypes, paramNames);
            return _mi;
        }

        #region Exception handling
        public override Label BeginExceptionBlock() { return base.BeginExceptionBlock(); }
        public override void ThrowException(Type excType)
        {
            base.ThrowException(excType);
            _valueTypeStack.Push(excType);
        }
        public override void BeginCatchBlock(Type exceptionType)
        {
            base.BeginCatchBlock(exceptionType);
            _valueTypeStack.Push(exceptionType);
        }
        public override void BeginFinallyBlock() { base.BeginFinallyBlock(); }
        public override void EndExceptionBlock() { base.EndExceptionBlock(); }
        #endregion

        #region Types and Operations
        class BinaryTypes : Dictionary<Type, Dictionary<Type, Type>>
        {
            public Type this[Type left, Type right]
            {
                get
                {
                    Dictionary<Type, Type> dic;
                    if (!TryGetValue(left, out dic))
                        return null;
                    Type returnType;
                    if (!dic.TryGetValue(right, out returnType))
                        return null;
                    return returnType;
                }
            }

            public void Add(Type left, Type right, Type result)
            {
                Dictionary<Type, Type> dic;
                if (!TryGetValue(left, out dic))
                {
                    dic = new Dictionary<Type, Type>();
                    Add(left, dic);
                }
                dic[right] = result;
            }
        }

        //http://msdn.microsoft.com/en-us/library/y5b434w4(v=vs.110).aspx
        class ConstType<T> : TypeDelegator
        {
            T _value;

            public ConstType(T value)
                : base(typeof(T))
            {
                _value = value;
            }

            bool CanCast<U>()
            {
                return _value is U;
            }

            public override bool IsSubclassOf(Type c)
            {
                if (c == typeof(T))
                    return true;
                try
                {
                    System.Convert.ChangeType(_value, c);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        Type BinaryOperation(string name, BinaryTypes types)
        {
            var rightType = _valueTypeStack.Pop();
            var leftType = _valueTypeStack.Pop();

            var resultType = types[leftType, rightType];
            if (IgnoreValidationFlag == false)
                Trace.Assert(resultType != null, "{0}({1}, {2}) is invalid IL", name, leftType, rightType);
            _valueTypeStack.Push(resultType);
            return resultType;
        }

        bool TypesMatch(Type expectedType, Type actualType, bool assertOnMismatch = true)
        {
            var matched =
              actualType == expectedType
              || actualType.IsSubclassOf(expectedType)
              || (actualType.IsEnum && expectedType == CodeGen.Types.ClrSys.Int32)
              || (actualType == CodeGen.Types.ClrSys.Int32 && expectedType.IsEnum)
              ;
            Trace.Assert(
              matched || !assertOnMismatch
              , "actual type {0} does not match expected type {1}", actualType, expectedType);
            return matched;
        }

        void ExpectedType(Type expectedType)
        {
            if (IgnoreValidationFlag == true) return;
            if (expectedType == null)
            {
                Trace.Assert(_valueTypeStack.Count == 0, "Expected empty stack, but stuff are left on stack");
            }
            else
            {
                TypesMatch(expectedType, _valueTypeStack.Pop());
            }
        }

        /// <summary>
        /// One of many types is accepted
        /// </summary>
        Type ExpectedTypeSet(params Type[] acceptableTypes)
        {
            if (IgnoreValidationFlag == true) return null;
            Trace.Assert(acceptableTypes != null && acceptableTypes.Length > 0, "Invalid situation, expected types here!");
            var valueType = _valueTypeStack.Pop();
            foreach (var t in acceptableTypes)
                if (TypesMatch(t, valueType))
                    return t;
            Trace.Fail("type {0} on top of stack does not match any of the expected types", valueType);
            return null;
        }

        /// <summary>
        /// types from right to left are expected. types[types.length-1] is on top of stack!
        /// </summary>
        void ExpectedTypeList(params Type[] expectedTypes)
        {
            if (IgnoreValidationFlag == true) return;
            Trace.Assert(expectedTypes != null && expectedTypes.Length > 0, "Invalid situation, expected types here!");
            for (var i = expectedTypes.Length - 1; i >= 0; --i)
            {
                var valueType = _valueTypeStack.Pop();
                var t = expectedTypes[i];
                TypesMatch(t, valueType);
            }
        }

        public void DeactivateValidation()
        {
            IgnoreValidationFlag = true;
            _valueTypeStack.IgnoreFlag = true;
        }

        public void ReactivateValidation()
        {
            IgnoreValidationFlag = false;
            _valueTypeStack.IgnoreFlag = false;
        }

        static BinaryTypes BinaryOp = new BinaryTypes
    {
      {Types.ClrSys.Int32, Types.ClrSys.Int32, Types.ClrSys.Int32},
      {Types.ClrSys.UInt64, Types.ClrSys.UInt64, Types.ClrSys.UInt64},
      {Types.ClrSys.Double, Types.ClrSys.Double, Types.ClrSys.Double},
    };

        static BinaryTypes IntBinaryOp = new BinaryTypes
    {
      {Types.ClrSys.Int32, Types.ClrSys.Int32, Types.ClrSys.Int32},
      {Types.ClrSys.UInt64, Types.ClrSys.UInt64, Types.ClrSys.UInt64},
    };

        static BinaryTypes CompareOp = new BinaryTypes
    {
      {Types.ClrSys.Int32, Types.ClrSys.Int32, Types.ClrSys.Boolean},
      {Types.ClrSys.Double, Types.ClrSys.Double, Types.ClrSys.Boolean},
      {Types.PropertyMap.TypeOf, Types.PropertyMap.TypeOf, Types.ClrSys.Boolean},
    };

        static Type[] NumberTypes = 
    {
      Types.ClrSys.Int32,
      Types.ClrSys.Double
    };

        static Type[] NumberOrEnumTypes = 
    {
      Types.ClrSys.Int32,
      Types.ClrSys.Double,
      typeof(mdr.ValueTypes),
      typeof(mdr.PropertyDescriptor.Attributes)
    };
        #endregion

        #region Operations
        public override void Add() { base.Add(); BinaryOperation("Add", BinaryOp); }
        public override void And() { base.And(); BinaryOperation("And", IntBinaryOp); }
        public override void Ceq() { base.Ceq(); BinaryOperation("Ceq", CompareOp); }
        public override void Cgt() { base.Cgt(); BinaryOperation("Cgt", CompareOp); }
        public override void Clt() { base.Clt(); BinaryOperation("Clt", CompareOp); }
        public override void Conv_I4() { base.Conv_I4(); ExpectedTypeSet(NumberOrEnumTypes); _valueTypeStack.Push(Types.ClrSys.Int32); }
        public override void Conv_R8() { base.Conv_R8(); ExpectedTypeSet(NumberTypes); _valueTypeStack.Push(Types.ClrSys.Double); }
        public override void Conv_U4() { base.Conv_U4(); ExpectedTypeSet(NumberTypes); _valueTypeStack.Push(Types.ClrSys.UInt32); }
        public override void Div() { base.Div(); BinaryOperation("Div", BinaryOp); }
        public override void Dup()
        {
            base.Dup();
            var value = _valueTypeStack.Pop();
            _valueTypeStack.Push(value);
            _valueTypeStack.Push(value);
        }

        public override void Mul() { base.Mul(); BinaryOperation("Mul", BinaryOp); }
        public override void Neg() { base.Neg(); _valueTypeStack.Push(ExpectedTypeSet(NumberTypes)); }
        public override void Not() { base.Not(); ExpectedType(Types.ClrSys.Int32); _valueTypeStack.Push(Types.ClrSys.Int32); }
        public override void Or() { base.Or(); BinaryOperation("Or", IntBinaryOp); }
        public override void Pop() { base.Pop(); _valueTypeStack.Pop(); }
        public override void Rem() { base.Rem(); BinaryOperation("Rem", BinaryOp); }
        public override void Ret()
        {
            base.Ret();
            var returnType = _mi.ReturnType;
            if (returnType == null || returnType == Types.ClrSys.Void)
            {
                Trace.Assert(_valueTypeStack.Count == 0, "'{0}' cannot return value! There are {1} items left on the stack!", _mi, _valueTypeStack.Count);
            }
            else
            {
                Trace.Assert(_valueTypeStack.Count == 1, "'{0}' must return a value! There are {0} items left on the stack!", _mi, _valueTypeStack.Count);
                var value = _valueTypeStack.Pop();
                Trace.Assert(TypesMatch(returnType, value, false), "expression type {0} does not match the return type of {1}", value, _mi);
            }
        }
        public override void Shl() { base.Shl(); BinaryOperation("Shl", IntBinaryOp); }
        public override void Shr() { base.Shr(); BinaryOperation("Shr", IntBinaryOp); }
        public override void Shr_Un() { base.Shr_Un(); BinaryOperation("Shr_Un", IntBinaryOp); }
        public override void Sub() { base.Sub(); BinaryOperation("Sub", BinaryOp); }
        public override void Xor() { base.Xor(); BinaryOperation("Xor", IntBinaryOp); }

        public override void Ldarg_0() { base.Ldarg_0(); _valueTypeStack.Push(_mi.GetParameters()[0].ParameterType); }
        public override void Ldarg_1() { base.Ldarg_1(); _valueTypeStack.Push(_mi.GetParameters()[1].ParameterType); }
        public override void Ldarg_2() { base.Ldarg_2(); _valueTypeStack.Push(_mi.GetParameters()[2].ParameterType); }
        public override void Ldarg_3() { base.Ldarg_3(); _valueTypeStack.Push(_mi.GetParameters()[3].ParameterType); }
        public override void Ldarg(int index) { base.Ldarg(index); _valueTypeStack.Push(_mi.GetParameters()[index].ParameterType); }
        public override void Ldc_I4_0() { base.Ldc_I4_0(); _valueTypeStack.Push(/*new ConstType<int>(0)); }*/ Types.ClrSys.Int32); }
        public override void Ldc_I4_1() { base.Ldc_I4_1(); _valueTypeStack.Push(/*new ConstType<int>(0)); }*/ Types.ClrSys.Int32); }
        public override void Ldnull() { base.Ldnull(); _valueTypeStack.Push(Types.ClrSys.Int32); }

        public override void Ldc_I4_S(byte arg) { base.Ldc_I4_S(arg); _valueTypeStack.Push(Types.ClrSys.Char); }
        public override void Ldc_I4(int arg) { base.Ldc_I4(arg); _valueTypeStack.Push(/*new ConstType<int>(arg)); }*/ Types.ClrSys.Int32); }
        public override void Ldc_I4(bool arg) { base.Ldc_I4(arg); _valueTypeStack.Push(Types.ClrSys.Boolean); }
        public override void Ldc_I8(long arg) { base.Ldc_I8(arg); _valueTypeStack.Push(/*new ConstType<long>(arg)); }*/ Types.ClrSys.Int64); }
        public override void Ldc_U8(ulong arg) { base.Ldc_U8(arg); _valueTypeStack.Push(/*new ConstType<ulong>(arg)); }*/ Types.ClrSys.UInt64); }
        public override void Ldc_R8(double arg) { base.Ldc_R8(arg); _valueTypeStack.Push(Types.ClrSys.Double); }
        public override void Ldstr(String str) { base.Ldstr(str); _valueTypeStack.Push(Types.ClrSys.String.TypeOf); }

        public override void Ldloca(LocalBuilder local) { base.Ldloca(local); _valueTypeStack.Push(local.LocalType.MakeByRefType()); }
        public override void Ldloc(LocalBuilder local) { base.Ldloc(local); _valueTypeStack.Push(local.LocalType); }
        public override void Stloc(LocalBuilder local) { base.Stloc(local); ExpectedType(local.LocalType); }

        public override void Leave(Label label) { base.Leave(label); /*ExpectedType(null);*/ }
        public override void Br(Label label) { base.Br(label); /*ExpectedType(null);*/ }
        public override void Brfalse(Label label) { base.Brfalse(label); ExpectedType(Types.ClrSys.Boolean); }
        public override void Brtrue(Label label) { base.Brtrue(label); ExpectedType(Types.ClrSys.Boolean); }
        public override void Beq(Label label) { base.Beq(label); BinaryOperation("Beq", CompareOp); _valueTypeStack.Pop(); }
        public override void Bne_Un(Label label) { base.Bne_Un(label); BinaryOperation("Beq", CompareOp); _valueTypeStack.Pop(); }
        public override void Bgt(Label label) { base.Bgt(label); BinaryOperation("Beq", CompareOp); _valueTypeStack.Pop(); }
        public override void Bge(Label label) { base.Bge(label); BinaryOperation("Beq", CompareOp); _valueTypeStack.Pop(); }
        public override void Blt(Label label) { base.Blt(label); BinaryOperation("Beq", CompareOp); _valueTypeStack.Pop(); }
        public override void Ble(Label label) { base.Ble(label); BinaryOperation("Beq", CompareOp); _valueTypeStack.Pop(); }

        public override void Switch(Label[] labels) { base.Switch(labels); ExpectedType(Types.ClrSys.Int32); }

        public override void Castclass(Type cls)
        {
            base.Castclass(cls);
            _valueTypeStack.Pop();
            _valueTypeStack.Push(cls);
        }
        public override void Initobj(Type cls)
        {
            base.Initobj(cls);
            ExpectedType(cls.MakeByRefType());
        }
        public override void Ldobj(Type cls)
        {
            base.Ldobj(cls);
            ExpectedType(cls.MakeByRefType());
            _valueTypeStack.Push(cls);
        }
        public override void Stobj(Type cls)
        {
            base.Stobj(cls);
            ExpectedType(cls);
            ExpectedType(cls.MakeByRefType());
        }
        public override void Cpobj(Type cls)
        {
            base.Cpobj(cls);
            ExpectedType(cls.MakeByRefType());
            ExpectedType(cls.MakeByRefType());
        }
        public override void NewArr(Type cls)
        {
            base.NewArr(cls);
            ExpectedType(Types.ClrSys.Int32);
            _valueTypeStack.Push(cls.MakeArrayType());
        }

        void CheckMethodArgs(MethodBase m)
        {
            var args = m.GetParameters();
            for (var i = args.Length - 1; i >= 0; --i)
                ExpectedType(args[i].ParameterType);
            if (m.IsConstructor)
                _valueTypeStack.Push(m.DeclaringType);
            else
            {
                var mi = m as MethodInfo;
                Trace.Assert(mi != null, "Invalid situation, non-constructor method must be MethodInfo");
                if (!mi.IsStatic)
                {
                    var objType = mi.DeclaringType;
                    ExpectedType(objType.IsValueType ? objType.MakeByRefType() : objType);
                }
                if (mi.ReturnType != null && mi.ReturnType != Types.ClrSys.Void)
                    _valueTypeStack.Push(mi.ReturnType);
            }
        }

        public override void Newobj(ConstructorInfo con) { base.Newobj(con); CheckMethodArgs(con); }
        public override void Call(MethodInfo meth) { base.Call(meth); CheckMethodArgs(meth); }
        public override void Callvirt(MethodInfo meth) { base.Callvirt(meth); CheckMethodArgs(meth); }

        public override void Ldflda(FieldInfo field)
        {
            base.Ldflda(field);
            var objType = field.DeclaringType;
            ExpectedType(objType.IsValueType ? objType.MakeByRefType() : objType);
            _valueTypeStack.Push(field.FieldType.MakeByRefType());
        }
        public override void Ldfld(FieldInfo field)
        {
            base.Ldfld(field);
            var objType = field.DeclaringType;
            ExpectedType(objType.IsValueType ? objType.MakeByRefType() : objType);
            _valueTypeStack.Push(field.FieldType);
        }
        public override void Stfld(FieldInfo field)
        {
            base.Stfld(field);
            ExpectedType(field.FieldType);
            var objType = field.DeclaringType;
            ExpectedType(objType.IsValueType ? objType.MakeByRefType() : objType);
        }

        public override void Ldsflda(FieldInfo field) { base.Ldsflda(field); _valueTypeStack.Push(field.FieldType.MakeByRefType()); }
        public override void Ldsfld(FieldInfo field) { base.Ldsfld(field); _valueTypeStack.Push(field.FieldType); }
        public override void Stsfld(FieldInfo field) { base.Stsfld(field); ExpectedType(field.FieldType); }

        public override void Ldlen()
        {
            base.Ldlen();
            var arrayType = _valueTypeStack.Pop();
            if (IgnoreValidationFlag == false)
                Trace.Assert(arrayType.IsArray, "Expected array types, but found {0} on stack", arrayType);
            _valueTypeStack.Push(Types.ClrSys.UInt32);
        }
        public override void Ldelem_Ref()
        {
            base.Ldelem_Ref();
            ExpectedType(Types.ClrSys.Int32); //Index type
            var arrayType = _valueTypeStack.Pop();
            if (IgnoreValidationFlag == false)
                Trace.Assert(arrayType.IsArray, "Expected array types, but found {0} on stack", arrayType);
            _valueTypeStack.Push(arrayType.GetElementType());
        }
        public override void Ldelema(Type cls)
        {
            base.Ldelema(cls);
            ExpectedType(Types.ClrSys.Int32); //Index type
            ExpectedType(cls.MakeArrayType()); //array type
            _valueTypeStack.Push(cls.MakeByRefType());
        }
        public override void Stelem(Type cls)
        {
            base.Stelem(cls);
            ExpectedType(cls); //Value type
            ExpectedType(Types.ClrSys.Int32); //Index type
            ExpectedType(cls.MakeArrayType()); //array type
        }

        public override void Ldind_Ref()
        {
            base.Ldind_Ref();
            var addressType = _valueTypeStack.Pop();
            if (IgnoreValidationFlag == false)
                Trace.Assert(addressType.IsByRef && addressType.HasElementType, "Expected ref address but found {0}", addressType);
            _valueTypeStack.Push(addressType.GetElementType());
        }
        public override void Stind_Ref()
        {
            base.Stind_Ref();
            _valueTypeStack.Pop();
            var addressType = _valueTypeStack.Pop();
            if (IgnoreValidationFlag == false)
                Trace.Assert(addressType.IsByRef && addressType.HasElementType, "Expected ref address but found {0}", addressType);

        }

        public override void WriteComment(string comment) { }
        #endregion
    }
}

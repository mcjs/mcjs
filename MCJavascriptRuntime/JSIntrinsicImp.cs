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
using System.Reflection;
using System.Reflection.Emit;

namespace MCJavascript
{
    class JSIntrinsicImp
    {
        public mdr.ValueTypes ReturnType { get; private set; }
        public string Name { get; private set; }
        public Action<ILGen.BaseILGenerator, Expressions.MethodCall> Execute { get; private set; }
        public JSIntrinsicImp(mdr.ValueTypes returnType, string name, Action<ILGen.BaseILGenerator, Expressions.MethodCall> execute)
        {
            ReturnType = returnType;
            Name = name;
            Execute = execute;
        }

        #region StaticList

        static Dictionary<string, JSIntrinsicImp> _funcs = new Dictionary<string, JSIntrinsicImp>();
        internal static JSIntrinsicImp Get(string name)
        {
            JSIntrinsicImp f;
            _funcs.TryGetValue(name, out f);
            return f;
        }
        internal static JSIntrinsicImp Add(JSIntrinsicImp intrinsic)
        {
            _funcs[intrinsic.Name] = intrinsic;
            return intrinsic;
        }
        internal static JSIntrinsicImp Add(mdr.ValueTypes returnType, string name, Action<ILGen.BaseILGenerator, Expressions.MethodCall> execute)
        {
            return Add(new JSIntrinsicImp(returnType, name, execute));
        }

        #endregion
    }
    class V8IntrinsicImps
    {
        public static void Init()
        {
            JSIntrinsicImp.Add(mdr.ValueTypes.Boolean, "@_IsSmi", (ilGen, method) => 
            { 
                ilGen.Ldc_I4(false); 
            });
            JSIntrinsicImp.Add(mdr.ValueTypes.Double, "TO_NUMBER", (cg, method) =>
            {
                System.Diagnostics.Debug.Assert(method.Arguments.Count == 1, "invalid number of arguments");
                cg.ToDouble(method.Arguments[0]);
            });

        }
    }
}
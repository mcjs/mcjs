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
using System.Linq;
using System.Text;

namespace MCJavascript
{
    class Symbols
    {
        public JSFunctionImp FuncImp { get; private set; }
        public mdr.DFunction Func { get; private set; }
        public mdr.DObject This { get; private set; }

        public Symbols(JSFunctionImp funcImp, mdr.DFunction func, mdr.DObject dis)
        {
            FuncImp = funcImp;
            Func = func;
            This = dis;
        }

        public JSFunctionImp.CodeCache.Guard Guard { get; set;}

        public class Info
        {
            public string Name { get; private set; }
            public bool IsGlobal { get; private set; }
            public JitInfo.StorageType Storage { get; private set; }
            public TypeCode Code { get; set; }

            public Info(string name, bool isGlobal, JitInfo.StorageType storage)
            {
                Name = name;
                IsGlobal = IsGlobal;
                Storage = storage;
            }
        }
        Dictionary<string, Info> _items = new Dictionary<string, Info>();

        public Info Get(string name)
        {
            Info i;
            _items.TryGetValue(name, out i);
            return i;
        }
        Info Set(Info info)
        {
            System.Diagnostics.Debug.Assert(!_items.ContainsKey(info.Name), "Item already exists");
            _items[info.Name] = info;
            return info;
        }
        public Info GetOrAdd(string name)
        {
            var info = Get(name);
            if (info == null)
            {
                var decl = FuncImp.GetDeclaration(name);
                bool isGlobal;
                var storage = JitInfo.StorageType.LocalVar;
                var code = TypeCode.Empty;
                if (decl == null)
                {
                    isGlobal = true;
                    var v = JSLangImp.GlobalObj.GetField(name);
                    if (v==null)
                        throw new Exception(string.Format("Undeclared variable {0} found", name));
                    code = v.Value.DType.Code;
                    storage = JitInfo.StorageType.DVar;
                    //add to guard?
                }
                else
                {
                    isGlobal = false;
                    if (decl.IsClosedOn)
                    {
                        var v = Func.GetField(name);
                        code = v.Value.DType.Code;
                        storage = JitInfo.StorageType.DVar;
                        //add to guard?
                    }
                }
                info = Set(new Symbols.Info(name, isGlobal, storage));
                info.Code = code;
            }
            return info;
        }
    }
}

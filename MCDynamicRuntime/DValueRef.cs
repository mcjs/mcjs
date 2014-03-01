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

namespace mdr
{
    /// <summary>
    /// We use this struct to pass value references around. This is because in C# we cannot directly work with & types. 
    /// </summary>
    public struct DValueRef
    {
        public DValue[] FieldArray;
        public int FieldIndex;

        public static void SetTemp(out DValueRef result)
        {
            result.FieldArray = DefaultRef.Items;
            result.FieldIndex = _currFieldIndex++;
            if (_currFieldIndex  >= DefaultRef.Length) 
                _currFieldIndex = 1;
        }
        public static void SetDefault(out DValueRef result)
        {
            result.FieldArray = DefaultRef.Items;
            result.FieldIndex = DefaultUndefinedRefIndex;
        }
        //We use this for when there is no return results, and we want a ref to an undefined value
        static readonly DValueArray DefaultRef;
        static readonly int DefaultUndefinedRefIndex;
        static int _currFieldIndex;
        static DValueRef()
        {
            DefaultRef = new DValueArray(20);
            //var t = DefaultRef.GetOrAddDTypeOfField("undefined");
            //DefaultUndefinedRefIndex = t.FieldIndex;//This is to make sure we have a filed for it. The default value is undefinded anyways.
            DefaultUndefinedRefIndex = 0;
            _currFieldIndex = 1;
        }
    }
}

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
using mdr;

namespace mjr.Operations.Binary
{
  //TODO: remove this file from the project
  /// <summary>
    /// EMCA-262, Section 11.11
    /// This code should never be called, since semantics of JS requires translation to ( ? : ) operation 
    /// </summary>
    public static partial class LogicalAnd
    {
        //public static void Run(ref mdr.DValue i0, ref mdr.DValue i1, ref mdr.DValue result)
        //{
        //    if (!i0.ToBoolean())
        //        result.Set(ref i0);
        //    else
        //        result.Set(ref i1);
        //}
        public static void Run(/*const*/ ref mdr.DValue i0, string i1, ref mdr.DValue result) { throw new NotImplementedException(); }
        public static void Run(/*const*/ ref mdr.DValue i0, double i1, ref mdr.DValue result) { throw new NotImplementedException(); }
        public static void Run(/*const*/ ref mdr.DValue i0, Int32 i1, ref mdr.DValue result) { throw new NotImplementedException(); }
        public static void Run(/*const*/ ref mdr.DValue i0, bool i1, ref mdr.DValue result) { throw new NotImplementedException(); }
        public static void Run(/*const*/ ref mdr.DValue i0, mdr.DObject i1, ref mdr.DValue result) { throw new NotImplementedException(); }
        public static void Run(string i0, /*const*/ ref mdr.DValue i1, ref mdr.DValue result) { throw new NotImplementedException(); }
        public static void Run(double i0, /*const*/ ref mdr.DValue i1, ref mdr.DValue result) { throw new NotImplementedException(); }
        public static void Run(int i0, /*const*/ ref mdr.DValue i1, ref mdr.DValue result) { throw new NotImplementedException(); }
        public static void Run(bool i0, /*const*/ ref mdr.DValue i1, ref mdr.DValue result) { throw new NotImplementedException(); }
        public static void Run(mdr.DObject i0, /*const*/ ref mdr.DValue i1, ref mdr.DValue result) { throw new NotImplementedException(); }
    }
}
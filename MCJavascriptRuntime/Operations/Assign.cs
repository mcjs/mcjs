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
using System.Runtime.CompilerServices;

namespace mjr.Operations
{
  /// <summary>
  ///Sometimes because of the order of code generation, we need to use the folloiwing helper functions to store values
  ///The difference is that for calling .Set on DValue, first the dest is pushed, then the value. 
  ///With these functions we can have first the value pushed and then the dest
  /// </summary>
  public static partial class Assign
  {
    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void Run(/*const*/ ref mdr.DValue i0, ref mdr.DValue result) { result.Set(ref i0); }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void Run(mdr.DUndefined i0, ref mdr.DValue result) { result.Set(i0); }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void Run(mdr.DNull i0, ref mdr.DValue result) { result.Set(i0); }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void Run(bool i0, ref mdr.DValue result) { result.Set(i0); }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void Run(string i0, ref mdr.DValue result) { result.Set(i0); }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void Run(char i0, ref mdr.DValue result) { result.Set(i0); }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void Run(double i0, ref mdr.DValue result) { result.Set(i0); }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void Run(int i0, ref mdr.DValue result) { result.Set(i0); }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void Run(long i0, ref mdr.DValue result) { result.Set(i0); }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void Run(uint i0, ref mdr.DValue result) { result.Set(i0); }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void Run(ulong i0, ref mdr.DValue result) { result.Set(i0); }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void Run(mdr.DObject i0, ref mdr.DValue result) { result.Set(i0); }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void Run(mdr.DFunction i0, ref mdr.DValue result) { result.Set(i0); }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void Run(mdr.DArray i0, ref mdr.DValue result) { result.Set(i0); }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void Run(object i0, ref mdr.DValue result) { result.Set(i0); }

  }
}

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

namespace mjr
{
    public class JSException : System.Exception
    {
        public mdr.DValue Value;

        public JSException()
            : base()
        {
            //JSRuntime.Instance.CurrentException = this;
        }

        public JSException(string e) : this() { Value.Set(e); }

        public JSException(char e) : this() { Value.Set(e); }

        public JSException(bool e) : this() { Value.Set(e); }

        public JSException(float e) : this() { Value.Set(e); }

        public JSException(double e) : this() { Value.Set(e); }

        public JSException(sbyte e) : this() { Value.Set(e); }

        public JSException(short e) : this() { Value.Set(e); }

        public JSException(int e) : this() { Value.Set(e); }

        public JSException(long e) : this() { Value.Set(e); }

        public JSException(byte e) : this() { Value.Set(e); }

        public JSException(ushort e) : this() { Value.Set(e); }

        public JSException(uint e) : this() { Value.Set(e); }

        public JSException(ulong e) : this() { Value.Set(e); }

        public JSException(mdr.DObject e) : this() { Value.Set(e); }

        public JSException(ref mdr.DValue e) : this() { Value.Set(ref e); }


        public static void Throw(string e) { throw new JSException(e); }

        public static void Throw(char e) { throw new JSException(e); }

        public static void Throw(bool e) { throw new JSException(e); }

        public static void Throw(float e) { throw new JSException(e); }

        public static void Throw(double e) { throw new JSException(e); }

        public static void Throw(sbyte e) { throw new JSException(e); }

        public static void Throw(short e) { throw new JSException(e); }

        public static void Throw(int e) { throw new JSException(e); }

        public static void Throw(long e) { throw new JSException(e); }

        public static void Throw(byte e) { throw new JSException(e); }

        public static void Throw(ushort e) { throw new JSException(e); }

        public static void Throw(uint e) { throw new JSException(e); }

        public static void Throw(ulong e) { throw new JSException(e); }

        public static void Throw(mdr.DObject e) { throw new JSException(e); }

        public static void Throw(mdr.DArray e) { throw new JSException(e); }

        public static void Throw(mdr.DFunction e) { throw new JSException(e); }

        public static void Throw(ref mdr.DValue e) { throw new JSException(ref e); }

    }

    public class JSSpeculationFailedException : System.Exception
    {
        public int icIndex;
        public int expectedType;

        public JSSpeculationFailedException(int icIndex, int expectedType)
            : base()
        {
            this.icIndex = icIndex;
            this.expectedType = expectedType;
        }
        //public static void Throw(string e) { Console.WriteLine(e); throw new JSSpeculationFailedException(e); }
        public static void Throw(int icIndex, int expectedType)
        {
            //Console.WriteLine("JSSpeculationFailedException thrown!: " + icIndex + " : " + expectedType); 
            throw new JSSpeculationFailedException(icIndex, expectedType);
        }
    }

    public class JSDeoptFailedException : System.Exception
    {
        public JSDeoptFailedException() : base()
	{
	}
    }
}

// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using IDLCodeGen.IDL;
using IDLCodeGen.Util;

namespace IDLCodeGen.Conversions
{
  public abstract class CPPType : SimpleConvertedType
  {
    public CPPType(IDLType type) : base(type) { }

    private static readonly Dictionary<string, string> primitiveTypeMap = new Dictionary<string, string> 
		{
			{ "void",               "void"               }, 
			{ "boolean",            "bool"               }, 
			{ "short",              "int16_t"            }, 
			{ "long",               "int32_t"            }, 
			{ "long long",          "int64_t"            }, 
			{ "unsigned short",     "uint16_t"           }, 
			{ "unsigned long",      "uint32_t"           }, 
			{ "unsigned long long", "uint64_t"           }, 
			{ "float",              "float"              }, 
			{ "DOMString",          "MonoString*"        }, 
		};

    protected override string ConvertedPrimitiveType() { return primitiveTypeMap[Base.Name]; }
  }

  public class CPPArgType : CPPType
  {
    public CPPArgType(IDLType type) : base(type) { }

    protected override string ConvertedObjectType() { return Base.Name + "*"; }
  }

  public class CPPRetType : CPPType
  {
    public CPPRetType(IDLType type) : base(type) { }

    protected override string ConvertedObjectType() { return "MonoObject*"; }
  }

  public static class IDLTypeCPPExtensions
  {
    public static CPPArgType AsCPPArg(this IDLType type) { return new CPPArgType(type); }
    public static CPPRetType AsCPPRet(this IDLType type) { return new CPPRetType(type); }
  }
}

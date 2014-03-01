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
  public abstract class CSType : SimpleConvertedType
  {
    public CSType(IDLType type) : base(type) { }

    private static readonly Dictionary<string, string> primitiveTypeMap = new Dictionary<string, string> 
    {
      { "void",               "void"    },
      { "boolean",            "bool"    },
      { "short",              "Int16"   },
      { "long",               "Int32"   },
      { "long long",          "Int64"   },
      { "unsigned short",     "UInt16"  },
      { "unsigned long",      "UInt32"  },
      { "unsigned long long", "UInt64"  },
      { "float",              "float"   },
      { "DOMString",          "string"  },
    };

    protected override string ConvertedPrimitiveType() { return primitiveTypeMap[Base.Name]; }
  }

  public class CSArgType : CSType
  {
    public CSArgType(IDLType type) : base(type) { }

    protected override string ConvertedObjectType() { return "ref ".If(Base.ByRef) + Base.Name; }
  }

  public class CSInternalArgType : CSType
  {
    public CSInternalArgType(IDLType type) : base(type) { }

    protected override string ConvertedObjectType() { return Base.ByRef ? "ref " + Base.Name : "IntPtr"; }
  }

  public class CSRetType : CSType
  {
    public CSRetType(IDLType type) : base(type) { }

    protected override string ConvertedObjectType() { return Base.Name; }
  }

  public static class IDLTypeCSExtensions
  {
    public static CSArgType AsCSArg(this IDLType type) { return new CSArgType(type); }
    public static CSInternalArgType AsCSInternalArg(this IDLType type) { return new CSInternalArgType(type); }
    public static CSRetType AsCSRet(this IDLType type) { return new CSRetType(type); }
  }
}

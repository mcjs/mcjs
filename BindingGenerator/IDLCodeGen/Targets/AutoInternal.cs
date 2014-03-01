// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

using IDLCodeGen.IDL;
using IDLCodeGen.Conversions;
using IDLCodeGen.Util;

namespace IDLCodeGen.Targets
{
  class AutoInternal : IDLTarget
  {
    public override string Filename { get { return "AutoInternal.cs"; }  }

    protected override void Generate(Writer Write, IDLXML idl)
    {
      Write(@"
using System;
using System.Runtime.CompilerServices;
using mwr.Extensions;

// Disable warnings related to new and override.
#pragma warning disable 108, 109, 114

namespace mwr.DOM
{
");

      foreach (var iface in idl.Interfaces)
      {
        Write(@"
public partial class ${Name}
{
  public new partial class Internal
  {
".FormatWith(iface));

        foreach (var op in (from o in iface.Operations where !o.IsRuntime select o))
        {
          var argList = string.Join("", op.Args.Select(a => ", " + a.Type.AsCSInternalArg() + " arg" + a.Index));

          Write(@"
    [MethodImplAttribute(MethodImplOptions.InternalCall)]
    extern public static ${returnType} ${name}(IntPtr domObject${args});

".FormatWith(new { returnType = op.RetType.AsCSRet(),
                   name = op.Name,
                   args = argList }));
        }

        foreach (var attr in (from a in iface.Attributes where !a.IsEventHandler select a))
        {
          if (!attr.IsReadOnly)
            Write(@"
    [MethodImplAttribute(MethodImplOptions.InternalCall)]
    extern public static void ${name}Setter(IntPtr domObject, ${argType} arg);

".FormatWith(new { name = attr.Name, argType = attr.SetterType.AsCSInternalArg() }));

          Write(@"
    [MethodImplAttribute(MethodImplOptions.InternalCall)]
    extern public static ${returnType} ${name}Getter(IntPtr domObject);

".FormatWith(new { returnType = attr.GetterType.AsCSRet(), name = attr.Name }));
        }
        Write(@"
  }
}

");
      }
      Write(@"
}
");
    }
  }
}

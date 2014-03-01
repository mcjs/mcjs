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
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

using IDLCodeGen.IDL;
using IDLCodeGen.Conversions;
using IDLCodeGen.Util;

namespace IDLCodeGen.Targets
{
  class AutoAPI : IDLTarget
  {
    public override string Filename { get { return "AutoAPI.cs"; }  }

    protected override void Generate(Writer Write, IDLXML idl)
    {
#if ENABLE_ZOOMM_MONO_EXTENSIONS
      // Only use .NET 4.5 MethodImplOptions on Mono platforms (UNIX / Mac OS X) for now.
      var os = (int) Environment.OSVersion.Platform;
      var methodImplOptions = "[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]"
                              .If(os == 4 || os == 6 || os == 128);
#else
      var methodImplOptions = "";
#endif

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
".FormatWith(iface));

        // Turn operations into methods.
        foreach (var op in (from o in iface.Operations where !o.IsRuntime select o))
        {
          var argList = string.Join(", ", op.Args.Select(a => a.Type.AsCSArg() + " arg" + a.Index));
          var callArgList = string.Join("", op.Args.Select(a => ", " + "ref ".If(a.Type.ByRef) + "arg" + a.Index
                                                                + ".AsDOMPtr()".If(a.Type.IsObject && !a.Type.ByRef)));

          Write(@"
    ${implOpts}
    public ${returnType} ${name}(${args})
    {
      ${ret}${iName}.Bindings.${oName}(this.AsDOMPtr()${callArgs});
    }

".FormatWith(new {
                   implOpts = methodImplOptions,
                   returnType = op.RetType.AsCSRet(),
                   name = op.CapitalizedName,
                   args = argList,
                   ret =  "return ".If(!op.RetType.IsVoid),
                   iName = iface.Name,
                   oName = op.Name,
                   callArgs = callArgList,
                 }));
        }

        // Turn attributes into properties. In the case where getterType != setterType,
        // we generate a method for the setter.
        foreach (var attr in (from a in iface.Attributes where !a.IsEventHandler select a))
        {
          Write(@"
    public ${type} ${name}
    {
      ${implOpts}
      get
      {
        return ${iName}.Bindings.${aName}Getter(this.AsDOMPtr());
      }
".FormatWith(new {
                   implOpts = methodImplOptions,
                   name = attr.CapitalizedName,
                   type = attr.GetterType.AsCSRet(),
                   iName = iface.Name,
                   aName = attr.Name,
                 }));

          if (!attr.IsReadOnly && attr.GetterType == attr.SetterType)
          {
            Write(@"

      ${implOpts}
      set
      {
        ${iName}.Bindings.${aName}Setter(this.AsDOMPtr(), value${toCPP});
      }
".FormatWith(new {
                   implOpts = methodImplOptions,
                   iName = iface.Name,
                   aName = attr.Name,
                   toCPP = ".AsDOMPtr()".If(attr.SetterType.IsObject),
                 }));
          }

          Write(@"
    }

");

          // Generate a setter method if the set type and get type are different.
          if (!attr.IsReadOnly && attr.GetterType != attr.SetterType)
          {
            Write(@"

    ${implOpts}
    void Set${aName}(${type} value)
    {
        ${iName}.Bindings.${aName}Setter(this.AsDOMPtr(), value${toCPP});
    }

".FormatWith(new { 
                   implOpts = methodImplOptions,
                   aName = attr.Name,
                   type = attr.SetterType.AsCSRet(),
                   iName = iface.Name,
                   toCPP = ".AsDOMPtr()".If(attr.SetterType.IsObject),
                 }));
          }
        }

        // Turn constants into read-only properties.
        foreach (var constant in iface.Constants)
          Write(@"
    public ${type} ${name} { ${implOpts} get { return ${value}; } }
".FormatWith(new {
                   implOpts = methodImplOptions,
                   type = constant.Type.AsCSRet(),
                   name = constant.CapitalizedName,
                   value = constant.Value,
                 }));

        Write(@"
}

");
      }

      Write(@"
}
");
    }
  }
}

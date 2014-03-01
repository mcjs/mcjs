// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
using System.Linq;

using IDLCodeGen.IDL;
using IDLCodeGen.Conversions;
using IDLCodeGen.Util;

namespace IDLCodeGen.Targets
{
  class AutoDOMBindCPP : IDLTarget
  {
    public override string Filename { get { return "AutoDOMBind.cpp"; }  }

    protected override void Generate(Writer Write, IDLXML idl)
    {
      Write(@"
#include <mono/metadata/exception.h>
#include ""javascript/WrappedException.h""
#include ""AutoDOMBind.h""

void addMonoInternalsForBindings()
{
");

      foreach (var iface in idl.Interfaces)
      {
        foreach (var op in (from o in iface.Operations where !o.IsRuntime select o))
        {
          // Create argument list for C# internal call.
          var argList = op.Args.Select(a => a.Type.AsMonoArg().ToString());
          var csArgList = string.Join(",", (new[] { "intptr" }).Concat(argList));

          // Create argument list for C trampoline function.
          argList = op.Args.Select(a => a.Type.AsCPPArg().ToString());
          var cArgList = string.Join(", ", (new[] { "void*" }).Concat(argList));

          Write(@"
  {
    ${returnType} (*fn)(${cArgs}) = &${iName}_${oName};
    mono_add_internal_call(""mwr.DOM.${iName}/Internal::${oName}(${csArgs})"", (const void*)fn);
  }

".FormatWith(new { returnType = op.RetType.AsCPPRet(),
                   cArgs = cArgList,
                   csArgs = csArgList,
                   iName = iface.Name,
                   oName = op.Name }));
        }

        foreach (var attr in (from a in iface.Attributes where !a.IsEventHandler select a))
        {

          Write(@"
  mono_add_internal_call(""mwr.DOM.${iName}/Internal::${aName}Getter"", (const void*)${iName}_${aName}Getter);

".FormatWith(new { iName = iface.Name, aName = attr.Name }));

          if (!attr.IsReadOnly)
            Write(@"
  mono_add_internal_call(""mwr.DOM.${iName}/Internal::${aName}Setter"", (const void*)${iName}_${aName}Setter);
".FormatWith(new { iName = iface.Name, aName = attr.Name }));
        }
      }

      Write(@"
}
");

      foreach (var iface in idl.Interfaces)
      {
        foreach (var op in (from o in iface.Operations where !o.IsRuntime select o))
        {
          var retConverter = op.RetType.IsObject ? "convertToCSObject"
                           : op.RetType.IsString ? "convertToCSString"
                           : "";

          // Create argument list for C trampoline function.
          var argList = op.Args.Select(a => a.Type.AsCPPArg() + " arg" + a.Index);
          var cArgList = string.Join(", ", (new[] { "void* domObject" }).Concat(argList));

          // Create argument list for C++ method invocation.
          argList = op.Args.Select(a => "(DOMString*)".If(a.Type.IsString) + "arg" + a.Index);
          var cppArgList = string.Join(", ", argList);

          Write(@"
${retType} ${iName}_${oName}(${cArgs})
{
  ScopedTimer timer(""Binding.${iName}.${oName}"");
".FormatWith(new { retType = op.RetType.AsCPPRet(),
                    iName = iface.Name,
                    oName = op.Name,
                    cArgs = cArgList }));

          if (op.IsUnsafe)
            Write(@"
  auto targetObject = static_cast<${Name}*>(domObject);
".FormatWith(iface));
          else
            Write(@"
  auto wrappedObject = static_cast<WrappedObject*>(domObject);
  auto targetObject = static_cast<${Name}*>(wrappedObject);
  AssertPointer(targetObject);
  ZDASSERT(targetObject == dynamic_cast<${Name}*>(wrappedObject),
     ""Dynamic and static cast do not return the same value - this is probably caused by multiple inheritance, which we do not allow"");
".FormatWith(iface));

          Write(@"

  try
  {
    return ${retConverter}(targetObject->${oName}(${cppArgs}));
  }
  catch (const WrappedException* err)
  {
    ZDLOG(""Caught C++-level exception; rethrowing at the C# layer..."");
    mono_raise_exception(convertToCSException(err));
    return ${defaultReturn}; // Avoid compiler warning about missing return; this line will never be executed.
  }
}

".FormatWith(new { retConverter = retConverter,
                   oName = op.Name,
                   cppArgs = cppArgList,
                   defaultReturn = "0".If(!op.RetType.IsVoid) }));
        }

        foreach (var attr in (from a in iface.Attributes where !a.IsEventHandler select a))
        {
          var getConverter = attr.GetterType.IsObject ? "convertToCSObject"
                           : attr.GetterType.IsString ? "convertToCSString"
                           : "";
          var setConverter = "(DOMString*)".If(attr.SetterType.IsString);

          Write(@"

${retType} ${iName}_${aName}Getter(void* domObject)
{
  ScopedTimer timer(""Binding.${iName}.${aName}Getter"");
  auto wrappedObject = static_cast<WrappedObject*>(domObject);
  auto targetObject = static_cast<${iName}*>(wrappedObject);
  AssertPointer(targetObject);
  ZDASSERT(targetObject == dynamic_cast<${iName}*>(wrappedObject),
     ""Dynamic and static cast do not return the same value - this is probably caused by multiple inheritance, which we do not allow"");

  try
  {
    return ${getConverter}(targetObject->${aName}());
  }
  catch (const WrappedException* err)
  {
    ZDLOG(""Caught C++-level exception; rethrowing at the C# layer..."");
    mono_raise_exception(convertToCSException(err));
    return 0; // Avoid compiler warning about missing return; this line will never be executed.
  }
}

".FormatWith(new { retType = attr.GetterType.AsCPPRet(),
                   iName = iface.Name,
                   aName = attr.Name,
                   getConverter = getConverter }));

          if (!attr.IsReadOnly)
            Write(@"
void ${iName}_${aName}Setter(void* domObject, ${argType} value)
{
  ScopedTimer timer(""Binding.${iName}.${aName}Setter"");
  auto wrappedObject = static_cast<WrappedObject*>(domObject);
  auto targetObject = static_cast<${iName}*>(wrappedObject);
  AssertPointer(targetObject);
  ZDASSERT(targetObject == dynamic_cast<${iName}*>(wrappedObject),
     ""Dynamic and static cast do not return the same value - this is probably caused by multiple inheritance, which we do not allow"");

  try
  {
    targetObject->${setter}(${setConverter}value);
  }
  catch (const WrappedException* err)
  {
    ZDLOG(""Caught C++-level exception; rethrowing at the C# layer..."");
    mono_raise_exception(convertToCSException(err));
  }
}

".FormatWith(new { iName = iface.Name,
                    aName = attr.Name,
                    argType = attr.SetterType.AsCPPArg(),
                    setter = "set" + attr.CapitalizedName,
                    setConverter = setConverter }));
        }
      }
    }
  }
}

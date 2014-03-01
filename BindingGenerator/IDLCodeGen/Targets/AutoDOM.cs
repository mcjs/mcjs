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
  class AutoDOM : IDLTarget
  {
    public override string Filename { get { return "AutoDOM.cs"; }  }

    protected override void Generate(Writer Write, IDLXML idl)
    {
      Write(@"
using System;
using System.Runtime.CompilerServices;

using mwr.Extensions;

namespace mwr.DOM
{
");

      foreach (var iface in (from i in idl.Interfaces where !i.IsPrivate select i))
      {
        Write(@"
public partial class ${Name} : ${Superclass}
{
  public ${Name}(IntPtr objPtr)
    : base(objPtr)
  {
    Initialize();
".FormatWith(iface));
        if (iface.IsList)
        {//Forcing ItemAccessor to be initialized if it is null to fix the ThreadStatic issue (TODO: A more elegant solution must be implement!)
            Write(@"
    if (ItemAccessor == null)
    {
        ItemAccessor = new mdr.PropertyDescriptor(null)
        {
            Getter = (mdr.PropertyDescriptor pd, mdr.DObject obj, ref mdr.DValue value) =>
            {
                value.Set(DOMBinder.CheckUndefined(Bindings.item(obj.AsDOMPtr(), (UInt32)pd.Index)));
            },
            Setter = (mdr.PropertyDescriptor pd, mdr.DObject obj, ref mdr.DValue value) =>
            {
                m.Util.Diagnose.Trace.Fail(""Cannot assign to the WrappedObject.item's elements"");
            },
        };
    }
");
        }
          Write(@"
  }

  partial void Initialize();

  public new partial class Internal : ${Superclass}.Internal {}
  public new partial class Bindings : ${Superclass}.Bindings {}

".FormatWith(iface));

        if (iface.IsList)
        {
          Write(@"
  // A PropertyDescriptor for this list's items; for now shared as an optimization.
  [ThreadStatic]
  static mdr.PropertyDescriptor ItemAccessor = null;

  // Override the GetPropertyDescriptor methods since this is a list type;
  // this will allow expressions of the form obj[10] to work as expected.
  public override mdr.PropertyDescriptor GetPropertyDescriptor(int field)
  {
    ItemAccessor.Index = field;
    return ItemAccessor;
  }

  public override mdr.PropertyDescriptor GetPropertyDescriptor(double field)
  {
    var intField = (int)field;

    if (field == intField) return GetPropertyDescriptor(intField);
    else                   return base.GetPropertyDescriptor(field);
  }

  public override mdr.PropertyDescriptor GetPropertyDescriptor(string field)
  {
    int i;
    if (int.TryParse(field, out i))
        return GetPropertyDescriptor(i);
    else
    {
#     if DEBUG
        var pd = Map.GetPropertyDescriptor(field);
        if (pd == null)
            m.Util.Diagnose.Debug.Warning("" #### The field {0} does not exist #### "", field);
#     endif

      return base.GetPropertyDescriptor(field);
    }
  }
");
        }
        Write(@"
  public static new void PreparePrototype(mdr.DObject prototype)
  {
     FillPrototype(prototype);
     CustomFillPrototype(prototype);
  }

  static partial void CustomFillPrototype(mdr.DObject prototype);

  private static void FillPrototype(mdr.DObject prototype)
  {
    mdr.Runtime.Instance.GetMapMetadataOfPrototype(prototype).Name = ""${Name}"";

".FormatWith(iface));

        foreach (var overloads in (from o in iface.Operations where !o.IsPrivate group o by o.Name into overloads select overloads))
        {
          Write(@"
    prototype.SetField(""${Name}"", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
    {
      switch (callFrame.PassedArgsCount)
      {
        default:
".FormatWith(overloads.First()));

          foreach (var op in (from o in overloads orderby o.Args.Count() descending select o))
          {
            var retSetPre = op.RetType.IsVoid ? ""
                          : op.RetType.IsNullable ? "callFrame.Return.SetNullable("
                          : "callFrame.Return.Set(";
            var retSetPost = ")".If(retSetPre != "");

            var argumentList = string.Join("", op.Args.Select(a => ", arg" + a.Index));

            Write(@"
        case ${argsCount}:
        {
          /* ${idl} */
          m.Util.Diagnose.Debug.WriteLine(""++$> calling ${iName}:${oName}(this${argList})"");
          callFrame.SetExpectedArgsCount(${argsCount});
".FormatWith(new { argsCount = op.Args.Count(),
                   idl = op.WebIDL,
                   iName = iface.Name,
                   oName = op.Name,
                   argList = argumentList }));

            if (op.IsRuntime)
              Write(@"
          ${Name}(ref callFrame);
".FormatWith(op));
            else
            {
              foreach (var arg in op.Args)
                if (arg.Type.AsJSArg().ToString() != "DOMPtr")
                {
                  Write(@"
          var arg${idx} = mjr.Operations.Convert.To${argType}.Run(ref ${callFrameArg});
".FormatWith(new
   {
     idx = arg.Index,
     callFrameArg = CallFrameArg(arg.Index),
     argType = arg.Type.AsJSArg()
   }));
                }
                else
                {
                  Write(@"
          var arg${idx} = ${callFrameArg}.As${argType}();
".FormatWith(new
 {
   idx = arg.Index,
   callFrameArg = CallFrameArg(arg.Index),
   argType = arg.Type.AsJSArg()
 }));
                }



              Write(@"
          ${retSetPre}Bindings.${oName}(callFrame.This.AsDOMPtr()${argList})${retSetPost};
".FormatWith(new { retSetPre = retSetPre,
                   oName = op.Name,
                   argList = argumentList,
                   retSetPost = retSetPost }));
            }

            Write(@"
          break;
        }
");
          }

          Write(@"
      }
    }));

");
        }

        foreach (var attr in iface.Attributes)
        {
          if (attr.IsEventHandler)
            Write(@"
    /* ${WebIDL} */
    prototype.DefineOwnProperty(""${Name}"", EventHandlerProperty.CreateProperty(""${Name}""));

".FormatWith(attr));
          else
          {
            Write(@"
    /* ${idl} */
    prototype.DefineOwnProperty(""${aName}"", new mdr.DProperty()
    {
      TargetValueType = mdr.ValueTypes.${aType},
".FormatWith(new { idl = attr.WebIDL,
                   aName = attr.Name,
                   aType = attr.GetterType.AsJSDynamic() }));

            if (!attr.IsReadOnly)
            {
              if (attr.SetterType.AsJSArg().ToString() != "DOMPtr")
              {
                Write(@"
      OnSetDValue = (mdr.DObject This, ref mdr.DValue v) => {
        var arg = mjr.Operations.Convert.To${aType}.Run(ref v);
        Bindings.${aName}Setter(This.AsDOMPtr(), arg);
      },
".FormatWith(new { aType = attr.SetterType.AsJSArg(), aName = attr.Name }));
              }
              else
              {
                Write(@"
      OnSetDValue = (mdr.DObject This, ref mdr.DValue v) => {
        var arg = v.As${aType}();
        Bindings.${aName}Setter(This.AsDOMPtr(), arg);
      },
".FormatWith(new { aType = attr.SetterType.AsJSArg(), aName = attr.Name }));
              }
            }


            Write(@"
      OnGetDValue = (mdr.DObject This, ref mdr.DValue v) => {
        v.Set${nullable}(Bindings.${aName}Getter(This.AsDOMPtr()));
      },
    }${jsProperties});

".FormatWith(new { 
                   aName = attr.Name,
                   nullable = "Nullable".If(attr.GetterType.IsNullable),
                   jsProperties = ", mdr.PropertyDescriptor.Attributes.NotWritable".If(attr.IsReadOnly) ,
                 }));
          }
        }

        foreach (var constant in iface.Constants)
          Write(@"
    /* ${WebIDL} */
    prototype.SetField(""${Name}"", ${Value});
".FormatWith(constant));

        Write(@"
  }
}; // end of ${Name}

".FormatWith(iface));
      }

      Write(@"
}
");
    }

    private string CallFrameArg(int index)
    {
      if (index < 4) return "callFrame.Arg{0}".Formatted(index);
      else           return "callFrame.Arguments[{0}]".Formatted(index - 4);
    }
  }
}

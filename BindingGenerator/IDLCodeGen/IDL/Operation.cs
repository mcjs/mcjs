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
using System;

namespace IDLCodeGen.IDL
{
  public class Operation : IDLElement
  {
    public string WebIDL { get; private set; }
    public string Name { get; private set; }
    public string CapitalizedName { get; private set; }
    public bool IsRuntime { get; private set; }
    public bool IsPrivate { get; private set; }
    public bool IsUnsafe { get; private set; }
    public bool DisableHooks { get; private set; }
    public IDLType RetType { get; private set; }
    public IEnumerable<Argument> Args { get; private set; }

    public Operation(XElement operation)
      : base(operation)
    {
      WebIDL = operation.Element("webidl").Value.Trim();
      Name = operation.Attribute("name").Value;
			CapitalizedName = char.ToUpper(Name[0]) + Name.Substring(1);
      IsRuntime = HasExtendedAttribute("Runtime");
      IsPrivate = HasExtendedAttribute("Private");
      IsUnsafe = HasExtendedAttribute("Unsafe");
      DisableHooks = HasExtendedAttribute("DisableHooks");
      RetType = new IDLType(operation.Element("Type"));
      Args = from a in Numbered(operation.Element("ArgumentList").Elements("Argument")) select new Argument(a.Item2, a.Item1);

      // Some sanity checks.
      if (IsRuntime && IsPrivate) throw new ArgumentException(String.Format("Operation {0} cannot be both Runtime and Private", Name));
      if (IsUnsafe && !IsPrivate) throw new ArgumentException(String.Format("Operation {0} cannot be Unsafe unless it is Private", Name));
      if (!IsPrivate && Args.Any(a => a.Type.ByRef || a.Type.IsPrivate))
        throw new ArgumentException(String.Format("Operation {0} cannot have ByRef or Private arguments unless it is Private", Name));
    }
  }
}

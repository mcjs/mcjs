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

namespace IDLCodeGen.IDL
{
  public class Interface : IDLElement
  {
    public string Name { get; private set; }
    public bool IsList { get; private set; }
    public bool IsPrivate { get; private set; }
    public bool IsRuntime { get; private set; }
    public string Superclass { get; private set; }
    public IEnumerable<Operation> Operations { get; private set; }
    public IEnumerable<Attribute> Attributes { get; private set; }
    public IEnumerable<Constant> Constants { get; private set; }

    public Interface(XElement iface)
      : base(iface)
    {
      Name = iface.Attribute("name").Value;
      IsList = HasExtendedAttribute("List");
      IsPrivate = HasExtendedAttribute("Private");
      IsRuntime = HasExtendedAttribute("Runtime");

      Superclass = iface.Element("InterfaceInheritance") != null
                 ? iface.Element("InterfaceInheritance").Element("Name").Attribute("name").Value
                 : "WrappedObject";

      Operations = from o in iface.Elements("Operation") select new Operation(o);
      Attributes = from a in iface.Elements("Attribute") select new Attribute(a);
      Constants = from c in iface.Elements("Const") select new Constant(c);
    }
  }
}

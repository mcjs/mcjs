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
  public class IDLType : IDLElement
  {
    public bool IsPrimitive { get; private set; }
    public bool IsObject { get; private set; }
    public bool IsString { get; private set; }
    public bool IsVoid { get; private set; }
    public bool IsNullable { get; private set; }
    public bool ByRef { get; private set; }
    public bool IsPrivate { get; private set; }
    public string Name { get; private set; }

    public IDLType(XElement type, IDLElement ownerElement = null)
      : base(type)
    {
      IsPrimitive = Elem.Attribute("type") != null;
      IsObject = Elem.Attribute("name") != null;
      IsString = IsPrimitive && Elem.Attribute("type").Value == "DOMString";
      IsVoid = IsPrimitive && Elem.Attribute("type").Value == "void";
      IsNullable = IsString || IsObject;
      Name = IsPrimitive ? Elem.Attribute("type").Value : Elem.Attribute("name").Value;

      // Some attributes on the owner element make sense as type annotations.
      ByRef = ownerElement != null ? ownerElement.HasExtendedAttribute("ByRef") : false;
      IsPrivate = ownerElement != null ? ownerElement.HasExtendedAttribute("Private") : false;
    }

#region Equality
    public bool Equals(IDLType other)
    {
      if (object.ReferenceEquals(other, null)) return false;
      else                                     return IsPrimitive == other.IsPrimitive && Name == other.Name;
    }

    public override bool Equals(object other) 
    {
      var o = other as IDLType;
      if (object.ReferenceEquals(o, null)) return false;
      else                                 return IsPrimitive == o.IsPrimitive && Name == o.Name;
    }

    public static bool operator ==(IDLType a, IDLType b)
    {
      if      (object.ReferenceEquals(a, b))                                       return true;
      else if (object.ReferenceEquals(a, null) || object.ReferenceEquals(b, null)) return false;
      else if (a.IsPrimitive == b.IsPrimitive && a.Name == b.Name)                 return true;
      else                                                                         return false;
    }

    public static bool operator !=(IDLType a, IDLType b)
    {
      return !(a == b);
    }

    public override int GetHashCode()
    {
      return IsPrimitive ? ~Name.GetHashCode() : Name.GetHashCode();
    }
#endregion
  }
}

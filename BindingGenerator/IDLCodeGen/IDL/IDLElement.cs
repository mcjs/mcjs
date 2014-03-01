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
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

namespace IDLCodeGen.IDL
{
}
public class IDLElement
{
  public XElement Elem { get; private set; }

  public IDLElement(XElement elem)
  {
    Elem = elem;
  }

  public IEnumerable<XElement> GetExtendedAttributes(string attrName)
  {
    if (Elem.Element("ExtendedAttributeList") != null)
    {
      return from attr in Elem.Element("ExtendedAttributeList").Elements("ExtendedAttribute")
             where attr.Attribute("name").Value == attrName
             select attr;
    }
    else
      return Enumerable.Empty<XElement>();
  }

  public XElement GetExtendedAttribute(string attrName)
  {
    if (!HasExtendedAttribute(attrName)) return null;
    else return GetExtendedAttributes(attrName).First();
  }

  public bool HasExtendedAttribute(string attrName)
  {
    return GetExtendedAttributes(attrName).Count() > 0;
  }

  public string GetExtendedAttributeValue(string attrName)
  {
    var attr = GetExtendedAttribute(attrName);
    if (attr != null) return (string)attr.Attribute("value");
    else return null;
  }

  // Returns an sequence of tuples, where the first item in the tuple is a 0-based index, and the second
  // is an item from the original sequence.
  protected static IEnumerable<Tuple<int, T>> Numbered<T>(IEnumerable<T> sequence)
  {
    return Enumerable.Range(0, sequence.Count()).Zip(sequence, Tuple.Create);
  }

}


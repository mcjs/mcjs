// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using m.Util.Diagnose;

namespace mjr
{
    public class JSPropertyNameEnumerator
    {
        mdr.DObject _dobject;
        mdr.PropertyMap _map;
        int _elementsIndex;
        string _current;
        System.Collections.Generic.HashSet<int> _visiteds = new System.Collections.Generic.HashSet<int>();
        System.Collections.Generic.LinkedList<string> _propNames = new System.Collections.Generic.LinkedList<string>();
        System.Collections.Generic.LinkedListNode<string> _currentNode;

        public JSPropertyNameEnumerator(mdr.DObject dobject)
        {
            _dobject = dobject;
            _elementsIndex = -1;
            _current = null;
        }
        public bool MoveNext()
        {
            // Debug.WriteLine("calling PropertyNameEnumerator.MoveNext");
            while (_dobject != null)
            {
                var array = _dobject as mdr.DArray;
                if (array != null)
                {
                    while (++_elementsIndex < array.ElementsLength)
                    {
                        if (!_visiteds.Contains(-_elementsIndex))
                        {
                            _visiteds.Add(-_elementsIndex);
                            _current = _elementsIndex.ToString();
                            return true;
                        }
                    }
                }

                /*
                ///Spec says the order of retreiving the properties does not matter, so the following code is faster
                ///However some stupid websites (e.g. BBC) count on the fact that Browsers list properties with a certain order
                ///So, we had to change to the slower to be browser compatible, rather than spec compatible
                if (_map == null)
                    _map = _dobject.Map;

                while (_map != null)
                {
                    var prop = _map.Property;
                    _map = _map.Parent;

                    if (!prop.IsNotEnumerable && !_visiteds.Contains(prop.NameId))
                    {
                        _visiteds.Add(prop.NameId);
                        _current = prop.Name;
                        return true;
                    }
                }
                _dobject = _dobject.Prototype;
                _elementsIndex = -1;
                _current = null;
                 */
                if (_currentNode == null)
                {
                    //We may have reached to the end of collected properties, but meanwhile some new ones may have been added
                    _propNames.Clear();
                    if (_map != null && _map != _dobject.Map)
                      _map = _dobject.Map;
                    for (var m = _dobject.Map; m != _map; m = m.Parent)
                    {
                        var prop = m.Property;
                        if (!prop.IsNotEnumerable && !_visiteds.Contains(prop.NameId))
                        {
                          _visiteds.Add(prop.NameId);
                          _propNames.AddFirst(prop.Name);
                        }
                    }
                    _map = _dobject.Map;
                    _currentNode = _propNames.First;

                    if (_currentNode == null)
                    {
                        _dobject = _dobject.Prototype;
                        _elementsIndex = -1;
                        _current = null;
                        _map = null;
                        continue; //Jump to begining of the loop!
                    }
                }
                _current = _currentNode.Value;
                _currentNode = _currentNode.Next;
                return true;
            }
            return false;
        }
        public string GetCurrent()
        {
            return _current;
        }
    }

}

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

namespace DOMBinding
{
    class DOMtoJSMap
    {
        //This is dynamic map that maps C++ objects to their corresponding wrappers on the JS side
        //public WrappedObject[] objectMap = new WrappedObject[50];
        public WeakReference[] objectMap = new WeakReference[50];

        //This is a list of free entries in the map
        private List<int> freeIndeces = new List<int>();

        public DOMtoJSMap()
        {
            for (int i = 0; i < objectMap.Length; i++)
                freeIndeces.Add(i);
            objectMap[0] = null;
            //index zero is considered invalid
        }

        private void EnlargeMap()
        {
            WeakReference[] newObjectMap = new WeakReference[objectMap.Length * 2];
            Array.Copy(objectMap, newObjectMap, objectMap.Length);
            for (int i = objectMap.Length; i < newObjectMap.Length; i++)
                freeIndeces.Add(i);
            objectMap = newObjectMap;
        }

        public int Add(WrappedObject dw)
        {
            if (freeIndeces.Count > 0) {
                int index = freeIndeces[0];
                System.Diagnostics.Debug.Assert(objectMap[index] == null);
                objectMap[index] = new WeakReference(dw);
                freeIndeces.RemoveAt(0);
                return index;
            } else {
                EnlargeMap();
                return (Add(dw));
            }
        }

        public void Release(int index)
        {
            System.Diagnostics.Debug.Assert(index < objectMap.Length);
            objectMap[index] = null;
            freeIndeces.Add(index);
        }
    }
}


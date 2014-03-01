// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
﻿using m.Util.Diagnose;

//#define DISABLE_CACHE

namespace mdr
{
    internal class PropertyCache
    {
        internal class LineItem
        {
            public PropertyDescriptor Property;
            public PropertyMap Map;
        }

        const int IndexBitWidth = 5;
        const int LineCount = (1 << IndexBitWidth);
        LineItem[] _lines = new LineItem[LineCount];

        const int BitStart = 0;
        const int BitMask = ~(-1 << IndexBitWidth);
        int GetIndex(int fieldId)
        {
            var index = (fieldId >> BitStart) & BitMask;
            Debug.Assert((index >= 0) && (index < LineCount), "Invalid situation {0} not in [0..{1})!", index, LineCount);
            return index;
        }
        public PropertyDescriptor Get(int fieldId, PropertyMap map)
        {
            var index = GetIndex(fieldId);
            var line = _lines[index];
            if (
                line == null
                || line.Map != map
                || line.Property.NameId != fieldId
                )
            {
                return null;
            }
            return line.Property;
        }
        public void Add(PropertyDescriptor property, PropertyMap map)
        {
            var index = GetIndex(property.NameId);
            var line = _lines[index];
            if (line == null)
                _lines[index] = line = new LineItem();
            line.Property = property;
            line.Map = map;
        }
    }
}

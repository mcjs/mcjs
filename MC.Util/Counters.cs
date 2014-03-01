// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
﻿using System.Collections.Generic;

namespace m.Util
{
    public class Counters
    {
        public class Counter
        {
            public string Name { get; private set; }
            public long Count;
            public string Notes { get; private set; }

            public Counter(string name, string notes = null)
            {
                Name = name;
                Notes = notes;
                Count = 0;
            }
        }

        readonly List<Counter> _counters;
        readonly string Name;

        public Counters(string name = null)
        {
            _counters = new List<Counter>();
            if (string.IsNullOrEmpty(name))
                Name = "Counters";
            else
                Name = name;
        }

        public void PrintCounters(System.IO.TextWriter output)
        {
            string counterTagName = "C";
            if (_counters == null || _counters.Count == 0)
                return;
            output.WriteLine("<{0}>", Name);
            for (var i = 0; i < _counters.Count; ++i)
            {
                var counter = _counters[i];
                output.Write(" <{0} n='{1}' c='{2}'", counterTagName, counter.Name, counter.Count);
                if (string.IsNullOrEmpty(counter.Notes))
                    output.WriteLine("/>");
                else
                    output.WriteLine(">{0}</{1}>", counter.Notes, counterTagName);
            }
            output.WriteLine("</{0}>", Name);
        }

        public Counter Add(string name, string notes = null)
        {
            int counterId;
            return Add(name, notes, out counterId);
        }
        public Counter Add(string name, string notes, out int counterId)
        {
            var counter = new Counter(name, notes);
            counterId = _counters.Count;
            _counters.Add(counter);
            return counter;
        }

        public Counter GetCounter(string name, string notes = null) { return GetCounter(FindId(name, notes, true)); }
        public Counter GetCounter(int counterId) { return _counters[counterId]; }
        public int FindId(string name, string notes = null, bool addIfMissing = true)
        {
            for (var i = 0; i < _counters.Count; ++i)
            {
                var counter = _counters[i];
                if (counter.Name == name && counter.Notes == notes)
                    return i;
            }
            if (addIfMissing)
            {
                int id;
                Add(name, notes, out id);
                return id;

            }
            else
                return -1;
        }
        public IEnumerable<Counter> Find(string name, string notes = null)
        {
            var counters = new List<Counter>();
            for (var i = 0; i < _counters.Count; ++i)
            {
                var counter = _counters[i];
                if (counter.Name == name && (counter.Notes == notes || notes == null))
                    counters.Add(counter);
            }
            return counters;
        }



    }
}

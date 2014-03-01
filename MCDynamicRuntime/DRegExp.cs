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
using System.Text.RegularExpressions;

using m.Util.Diagnose;

namespace mdr
{
    public class DRegExp : DObject
    {
        public Regex Value { get; private set; }
        public bool Global { get; private set; }
        public bool IgnoreCase { get; private set; }
        public bool Multiline { get; private set; }
        public int LastIndex { get; set; }
        public GroupCollection MatchedGroups { get; private set; }
        public string Source { get; private set; }

        public DRegExp(string pattern)
            : this(pattern, "")
        {
        }

        public DRegExp(string pattern, string flags)
            : base(Runtime.Instance.DRegExpMap)
        {
            var g = false;
            var i = false;
            var m = false;
            foreach (char c in flags)
            {
                switch (c)
                {
                    case 'g':
                        if (g) Trace.Fail("Syntax Error: more than one g flags were found");
                        g = true;
                        break;
                    case 'i':
                        if (i) Trace.Fail("Syntax Error: more than one i flags were found");
                        i = true;
                        break;
                    case 'm':
                        if (m) Trace.Fail("Syntax Error: more than one m flags were found");
                        m = true;
                        break;
                    default:
                        Trace.Fail("Syntax Error: Unknown regular expression flag was found");
                        break;
                }
            }

            // Save the original pattern, before any modifications, so that JavaScript sees what it expects.
            Source = pattern;

            // TODO: Is this still needed? If so, document why.
            if (pattern.StartsWith("^") && m)
                pattern = "(?!\r|\n|\r\n)" + pattern.Substring(1);

            RegexOptions options = RegexOptions.ECMAScript | RegexOptions.Compiled;

            Global = g;
            IgnoreCase = i;
            Multiline = m;
            if (m)
                options |= RegexOptions.Multiline;

            if (i)
                options |= RegexOptions.IgnoreCase;

            try
            {
              // In the common case we can just use the user-supplied pattern.
              Value = new Regex(pattern, options);
            }
            catch (ArgumentException)
            {
              // The Regex constructor failed, almost certainly because of inconsistencies between how JavaScript
              // behaves and how .NET behaves. We apply fixes for potential problems with the pattern here and then retry.
              // TODO: Find better solutions for these problems.

              // There seems to be a bug in .NET where \p, \P, and \k are interpreted as special, even
              // with RegexOptions.ECMAScript, though they shouldn't be. We strip them out to avoid that problem.
              // TODO: Remove this when it's no longer needed.
              // TODO: This almost certainly is not needed for Mono; make it conditional.
              pattern = Regex.Replace(pattern, @"\\(p|P|k)", @"$1", RegexOptions.Compiled);

              // Similar to the issue above, there's an incompatibility here between what JavaScript does
              // and what .NET does: \cX matches a control character if X is in [A-Za-z], but otherwise,
              // JavaScript silently ignores the \c and matching fails, while .NET raises an exception
              // when we try to create the Regex object. To work around this, for now we replace \c[^A-Za-z] with
              // \u0000, although this is obviously not a perfect fix.
              pattern = Regex.Replace(pattern, @"\\c([^A-Za-z]|$)", @"\\u0000$1", RegexOptions.Compiled);

              // Very similar issue to the above, but involving \uXXXX with missing XXXX and \xXX with missing XX.
              pattern = Regex.Replace(pattern, @"\\u(?![0-9a-fA-F][0-9a-fA-F][0-9a-fA-F][0-9a-fA-F])", @"u", RegexOptions.Compiled);
              pattern = Regex.Replace(pattern, @"\\x(?![0-9a-fA-F][0-9a-fA-F])", @"x", RegexOptions.Compiled);

              // Retry, hopefully now succeeding.
              Value = new Regex(pattern, options);
            }

            LastIndex = 0;
        }

        public override string ToString()
        {
            return "/" + Value.ToString() + "/" + (Global ? "g" : String.Empty) + (IgnoreCase ? "i" : String.Empty) + (Multiline ? "m" : String.Empty);
        }

        public Match MatchImplementation(string S)
        {
          //TODO: Check if a null S is ok, or a runtime problem
          if (S != null && Value != null)
          {
            int length = S.Length;
            int i = LastIndex;
            if (i >= 0 && i < length)
            {
              Debug.WriteLine("calling Value.Match Value {0} \n S {1} \n i {2}", Value, S, i);
              Match match = null;
#if SKIPREGEXEXP
            try
            {
#endif
              match = Value.Match(S, i);
#if SKIPREGEXEXP
            }
            catch (System.Exception)
            {
               Debug.WriteLine("Silencing a regex Match exception");
               return null;
            }
#endif
              if (match != null)
              {
                MatchedGroups = match.Groups;
                if (match.Success)
                {
                  if (Global)
                    LastIndex = match.Index + match.Length;

                  match.NextMatch();
                  return match;
                }
              }
            }
          }
          //else for all failed cases
          LastIndex = 0;
          return null;
        }

        public mdr.DObject ExecImplementation(string S)
        {
            if (S == null)
                return Runtime.Instance.DefaultDNull;
            Match match = MatchImplementation(S);
            if (match == null)
                return Runtime.Instance.DefaultDNull;

            int n = match.Captures.Count;
            mdr.DArray result = new mdr.DArray(n);
            result.SetField("index", match.Index);
            result.SetField("input", S);
            //result.SetField(0, match.Value);
            if (MatchedGroups != null)
            {
              for (int g = 0; g < MatchedGroups.Count; g++)
              {
                 if (MatchedGroups[g].Captures.Count > 0)
                 {
                    result.SetField(g, MatchedGroups[g].Captures[0].Value);
                 }
              }
            }
            return result;
        }

        public override string GetTypeOf() { return "RegExp"; }
    }
}

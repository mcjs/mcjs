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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace IDLCodeGen.Util
{
  static class StringExtensions
  {
    private static object Eval(object container, string expression)
    {
      //TODO:We can also add support for indexed information such as e[0]. In the advanced case can make this as complext as System.Web.UI.DataBinder.Eval
      try
      {
        var prop = container.GetType().GetProperty(expression);
        return prop.GetValue(container, null);
      }
      catch (Exception)
      {
        throw new ArgumentException(string.Format("Cannot retrieve property {0} from format object.", expression));
      }
    }

    public static string FormatWith(this string format, object source)
    {
      return FormatWith(format, null, source);
    }

    public static string FormatWith(this string format, IFormatProvider provider, object source)
    {
      if (format == null) throw new ArgumentNullException("format");

      var r = new Regex(@"(?<start>\$\{)(?<property>[\w\.\[\]]+)(?<format>:[^\}\s]+)?(?<end>\})",
                        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

      return r.Replace(format, delegate(Match m)
      {
        var propertyGroup = m.Groups["property"];
        var formatGroup = m.Groups["format"];
        return string.Format(provider, "{0" + formatGroup.Value + "}", Eval(source, propertyGroup.Value));
      });
    }

    public static string If(this string value, bool test)
    {
      return test ? value : "";
    }

    public static string Formatted(this string format, params object[] args)
    {
      return string.Format(format, args);
    }
  }
}

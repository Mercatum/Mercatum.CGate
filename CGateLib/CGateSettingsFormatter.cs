using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;


namespace Mercatum.CGate
{
    /// <summary>
    /// Formats settings into strings used as inputs in many CGate functions.
    /// </summary>
    internal static class CGateSettingsFormatter
    {
        public static string FormatKeyValuePairs(IDictionary<string, string> settings,
                                                 string valueSeparator = "=",
                                                 string parametersSeparator = ";")
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach( var item in settings )
            {
                if( stringBuilder.Length > 0 )
                    stringBuilder.Append(parametersSeparator);
                stringBuilder.Append(item.Key);
                stringBuilder.Append(valueSeparator);
                stringBuilder.Append(item.Value);
            }
            return stringBuilder.ToString();
        }
    }
}

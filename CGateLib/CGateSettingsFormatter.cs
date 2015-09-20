using System.Collections.Generic;
using System.Text;


namespace Mercatum.CGate
{
    /// <summary>
    /// Formats parameters into a string.
    /// </summary>
    internal static class CGateSettingsFormatter
    {
        public static string FormatParameters(IDictionary<string, string> parameters,
                                              string valueSeparator = "=",
                                              string parametersSeparator = ";")
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach( var parameter in parameters )
            {
                if( stringBuilder.Length > 0 )
                    stringBuilder.Append(parametersSeparator);
                stringBuilder.Append(parameter.Key);
                stringBuilder.Append(valueSeparator);
                stringBuilder.Append(parameter.Value);
            }
            return stringBuilder.ToString();
        }


        public static string FormatSchemeSource(SchemeSource schemeSource)
        {
            return string.Format("|FILE|{0}|{1}", schemeSource.Path, schemeSource.Section);
        }
    }
}

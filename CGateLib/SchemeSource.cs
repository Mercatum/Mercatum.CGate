using System;


namespace Mercatum.CGate
{
    public class SchemeSource
    {
        public string Path { get; private set; }

        public string Section { get; private set; }


        public SchemeSource(string path, string section)
        {
            if( string.IsNullOrEmpty(path) )
                throw new ArgumentOutOfRangeException("path", path, "Path should be specified");

            if( string.IsNullOrEmpty(section) )
                throw new ArgumentOutOfRangeException("section",
                                                      section,
                                                      "Section should be specified");

            Path = path;
            Section = section;
        }
    }
}

using System.Collections.Generic;

namespace Mercatum.CGateMonitor.Bridge
{
    internal class DataStreamSpec
    {
        private readonly List<DataTableSpec> _tables = new List<DataTableSpec>();

        public string StreamName { get; private set; }
        public string SchemaName { get; private set; }

        public IList<DataTableSpec> Tables
        {
            get { return _tables; }
        }

        public DataStreamSpec(string streamName,
                              string schemaName,
                              params string[] tables)
        {
            StreamName = streamName;
            SchemaName = schemaName;

            foreach( string tableName in tables )
                _tables.Add(new DataTableSpec(tableName));
        }
    }
}

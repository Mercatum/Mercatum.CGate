namespace Mercatum.CGateMonitor.Bridge
{
    class DataTableSpec
    {
        public string TableName { get; private set; }

        public DataTableSpec(string tableName)
        {
            TableName = tableName;
        }
    }
}

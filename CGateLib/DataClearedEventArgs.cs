using System;


namespace Mercatum.CGate
{
    public class DataClearedEventArgs : EventArgs
    {
        public string TableName { get; private set; }

        public long Revision { get; private set; }


        public DataClearedEventArgs(string tableName,
                                    long revision)
        {
            TableName = tableName;
            Revision = revision;
        }
    }
}
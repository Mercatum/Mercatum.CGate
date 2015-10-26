using System;

using ru.micexrts.cgate.message;


namespace Mercatum.CGate
{
    public class DataArrivedEventArgs : EventArgs
    {
        public string TableName { get; private set; }

        public long Revision { get; private set; }

        // TODO: replace with ChangeType
        public bool Inserted { get; private set; }

        public AbstractDataMessage Message { get; private set; }


        public DataArrivedEventArgs(string tableName,
                                    long revision,
                                    bool inserted,
                                    AbstractDataMessage message)
        {
            TableName = tableName;
            Revision = revision;
            Inserted = inserted;
            Message = message;
        }
    }
}
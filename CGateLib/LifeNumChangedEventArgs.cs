using System;


namespace Mercatum.CGate
{
    public class LifeNumChangedEventArgs : EventArgs
    {
        public uint LifeNum { get; private set; }


        public LifeNumChangedEventArgs(uint lifeNum)
        {
            LifeNum = lifeNum;
        }
    }
}
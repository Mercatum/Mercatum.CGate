namespace Mercatum.CGate
{
    public static class CGateEnvironment
    {
        private static bool _initialized = false;

        public static bool Initialized
        {
            get { return _initialized; }
        }

        public static void Init()
        {
            ru.micexrts.cgate.CGate.Open("ini=ini/cgate.ini;key=11111111");
            _initialized = true;
        }

        public static void Close()
        {
            ru.micexrts.cgate.CGate.Close();
            _initialized = false;
        }
    }
}

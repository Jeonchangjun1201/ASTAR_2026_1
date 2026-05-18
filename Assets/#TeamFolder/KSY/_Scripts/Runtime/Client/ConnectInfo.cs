namespace KSY.Clients
{
    public static class ConnectInfo
    {
        private static string _IpAddress = "127.0.0.1";
        public static string IPAddress => _IpAddress;
        private static int _port = 9696;
        public static int Port => _port;
    }
}


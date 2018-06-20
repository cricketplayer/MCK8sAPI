using System;
using System.Collections.Generic;

namespace MCK8sAPI
{
    public class Server
    {
        public Server()
        {
        }

        public string Name
        {
            get;
            set;
        }

        public List<Endpoints> Endpoints
        {
            get;
            set;
        }

    }

    public class Endpoints
    {
        public string Minecraft
        {
            get;
            set;
        }

        public string Rcon
        {
            get;
            set;
        }
    }
}

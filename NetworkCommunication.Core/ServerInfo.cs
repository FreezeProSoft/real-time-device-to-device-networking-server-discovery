using System;

namespace NetworkCommunication.Core
{   
    [Serializable]
    public class ServerInfo
    {
        public Guid id;

        public ServerInfoState started;

        public string name;

        public string address;
    }
    
}

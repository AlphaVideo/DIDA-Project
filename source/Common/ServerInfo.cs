using System.Xml.Linq;
using Grpc.Net.Client;

namespace Common
{
    public class ServerInfo
    {
        GrpcChannel _channel;
        string _address;

        public ServerInfo(string hostname, int port)
        {
            _channel = GrpcChannel.ForAddress("http://" + hostname + ":" + port);
            _address = "http://" + hostname + ":" + port;
        }

        public GrpcChannel GetChannel()
        {
            return _channel;
        }

        public override string ToString()
        {
            return _address;
        }
    }
}
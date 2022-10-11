using System.Xml.Linq;
using Grpc.Net.Client;

namespace Common
{
    public class ServerInfo
    {
        protected GrpcChannel _channel;
        protected string _address;

        public ServerInfo(string hostname, int port)
        {
            _address = "http://" + hostname + ":" + port;
            _channel = GrpcChannel.ForAddress(_address);
            
        }

        public ServerInfo(string url)
        {
            _address = url;
            _channel = GrpcChannel.ForAddress(url);
        }

        public GrpcChannel Channel => _channel;

        public string Address => _address;

        public override string ToString()
        {
            return _address;
        }
    }
}
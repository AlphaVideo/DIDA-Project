using Grpc.Net.Client;

namespace Common
{
    public class ServerInfo
    {
        GrpcChannel _channel;

        public ServerInfo(string hostname, int port)
        {
            _channel = GrpcChannel.ForAddress("http://" + hostname + ":" + port);
        }

        public GrpcChannel GetChannel()
        {
            return _channel;
        }
    }
}
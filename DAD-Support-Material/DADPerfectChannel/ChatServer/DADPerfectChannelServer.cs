using Grpc.Core;
using Grpc.Core.Interceptors;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DADPerfectChannel {
    // ChatServerService is the namespace defined in the protobuf
    // ChatServerServiceBase is the generated base implementation of the service
    public class DADPerfectChannelService : PerfectChannelService.PerfectChannelServiceBase {
        private Dictionary<string, string> clientMap = new Dictionary<string, string>();

        public DADPerfectChannelService() {
        }

        public override Task<PerfectChannelReply> Test(
            PerfectChannelRequest request, ServerCallContext context) {
            return Task.FromResult(Reg(request));
        }

        public PerfectChannelReply Reg(PerfectChannelRequest request) {
            
            lock (this) {
                Console.WriteLine($"Received request with message: {request.Message}");
            }
            return new PerfectChannelReply
            {
                Status = true
            };
        }
    }
    class DADPerfectChannelServer {
        static void Main(string[] args) {
            const int ServerPort = 1001;
            const string ServerHostname = "localhost";

            Server server = new Server
            {
                Services = { PerfectChannelService.BindService(new DADPerfectChannelService()).Intercept(new ServerInterceptor()) },
                Ports = { new ServerPort(ServerHostname, ServerPort, ServerCredentials.Insecure) }
            };
            server.Start();
            Console.WriteLine("ChatServer server listening on port " + ServerPort);
            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();

        }
    }
    public class ServerInterceptor : Interceptor {

        public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation) {
            string callId = context.RequestHeaders.GetValue("dad");
            Console.WriteLine("DAD header: " + callId);
            return base.UnaryServerHandler(request, context, continuation);
        }

    }
}


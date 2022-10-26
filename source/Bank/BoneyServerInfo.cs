using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class BoneyServerInfo : ServerInfo
    {
        private BoneyService.BoneyServiceClient _client;
        public BoneyServerInfo(string hostname, int port) : base(hostname, port)
        {
            _client = new BoneyService.BoneyServiceClient(_channel);
        }

        public BoneyServerInfo(string url) : base(url)
        {
            _client = new BoneyService.BoneyServiceClient(_channel);
        }

        public BoneyService.BoneyServiceClient Client => _client;
    }
}

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
    internal class BankServerInfo : ServerInfo
    {
        private BankService.BankServiceClient _client;
        public BankServerInfo(string hostname, int port) : base(hostname, port)
        {
            _client = new BankService.BankServiceClient(_channel);
        }

        public BankServerInfo(string url) : base(url)
        {
            _client = new BankService.BankServiceClient(_channel);
        }

        public BankService.BankServiceClient Client => _client;
    }
}

using Google.Protobuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{

    internal class PerfectChannel<TRequest, TResponse>
    {

        public delegate TResponse remote_call(TRequest request);

        private remote_call call;
        public PerfectChannel(remote_call call){
            this.call = call;
        }


        public TResponse send(string call_name, TRequest request)
        {
            while (true) {
                try
                {
                    TResponse response = call(request);

                    //if (((IMessage)response).)

                    return response;

                }
                catch (Exception) { continue; }
            }
        }


    }
}

using Google.Protobuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core.Interceptors;
using Grpc.Core;
using System.Linq.Expressions;

namespace Common
{

    public class PerfectChannel : Interceptor
    {
        private int _wait_time;
        private bool _frozen = false;
        private readonly object _lock = new object();
        private ManualResetEvent _frozen_lock = new ManualResetEvent(true);

        public PerfectChannel(int slotDuration) 
        {
            _wait_time = slotDuration/4;
        }

        public void Freeze() { 
            lock (_lock)
            {
                _frozen = true; 
                _frozen_lock.Reset();
            }
        }

        public void Unfreeze() {
            lock (_lock)
            {
                _frozen = false;
                _frozen_lock.Set();
            }
        }

        public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            lock (_lock)
            {
                if (_frozen)
                {
                    throw new RpcException(new Status(StatusCode.Unavailable, "Server Frozen!"));
                }
            }
            return continuation(request, context);
        }

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            _frozen_lock.WaitOne();

            while (true)
            {
                try
                {
                    return continuation(request, context);
                } catch (RpcException e)
                {
                    if (e.StatusCode != StatusCode.Unavailable)
                    {
                        throw e;
                    }

                    Thread.Sleep(_wait_time);
                }
            }
        }
    }
}

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
        private int _timeslotDuration;
        private bool _frozen = false;
        private readonly object _lock = new object();
        private Dictionary<string, int> _line_tickets = new Dictionary<string, int>();
        private Dictionary<string, int> _sent_tickets = new Dictionary<string, int>();
        private ManualResetEvent _frozen_lock = new ManualResetEvent(true);

        public PerfectChannel(int slotDuration) 
        {
            _timeslotDuration = slotDuration;
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

            int my_ticket = TicketAgent(context.Host);
            ValidateTicket(context.Host, my_ticket);

            while (true)
            {
                try
                {
                    TResponse response = continuation(request, context);
                    StashTicket(context.Host, my_ticket);
                    return response;
                } catch (RpcException e)
                {
                    if (e.StatusCode != StatusCode.Unavailable)
                    {
                        StashTicket(context.Host, my_ticket);
                        throw e;
                    }

                    Thread.Sleep(_timeslotDuration/2);
                }
            }
        }

        private int TicketAgent(string? host)
        {
            if (host is null) { host = "No_Host"; }

            lock (_line_tickets)
            {
                if (_line_tickets.ContainsKey(host)) return ++_line_tickets[host];
                _line_tickets[host] = 0;
            }

            lock (_sent_tickets) _sent_tickets[host] = 0;

            return 0;
        }

        private void ValidateTicket(string? host, int ticket)
        {
            if (host is null) { host = "No_Host"; }

            while (true)
            {
                lock (_sent_tickets)
                {
                    if (_sent_tickets[host] >= ticket) return;
                }

                Thread.Sleep(_timeslotDuration / 10);
            }
        }

        private void StashTicket(string? host, int ticket)
        {
            if (host is null) { host = "No_Host"; }

            lock (_sent_tickets) _sent_tickets[host]++;
        }
    }
}

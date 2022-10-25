using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bank.Services
{
    internal class PrimaryBackupServiceImpl : PrimaryBackupService.PrimaryBackupServiceBase
    {




        public override Task<PromiseReply> Promise(PromiseRequest request, ServerCallContext context)
        {
            PromiseReply reply = new();


            return Task.FromResult(reply);
        }

        public override Task<CommitReply> Commit(CommitRequest request, ServerCallContext context)
        {
            CommitReply reply = new();


            return Task.FromResult(reply);
        }
        public override Task<ListPendingReply> ListPending(ListPendingRequest request, ServerCallContext context)
        {
            ListPendingReply reply = new();


            return Task.FromResult(reply);
        }
    }
}

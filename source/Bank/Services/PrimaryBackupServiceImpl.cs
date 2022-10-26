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
        private PrimaryBackup _datacentre;
        internal PrimaryBackupServiceImpl(PrimaryBackup datacentre)
        {
            _datacentre = datacentre;
        }



        public override Task<PrepareReply> Prepare(PrepareRequest request, ServerCallContext context)
        {
            PrepareReply reply = new();
            
            // if sender is primary for current slot
            //     reply.ack = true
            // else
            //     reply.ack = false

            // 

            return Task.FromResult(reply);
        }

        public override Task<EmptyReply> Commit(CommitRequest request, ServerCallContext context)
        {
            // update operation database to include newly commited value
            // execute all possible operations (where seq number is the "next ao ultimo" executado)
            // by executing the operation, a reply message must be sent to the customer

            return Task.FromResult(new EmptyReply());
        }
        public override Task<ListPendingReply> ListPending(ListPendingRequest request, ServerCallContext context)
        {
            ListPendingReply reply = new();


            return Task.FromResult(reply);
        }
    }
}

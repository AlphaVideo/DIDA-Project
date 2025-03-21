﻿using Grpc.Core;
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
			reply.Ack = _datacentre.canPrepare(request.SenderPid, request.SeqNumber);

			return Task.FromResult(reply);
		}

		public override Task<EmptyReply> Commit(CommitRequest request, ServerCallContext context)
		{
			Console.WriteLine("[PrmBck] Received commit(cust={0}, msgId={1}, seq={2})", request.CustomerId, request.MsgId, request.SeqNumber);
			_datacentre.commitOperationLock(request.CustomerId, request.MsgId, request.SeqNumber);

			return Task.FromResult(new EmptyReply());
		}
		public override Task<ListPendingReply> ListPending(ListPendingRequest request, ServerCallContext context)
		{
			ListPendingReply reply = _datacentre.getNewerThan(request.LastSeqNum);

			return Task.FromResult(reply);
		}
	}
}

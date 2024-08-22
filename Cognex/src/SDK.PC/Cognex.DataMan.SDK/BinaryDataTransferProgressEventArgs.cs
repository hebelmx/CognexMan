using System;
using System.Runtime.CompilerServices;

namespace Cognex.DataMan.SDK
{
	public class BinaryDataTransferProgressEventArgs : EventArgs
	{
		public int BytesTransferred
		{
			get;
			private set;
		}

		public TransferDirection Direction
		{
			get;
			private set;
		}

		public int ResponseId
		{
			get;
			private set;
		}

		public ResultTypes ResultType
		{
			get;
			private set;
		}

		public int TotalDataSize
		{
			get;
			private set;
		}

		internal BinaryDataTransferProgressEventArgs(TransferDirection direction, int totalDataSize, int bytesTransferred, ResultTypes resultType, int responseId)
		{
			this.Direction = direction;
			this.TotalDataSize = totalDataSize;
			this.BytesTransferred = bytesTransferred;
			this.ResultType = resultType;
			this.ResponseId = responseId;
		}
	}
}
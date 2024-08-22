using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Cognex.DataMan.SDK
{
	internal class DmccMessage
	{
		public MemoryStream BinaryData
		{
			get;
			internal set;
		}

		public int CommandId
		{
			get;
			internal set;
		}

		public string PayLoad
		{
			get;
			internal set;
		}

		public int ResponseId
		{
			get;
			internal set;
		}

		public int ResponseStatusCode
		{
			get;
			internal set;
		}

		public DmccMessageStatusCodeRequestType StatusCodeRequestType
		{
			get;
			internal set;
		}

		public DmccMessageType Type
		{
			get;
			internal set;
		}

		public DmccMessage()
		{
			this.CommandId = -1;
			this.Type = DmccMessageType.Unknown;
			this.StatusCodeRequestType = DmccMessageStatusCodeRequestType.Unknown;
			this.ResponseId = -1;
			this.ResponseStatusCode = 0;
			this.PayLoad = "";
			this.BinaryData = new MemoryStream();
		}
	}
}
using System;
using System.Runtime.CompilerServices;

namespace Cognex.DataMan.SDK
{
	public class AutomaticResponseArrivedEventArgs : EventArgs
	{
		public byte[] Data
		{
			get;
			private set;
		}

		public ResultTypes DataType
		{
			get;
			private set;
		}

		public int ResponseId
		{
			get;
			private set;
		}

		internal AutomaticResponseArrivedEventArgs(int responseId, ResultTypes dataType, byte[] data)
		{
			this.ResponseId = responseId;
			this.DataType = dataType;
			this.Data = data;
		}
	}
}
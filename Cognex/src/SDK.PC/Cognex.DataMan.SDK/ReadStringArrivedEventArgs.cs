using System;
using System.Runtime.CompilerServices;

namespace Cognex.DataMan.SDK
{
	public class ReadStringArrivedEventArgs : EventArgs
	{
		public string ReadString
		{
			get;
			private set;
		}

		public int ResultId
		{
			get;
			private set;
		}

		internal ReadStringArrivedEventArgs(int resultId, string readString)
		{
			this.ResultId = resultId;
			this.ReadString = readString;
		}
	}
}
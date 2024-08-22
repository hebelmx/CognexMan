using System;
using System.Runtime.CompilerServices;

namespace Cognex.DataMan.SDK
{
	public class StatusEventArrivedEventArgs : EventArgs
	{
		public string StatusEventData
		{
			get;
			private set;
		}

		internal StatusEventArrivedEventArgs(string statusEventData)
		{
			this.StatusEventData = statusEventData;
		}
	}
}
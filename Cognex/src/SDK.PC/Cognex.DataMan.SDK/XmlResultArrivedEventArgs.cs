using System;
using System.Runtime.CompilerServices;

namespace Cognex.DataMan.SDK
{
	public class XmlResultArrivedEventArgs : EventArgs
	{
		public int ResultId
		{
			get;
			private set;
		}

		public string XmlResult
		{
			get;
			private set;
		}

		public XmlResultArrivedEventArgs()
		{
			this.ResultId = 0;
			this.XmlResult = null;
		}

		public XmlResultArrivedEventArgs(int resultId, string xmlResult)
		{
			this.ResultId = resultId;
			this.XmlResult = xmlResult;
		}
	}
}
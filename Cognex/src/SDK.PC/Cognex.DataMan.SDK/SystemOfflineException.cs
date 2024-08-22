using System;

namespace Cognex.DataMan.SDK
{
	public class SystemOfflineException : DataManException
	{
		public SystemOfflineException()
		{
		}

		public override string ToString()
		{
			return "System offline";
		}
	}
}
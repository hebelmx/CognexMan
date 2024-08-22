using System;

namespace Cognex.DataMan.SDK
{
	public class SystemDisconnectedException : DataManException
	{
		public SystemDisconnectedException()
		{
		}

		public override string ToString()
		{
			return "System disconnected";
		}
	}
}
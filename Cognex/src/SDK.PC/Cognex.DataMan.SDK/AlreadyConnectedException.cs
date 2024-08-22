using System;

namespace Cognex.DataMan.SDK
{
	public class AlreadyConnectedException : DataManException
	{
		public AlreadyConnectedException()
		{
		}

		public override string ToString()
		{
			return "Already connected";
		}
	}
}
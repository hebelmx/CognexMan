using System;
using System.Runtime.CompilerServices;

namespace Cognex.DataMan.SDK
{
	public class UnknownErrorException : DataManException
	{
		public string Command
		{
			get;
			private set;
		}

		public UnknownErrorException(string command)
		{
			this.Command = command;
		}

		public override string ToString()
		{
			return "Unknown exception";
		}
	}
}
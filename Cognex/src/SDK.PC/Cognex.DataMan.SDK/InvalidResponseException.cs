using System;
using System.Runtime.CompilerServices;

namespace Cognex.DataMan.SDK
{
	public class InvalidResponseException : DataManException
	{
		public string Command
		{
			get;
			private set;
		}

		public InvalidResponseException(string command)
		{
			this.Command = command;
		}

		public override string ToString()
		{
			return "Invalid response";
		}
	}
}
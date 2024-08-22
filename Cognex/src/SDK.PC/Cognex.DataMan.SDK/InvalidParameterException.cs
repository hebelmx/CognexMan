using System;
using System.Runtime.CompilerServices;

namespace Cognex.DataMan.SDK
{
	public class InvalidParameterException : DataManException
	{
		public string Command
		{
			get;
			private set;
		}

		public InvalidParameterException(string command)
		{
			this.Command = command;
		}

		public override string ToString()
		{
			return "Invalid parameter";
		}
	}
}
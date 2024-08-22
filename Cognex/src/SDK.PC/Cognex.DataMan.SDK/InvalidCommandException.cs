using System;
using System.Runtime.CompilerServices;

namespace Cognex.DataMan.SDK
{
	public class InvalidCommandException : DataManException
	{
		public string Command
		{
			get;
			private set;
		}

		public InvalidCommandException(string command)
		{
			this.Command = command;
		}

		public override string ToString()
		{
			return "Invalid command";
		}
	}
}
using System;
using System.Runtime.CompilerServices;

namespace Cognex.DataMan.SDK
{
	public class IncorrectChecksumException : DataManException
	{
		public string Command
		{
			get;
			private set;
		}

		public IncorrectChecksumException(string command)
		{
			this.Command = command;
		}

		public override string ToString()
		{
			return "Checksum incorrect";
		}
	}
}
using System;
using System.Runtime.CompilerServices;

namespace Cognex.DataMan.SDK
{
	public class OperationCanceledException : DataManException
	{
		public string Command
		{
			get;
			private set;
		}

		public OperationCanceledException(string command)
		{
			this.Command = command;
		}

		public override string ToString()
		{
			return "Operation canceled";
		}
	}
}
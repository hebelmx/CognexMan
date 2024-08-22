using System;
using System.Runtime.CompilerServices;

namespace Cognex.DataMan.SDK
{
	public class ParameterRejectedException : DataManException
	{
		public string Command
		{
			get;
			private set;
		}

		public ParameterRejectedException(string command)
		{
			this.Command = command;
		}

		public override string ToString()
		{
			return "Parameter rejected";
		}
	}
}
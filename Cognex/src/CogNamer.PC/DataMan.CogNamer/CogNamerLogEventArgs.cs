using System;
using System.Runtime.CompilerServices;

namespace Cognex.DataMan.CogNamer
{
	public class CogNamerLogEventArgs
	{
		public string Message
		{
			get;
			private set;
		}

		public CogNamerListener.LogType Type
		{
			get;
			private set;
		}

		public CogNamerLogEventArgs(CogNamerListener.LogType type, string message)
		{
			this.Type = type;
			this.Message = message;
		}
	}
}
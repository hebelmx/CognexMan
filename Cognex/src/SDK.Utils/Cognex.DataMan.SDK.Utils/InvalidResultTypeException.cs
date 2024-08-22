using Cognex.DataMan.SDK;
using System;
using System.Runtime.CompilerServices;

namespace Cognex.DataMan.SDK.Utils
{
	public class InvalidResultTypeException : DataManException
	{
		public string Reason
		{
			get;
			private set;
		}

		public InvalidResultTypeException(string reason)
		{
			this.Reason = reason;
		}

		public override string ToString()
		{
			return this.Reason;
		}
	}
}
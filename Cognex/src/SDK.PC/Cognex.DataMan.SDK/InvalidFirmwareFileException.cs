using System;
using System.Runtime.CompilerServices;

namespace Cognex.DataMan.SDK
{
	public class InvalidFirmwareFileException : DataManException
	{
		public string Reason
		{
			get;
			private set;
		}

		public InvalidFirmwareFileException(string reason)
		{
			this.Reason = reason;
		}

		public override string ToString()
		{
			return string.Format("The firmware is invalid, can't be uploaded to the device. Reason: {0}", this.Reason);
		}
	}
}
using System;
using System.Runtime.CompilerServices;

namespace Cognex.DataMan.SDK
{
	public class OffProtocolByteReceivedEventArgs : EventArgs
	{
		public byte Byte
		{
			get;
			private set;
		}

		internal OffProtocolByteReceivedEventArgs(byte offProtocolByte)
		{
			this.Byte = offProtocolByte;
		}
	}
}
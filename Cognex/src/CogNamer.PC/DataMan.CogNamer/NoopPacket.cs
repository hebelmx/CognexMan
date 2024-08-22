using System;

namespace Cognex.DataMan.CogNamer
{
	public class NoopPacket : CogNamerPacket
	{
		public NoopPacket() : this(FlagType.None, ErrorCode.Success)
		{
		}

		public NoopPacket(FlagType flags, ErrorCode errorCode) : base(CommandType.Noop, flags, errorCode)
		{
		}
	}
}
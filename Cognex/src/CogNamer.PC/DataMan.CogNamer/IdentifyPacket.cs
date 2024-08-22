using System;

namespace Cognex.DataMan.CogNamer
{
	public class IdentifyPacket : CogNamerPacket
	{
		public IdentifyPacket() : this(FlagType.None, ErrorCode.Success)
		{
		}

		public IdentifyPacket(FlagType flags, ErrorCode errorCode) : base(CommandType.Identify, flags, errorCode)
		{
		}
	}
}
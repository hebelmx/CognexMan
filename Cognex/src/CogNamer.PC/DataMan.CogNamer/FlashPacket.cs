using System;

namespace Cognex.DataMan.CogNamer
{
	public class FlashPacket : CogNamerPacket
	{
		public FlashPacket() : this(FlagType.None, ErrorCode.Success)
		{
		}

		public FlashPacket(FlagType flags, ErrorCode errorCode) : base(CommandType.Flash, flags, errorCode)
		{
		}
	}
}
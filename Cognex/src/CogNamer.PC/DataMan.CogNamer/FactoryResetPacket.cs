using System;

namespace Cognex.DataMan.CogNamer
{
	public class FactoryResetPacket : CogNamerPacket
	{
		public FactoryResetPacket() : this(FlagType.None, ErrorCode.Success)
		{
		}

		public FactoryResetPacket(FlagType flags, ErrorCode errorCode) : base(CommandType.FactoryReset, flags, errorCode)
		{
		}
	}
}
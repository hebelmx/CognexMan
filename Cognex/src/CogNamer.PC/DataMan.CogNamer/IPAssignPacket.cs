using System;

namespace Cognex.DataMan.CogNamer
{
	public class IPAssignPacket : CogNamerPacket
	{
		public IPAssignPacket() : this(FlagType.None, ErrorCode.Success)
		{
		}

		public IPAssignPacket(FlagType flags, ErrorCode errorCode) : base(CommandType.IPAssign, flags, errorCode)
		{
		}
	}
}
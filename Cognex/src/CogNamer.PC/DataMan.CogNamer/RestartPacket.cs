using System;

namespace Cognex.DataMan.CogNamer
{
	public class RestartPacket : CogNamerPacket
	{
		public RestartPacket() : base(CommandType.Restart, FlagType.None, ErrorCode.Success)
		{
		}

		public RestartPacket(FlagType flags, ErrorCode errorCode) : base(CommandType.Restart, flags, errorCode)
		{
		}
	}
}
using System;

namespace Cognex.DataMan.CogNamer
{
	public class ResetPasswordPacket : CogNamerPacket
	{
		public ResetPasswordPacket() : base(CommandType.ResetPassword, FlagType.None, ErrorCode.Success)
		{
		}

		public ResetPasswordPacket(FlagType flags, ErrorCode errorCode) : base(CommandType.ResetPassword, flags, errorCode)
		{
		}
	}
}
using System;

namespace Cognex.DataMan.CogNamer
{
	public class GetAttributePacket : CogNamerPacket
	{
		public GetAttributePacket() : this(FlagType.None, ErrorCode.Success)
		{
		}

		public GetAttributePacket(FlagType flags, ErrorCode errorCode) : base(CommandType.GetAttribute, flags, errorCode)
		{
		}
	}
}
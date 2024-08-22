using System;

namespace Cognex.DataMan.CogNamer
{
	public class QueryCachePacket : CogNamerPacket
	{
		public QueryCachePacket() : this(FlagType.None, ErrorCode.Success)
		{
		}

		public QueryCachePacket(FlagType flags, ErrorCode errorCode) : base(CommandType.QueryCache, flags, errorCode)
		{
		}
	}
}
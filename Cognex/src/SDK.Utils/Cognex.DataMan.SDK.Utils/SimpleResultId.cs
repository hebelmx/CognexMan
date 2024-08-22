using Cognex.DataMan.SDK;
using System;

namespace Cognex.DataMan.SDK.Utils
{
	public class SimpleResultId
	{
		public readonly ResultTypes Type;

		public readonly int Id;

		private readonly int _hashCode;

		public SimpleResultId(ResultTypes type, int id)
		{
			this.Type = type;
			this.Id = id;
			this._hashCode = this.Type.GetHashCode() ^ this.Id.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (!(obj is SimpleResultId))
			{
				return false;
			}
			return SimpleResultId.Equals((SimpleResultId)obj, this);
		}

		public static bool Equals(SimpleResultId id1, SimpleResultId id2)
		{
			if (id1.Id != id2.Id)
			{
				return false;
			}
			return id1.Type == id2.Type;
		}

		public override int GetHashCode()
		{
			return this._hashCode;
		}
	}
}
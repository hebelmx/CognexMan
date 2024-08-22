using Cognex.DataMan.SDK;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Cognex.DataMan.SDK.Utils
{
	public class SimpleResult
	{
		private string _dataAsString;

		public DateTime ArrivedAtUtc
		{
			get;
			private set;
		}

		public byte[] Data
		{
			get;
			private set;
		}

		public SimpleResultId Id
		{
			get;
			private set;
		}

		public bool IsArrived
		{
			get;
			private set;
		}

		public SimpleResult(SimpleResultId id)
		{
			this.Id = id;
			this.SetData(null, DateTime.MinValue);
		}

		public SimpleResult(ResultTypes type, int id) : this(new SimpleResultId(type, id))
		{
		}

		public SimpleResult(SimpleResultId id, byte[] data, DateTime arrivedAtUtc)
		{
			this.Id = id;
			this.SetData(data, arrivedAtUtc);
		}

		public string GetDataAsString()
		{
			if (this._dataAsString != null)
			{
				return this._dataAsString;
			}
			this._dataAsString = (this.Data != null ? Encoding.UTF8.GetString(this.Data, 0, (int)this.Data.Length) : "");
			return this._dataAsString;
		}

		public void SetData(byte[] data, DateTime arrivedAtUtc)
		{
			this.Data = data;
			this._dataAsString = null;
			if (data != null)
			{
				this.IsArrived = true;
				this.ArrivedAtUtc = arrivedAtUtc;
				return;
			}
			this.IsArrived = false;
			this.ArrivedAtUtc = DateTime.MinValue;
		}
	}
}
using System;

namespace Cognex.DataMan.SDK
{
	public interface ISystemConnector
	{
		DateTime LastOperationTime
		{
			get;
		}

		ILogger Logger
		{
			get;
			set;
		}

		ConnectionState State
		{
			get;
		}

		void Connect();

		void Connect(int timeout);

		bool Disconnect();

		int Read(byte[] buffer, int offset, int count, int timeout);

		bool Write(byte[] buffer, int offset, int count, int timeout);
	}
}
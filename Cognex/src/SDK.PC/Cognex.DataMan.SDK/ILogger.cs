using System;

namespace Cognex.DataMan.SDK
{
	public interface ILogger
	{
		bool Enabled
		{
			get;
			set;
		}

		int GetNextUniqueSessionId();

		void Log(string function, string message);

		void LogTraffic(int sessionId, bool isRead, byte[] buffer, int offset, int count);
	}
}
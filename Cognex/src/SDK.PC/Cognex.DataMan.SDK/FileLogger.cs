using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Cognex.DataMan.SDK
{
	public class FileLogger : ILogger, IDisposable
	{
		private static int _nextSessionId;

		private bool disposed;

		private FileStream logFile;

		public bool Enabled
		{
			get;
			set;
		}

		static FileLogger()
		{
		}

		public FileLogger(string logFileName)
		{
			this.logFile = new FileStream(logFileName, FileMode.Append, FileAccess.Write, FileShare.Read);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposingByUser)
		{
			if (!this.disposed)
			{
				this.disposed = true;
				if (disposingByUser)
				{
					this.logFile.Close();
					this.logFile = null;
				}
			}
		}

		~FileLogger()
		{
			this.Dispose(false);
		}

		public int GetNextUniqueSessionId()
		{
			return Interlocked.Increment(ref FileLogger._nextSessionId);
		}

		public void Log(string function, string message)
		{
			if (!this.Enabled)
			{
				return;
			}
			string str = "";
			try
			{
				str = string.Format("{0:X08}->{1}: {2}\r\n", Thread.CurrentThread.ManagedThreadId, function, message);
				byte[] bytes = Encoding.UTF8.GetBytes(str);
				this.logFile.Write(bytes, 0, (int)bytes.Length);
				this.logFile.Flush();
			}
			catch (Exception exception)
			{
			}
		}

		public void LogTraffic(int sessionId, bool isRead, byte[] buffer, int offset, int count)
		{
			if (!this.Enabled)
			{
				return;
			}
			string str = (isRead ? "read" : "written");
			try
			{
				StringBuilder stringBuilder = new StringBuilder();
				DateTime now = DateTime.Now;
				stringBuilder.Append(string.Format("{0}-----------------------------------------------------------\r\n", now.ToString("O")));
				stringBuilder.Append(string.Format("{0} bytes were {1}:\r\n", count, str));
				byte[] bytes = Encoding.UTF8.GetBytes(stringBuilder.ToString());
				this.logFile.Write(bytes, 0, (int)bytes.Length);
				this.WriteBytesAsPrintable(buffer, offset, count);
				this.logFile.WriteByte(13);
				this.logFile.WriteByte(10);
				this.logFile.Flush();
			}
			catch (Exception exception)
			{
			}
		}

		private void WriteBytesAsPrintable(byte[] buffer, int offset, int count)
		{
			if (buffer == null || count < 1 || offset + count > (int)buffer.Length)
			{
				return;
			}
			bool flag = false;
			int num = offset;
			while (num < (int)buffer.Length)
			{
				if (buffer[num] < 32 || buffer[num] >= 127)
				{
					flag = true;
					break;
				}
				else
				{
					num++;
				}
			}
			if (!flag)
			{
				this.logFile.Write(buffer, offset, count);
				return;
			}
			for (int i = offset; i < (int)buffer.Length && i < offset + count; i++)
			{
				if (buffer[i] < 32 || buffer[i] >= 127)
				{
					byte[] bytes = Encoding.UTF8.GetBytes(string.Format("<0x{0:X02}>", buffer[i]));
					this.logFile.Write(bytes, 0, (int)bytes.Length);
				}
				else
				{
					this.logFile.WriteByte(buffer[i]);
				}
			}
		}
	}
}
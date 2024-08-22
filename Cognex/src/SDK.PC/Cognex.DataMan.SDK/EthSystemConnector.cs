using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Cognex.DataMan.SDK
{
	public class EthSystemConnector : ISystemConnector, IDisposable
	{
		private TcpClient _tcpClient;

		private NetworkStream _stream;

		private DateTime _lastOperationTime = DateTime.MinValue;

		private ConnectionState _connectionState;

		private int _sessionId;

		public IPAddress Address
		{
			get;
			private set;
		}

		public DateTime LastOperationTime
		{
			get
			{
				return this._lastOperationTime;
			}
		}

		public ILogger Logger
		{
			get;
			set;
		}

		public string Password
		{
			get;
			set;
		}

		public int Port
		{
			get;
			private set;
		}

		public System.Net.Sockets.Socket Socket
		{
			get
			{
				if (this._tcpClient == null)
				{
					return null;
				}
				return this._tcpClient.Client;
			}
		}

		public ConnectionState State
		{
			get
			{
				return this._connectionState;
			}
		}

		public string UserName
		{
			get;
			set;
		}

		public EthSystemConnector(IPAddress address)
		{
			this.Address = address;
			this.Port = 23;
			this.UserName = "admin";
			this.Password = "";
		}

		public EthSystemConnector(IPAddress address, int port)
		{
			this.Address = address;
			this.Port = port;
			this.UserName = "admin";
			this.Password = "";
		}

		private bool Authenticate(string UserName, string Password, int timeout)
		{
			this.Log("EthSystemConnector.Authenticate", string.Format("Authentication started, UserName={0}, Password={1}, timeout={2}", UserName, (string.IsNullOrEmpty(Password) ? "[empty]" : "[not empty]"), timeout));
			byte[] numArray = new byte[1];
			string str = "";
			this.WriteString(string.Concat(UserName, "\r\n"), timeout);
			this.WriteString(string.Concat(Password, "\r\n"), timeout);
			this.WriteString("||:23456;1>SET DATA.RESULT-TYPE 0\r\n", timeout);
			DateTime dateTime = (timeout < 0 ? DateTime.MaxValue : DateTime.Now.AddMilliseconds((double)timeout));
			while (DateTime.Now.CompareTo(dateTime) < 0)
			{
				int num = this.Read(numArray, 0, 1, 500);
				if (num == 0)
				{
					continue;
				}
				if (num < 0)
				{
					break;
				}
				str = string.Concat(str, (char)numArray[0]);
				if (numArray[0] != 10)
				{
					continue;
				}
				if (-1 != str.IndexOf("Login failed"))
				{
					return false;
				}
				if (-1 == str.IndexOf("Login succeeded"))
				{
					return true;
				}
				while (DateTime.Now.CompareTo(dateTime) < 0)
				{
					num = this.Read(numArray, 0, 1, 500);
					if (num == 0)
					{
						continue;
					}
					if (num != 1)
					{
						return false;
					}
					if (numArray[0] != 10)
					{
						continue;
					}
					return true;
				}
			}
			return false;
		}

		public void Connect()
		{
			this.Connect(-1);
		}

		public void Connect(int timeout)
		{
			int nextUniqueSessionId;
			lock (this)
			{
				if (this._connectionState != ConnectionState.Connected)
				{
					this._connectionState = ConnectionState.Connecting;
					if (this.Logger != null)
					{
						nextUniqueSessionId = this.Logger.GetNextUniqueSessionId();
					}
					else
					{
						EthSystemConnector ethSystemConnector = this;
						int num = ethSystemConnector._sessionId;
						int num1 = num;
						ethSystemConnector._sessionId = num + 1;
						nextUniqueSessionId = num1;
					}
					this._sessionId = nextUniqueSessionId;
					this.Log("EthSystemConnector.Connect", string.Format("Connecting, timeout={0}...", (timeout < 0 ? "infinite" : string.Concat(timeout.ToString(), "ms"))));
					try
					{
						this.ConnectImpl(timeout);
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						this._connectionState = ConnectionState.Disconnected;
						this.DisconnectImpl();
						this.Log("EthSystemConnector.Connect", "Failed to connect.");
						throw exception;
					}
					this._connectionState = ConnectionState.Connected;
					this.Log("EthSystemConnector.Connect", "Succeeded.");
				}
			}
		}

		private void ConnectImpl(int timeout)
		{
			DateTime now = DateTime.Now;
			Exception socketException = null;
			this._tcpClient = new TcpClient();
			this._stream = null;
			if (timeout < 0)
			{
				try
				{
					this._tcpClient.Connect(this.Address, this.Port);
				}
				catch (Exception exception1)
				{
					socketException = exception1;
					if (socketException != null)
					{
						this.DisconnectImpl();
						throw socketException;
					}
					return;
				}
			}
			else if (!this._tcpClient.BeginConnect(this.Address, this.Port, (IAsyncResult p) => {
				try
				{
					this._tcpClient.EndConnect(p);
				}
				catch (Exception exception)
				{
					socketException = exception;
				}
			}, null).AsyncWaitHandle.WaitOne(timeout))
			{
				socketException = new SocketException(10060);
				if (socketException != null)
				{
					this.DisconnectImpl();
					throw socketException;
				}
				return;
			}
			else if (socketException != null)
			{
				if (socketException != null)
				{
					this.DisconnectImpl();
					throw socketException;
				}
				return;
			}
			try
			{
				this._stream = this._tcpClient.GetStream();
			}
			catch (Exception exception2)
			{
				socketException = exception2;
				if (socketException != null)
				{
					this.DisconnectImpl();
					throw socketException;
				}
				return;
			}
			this.Log("EthSystemConnector.ConnectImpl", string.Format("TCP connection opened to {0}:{1}", this.Address, this.Port));
			TimeSpan timeSpan = DateTime.Now - now;
			int num = Convert.ToInt32(timeSpan.TotalMilliseconds);
			int num1 = (timeout < 0 ? -1 : timeout - num);
			if (timeout >= 0 && num1 < 0)
			{
				socketException = new SocketException(10060);
			}
			else if (!this.Authenticate(this.UserName, this.Password, num1))
			{
				socketException = new LoginFailedException();
			}
			if (socketException != null)
			{
				this.DisconnectImpl();
				throw socketException;
			}
		}

		public bool Disconnect()
		{
			bool flag;
			lock (this)
			{
				if (this._connectionState == ConnectionState.Connected)
				{
					this._connectionState = ConnectionState.Disconnecting;
					this.Log("EthSystemConnector.Disconnect", "Disconnecting...");
					try
					{
						this.DisconnectImpl();
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						this.Log("EthSystemConnector.Disconnect", string.Concat("Exception occured: ", exception.ToString()));
					}
					this._connectionState = ConnectionState.Disconnected;
					this.Log("EthSystemConnector.Disconnect", "Succeeded.");
					return true;
				}
				else
				{
					flag = false;
				}
			}
			return flag;
		}

		private void DisconnectImpl()
		{
			if (this._stream != null)
			{
				this._stream.Close();
				this._stream.Dispose();
				this._stream = null;
			}
			if (this._tcpClient != null)
			{
				string str = this.Address.ToString();
				int port = this.Port;
				this.Log("EthSystemConnector", string.Format("Closing TCP connection to {0}:{1}", str, port.ToString()));
				this._tcpClient.Close();
				this._tcpClient = null;
			}
		}

		public void Dispose()
		{
			this.Disconnect();
			GC.SuppressFinalize(this);
		}

		~EthSystemConnector()
		{
			this.Dispose();
		}

		private void Log(string function, string message)
		{
			if (this.Logger != null && this.Logger.Enabled)
			{
				this.Logger.Log(function, message);
			}
		}

		private void LogTraffic(bool isRead, byte[] buffer, int offset, int count)
		{
			if (this.Logger != null && this.Logger.Enabled)
			{
				this.Logger.LogTraffic(this._sessionId, isRead, buffer, offset, count);
			}
		}

		public int Read(byte[] buffer, int offset, int count, int timeout)
		{
			int num;
			num = (timeout != -1 ? this.ReadImpl(buffer, offset, count, timeout) : this.ReadImpl(buffer, offset, count));
			if (num == 0 && (this._tcpClient == null || !this._tcpClient.Client.Connected))
			{
				num = -1;
			}
			if (num > 0)
			{
				this._lastOperationTime = DateTime.Now;
				this.LogTraffic(true, buffer, offset, num);
			}
			return num;
		}

		private int ReadImpl(byte[] buffer, int offset, int count)
		{
			int num;
			try
			{
				num = this._stream.Read(buffer, offset, count);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (this._connectionState != ConnectionState.Disconnected)
				{
					this.Log("EthSystemConnector.Read", string.Concat("Exception occured: ", exception.ToString()));
				}
				return -1;
			}
			return num;
		}

		private int ReadImpl(byte[] buffer, int offset, int count, int timeout)
		{
			int num;
			DateTime now = DateTime.Now + new TimeSpan(0, 0, 0, 0, timeout);
			this.Log("EthSystemConnector.Read", string.Concat("Reading with time-out ", timeout, "ms..."));
			try
			{
				while (DateTime.Now < now)
				{
					if (this._stream.DataAvailable)
					{
						num = this._stream.Read(buffer, offset, count);
						return num;
					}
					else
					{
						Thread.Sleep(10);
					}
				}
				this.Log("EthSystemConnector.Read", "Timed out.");
				num = 0;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (this._connectionState != ConnectionState.Disconnected)
				{
					this.Log("EthSystemConnector.Read", string.Concat("Exception occured: ", exception.ToString()));
				}
				return -1;
			}
			return num;
		}

		public bool Write(byte[] buffer, int offset, int count, int timeout)
		{
			bool flag;
			this.LogTraffic(false, buffer, offset, count);
			try
			{
				if (this._stream.CanTimeout)
				{
					this._stream.WriteTimeout = timeout;
				}
				this._stream.Write(buffer, offset, count);
				this._stream.Flush();
				flag = true;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				this.Log("EthSystemConnector.Write", string.Concat("Exception occured: ", exception.ToString()));
				return false;
			}
			return flag;
		}

		private bool WriteString(string text, int timeout)
		{
			byte[] bytes = Encoding.ASCII.GetBytes(text);
			return this.Write(bytes, 0, (int)bytes.Length, timeout);
		}
	}
}
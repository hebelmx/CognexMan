using System;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Cognex.DataMan.SDK
{
	public class SerSystemConnector : ISystemConnector, IDisposable
	{
		private SerialPort _serialPort;

		private DateTime _lastOperationTime = DateTime.MinValue;

		private ConnectionState _connectionState;

		private int _sessionId;

		public int Baudrate
		{
			get;
			set;
		}

		public int DataBits
		{
			get;
			set;
		}

		public System.IO.Ports.Handshake Handshake
		{
			get;
			set;
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

		public System.IO.Ports.Parity Parity
		{
			get;
			set;
		}

		public string PortName
		{
			get;
			set;
		}

		public ConnectionState State
		{
			get
			{
				return this._connectionState;
			}
		}

		public System.IO.Ports.StopBits StopBits
		{
			get;
			set;
		}

		public SerSystemConnector(string portName) : this(portName, 115200)
		{
		}

		public SerSystemConnector(string portName, int baudrate) : this(portName, baudrate, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One, System.IO.Ports.Handshake.None)
		{
		}

		public SerSystemConnector(string portName, int baudrate, System.IO.Ports.Parity parity, int dataBits, System.IO.Ports.StopBits stopBits, System.IO.Ports.Handshake handshake)
		{
			this.PortName = portName;
			this.Baudrate = baudrate;
			this.Parity = parity;
			this.DataBits = dataBits;
			this.StopBits = stopBits;
			this.Handshake = handshake;
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
						SerSystemConnector serSystemConnector = this;
						int num = serSystemConnector._sessionId;
						int num1 = num;
						serSystemConnector._sessionId = num + 1;
						nextUniqueSessionId = num1;
					}
					this._sessionId = nextUniqueSessionId;
					this.Log("SerSystemConnector.Connect", "Connecting...");
					try
					{
						this.ConnectImpl(timeout);
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						this._connectionState = ConnectionState.Disconnected;
						this.DisconnectImpl();
						this.Log("SerSystemConnector.Connect", "Failed to connect.");
						throw exception;
					}
					this._connectionState = ConnectionState.Connected;
					this.Log("SerSystemConnector.Connect", "Succeeded.");
				}
			}
		}

		private void ConnectImpl(int timeout)
		{
			if (this._serialPort != null)
			{
				throw new AlreadyConnectedException();
			}
			SerialPort serialPort = new SerialPort()
			{
				PortName = this.PortName,
				BaudRate = this.Baudrate,
				Parity = this.Parity,
				DataBits = this.DataBits,
				StopBits = this.StopBits,
				Handshake = this.Handshake,
				DtrEnable = true,
				ReadBufferSize = 368640,
				ReadTimeout = -1
			};
			serialPort.Open();
			this.Log("SerSystemConnector.ConnectImpl", string.Format("Opened serial port {0}", this.PortName));
			this._serialPort = serialPort;
		}

		public bool Disconnect()
		{
			bool flag;
			lock (this)
			{
				if (this._connectionState == ConnectionState.Connected)
				{
					this._connectionState = ConnectionState.Disconnecting;
					this.Log("SerSystemConnector.Disconnect", "Disconnecting...");
					try
					{
						this.DisconnectImpl();
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						this.Log("SerSystemConnector.Disconnect", string.Concat("Exception occured: ", exception.ToString()));
					}
					this._connectionState = ConnectionState.Disconnected;
					this.Log("SerSystemConnector.Disconnect", "Succeeded.");
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
			if (this._serialPort != null)
			{
				this.Log("SerSystemConnector.DisconnectImpl", string.Format("Closing serial port {0}", this._serialPort.PortName));
				this._serialPort.Close();
				this._serialPort = null;
			}
		}

		public void Dispose()
		{
			this.Disconnect();
		}

		~SerSystemConnector()
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
			if (num > 0)
			{
				this._lastOperationTime = DateTime.Now;
				this.LogTraffic(true, buffer, offset, num);
			}
			return num;
		}

		private int ReadImpl(byte[] buffer, int offset, int count)
		{
			try
			{
				while (this._serialPort.IsOpen)
				{
					Thread.Sleep(1);
					if (this._serialPort.BytesToRead != 0)
					{
						return this._serialPort.Read(buffer, offset, count);
					}
					else
					{
						Thread.Sleep(10);
					}
				}
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				this.Log("SerSystemConnector.Read", string.Concat("Exception occured: ", exception.ToString()));
			}
			return -1;
		}

		private int ReadImpl(byte[] buffer, int offset, int count, int timeout)
		{
			int num;
			DateTime now = DateTime.Now + new TimeSpan(0, 0, 0, 0, timeout);
			this.Log("SerSystemConnector.Read", string.Concat("Reading with time-out ", timeout, "ms..."));
			try
			{
				while (this._serialPort.IsOpen && DateTime.Now < now)
				{
					if (this._serialPort.BytesToRead != 0)
					{
						num = this._serialPort.Read(buffer, offset, count);
						return num;
					}
					else
					{
						Thread.Sleep(10);
					}
				}
				this.Log("SerSystemConnector.Read", "Timed out.");
				num = 0;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				this.Log("SerSystemConnector.Read", string.Concat("Exception occured: ", exception.ToString()));
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
				this._serialPort.WriteTimeout = timeout;
				this._serialPort.Write(buffer, offset, count);
				flag = true;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				this.Log("SerSystemConnector.Write", string.Concat("Exception occured: ", exception.ToString()));
				return false;
			}
			return flag;
		}
	}
}
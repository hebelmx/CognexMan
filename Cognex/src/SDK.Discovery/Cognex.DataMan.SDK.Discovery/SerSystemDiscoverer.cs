using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Cognex.DataMan.SDK.Discovery
{
	public class SerSystemDiscoverer : IDisposable
	{
		private const int _magicDmccCommandId = 23456;

		private Thread _scannerThread;

		private bool _isScannerThreadAlive;

		private bool _exitThread;

		public bool IsDiscoveryInProgress
		{
			get
			{
				return this._isScannerThreadAlive;
			}
		}

		public SerSystemDiscoverer()
		{
		}

		public void Discover()
		{
			if (!this._isScannerThreadAlive)
			{
				this._isScannerThreadAlive = true;
				this._exitThread = false;
				this._scannerThread = new Thread(new ThreadStart(this.ScanSerialPortsThreadFunc))
				{
					Name = "Serial Port Scanner Thread",
					IsBackground = true
				};
				this._scannerThread.Start();
			}
		}

		public void Dispose()
		{
			this.DoDispose(true);
			GC.SuppressFinalize(this);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		private void DoDispose(bool disposing)
		{
			if (this._isScannerThreadAlive)
			{
				this._isScannerThreadAlive = false;
				this._exitThread = true;
				this._scannerThread.Join();
				this._scannerThread = null;
			}
		}

		~SerSystemDiscoverer()
		{
			this.DoDispose(false);
		}

		private void Log(string message)
		{
		}

		private void LogSend(SerialPort serial_port, string msg)
		{
			this.Log(string.Format("Serial Discoverer Scanner Thread: {0}: sending '{1}'", serial_port.PortName, msg.TrimEnd(new char[0])));
		}

		private void ScanSerialPortsThreadFunc()
		{
			bool flag;
			try
			{
				try
				{
					this.Log("Serial Discoverer: Scanner Thread starts");
					RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("Hardware\\DeviceMap\\SerialComm");
					if (registryKey != null)
					{
						string[] valueNames = registryKey.GetValueNames();
						SerialPort serialPort = new SerialPort()
						{
							DtrEnable = true,
							ReadBufferSize = 4096,
							DataBits = 8,
							StopBits = StopBits.One,
							Handshake = Handshake.None,
							Parity = Parity.None,
							ReadTimeout = 250,
							WriteTimeout = 250
						};
						string[] strArrays = valueNames;
						for (int i = 0; i < (int)strArrays.Length; i++)
						{
							serialPort.PortName = registryKey.GetValue(strArrays[i]) as string;
							int[] numArray = new int[] { 115200, 57600, 38400, 19200, 9600 };
							int num = 0;
							while (num < (int)numArray.Length)
							{
								int num1 = numArray[num];
								bool flag1 = false;
								string str = null;
								string str1 = null;
								string str2 = null;
								int num2 = 0;
								string str3 = null;
								if (!this._exitThread)
								{
									try
									{
										serialPort.BaudRate = num1;
										serialPort.Open();
										Thread.Sleep(50);
										try
										{
											try
											{
												this.Log(string.Format("Serial Discoverer Scanner Thread: trying to connect to {0} at {1} Baud", serialPort.PortName, serialPort.BaudRate));
												str3 = this.SendDmccWaitResponse(serialPort, "SET DATA.RESULT-TYPE 0", out flag);
												str3 = this.SendDmccWaitResponse(serialPort, "GET DEVICE.SERIAL-NUMBER", out flag);
												flag1 = flag;
												if (flag)
												{
													str = str3;
												}
												str3 = this.SendDmccWaitResponse(serialPort, "GET DEVICE.NAME", out flag);
												if (flag)
												{
													str1 = str3;
												}
												str3 = this.SendDmccWaitResponse(serialPort, "GET DEVICE.TYPE", out flag);
												if (flag)
												{
													str2 = str3;
												}
												try
												{
													str3 = this.SendDmccWaitResponse(serialPort, "GET DEVICE.ID", out flag);
													if (flag)
													{
														string str4 = str3;
														if (!string.IsNullOrEmpty(str4))
														{
															num2 = int.Parse(str4);
														}
													}
												}
												catch
												{
												}
											}
											catch
											{
												flag1 = false;
											}
										}
										finally
										{
											serialPort.Close();
										}
										if (flag1)
										{
											this.Log(string.Format("Serial Discoverer Scanner Thread: device '{0}' discovered on {1} at {2}Baud", str1, serialPort.PortName, serialPort.BaudRate));
											try
											{
												SerSystemDiscoverer.SystemDiscoveredHandler systemDiscoveredHandler = this.SystemDiscovered;
												if (systemDiscoveredHandler != null)
												{
													systemDiscoveredHandler(new SerSystemDiscoverer.SystemInfo(serialPort.PortName, serialPort.BaudRate, num2, str2, str, str1));
												}
											}
											catch (Exception exception1)
											{
												Exception exception = exception1;
												this.Log(string.Format("Serial Discoverer Scanner Thread: error raising SystemDiscovered event for device '{0}' on {1}: {2}", str1, serialPort.PortName, exception.Message));
											}
											break;
										}
									}
									catch (UnauthorizedAccessException unauthorizedAccessException)
									{
										this.Log(string.Format("Serial Discoverer Scanner Thread: could not discover port: {0}", unauthorizedAccessException.Message));
										break;
									}
									catch (Exception exception2)
									{
										this.Log(string.Format("Serial Discoverer Scanner Thread: unexpected error occured: {0}", exception2.Message));
									}
									num++;
								}
								else
								{
									return;
								}
							}
						}
						this.Log("Serial Discoverer: Scanner Thread finished.");
					}
					else
					{
						return;
					}
				}
				catch (Exception exception3)
				{
					this.Log(string.Format("Serial Discoverer: Scanner Thread quits abnormally: {0}", exception3.Message));
				}
			}
			finally
			{
				this._isScannerThreadAlive = false;
			}
		}

		private string SendDmccWaitResponse(SerialPort serialPort, string command, out bool succeeded)
		{
			string str;
			string str1;
			succeeded = false;
			object[] objArray = new object[] { "||:", 23456, ";1>", command, "\r\n" };
			string str2 = string.Concat(objArray);
			serialPort.WriteLine(str2);
			this.LogSend(serialPort, str2);
			do
			{
				str = serialPort.ReadLine();
				this.Log(string.Format("Serial Discoverer Scanner Thread: {0}: received '{1}'", serialPort.PortName, str.TrimEnd(new char[0])));
			}
			while (!str.StartsWith("||") || !str.Contains(string.Concat(":", 23456)));
			int num = str.IndexOf('[');
			if (num < 0)
			{
				return str;
			}
			string str3 = "";
			int num1 = num + 1;
			while (num1 < str.Length)
			{
				char chr = str[num1];
				if (!char.IsDigit(chr))
				{
					if (chr != ']')
					{
						return str;
					}
					str = str.Substring(num1 + 1).TrimEnd(new char[0]);
					break;
				}
				else
				{
					str3 = string.Concat(str3, chr);
					num1++;
				}
			}
			try
			{
				int num2 = int.Parse(str3);
				if (num2 == 0)
				{
					succeeded = true;
					return str;
				}
				this.Log(string.Format("Serial Discoverer Scanner Thread: command failed with status code {0}: '{1}'", num2, str2.TrimEnd(new char[0])));
				return str;
			}
			catch
			{
				str1 = str;
			}
			return str1;
		}

		public event SerSystemDiscoverer.SystemDiscoveredHandler SystemDiscovered;

		public delegate void SystemDiscoveredHandler(SerSystemDiscoverer.SystemInfo systemInfo);

		public class SystemInfo
		{
			public int Baudrate
			{
				get;
				private set;
			}

			public int DeviceTypeId
			{
				get;
				private set;
			}

			public string Name
			{
				get;
				private set;
			}

			public string PortName
			{
				get;
				private set;
			}

			public string SerialNumber
			{
				get;
				private set;
			}

			public string Type
			{
				get;
				private set;
			}

			internal SystemInfo(string portName, int baudrate, int deviceTypeId, string type, string serialNumber, string name)
			{
				this.PortName = portName;
				this.Baudrate = baudrate;
				this.DeviceTypeId = deviceTypeId;
				this.Type = type;
				this.SerialNumber = serialNumber;
				this.Name = name;
			}

			public override string ToString()
			{
				return this.PortName;
			}
		}
	}
}
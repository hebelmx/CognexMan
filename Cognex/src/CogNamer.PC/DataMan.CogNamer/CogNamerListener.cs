using Cognex.DataMan.CogNamer.PlatformHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Cognex.DataMan.CogNamer
{
	public class CogNamerListener : IDisposable
	{
		public const int COGNAMER_PORT = 1069;

		public const int COGNAMER_ALTERNATIVE_PORT = 51069;

		private const int _listeningResolutionMs = 500;

		private const int _delayIfNoNetworkIntfFound = 500;

		private CogNamerListener.State _state;

		private object _stateLock;

		private List<CogNamerListenerSocket> _listeningSockets;

		private object _refreshLock;

		private ThreadStarter _refresherThread;

		private ThreadStarter _listenerThread;

		private ThreadStarter _dispatcherThread;

		private CancellationTokenSource _cancellation;

		private BlockingCollection<CogNamerPacketArrivedEventArgs> _packetArrivedEvents;

		private Dictionary<int, int> _devicesToDetect;

		private bool _recreateListeningSockets;

		private Cognex.DataMan.CogNamer.NetworkInterfaceDetector _networkInterfaceDetector;

		private bool _isDisposed;

		private int _isDiscoveryInProgress;

		private int _lastPacketProcessorPurgeTime;

		private LinkedList<CogNamerListener.PacketProcessor> _packetProcessors;

		private Dictionary<IPAddress, CogNamerListener.RecentlyDiscoveredDevice> _recentlyDiscoveredDevices;

		public bool Active
		{
			get
			{
				if (this._state == CogNamerListener.State.Started)
				{
					return true;
				}
				return this._state == CogNamerListener.State.Starting;
			}
			set
			{
				if (value == this.Active)
				{
					return;
				}
				if (value)
				{
					this.Start();
					return;
				}
				this.Stop(true);
			}
		}

		public bool DiscoverMisconfiguredDevices
		{
			get;
			set;
		}

		public bool IsDiscoveryInProgress
		{
			get
			{
				return this._isDiscoveryInProgress == 1;
			}
		}

		public Cognex.DataMan.CogNamer.NetworkInterfaceDetector NetworkInterfaceDetector
		{
			get
			{
				return this._networkInterfaceDetector;
			}
		}

		public bool ReportIdentifyRequests
		{
			get;
			set;
		}

		public CogNamerListener() : this(new List<int>())
		{
		}

		public CogNamerListener(IEnumerable<int> devicesToDetect)
		{
			this._isDisposed = false;
			this._isDiscoveryInProgress = 0;
			this._state = CogNamerListener.State.Stopped;
			this._stateLock = new object();
			this._refreshLock = new object();
			this.DiscoverMisconfiguredDevices = false;
			this.ReportIdentifyRequests = false;
			this._listeningSockets = new List<CogNamerListenerSocket>();
			this._packetArrivedEvents = new BlockingCollection<CogNamerPacketArrivedEventArgs>();
			this._devicesToDetect = new Dictionary<int, int>();
			this._networkInterfaceDetector = new Cognex.DataMan.CogNamer.NetworkInterfaceDetector();
			this._lastPacketProcessorPurgeTime = 0;
			this._packetProcessors = new LinkedList<CogNamerListener.PacketProcessor>();
			this._recentlyDiscoveredDevices = new Dictionary<IPAddress, CogNamerListener.RecentlyDiscoveredDevice>();
			foreach (int num in devicesToDetect)
			{
				this._devicesToDetect[num] = num;
			}
		}

		private void ClearPendingData()
		{
			CogNamerPacketArrivedEventArgs cogNamerPacketArrivedEventArg;
			while (this._packetArrivedEvents.TryDequeue(out cogNamerPacketArrivedEventArg))
			{
			}
		}

		private void CloseAndDisposeListeningSockets(bool disposeManagedObjects)
		{
			lock (this._listeningSockets)
			{
				if (disposeManagedObjects)
				{
					for (int i = 0; i < this._listeningSockets.Count; i++)
					{
						this._listeningSockets[i].CloseAndDisposeSocket();
					}
				}
				this._listeningSockets.Clear();
			}
		}

		private void CogNamerDispatcherThreadFunc(object obj)
		{
			try
			{
				this.Log(CogNamerListener.LogType.Debug, "Dispatcher Thread starts");
				CancellationToken token = this._cancellation.Token;
				while (true)
				{
					CogNamerPacketArrivedEventArgs cogNamerPacketArrivedEventArg = this._packetArrivedEvents.DequeueWithCancellation(token);
					CogNamerPacketArrivedEventArgs cogNamerPacketArrivedEventArg1 = cogNamerPacketArrivedEventArg;
					if (cogNamerPacketArrivedEventArg == null)
					{
						break;
					}
					try
					{
						CogNamerPacketArrivedEventHandler cogNamerPacketArrivedEventHandler = this.CogNamerPacketArrived;
						if (cogNamerPacketArrivedEventHandler != null)
						{
							cogNamerPacketArrivedEventHandler(this, cogNamerPacketArrivedEventArg1);
						}
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						this.Log(CogNamerListener.LogType.Error, string.Format("Dispatcher Thread error: {0}", exception.Message));
					}
				}
				this.Log(CogNamerListener.LogType.Debug, "Dispatcher Thread finished.");
			}
			catch (Exception exception3)
			{
				Exception exception2 = exception3;
				this.Log(CogNamerListener.LogType.Error, string.Format("Dispatcher Thread quits abnormally: {0}", exception2.Message));
			}
		}

		private void CogNamerListenerThreadFunc(object obj)
		{
			CogNamerListenerSocket cogNamerListenerSocket;
			try
			{
				this.Log(CogNamerListener.LogType.Debug, "Listener Thread starts");
				while (!this._cancellation.IsCancellationRequested)
				{
					try
					{
						this.CreateSenderReceiverSockets();
						this._recreateListeningSockets = false;
						Dictionary<int, CogNamerListenerSocket> nums = new Dictionary<int, CogNamerListenerSocket>();
						while (!this._recreateListeningSockets && !this._cancellation.IsCancellationRequested)
						{
							List<Socket> sockets = new List<Socket>();
							List<Socket> sockets1 = new List<Socket>();
							lock (this._listeningSockets)
							{
								foreach (CogNamerListenerSocket _listeningSocket in this._listeningSockets)
								{
									Socket socket = _listeningSocket.Socket;
									if (socket == null)
									{
										continue;
									}
									sockets.Add(socket);
									sockets1.Add(socket);
									nums[socket.GetHashCode()] = _listeningSocket;
								}
								if (sockets.Count < 1 || sockets1.Count < 1)
								{
									this._recreateListeningSockets = true;
									Thread.Sleep(500);
									continue;
								}
							}
							Socket.Select(sockets, null, sockets1, 500000);
							if (this._cancellation.IsCancellationRequested)
							{
								break;
							}
							foreach (Socket socket1 in sockets)
							{
								string str = "<unknown>";
								try
								{
									str = socket1.LocalEndPoint.ToString();
								}
								catch
								{
								}
								try
								{
									if (nums.TryGetValue(socket1.GetHashCode(), out cogNamerListenerSocket) && cogNamerListenerSocket != null)
									{
										int num = 0;
										while (cogNamerListenerSocket.Socket != null && socket1.Available > 0)
										{
											this.ReadFromSocketAndProcessPacket(cogNamerListenerSocket);
											num++;
										}
									}
								}
								catch (Exception exception1)
								{
									Exception exception = exception1;
									this.Log(CogNamerListener.LogType.Error, string.Format("Listener Thread: error reading data from socket {0}: {1} -> recreating listening sockets", str, exception.Message));
									this._recreateListeningSockets = true;
								}
							}
							if (sockets1.Count > 0)
							{
								this._recreateListeningSockets = true;
							}
							this.UnregisterExpiredPacketProcessors();
						}
					}
					catch (ThreadAbortException threadAbortException)
					{
						throw;
					}
					catch (Exception exception3)
					{
						Exception exception2 = exception3;
						this.Log(CogNamerListener.LogType.Error, string.Format("Listener Thread error: {0}", exception2.Message));
					}
				}
				this.Log(CogNamerListener.LogType.Debug, "Listener Thread finished.");
			}
			catch (Exception exception5)
			{
				Exception exception4 = exception5;
				this.Log(CogNamerListener.LogType.Error, string.Format("Listener Thread quits abnormally: {0}", exception4.Message));
			}
		}

		private IdentifyPacket CreateIdentifyPacket(FlagType flags)
		{
			IdentifyPacket identifyPacket = new IdentifyPacket(flags, ErrorCode.Success);
			lock (this._recentlyDiscoveredDevices)
			{
				if (this._recentlyDiscoveredDevices.Count > 0)
				{
					Dictionary<IPAddress, string> pAddresses = new Dictionary<IPAddress, string>();
					foreach (KeyValuePair<IPAddress, CogNamerListener.RecentlyDiscoveredDevice> _recentlyDiscoveredDevice in this._recentlyDiscoveredDevices)
					{
						if (pAddresses.ContainsKey(_recentlyDiscoveredDevice.Key))
						{
							continue;
						}
						pAddresses[_recentlyDiscoveredDevice.Key] = _recentlyDiscoveredDevice.Value.HostName;
					}
					identifyPacket.AddRecord(new KnownSystemsRecord(pAddresses));
				}
			}
			return identifyPacket;
		}

		private CogNamerPacket CreateIpAssignPacket(IPAddress currentDeviceIp, PhysicalAddress deviceMac, string username, string password, string hostname, bool useDhcp, IPAddress newDeviceIp, IPAddress subnet, IPAddress gateway, IPAddress dns, string domainName)
		{
			CogNamerPacket cogNamerPacket = new CogNamerPacket(CommandType.IPAssign, FlagType.None, ErrorCode.Success);
			cogNamerPacket.AddRecord(new MacAddressRecord(deviceMac));
			if (username != null && username != string.Empty)
			{
				cogNamerPacket.AddRecord(new CredentialsRecord(username, password));
			}
			cogNamerPacket.AddRecord(new HostNameRecord(hostname));
			if (newDeviceIp == null)
			{
				newDeviceIp = IPAddress.Any;
			}
			if (currentDeviceIp == null)
			{
				currentDeviceIp = IPAddress.Any;
			}
			if (dns == null)
			{
				dns = IPAddress.Any;
			}
			if (gateway == null)
			{
				gateway = IPAddress.Any;
			}
			if (domainName == null)
			{
				domainName = string.Empty;
			}
			bool flag = false;
			if (useDhcp)
			{
				NetworkSettingsRecord networkSettingsRecord = new NetworkSettingsRecord(useDhcp, flag, subnet, gateway, dns, domainName)
				{
					SkipSerializingDetailsForDhcp = true
				};
				cogNamerPacket.AddRecord(networkSettingsRecord);
			}
			else
			{
				cogNamerPacket.AddRecord(new IPAddressRecord(newDeviceIp));
				cogNamerPacket.AddRecord(new NetworkSettingsRecord(useDhcp, flag, subnet, gateway, dns, domainName));
			}
			return cogNamerPacket;
		}

		private CogNamerPacket CreateRestartPacket(PhysicalAddress deviceMac, string username, string password)
		{
			CogNamerPacket cogNamerPacket = new CogNamerPacket(CommandType.Restart, FlagType.None, ErrorCode.Success);
			cogNamerPacket.AddRecord(new MacAddressRecord(deviceMac));
			if (!string.IsNullOrEmpty(username))
			{
				cogNamerPacket.AddRecord(new CredentialsRecord(username, password));
			}
			return cogNamerPacket;
		}

		private void CreateSenderReceiverSockets()
		{
			lock (this._listeningSockets)
			{
				this.CloseAndDisposeListeningSockets(true);
				Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				SocketPortable.SetSocketOptionsForGlobalSenderReceiver(socket);
				socket.Bind(new IPEndPoint(IPAddress.Any, 0));
				SocketPortable.DisableUdpConnectionReset(socket);
				this._listeningSockets.Add(new CogNamerListenerSocket(CogNamerListenerType.GlobalSenderReceiver, socket));
				this.Log(CogNamerListener.LogType.Info, string.Format("Created sender/receiver socket for {0}", socket.LocalEndPoint.ToString()));
				foreach (NetworkInterfaceInfo availableInterface in this._networkInterfaceDetector.AvailableInterfaces)
				{
					IPEndPoint pEndPoint = new IPEndPoint(availableInterface.IPAddress, 1069);
					Socket socket1 = SocketPortable.CreateListenerSocket();
					socket1.Bind(pEndPoint);
					SocketPortable.DisableUdpConnectionReset(socket1);
					this._listeningSockets.Add(new CogNamerListenerSocket(CogNamerListenerType.InterfaceBroadcastReceiver, socket1));
					this.Log(CogNamerListener.LogType.Info, string.Format("Created broadcast receiver socket for {0}", pEndPoint.ToString()));
				}
			}
		}

		[Conditional("ENABLE_COGNAMER_DEBUG_LOG")]
		private void DebugLog(string message)
		{
			this.Log(CogNamerListener.LogType.Debug, message);
		}

		public static CogNamerDeviceScope DetermineDeviceScope(CogNamerPacket packet, IPEndPoint senderEndPoint, IPAddress networkInterfaceIpAddress, IPAddress networkInterfaceNetMask)
		{
			if (packet != null)
			{
				NetworkSettingsRecord networkSettingsRecord = packet.FindRecord(RecordType.NetworkSettings) as NetworkSettingsRecord;
				if (networkSettingsRecord == null)
				{
					return CogNamerDeviceScope.Unknown;
				}
				IPAddress pAddress = packet.ExtractIPAddress();
				if (networkSettingsRecord != null && pAddress != IPAddress.None)
				{
					IPAddress subNetMask = networkSettingsRecord.SubNetMask;
					if (!networkInterfaceIpAddress.Equals(IPAddress.None) && !networkInterfaceNetMask.Equals(IPAddress.None))
					{
						if (CogNamerListener.IsDeviceInSubnet(senderEndPoint.Address, subNetMask, networkInterfaceIpAddress, networkInterfaceNetMask))
						{
							return CogNamerDeviceScope.SubnetLocal;
						}
						if (EnumUtils.HasFlag(packet.Flags, FlagType.Broadcast))
						{
							return CogNamerDeviceScope.LinkLocal;
						}
						List<KeyValuePair<IPAddress, IPAddress>> keyValuePairs = new List<KeyValuePair<IPAddress, IPAddress>>();
						if (CogNamerListener.IsDeviceInSubnets(senderEndPoint.Address, subNetMask, keyValuePairs))
						{
							return CogNamerDeviceScope.RemoteSubnet;
						}
					}
					if (networkSettingsRecord.IsDhcpBased && CogNamerListener.IsLinkLocalAddress(pAddress))
					{
						return CogNamerDeviceScope.LinkLocal;
					}
					if (packet is BootupPacket)
					{
						return CogNamerDeviceScope.Unknown;
					}
					return CogNamerDeviceScope.RemoteSubnet;
				}
			}
			return CogNamerDeviceScope.Unknown;
		}

		private static CogNamerListener.ResolveMode DetermineResolveMode(IPAddress deviceAddress)
		{
			if (deviceAddress.Equals(IPAddress.Broadcast))
			{
				return CogNamerListener.ResolveMode.LocalBroadcast;
			}
			if (deviceAddress.Equals(IPAddress.Any))
			{
				return CogNamerListener.ResolveMode.SubnetBroadcast;
			}
			return CogNamerListener.ResolveMode.Unicast;
		}

		public void Dispose()
		{
			this.DoDispose(true);
			GC.SuppressFinalize(this);
		}

		private void DoDispose(bool disposeManagedObjects)
		{
			if (this._isDisposed)
			{
				return;
			}
			this._isDisposed = true;
			this.Stop(disposeManagedObjects);
		}

		~CogNamerListener()
		{
			this.DoDispose(false);
		}

		private string GetPacketDataForDebug(CogNamerPacket packet)
		{
			return "{}";
		}

		private CogNamerListenerSocket GetUnicastSenderReceiverSocket()
		{
			CogNamerListenerSocket cogNamerListenerSocket;
			lock (this._listeningSockets)
			{
				int num = 0;
				while (num < 2)
				{
					if (this._listeningSockets.Count <= 0)
					{
						this.CreateSenderReceiverSockets();
						num++;
					}
					else
					{
						CogNamerListenerSocket item = this._listeningSockets[0];
						if (item.Type != CogNamerListenerType.GlobalSenderReceiver)
						{
							cogNamerListenerSocket = null;
							return cogNamerListenerSocket;
						}
						else
						{
							cogNamerListenerSocket = item;
							return cogNamerListenerSocket;
						}
					}
				}
				cogNamerListenerSocket = null;
			}
			return cogNamerListenerSocket;
		}

		public static bool IsDeviceInSubnet(IPAddress deviceAddress, IPAddress deviceNetMask, IPAddress networkInterfaceIpAddress, IPAddress networkInterfaceNetMask)
		{
			if (!deviceNetMask.Equals(networkInterfaceNetMask))
			{
				return false;
			}
			if (deviceAddress.Equals(IPAddress.Any) || IPAddress.IsLoopback(deviceAddress))
			{
				return true;
			}
			uint num = BitConverter.ToUInt32(deviceAddress.GetAddressBytes(), 0);
			uint num1 = BitConverter.ToUInt32(deviceNetMask.GetAddressBytes(), 0);
			uint num2 = BitConverter.ToUInt32(networkInterfaceIpAddress.GetAddressBytes(), 0);
			uint num3 = BitConverter.ToUInt32(networkInterfaceNetMask.GetAddressBytes(), 0);
			return (num & num1) == (num2 & num3);
		}

		public static bool IsDeviceInSubnets(IPAddress deviceAddress, IPAddress deviceNetMask, List<KeyValuePair<IPAddress, IPAddress>> networkInterfaces)
		{
			bool flag;
			List<KeyValuePair<IPAddress, IPAddress>>.Enumerator enumerator = networkInterfaces.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					KeyValuePair<IPAddress, IPAddress> current = enumerator.Current;
					if (!CogNamerListener.IsDeviceInSubnet(deviceAddress, deviceNetMask, current.Key, current.Value))
					{
						continue;
					}
					flag = true;
					return flag;
				}
				return false;
			}
			finally
			{
				((IDisposable)enumerator).Dispose();
			}
			return flag;
		}

		public bool IsDirectedPacket(CogNamerPacket packet)
		{
			bool flag;
			if (packet != null)
			{
				MacAddressRecord macAddressRecord = packet.FindRecord(RecordType.MACAddress) as MacAddressRecord;
				if (macAddressRecord != null)
				{
					long macAddressLong = macAddressRecord.MacAddressLong;
					List<NetworkInterfaceInfo>.Enumerator enumerator = this._networkInterfaceDetector.AvailableInterfaces.GetEnumerator();
					try
					{
						while (enumerator.MoveNext())
						{
							if (enumerator.Current.MacAddress != macAddressLong)
							{
								continue;
							}
							flag = true;
							return flag;
						}
						return false;
					}
					finally
					{
						((IDisposable)enumerator).Dispose();
					}
					return flag;
				}
			}
			return false;
		}

		private bool IsEventEnabledForDeviceType(int cogNamerDeviceType)
		{
			if (cogNamerDeviceType == -1)
			{
				return false;
			}
			if (this._devicesToDetect.Count == 0)
			{
				return true;
			}
			return this._devicesToDetect.ContainsKey(cogNamerDeviceType);
		}

		public static bool IsLinkLocalAddress(IPAddress address)
		{
			if (address != null)
			{
				byte[] addressBytes = address.GetAddressBytes();
				if (addressBytes != null && (int)addressBytes.Length == 4)
				{
					if (addressBytes[0] != 169)
					{
						return false;
					}
					return addressBytes[1] == 254;
				}
			}
			return false;
		}

		private void Log(CogNamerListener.LogType type, string message)
		{
			CogNamerLogEventHandler cogNamerLogEventHandler = this.OnNewLogEntry;
			if (cogNamerLogEventHandler != null)
			{
				cogNamerLogEventHandler(this, new CogNamerLogEventArgs(type, message));
			}
		}

		private void OnNetworkInterfacePresenceChanged(object sender, EventArgs e)
		{
			this._recreateListeningSockets = true;
			NetworkInterfacePresenceChangedHandler networkInterfacePresenceChangedHandler = this.NetworkInterfacePresenceChanged;
			if (networkInterfacePresenceChangedHandler != null)
			{
				networkInterfacePresenceChangedHandler(this, e);
			}
			this.Refresh();
		}

		public void ProcessPacket(IPEndPoint remote_endpoint, int networkInterfaceIndex, byte[] buffer, int bytes_received)
		{
			if (remote_endpoint.Address == IPAddress.Any)
			{
				this.Log(CogNamerListener.LogType.Warning, string.Format("ProcessSocket: Received {0} bytes from invalid remote end point {1}. Ignoring data.", bytes_received, remote_endpoint.ToString()));
				return;
			}
			NetworkInterfaceInfo networkInterfaceInfo = null;
			IPAddress none = IPAddress.None;
			if (networkInterfaceIndex >= 0)
			{
				networkInterfaceInfo = this._networkInterfaceDetector.FindInterfaceByIndex(networkInterfaceIndex);
				if (networkInterfaceInfo != null)
				{
					IPAddress pAddress = networkInterfaceInfo.IPAddress;
				}
				else
				{
					this.Log(CogNamerListener.LogType.Warning, string.Format("ProcessSocket: Received {0} bytes from {1}, but its network interface could not be determined.", bytes_received, remote_endpoint.ToString()));
				}
			}
			try
			{
				CogNamerPacket cogNamerPacket = CogNamerPacket.CreateFromPacketBytes(buffer, bytes_received);
				if (cogNamerPacket == null)
				{
					this.Log(CogNamerListener.LogType.Warning, string.Format("ProcessSocket: Received packet of {0} bytes from {1} could not be parsed to a Cognamer packet. Ignoring data.", bytes_received, remote_endpoint.ToString()));
				}
				else if (cogNamerPacket.Command != CommandType.Identify || this.ReportIdentifyRequests)
				{
					CogNamerPacketArrivedEventArgs cogNamerPacketArrivedEventArg = new CogNamerPacketArrivedEventArgs(cogNamerPacket, remote_endpoint, networkInterfaceInfo);
					this.RunPacketProcessors(cogNamerPacketArrivedEventArg);
					int num = cogNamerPacket.ExtractCogNamerDeviceType();
					if (!this.ReportIdentifyRequests)
					{
						if (num == -1)
						{
							return;
						}
						if (!this.IsEventEnabledForDeviceType(num))
						{
							return;
						}
					}
					this.StoreAsRecentlyDiscoveredDevice(cogNamerPacketArrivedEventArg);
					this._packetArrivedEvents.Add(cogNamerPacketArrivedEventArg, this._cancellation.Token);
					return;
				}
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				this.Log(CogNamerListener.LogType.Warning, string.Format("ProcessSocket: Received packet of {0} bytes from {1} could not be parsed to a Cognamer packet. Ignoring data. ({2})", bytes_received, remote_endpoint.ToString(), exception.Message));
			}
		}

		private void ReadFromSocketAndProcessPacket(CogNamerListenerSocket cognamerSocket)
		{
			int num;
			IPEndPoint pEndPoint;
			if (cognamerSocket == null || cognamerSocket.Socket == null)
			{
				return;
			}
			byte[] numArray = new byte[1024];
			int num1 = SocketPortable.Receive(cognamerSocket.Socket, numArray, out pEndPoint, out num);
			this.ProcessPacket(pEndPoint, num, numArray, num1);
		}

		public void Refresh()
		{
			this.Refresh(true, this.DiscoverMisconfiguredDevices);
		}

		public bool Refresh(bool performSubnetBroadcast, bool performLimitedBroadcast)
		{
			bool flag;
			if (this._isDisposed)
			{
				this.Log(CogNamerListener.LogType.Error, string.Format("Refresh skipped, as listener is disposed", new object[0]));
				return false;
			}
			if (!this.Active)
			{
				this.Log(CogNamerListener.LogType.Error, string.Format("Refresh skipped, as listener is not Active", new object[0]));
				return false;
			}
			if (Interlocked.CompareExchange(ref this._isDiscoveryInProgress, 1, 0) != 0)
			{
				this.Log(CogNamerListener.LogType.Warning, string.Format("Refresh skipped, as discovery is in progress", new object[0]));
				return false;
			}
			lock (this._recentlyDiscoveredDevices)
			{
				this._recentlyDiscoveredDevices.Clear();
			}
			lock (this._refreshLock)
			{
				if (this._refresherThread == null)
				{
					this.Log(CogNamerListener.LogType.Debug, string.Format("Initiating refresh (performSubnetBroadcast={0}, performLimitedBroadcast={1})", (performSubnetBroadcast ? "yes" : "no"), (performLimitedBroadcast ? "yes" : "no")));
					this._refresherThread = new ThreadStarter(new ParameterizedThreadStart(this.RefresherThreadFunc), false, "CogNamer Refresher Thread", new CogNamerListener.RefreshParams(performSubnetBroadcast, performLimitedBroadcast));
					this._refresherThread.Start();
					return true;
				}
				else
				{
					this.Log(CogNamerListener.LogType.Warning, string.Format("Refresh Thread could not be started: still running", new object[0]));
					flag = false;
				}
			}
			return flag;
		}

		private void RefresherThreadFunc(object obj)
		{
			try
			{
				try
				{
					this.Log(CogNamerListener.LogType.Debug, "Refresher Thread starts");
					CogNamerListener.RefreshParams refreshParam = obj as CogNamerListener.RefreshParams;
					this.ClearPendingData();
					for (int i = 0; i < 3 && (i <= 0 || !this.WaitForCancelOrTimeout(1000)); i++)
					{
						IdentifyPacket identifyPacket = this.CreateIdentifyPacket(FlagType.None);
						IdentifyPacket identifyPacket1 = this.CreateIdentifyPacket(FlagType.Broadcast);
						foreach (NetworkInterfaceInfo availableInterface in this._networkInterfaceDetector.AvailableInterfaces)
						{
							if (refreshParam.PerformSubnetBroadcast)
							{
								this.SendUnicastPacket(identifyPacket, new IPEndPoint(availableInterface.SubnetBroadcastAddress, 1069));
							}
							if (!refreshParam.PerformLimitedBroadcast)
							{
								continue;
							}
							this.SendPacketWithBroadcastResponse(identifyPacket1, new IPEndPoint(IPAddress.Broadcast, 1069), availableInterface);
						}
					}
					this.Log(CogNamerListener.LogType.Debug, "Refresher Thread finished.");
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					this.Log(CogNamerListener.LogType.Error, string.Format("Refresher Thread quits abnormally: {0}", exception.Message));
				}
			}
			finally
			{
				this._refresherThread = null;
				this._isDiscoveryInProgress = 0;
			}
		}

		private void RegisterPacketProcessor(CogNamerListener.PacketProcessor processor)
		{
			lock (this._packetProcessors)
			{
				this._packetProcessors.AddLast(processor);
			}
		}

		private CogNamerListener.PacketProcessor RegisterPacketProcessor(string description, CogNamerListener.PacketProcessor.PacketProcessorEventHandler handler, int timeout)
		{
			CogNamerListener.PacketProcessor packetProcessor = new CogNamerListener.PacketProcessor(description, handler, timeout);
			this.RegisterPacketProcessor(packetProcessor);
			return packetProcessor;
		}

		public CogNamerPacketArrivedEventArgs Resolve(IPAddress deviceAddress, int retryCount, int timeout)
		{
			CogNamerPacketArrivedEventArgs cogNamerPacketArrivedEventArg = null;
			CogNamerListener.PacketProcessor packetProcessor = this.RegisterPacketProcessor("Resolve", (CogNamerListener.PacketProcessor processor, CogNamerPacketArrivedEventArgs packetdata) => {
				if (packetdata.Packet.ExtractIPAddress().Equals(deviceAddress) && cogNamerPacketArrivedEventArg == null)
				{
					cogNamerPacketArrivedEventArg = packetdata;
					processor.SetComplete();
				}
			}, retryCount * timeout);
			for (int i = 0; i < retryCount; i++)
			{
				try
				{
					IdentifyPacket identifyPacket = new IdentifyPacket();
					this.SendUnicastPacket(identifyPacket, new IPEndPoint(deviceAddress, 1069));
					if (i > 0)
					{
						this.SendUnicastPacket(identifyPacket, new IPEndPoint(deviceAddress, 51069));
					}
					packetProcessor.WaitForCompletionOrTimeout(timeout);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					string str = string.Format("Resolve error for {0} in try#{1}: {2}", deviceAddress, 1 + i, exception.Message);
					this.Log(CogNamerListener.LogType.Error, str);
				}
				if (packetProcessor.IsCompleted())
				{
					return cogNamerPacketArrivedEventArg;
				}
			}
			return null;
		}

		private void RunPacketProcessors(CogNamerPacketArrivedEventArgs packetData)
		{
			lock (this._packetProcessors)
			{
				int tickCount = Environment.TickCount;
				foreach (CogNamerListener.PacketProcessor _packetProcessor in this._packetProcessors)
				{
					if (_packetProcessor.IsCompleted())
					{
						continue;
					}
					_packetProcessor.Handler(_packetProcessor, packetData);
				}
			}
		}

		private void SendBytesWithRetriedBind(Socket socket, CommandType commandType, byte[] packet_bytes, IPEndPoint localEP, IPEndPoint remoteEP)
		{
			for (int i = 0; i < 3; i++)
			{
				try
				{
					socket.Bind(localEP);
					socket.SendTo(packet_bytes, remoteEP);
					return;
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (i >= 2)
					{
						this.Log(CogNamerListener.LogType.Error, string.Format("Sending of {0} packet to {1} failed ({2})", commandType, remoteEP.ToString(), exception.Message));
						break;
					}
					else
					{
						object[] objArray = new object[] { commandType, remoteEP.ToString(), 250, localEP.ToString(), exception.Message };
						this.Log(CogNamerListener.LogType.Warning, string.Format("Sending attempt of {0} packet to {1} temporarily failed, will retry in {2}ms (bind to {3} failed with {4})", objArray));
						Thread.Sleep(250);
					}
				}
			}
		}

		public bool SendFlash(IPAddress deviceAddress, PhysicalAddress deviceMac, int timeout)
		{
			CogNamerPacketArrivedEventArgs cogNamerPacketArrivedEventArg = null;
			CogNamerListener.PacketProcessor packetProcessor = this.RegisterPacketProcessor("Flash", (CogNamerListener.PacketProcessor processor, CogNamerPacketArrivedEventArgs packetdata) => {
				if (packetdata.Packet.ExtractMacAddress().Equals(deviceMac) && cogNamerPacketArrivedEventArg == null)
				{
					cogNamerPacketArrivedEventArg = packetdata;
					processor.SetComplete();
				}
			}, timeout);
			FlashPacket flashPacket = new FlashPacket(FlagType.None, ErrorCode.Success);
			flashPacket.AddRecord(new MacAddressRecord(deviceMac));
			this.SendUnicastPacket(flashPacket, new IPEndPoint(deviceAddress, 1069));
			packetProcessor.WaitForCompletionOrTimeout();
			if (cogNamerPacketArrivedEventArg != null)
			{
				return true;
			}
			return false;
		}

		public void SendPacket(CogNamerPacket packet, IPAddress remoteAddress, int port)
		{
			this.SendPacket(packet, remoteAddress, port, null);
		}

		public void SendPacket(CogNamerPacket packet, IPAddress remoteAddress, int port, NetworkInterfaceInfo interfaceInfo)
		{
			this.SendPacket(packet, new IPEndPoint(remoteAddress, port), interfaceInfo);
		}

		public void SendPacket(CogNamerPacket packet, IPEndPoint remoteEP, NetworkInterfaceInfo interfaceInfo)
		{
			CogNamerListener.ResolveMode resolveMode = CogNamerListener.DetermineResolveMode(remoteEP.Address);
			if (resolveMode != CogNamerListener.ResolveMode.LocalBroadcast && resolveMode != CogNamerListener.ResolveMode.SubnetBroadcast)
			{
				this.SendUnicastPacket(packet, remoteEP);
				return;
			}
			this.SendPacketWithBroadcastResponse(packet, remoteEP, interfaceInfo);
		}

		public void SendPacketWithBroadcastResponse(CogNamerPacket packet, IPEndPoint remoteEP, NetworkInterfaceInfo interfaceInfo)
		{
			byte[] numArray = packet.SerializeWithChangedFlags(FlagType.Broadcast);
			if (interfaceInfo != null)
			{
				IPEndPoint pEndPoint = new IPEndPoint(interfaceInfo.IPAddress, 1069);
				using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
				{
					object[] command = new object[] { packet.Command, remoteEP.ToString(), packet.Flags, interfaceInfo.Name, pEndPoint.ToString(), this.GetPacketDataForDebug(packet) };
					this.Log(CogNamerListener.LogType.Info, string.Format("Sending broadcast {0} packet to {1} with flags {2} on adapter {3} from {4}: {5}", command));
					SocketPortable.SetSocketOptionsForBroadcastSending(socket);
					try
					{
						this.SendBytesWithRetriedBind(socket, packet.Command, numArray, pEndPoint, remoteEP);
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						this.Log(CogNamerListener.LogType.Error, string.Format("Sending broadcast failed with message: {0}", exception.Message));
					}
				}
			}
			else
			{
				if (this._networkInterfaceDetector.AvailableInterfaces.Count == 0)
				{
					this._networkInterfaceDetector.RefreshAvailableInterfaces();
				}
				foreach (NetworkInterfaceInfo availableInterface in this._networkInterfaceDetector.AvailableInterfaces)
				{
					IPEndPoint pEndPoint1 = new IPEndPoint(availableInterface.IPAddress, 1069);
					object[] objArray = new object[] { packet.Command, remoteEP.ToString(), packet.Flags, availableInterface.Name, pEndPoint1.ToString(), this.GetPacketDataForDebug(packet) };
					this.Log(CogNamerListener.LogType.Info, string.Format("Sending broadcast {0} packet to {1} with flags {2} on adapter {3} from {4}: {5}", objArray));
					using (Socket socket1 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
					{
						SocketPortable.SetSocketOptionsForBroadcastSending(socket1);
						try
						{
							this.SendBytesWithRetriedBind(socket1, packet.Command, numArray, pEndPoint1, remoteEP);
						}
						catch (Exception exception3)
						{
							Exception exception2 = exception3;
							this.Log(CogNamerListener.LogType.Error, string.Format("Sending broadcast failed with message: {0}", exception2.Message));
						}
					}
				}
			}
		}

		private void SendRestart(IPAddress deviceIp, PhysicalAddress deviceMac, string username, string password)
		{
			CogNamerPacket cogNamerPacket = this.CreateRestartPacket(deviceMac, username, password);
			this.SendUnicastPacket(cogNamerPacket, new IPEndPoint(deviceIp, 1069));
		}

		public ErrorCode SendRestartAsBroadcast(PhysicalAddress deviceMac, string username, string password)
		{
			CogNamerPacket cogNamerPacket = this.CreateRestartPacket(deviceMac, username, password);
			int num = 3;
			int num1 = 250;
			for (int i = 0; i < num; i++)
			{
				this.SendPacketWithBroadcastResponse(cogNamerPacket, new IPEndPoint(IPAddress.Broadcast, 1069), null);
				Thread.Sleep(num1);
			}
			return ErrorCode.Success;
		}

		public ErrorCode SendRestartWaitResponse(IEnumerable<IPAddress> deviceIps, PhysicalAddress deviceMac, string username, string password)
		{
			CogNamerPacketArrivedEventArgs cogNamerPacketArrivedEventArg = null;
			int num = 3;
			int num1 = 1000;
			CogNamerListener.PacketProcessor packetProcessor = this.RegisterPacketProcessor("Restart", (CogNamerListener.PacketProcessor processor, CogNamerPacketArrivedEventArgs packetdata) => {
				if (packetdata.Packet.IsResponsePacketTo(CommandType.Restart) && packetdata.Packet.ExtractMacAddress().Equals(deviceMac) && cogNamerPacketArrivedEventArg == null)
				{
					cogNamerPacketArrivedEventArg = packetdata;
					processor.SetComplete();
				}
			}, num * num1);
			for (int i = 0; i < num; i++)
			{
				foreach (IPAddress deviceIp in deviceIps)
				{
					this.SendRestart(deviceIp, deviceMac, username, password);
				}
				packetProcessor.WaitForCompletionOrTimeout(num1);
				if (packetProcessor.IsCompleted())
				{
					return cogNamerPacketArrivedEventArg.Packet.Error;
				}
			}
			return ErrorCode.NonExistantRecord;
		}

		public ErrorCode SendSetNetworkAsBroadcast(IPAddress currentDeviceIp, PhysicalAddress deviceMac, string username, string password, string hostname, bool useDhcp, IPAddress newDeviceIp, IPAddress subnet, IPAddress gateway, IPAddress dns, string domainName)
		{
			CogNamerPacket cogNamerPacket = this.CreateIpAssignPacket(currentDeviceIp, deviceMac, username, password, hostname, useDhcp, newDeviceIp, subnet, gateway, dns, domainName);
			int num = 3;
			int num1 = 250;
			for (int i = 0; i < num; i++)
			{
				this.SendPacketWithBroadcastResponse(cogNamerPacket, new IPEndPoint(IPAddress.Broadcast, 1069), null);
				Thread.Sleep(num1);
			}
			return ErrorCode.Success;
		}

		public ErrorCode SendSetNetworkWaitResponse(IPAddress currentDeviceIp, PhysicalAddress deviceMac, string username, string password, string hostname, bool useDhcp, IPAddress newDeviceIp, IPAddress subnet, IPAddress gateway, IPAddress dns, string domainName)
		{
			CogNamerPacket cogNamerPacket = this.CreateIpAssignPacket(currentDeviceIp, deviceMac, username, password, hostname, useDhcp, newDeviceIp, subnet, gateway, dns, domainName);
			int num = 3;
			int num1 = 1000;
			CogNamerPacketArrivedEventArgs cogNamerPacketArrivedEventArg = null;
			CogNamerListener.PacketProcessor packetProcessor = this.RegisterPacketProcessor("IpAssign", (CogNamerListener.PacketProcessor processor, CogNamerPacketArrivedEventArgs packetdata) => {
				if (packetdata.Packet.IsResponsePacketTo(CommandType.IPAssign) && packetdata.Packet.ExtractMacAddress().Equals(deviceMac) && cogNamerPacketArrivedEventArg == null)
				{
					cogNamerPacketArrivedEventArg = packetdata;
					processor.SetComplete();
				}
			}, num * num1);
			for (int i = 0; i < num; i++)
			{
				this.SendUnicastPacket(cogNamerPacket, new IPEndPoint(currentDeviceIp, 1069));
				packetProcessor.WaitForCompletionOrTimeout(num1);
				if (packetProcessor.IsCompleted())
				{
					return cogNamerPacketArrivedEventArg.Packet.Error;
				}
			}
			return ErrorCode.NonExistantRecord;
		}

		public void SendUnicastPacket(CogNamerPacket packet, IPEndPoint remoteEP)
		{
			Socket socket;
			object[] command = new object[] { packet.Command, remoteEP.ToString(), packet.Flags, this.GetPacketDataForDebug(packet) };
			this.Log(CogNamerListener.LogType.Info, string.Format("Sending unicast {0} packet to {1} with flags {2} from global sender/receiver socket: {3}", command));
			byte[] numArray = packet.Serialize();
			CogNamerListenerSocket unicastSenderReceiverSocket = this.GetUnicastSenderReceiverSocket();
			if (unicastSenderReceiverSocket != null)
			{
				socket = unicastSenderReceiverSocket.Socket;
			}
			else
			{
				socket = null;
			}
			Socket socket1 = socket;
			if (socket1 != null)
			{
				socket1.SendTo(numArray, remoteEP);
				return;
			}
			this.Log(CogNamerListener.LogType.Error, string.Format("Failed sending unicast {0} packet to {1}: no unicast sender/receiver socket could be determined", packet.Command, remoteEP.ToString()));
		}

		private bool Start()
		{
			bool flag;
			lock (this._stateLock)
			{
				if (this._state == CogNamerListener.State.Stopped)
				{
					try
					{
						this._state = CogNamerListener.State.Starting;
						this._cancellation = new CancellationTokenSource();
						this._networkInterfaceDetector.Active = true;
						this._networkInterfaceDetector.RefreshAvailableInterfaces();
						this._refresherThread = null;
						this._listenerThread = new ThreadStarter(new ParameterizedThreadStart(this.CogNamerListenerThreadFunc), true, "CogNamer Listener Thread", null);
						this._dispatcherThread = new ThreadStarter(new ParameterizedThreadStart(this.CogNamerDispatcherThreadFunc), true, "CogNamer Dispatcher Thread", null);
						this._dispatcherThread.Start();
						this._listenerThread.Start();
						this._networkInterfaceDetector.PresenceChanged += new NetworkInterfacePresenceChangedHandler(this.OnNetworkInterfacePresenceChanged);
						this._state = CogNamerListener.State.Started;
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						this.Log(CogNamerListener.LogType.Error, string.Format("Could not start CogNamer Listener: {0}", exception.Message));
						this.Stop(true);
					}
					flag = true;
				}
				else
				{
					flag = false;
				}
			}
			return flag;
		}

		private bool Stop(bool disposeManagedObjects)
		{
			bool flag;
			lock (this._stateLock)
			{
				if (this._state != CogNamerListener.State.Stopped)
				{
					try
					{
						this._state = CogNamerListener.State.Stopping;
						if (disposeManagedObjects)
						{
							this._networkInterfaceDetector.PresenceChanged -= new NetworkInterfacePresenceChangedHandler(this.OnNetworkInterfacePresenceChanged);
							this._networkInterfaceDetector.Active = false;
							if (this._cancellation != null)
							{
								this._cancellation.Cancel();
							}
						}
						this.CloseAndDisposeListeningSockets(disposeManagedObjects);
						if (this._dispatcherThread != null)
						{
							if (disposeManagedObjects && !this._dispatcherThread.Join(1000))
							{
								this.Log(CogNamerListener.LogType.Error, string.Format("Forcing Dispatcher Thread to stop...", new object[0]));
								this._dispatcherThread.Abort();
								this._dispatcherThread.Join();
							}
							this._dispatcherThread = null;
						}
						if (this._listenerThread != null)
						{
							if (disposeManagedObjects && !this._listenerThread.Join(1000))
							{
								this.Log(CogNamerListener.LogType.Error, string.Format("Forcing Listener Thread to stop...", new object[0]));
								this._listenerThread.Abort();
								this._listenerThread.Join();
							}
							this._listenerThread = null;
						}
						this.ClearPendingData();
						this._state = CogNamerListener.State.Stopped;
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						this._state = CogNamerListener.State.Stopped;
						this.Log(CogNamerListener.LogType.Error, string.Format("Error stopping the listener: {0}", exception.Message));
					}
					flag = true;
				}
				else
				{
					flag = false;
				}
			}
			return flag;
		}

		private void StoreAsRecentlyDiscoveredDevice(CogNamerPacketArrivedEventArgs packetData)
		{
			IPAddress recentlyDiscoveredDevice = packetData.Packet.ExtractIPAddress();
			string str = packetData.Packet.ExtractHostName();
			if (recentlyDiscoveredDevice.Equals(IPAddress.None) && string.IsNullOrEmpty(str))
			{
				return;
			}
			lock (this._recentlyDiscoveredDevices)
			{
				this._recentlyDiscoveredDevices[recentlyDiscoveredDevice] = new CogNamerListener.RecentlyDiscoveredDevice(recentlyDiscoveredDevice, str, packetData.Packet.ExtractMacAddress(), packetData.Packet.ExtractServicePorts());
			}
		}

		[Conditional("ENABLE_COGNAMER_TRACE_LOG")]
		private void TraceLog(string message)
		{
			this.Log(CogNamerListener.LogType.Trace, message);
		}

		public bool TryGetDiscoveredDeviceByHostName(string hostName, out IPAddress ipAddress, out PhysicalAddress macAddress)
		{
			bool flag;
			lock (this._recentlyDiscoveredDevices)
			{
				foreach (KeyValuePair<IPAddress, CogNamerListener.RecentlyDiscoveredDevice> _recentlyDiscoveredDevice in this._recentlyDiscoveredDevices)
				{
					if (string.Compare(hostName, _recentlyDiscoveredDevice.Value.HostName, true) != 0)
					{
						continue;
					}
					ipAddress = _recentlyDiscoveredDevice.Value.Address;
					macAddress = _recentlyDiscoveredDevice.Value.MacAddress;
					flag = true;
					return flag;
				}
				ipAddress = IPAddress.None;
				macAddress = PhysicalAddress.None;
				return false;
			}
			return flag;
		}

		public bool TryGetDiscoveredDeviceByIp(IPAddress ipAddress, out PhysicalAddress macAddress, out string hostName, out Dictionary<string, int> servicePorts)
		{
			CogNamerListener.RecentlyDiscoveredDevice recentlyDiscoveredDevice;
			bool flag;
			lock (this._recentlyDiscoveredDevices)
			{
				if (!this._recentlyDiscoveredDevices.TryGetValue(ipAddress, out recentlyDiscoveredDevice))
				{
					macAddress = PhysicalAddress.None;
					hostName = "";
					servicePorts = null;
					return false;
				}
				else
				{
					hostName = recentlyDiscoveredDevice.HostName;
					macAddress = recentlyDiscoveredDevice.MacAddress;
					servicePorts = recentlyDiscoveredDevice.ServicePorts;
					flag = true;
				}
			}
			return flag;
		}

		public bool TryGetDiscoveredDeviceByMac(PhysicalAddress macAddress, out IPAddress ipAddress, out string hostName)
		{
			bool flag;
			lock (this._recentlyDiscoveredDevices)
			{
				foreach (KeyValuePair<IPAddress, CogNamerListener.RecentlyDiscoveredDevice> _recentlyDiscoveredDevice in this._recentlyDiscoveredDevices)
				{
					if (!_recentlyDiscoveredDevice.Value.MacAddress.Equals(macAddress))
					{
						continue;
					}
					ipAddress = _recentlyDiscoveredDevice.Value.Address;
					hostName = _recentlyDiscoveredDevice.Value.HostName;
					flag = true;
					return flag;
				}
				ipAddress = IPAddress.None;
				hostName = "";
				return false;
			}
			return flag;
		}

		private void UnregisterExpiredPacketProcessors()
		{
			LinkedListNode<CogNamerListener.PacketProcessor> next = null;
			int tickCount = Environment.TickCount;
			if (tickCount - this._lastPacketProcessorPurgeTime < 100)
			{
				return;
			}
			this._lastPacketProcessorPurgeTime = tickCount;
			lock (this._packetProcessors)
			{
				for (LinkedListNode<CogNamerListener.PacketProcessor> i = this._packetProcessors.First; i != null; i = next)
				{
					next = i.Next;
					if (i.Value.IsCompleted() || i.Value.IsExpired(tickCount))
					{
						this._packetProcessors.Remove(i);
					}
				}
			}
		}

		private bool WaitForCancelOrTimeout(int timeout)
		{
			if (!this.Active)
			{
				return true;
			}
			CancellationTokenSource cancellationTokenSource = this._cancellation;
			cancellationTokenSource.Token.WaitOne(timeout);
			if (this.Active && !cancellationTokenSource.IsCancellationRequested)
			{
				return false;
			}
			return true;
		}

		public event CogNamerPacketArrivedEventHandler CogNamerPacketArrived;

		public event NetworkInterfacePresenceChangedHandler NetworkInterfacePresenceChanged;

		public event CogNamerLogEventHandler OnNewLogEntry;

		public enum LogType
		{
			Error,
			Warning,
			Info,
			Debug,
			Trace
		}

		private class PacketProcessor
		{
			private ManualResetEvent _processorCompletedEvent;

			public string Description
			{
				get;
				private set;
			}

			public int ExpirationTimeout
			{
				get;
				private set;
			}

			public int ExpiresAt
			{
				get;
				private set;
			}

			public CogNamerListener.PacketProcessor.PacketProcessorEventHandler Handler
			{
				get;
				private set;
			}

			public bool WaitResult
			{
				get;
				private set;
			}

			public PacketProcessor(string description, CogNamerListener.PacketProcessor.PacketProcessorEventHandler handler, int timeout)
			{
				this.Description = description;
				this.WaitResult = false;
				this.Handler = handler;
				this.ExpirationTimeout = timeout;
				this.ExpiresAt = Environment.TickCount + timeout;
				this._processorCompletedEvent = new ManualResetEvent(false);
			}

			public bool IsCompleted()
			{
				return this._processorCompletedEvent.WaitOne(0, false);
			}

			public bool IsExpired(int now)
			{
				return this.ExpiresAt < now;
			}

			public void SetComplete()
			{
				this._processorCompletedEvent.Set();
			}

			public bool WaitForCompletionOrTimeout()
			{
				return this._processorCompletedEvent.WaitOne(this.ExpirationTimeout, false);
			}

			public bool WaitForCompletionOrTimeout(int timeout)
			{
				return this._processorCompletedEvent.WaitOne(timeout, false);
			}

			public delegate void PacketProcessorEventHandler(CogNamerListener.PacketProcessor packetProcessor, CogNamerPacketArrivedEventArgs e);
		}

		private class RecentlyDiscoveredDevice
		{
			public IPAddress Address
			{
				get;
				private set;
			}

			public string HostName
			{
				get;
				private set;
			}

			public PhysicalAddress MacAddress
			{
				get;
				private set;
			}

			public Dictionary<string, int> ServicePorts
			{
				get;
				private set;
			}

			public RecentlyDiscoveredDevice(IPAddress address, string hostName, PhysicalAddress macAddress, Dictionary<string, int> servicePorts)
			{
				this.Address = address;
				this.HostName = hostName;
				this.MacAddress = macAddress;
				this.ServicePorts = servicePorts;
			}

			public override string ToString()
			{
				return string.Format("{0}, IP={1}, Mac={2}", this.HostName, this.Address.ToString(), this.MacAddress.ToString());
			}
		}

		private class RefreshParams
		{
			public bool PerformLimitedBroadcast
			{
				get;
				private set;
			}

			public bool PerformSubnetBroadcast
			{
				get;
				private set;
			}

			public RefreshParams(bool performSubnetBroadcast, bool performLimitedBroadcast)
			{
				this.PerformSubnetBroadcast = performSubnetBroadcast;
				this.PerformLimitedBroadcast = performLimitedBroadcast;
			}
		}

		private enum ResolveMode
		{
			LocalBroadcast,
			SubnetBroadcast,
			Unicast
		}

		private enum State
		{
			Stopped,
			Starting,
			Started,
			Stopping
		}
	}
}
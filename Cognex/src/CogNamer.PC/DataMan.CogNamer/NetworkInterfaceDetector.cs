using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace Cognex.DataMan.CogNamer
{
	public class NetworkInterfaceDetector : IDisposable
	{
		private bool _active;

		private List<NetworkInterfaceInfo> _availableInterfaces;

		public bool Active
		{
			get
			{
				return this._active;
			}
			set
			{
				if (this._active == value)
				{
					return;
				}
				this._active = value;
				if (this._active)
				{
					NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler(this.NetworkChange_NetworkAddressChanged);
					return;
				}
				NetworkChange.NetworkAddressChanged -= new NetworkAddressChangedEventHandler(this.NetworkChange_NetworkAddressChanged);
				lock (this._availableInterfaces)
				{
					this._availableInterfaces.Clear();
				}
			}
		}

		public List<NetworkInterfaceInfo> AvailableInterfaces
		{
			get
			{
				List<NetworkInterfaceInfo> networkInterfaceInfos = new List<NetworkInterfaceInfo>();
				lock (this._availableInterfaces)
				{
					networkInterfaceInfos.AddRange(this._availableInterfaces);
				}
				return networkInterfaceInfos;
			}
			private set
			{
				if (value != null)
				{
					lock (this._availableInterfaces)
					{
						this._availableInterfaces.Clear();
						this._availableInterfaces.AddRange(value);
					}
				}
			}
		}

		public NetworkInterfaceDetector()
		{
			this._active = false;
			this._availableInterfaces = new List<NetworkInterfaceInfo>();
		}

		private static long ConvertMacAddress(PhysicalAddress macAddress)
		{
			byte[] addressBytes = macAddress.GetAddressBytes();
			byte[] numArray = new byte[8];
			addressBytes.CopyTo(numArray, 0);
			return BitConverter.ToInt64(numArray, 0);
		}

		public void Dispose()
		{
			this.DoDispose(true);
			GC.SuppressFinalize(this);
		}

		private void DoDispose(bool disposeManagedResources)
		{
			this.Active = false;
		}

		~NetworkInterfaceDetector()
		{
			this.DoDispose(false);
		}

		public NetworkInterfaceInfo FindInterfaceByIndex(int interfaceIndex)
		{
			NetworkInterfaceInfo networkInterfaceInfo;
			lock (this._availableInterfaces)
			{
				foreach (NetworkInterfaceInfo _availableInterface in this._availableInterfaces)
				{
					if (_availableInterface.InterfaceIndex != interfaceIndex)
					{
						continue;
					}
					networkInterfaceInfo = _availableInterface;
					return networkInterfaceInfo;
				}
				return null;
			}
			return networkInterfaceInfo;
		}

		private IPAddress IPAddressParse(string ipString, IPAddress defaultIpAddress)
		{
			IPAddress pAddress;
			try
			{
				pAddress = IPAddress.Parse(ipString);
			}
			catch
			{
				pAddress = defaultIpAddress;
			}
			return pAddress;
		}

		private void Log(string message)
		{
			Trace.WriteLine(message);
		}

		private void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
		{
			this.RefreshAvailableInterfaces();
			NetworkInterfacePresenceChangedHandler networkInterfacePresenceChangedHandler = this.PresenceChanged;
			if (networkInterfacePresenceChangedHandler != null)
			{
				networkInterfacePresenceChangedHandler(this, EventArgs.Empty);
			}
		}

		public void RefreshAvailableInterfaces()
		{
			List<NetworkInterfaceInfo> networkInterfaceInfos = new List<NetworkInterfaceInfo>();
			try
			{
				NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
				for (int i = 0; i < (int)allNetworkInterfaces.Length; i++)
				{
					NetworkInterface networkInterface = allNetworkInterfaces[i];
					string str = string.Format("NetworkInterfaceDetector: intf {0} (Name={1}, Id={2}): ", networkInterface.Description, networkInterface.Name, networkInterface.Id);
					if (networkInterface.OperationalStatus == OperationalStatus.Up)
					{
						PhysicalAddress physicalAddress = networkInterface.GetPhysicalAddress();
						IPInterfaceProperties pProperties = networkInterface.GetIPProperties();
						if (physicalAddress == null || (int)physicalAddress.GetAddressBytes().Length != 6)
						{
							this.Log(string.Format("{0} skipped: no MAC", str));
						}
						else if (pProperties == null || pProperties.UnicastAddresses == null)
						{
							this.Log(string.Format("{0} skipped: missing/invalid IP address", str));
						}
						else
						{
							IPv4InterfaceProperties pv4Properties = pProperties.GetIPv4Properties();
							if (pv4Properties != null)
							{
								List<IPAddress> pAddresses = new List<IPAddress>();
								List<IPAddress> pAddresses1 = new List<IPAddress>();
								foreach (UnicastIPAddressInformation unicastAddress in pProperties.UnicastAddresses)
								{
									if (unicastAddress.Address.AddressFamily != AddressFamily.InterNetwork)
									{
										continue;
									}
									pAddresses.Add(unicastAddress.Address);
									pAddresses1.Add(unicastAddress.IPv4Mask);
								}
								if (pAddresses.Count == 0 || pAddresses1.Count == 0)
								{
									this.Log(string.Format("{0} skipped: no primary_addr/primary_subnet_mask found", str));
								}
								else
								{
									for (int j = 0; j < pAddresses.Count; j++)
									{
										IPAddress item = pAddresses[j];
										IPAddress pAddress = pAddresses1[j];
										int index = pv4Properties.Index;
										string name = networkInterface.Name;
										long num = NetworkInterfaceDetector.ConvertMacAddress(physicalAddress);
										bool networkInterfaceType = networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211;
										bool isDhcpEnabled = pv4Properties.IsDhcpEnabled;
										IPAddress none = IPAddress.None;
										IPAddress none1 = IPAddress.None;
										string domainName = "";
										try
										{
											none = (pProperties.DnsAddresses.Count > 0 ? pProperties.DnsAddresses[0] : IPAddress.None);
											none1 = (pProperties.GatewayAddresses.Count > 0 ? pProperties.GatewayAddresses[0].Address : IPAddress.None);
											domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
										}
										catch
										{
										}
										this.Log(string.Format("{0} added, intf_index={1}, ip={2}", str, index, item.ToString()));
										NetworkInterfaceInfo networkInterfaceInfo = new NetworkInterfaceInfo(index, name, num, networkInterfaceType, item, pAddress, none, none1, domainName, isDhcpEnabled);
										networkInterfaceInfos.Add(networkInterfaceInfo);
									}
								}
							}
							else
							{
								this.Log(string.Format("{0} skipped: missing ipv4_properties", str));
							}
						}
					}
					else
					{
						this.Log(string.Format("{0} skipped: down", str));
					}
				}
			}
			catch (Exception exception1)
			{
				this.Log(string.Format("Error: could not determine network interfaces: {0}", exception1.Message));
				try
				{
					string environmentVariable = Environment.GetEnvironmentVariable("COGNAMER_INTERFACES");
					if (string.IsNullOrEmpty(environmentVariable))
					{
						this.Log(string.Format("User-defined network interface not specified, no interface could be detected", new object[0]));
					}
					else
					{
						int num1 = 1;
						this.Log(string.Format("User-defined network interface specified, parsing it: {0}", environmentVariable));
						string[] strArrays = environmentVariable.Split(new char[] { ';' });
						for (int k = 0; k < (int)strArrays.Length; k++)
						{
							string str1 = strArrays[k];
							string str2 = string.Format("Interface #{0} ({1})", num1, str1);
							bool flag = false;
							IPAddress any = IPAddress.Any;
							if (IPAddress.TryParse(str1, out any))
							{
								PhysicalAddress physicalAddress1 = PhysicalAddress.Parse(string.Format("00-D0-24-00-00-{0:00}", num1));
								IPAddress pAddress1 = IPAddress.Parse("255.255.255.0");
								IPAddress any1 = IPAddress.Any;
								IPAddress any2 = IPAddress.Any;
								string str3 = "";
								bool flag1 = false;
								this.Log(string.Format("User-defined network interface added: '{0}', intf_index={1}, ip={2}", str2, num1, str1));
								NetworkInterfaceInfo networkInterfaceInfo1 = new NetworkInterfaceInfo(num1, str2, MacAddressRecord.MacConvertBytesToLong(physicalAddress1.GetAddressBytes()), flag, any, pAddress1, any1, any2, str3, flag1);
								networkInterfaceInfos.Add(networkInterfaceInfo1);
							}
							else
							{
								this.Log(string.Format("User-defined network interface skipped: {0}", str1));
							}
						}
					}
				}
				catch (Exception exception)
				{
					this.Log(string.Format("Error: failed parsing user-defined network interfaces: {0}", exception.Message));
				}
			}
			this.AvailableInterfaces = networkInterfaceInfos;
		}

		public event NetworkInterfacePresenceChangedHandler PresenceChanged;
	}
}
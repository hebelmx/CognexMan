using Cognex.DataMan.CogNamer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace Cognex.DataMan.SDK.Discovery
{
	public class EthSystemDiscoverer : IDisposable
	{
		private const uint DataManFamily = 1280;

		private bool _isDisposed;

		private static Dictionary<PhysicalAddress, object> m_Addresses;

		private CogNamerListener _cogNamerListener;

		private readonly object _listenerLocker = new object();

		public bool IsDiscoveryInProgress
		{
			get
			{
				if (this._cogNamerListener == null)
				{
					return false;
				}
				return this._cogNamerListener.IsDiscoveryInProgress;
			}
		}

		static EthSystemDiscoverer()
		{
			EthSystemDiscoverer.m_Addresses = new Dictionary<PhysicalAddress, object>();
		}

		public EthSystemDiscoverer()
		{
			this._cogNamerListener = new CogNamerListener()
			{
				DiscoverMisconfiguredDevices = true
			};
			this._cogNamerListener.CogNamerPacketArrived += new CogNamerPacketArrivedEventHandler(this.CogNamerPacketArrived);
			this._cogNamerListener.Active = true;
		}

		private void CogNamerPacketArrived(object sender, CogNamerPacketArrivedEventArgs args)
		{
			int num;
			int num1 = args.Packet.ExtractCogNamerDeviceType();
			PhysicalAddress packet = args.Packet.ExtractMacAddress();
			if ((long)(num1 & 65280) == (long)1280)
			{
				bool flag = false;
				if (EthSystemDiscoverer.m_Addresses.ContainsKey(packet))
				{
					flag = false;
				}
				else
				{
					EthSystemDiscoverer.m_Addresses[packet] = args.Packet;
					flag = true;
				}
				if (flag)
				{
					EthSystemDiscoverer.SystemDiscoveredHandler systemDiscoveredHandler = this.SystemDiscovered;
					if (systemDiscoveredHandler != null)
					{
						if (!args.Packet.ExtractServicePorts().TryGetValue("telnet", out num) || num < 1)
						{
							num = 23;
						}
						IPAddress none = IPAddress.None;
						IPAddress gateway = IPAddress.None;
						bool isDhcpBased = true;
						NetworkSettingsRecord networkSettingsRecord = args.Packet.FindRecord(RecordType.NetworkSettings) as NetworkSettingsRecord;
						if (networkSettingsRecord != null)
						{
							none = networkSettingsRecord.SubNetMask;
							gateway = networkSettingsRecord.Gateway;
							isDhcpBased = networkSettingsRecord.IsDhcpBased;
						}
						systemDiscoveredHandler(new EthSystemDiscoverer.SystemInfo(num1, args.Packet.ExtractModelNumber(), args.Packet.ExtractHostName(), args.Packet.ExtractIPAddress(), num, none, gateway, MacAddressRecord.MacConvertBytesToLong(packet.GetAddressBytes()), args.Packet.ExtractSerialNumber(), isDhcpBased));
					}
				}
			}
		}

		private static PhysicalAddress ConvertToMacAddress(string macAddress)
		{
			string upper = macAddress.ToUpper();
			Match match = Regex.Match(upper, "([^\\-]{1,2}\\-){1,}");
			if (match.Groups.Count <= 1)
			{
				match = Regex.Match(upper, "([^\\:]{1,2}\\:){1,}");
				if (match.Groups.Count > 1)
				{
					if (match.Groups[1].Captures.Count != 5)
					{
						throw new FormatException("MAC address has at least one but not five colons (:).");
					}
					upper = Regex.Replace(upper, ":", "");
				}
			}
			else
			{
				if (match.Groups[1].Captures.Count != 5)
				{
					throw new FormatException("MAC address has at least one but not five dashes (-).");
				}
				upper = Regex.Replace(upper, "-", "");
			}
			byte[] bytes = BitConverter.GetBytes(Convert.ToInt64(upper, 16));
			byte[] numArray = new byte[6];
			Array.Copy(bytes, numArray, 6);
			numArray = EthSystemDiscoverer.ReverseBytes(numArray);
			return new PhysicalAddress(numArray);
		}

		public void Discover()
		{
			if (this.IsDiscoveryInProgress)
			{
				return;
			}
			EthSystemDiscoverer.m_Addresses.Clear();
			this._cogNamerListener.Refresh();
		}

		public void Dispose()
		{
			this.DoDispose(true);
			GC.SuppressFinalize(this);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		private void DoDispose(bool disposingManagedResources)
		{
			if (this._isDisposed)
			{
				return;
			}
			this._isDisposed = true;
			if (disposingManagedResources && this._cogNamerListener != null)
			{
				this._cogNamerListener.Active = false;
				this._cogNamerListener.Dispose();
				this._cogNamerListener = null;
			}
		}

		~EthSystemDiscoverer()
		{
			this.DoDispose(false);
		}

		public void ForceNetworkSettings(string macAddress, bool useDHCP, IPAddress ipAddress, IPAddress subnetMask, IPAddress defaultGateway, string username, string password, string hostName)
		{
			IPAddress pAddress;
			string str;
			EthSystemDiscoverer.SendSetNetworkResult sendSetNetworkResult = EthSystemDiscoverer.SendSetNetworkResult.UnknownError;
			PhysicalAddress none = PhysicalAddress.None;
			try
			{
				none = EthSystemDiscoverer.ConvertToMacAddress(macAddress);
				if (this._cogNamerListener.TryGetDiscoveredDeviceByMac(none, out pAddress, out str))
				{
					IPAddress none1 = IPAddress.None;
					string str1 = "";
					bool flag = true;
					sendSetNetworkResult = this.SendSetNetwork(pAddress, none, username, password, hostName, useDHCP, ipAddress, subnetMask, defaultGateway, none1, str1, flag);
				}
				else
				{
					this.Log(string.Concat("EthSystemDiscoverer: SendSetNetwork could not find device with MAC adddress ", none.ToString()));
					sendSetNetworkResult = EthSystemDiscoverer.SendSetNetworkResult.DeviceNotFoundByMac;
				}
			}
			catch (Exception exception)
			{
				this.Log(string.Format("EthSystemDiscoverer: SendSetNetwork failed with exception: {0}", exception.Message));
				sendSetNetworkResult = EthSystemDiscoverer.SendSetNetworkResult.UnknownError;
			}
			switch (sendSetNetworkResult)
			{
				case EthSystemDiscoverer.SendSetNetworkResult.Succeeded:
				{
					return;
				}
				case EthSystemDiscoverer.SendSetNetworkResult.UnknownError:
				{
					throw new InvalidOperationException("Unexpected error occured");
				}
				case EthSystemDiscoverer.SendSetNetworkResult.DeviceNotFoundByMac:
				{
					throw new InvalidOperationException("Device not found by MAC address");
				}
				case EthSystemDiscoverer.SendSetNetworkResult.OperationNotSupportedByDevice:
				{
					throw new InvalidOperationException("Operation not supported by device");
				}
				case EthSystemDiscoverer.SendSetNetworkResult.AccessRightProblem:
				{
					throw new InvalidOperationException("Access right problem occured on the device");
				}
				default:
				{
					throw new InvalidOperationException("Unexpected error occured");
				}
			}
		}

		private EthSystemDiscoverer.SendSetNetworkResult GetResultFromCogNamerErrorCode(ErrorCode cogNamerErrorCode)
		{
			switch (cogNamerErrorCode)
			{
				case ErrorCode.Success:
				{
					return EthSystemDiscoverer.SendSetNetworkResult.Succeeded;
				}
				case ErrorCode.Failed:
				{
					return EthSystemDiscoverer.SendSetNetworkResult.UnknownError;
				}
				case ErrorCode.Unsupported:
				{
					return EthSystemDiscoverer.SendSetNetworkResult.OperationNotSupportedByDevice;
				}
				case ErrorCode.InvalidUsername:
				case ErrorCode.InvalidPassword:
				case ErrorCode.NoPermissions:
				{
					return EthSystemDiscoverer.SendSetNetworkResult.AccessRightProblem;
				}
				case ErrorCode.MissingInputData:
				case ErrorCode.InvalidInputData:
				case ErrorCode.CommandOnlySupportedAtBootup:
				case ErrorCode.NonExistantRecord:
				{
					return EthSystemDiscoverer.SendSetNetworkResult.UnknownError;
				}
				default:
				{
					return EthSystemDiscoverer.SendSetNetworkResult.UnknownError;
				}
			}
		}

		private void Log(string message)
		{
		}

		private static byte[] ReverseBytes(byte[] bytes)
		{
			byte[] numArray = new byte[(int)bytes.Length];
			for (int i = 0; i < (int)numArray.Length; i++)
			{
				numArray[i] = bytes[(int)bytes.Length - i - 1];
			}
			return numArray;
		}

		private EthSystemDiscoverer.SendSetNetworkResult SendSetNetwork(IPAddress currentDeviceIp, PhysicalAddress deviceMac, string username, string password, string hostname, bool useDhcp, IPAddress newDeviceIp, IPAddress subnet, IPAddress gateway, IPAddress dns, string domainName, bool reset)
		{
			IPAddress pAddress;
			string str;
			EthSystemDiscoverer.SendSetNetworkResult resultFromCogNamerErrorCode;
			if (this._isDisposed)
			{
				return EthSystemDiscoverer.SendSetNetworkResult.UnknownError;
			}
			try
			{
				if (this._cogNamerListener.TryGetDiscoveredDeviceByMac(deviceMac, out pAddress, out str))
				{
					string[] strArrays = new string[] { "EthSystemDiscoverer: Send Set Network ", hostname, " (", newDeviceIp.ToString(), ")" };
					this.Log(string.Concat(strArrays));
					lock (this._listenerLocker)
					{
						bool flag = false;
						ErrorCode errorCode = this._cogNamerListener.SendSetNetworkWaitResponse(currentDeviceIp, deviceMac, username, password, hostname, useDhcp, newDeviceIp, subnet, gateway, dns, domainName);
						if (errorCode != ErrorCode.Success)
						{
							this.Log(string.Format("EthSystemDiscoverer: SendSetNetwork failed (ErrorCode: {0})", errorCode.ToString()));
							if (errorCode != ErrorCode.NonExistantRecord)
							{
								resultFromCogNamerErrorCode = this.GetResultFromCogNamerErrorCode(errorCode);
								return resultFromCogNamerErrorCode;
							}
							else
							{
								this.Log(string.Format("SendSetNetwork: No response arrived from {0}, trying with broadcast...", currentDeviceIp.ToString()));
								errorCode = this._cogNamerListener.SendSetNetworkAsBroadcast(currentDeviceIp, deviceMac, username, password, hostname, useDhcp, newDeviceIp, subnet, gateway, dns, domainName);
								flag = true;
							}
						}
						ErrorCode errorCode1 = ErrorCode.Success;
						if (flag)
						{
							errorCode1 = this._cogNamerListener.SendRestartAsBroadcast(deviceMac, username, password);
						}
						else
						{
							List<IPAddress> pAddresses = new List<IPAddress>();
							if (!currentDeviceIp.Equals(newDeviceIp))
							{
								pAddresses.Add(currentDeviceIp);
								pAddresses.Add(newDeviceIp);
							}
							else
							{
								pAddresses.Add(newDeviceIp);
							}
							errorCode1 = this._cogNamerListener.SendRestartWaitResponse(pAddresses, deviceMac, username, password);
						}
						if (errorCode1 != ErrorCode.Success)
						{
							this.Log(string.Format("EthSystemDiscoverer: SendRestart failed (ErrorCode: {0})", errorCode1.ToString()));
							resultFromCogNamerErrorCode = this.GetResultFromCogNamerErrorCode(errorCode1);
						}
						else
						{
							this.Log(string.Format("EthSystemDiscoverer: SendRestart succeeded", new object[0]));
							resultFromCogNamerErrorCode = EthSystemDiscoverer.SendSetNetworkResult.Succeeded;
						}
					}
				}
				else
				{
					this.Log(string.Concat("EthSystemDiscoverer: SendSetNetwork could not find device with MAC adddress ", deviceMac.ToString()));
					resultFromCogNamerErrorCode = EthSystemDiscoverer.SendSetNetworkResult.DeviceNotFoundByMac;
				}
			}
			catch (Exception exception)
			{
				throw new InvalidOperationException(exception.Message);
			}
			return resultFromCogNamerErrorCode;
		}

		public event EthSystemDiscoverer.SystemDiscoveredHandler SystemDiscovered;

		private enum SendSetNetworkResult
		{
			Succeeded,
			UnknownError,
			DeviceNotFoundByMac,
			OperationNotSupportedByDevice,
			AccessRightProblem
		}

		public delegate void SystemDiscoveredHandler(EthSystemDiscoverer.SystemInfo systemInfo);

		public class SystemInfo
		{
			public int CognamerDeviceTypeId
			{
				get;
				private set;
			}

			public IPAddress DefaultGateway
			{
				get;
				private set;
			}

			public IPAddress IPAddress
			{
				get;
				private set;
			}

			public bool IsDhcpEnabled
			{
				get;
				private set;
			}

			public long MacAddress
			{
				get;
				private set;
			}

			public string Name
			{
				get;
				private set;
			}

			public int Port
			{
				get;
				private set;
			}

			public string SerialNumber
			{
				get;
				private set;
			}

			public IPAddress SubnetMask
			{
				get;
				private set;
			}

			public string Type
			{
				get;
				private set;
			}

			internal SystemInfo(int cognamerDeviceTypeId, string systemType, string systemName, IPAddress ipAddress, int port, IPAddress subnetMask, IPAddress defaultGateway, long macAddress, string serialNumber, bool isDhcpEnabled)
			{
				this.CognamerDeviceTypeId = cognamerDeviceTypeId;
				this.Type = systemType;
				this.Name = systemName;
				this.IPAddress = ipAddress;
				this.Port = port;
				this.SubnetMask = subnetMask;
				this.DefaultGateway = defaultGateway;
				this.MacAddress = macAddress;
				this.SerialNumber = serialNumber;
				this.IsDhcpEnabled = isDhcpEnabled;
			}

			public override string ToString()
			{
				return this.Name;
			}
		}
	}
}
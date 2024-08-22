using Cognex.DataMan.CogNamer.PlatformHelpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;

namespace Cognex.DataMan.SDK
{
    public class DataManSystem : IDisposable
    {
        public const int INFINITE = -1;

        private static int _nextCommandId;

        private IDmccParser _parser;

        private DmccPendingCommandsContainer _pendingCommands = new DmccPendingCommandsContainer();

        private Queue<CommandInfo> _queuedCommands = new Queue<CommandInfo>();

        private Queue<DataManSystem.GenericCallback> _queuedCallbacks = new Queue<DataManSystem.GenericCallback>();

        private ISystemConnector _connector;

        private Thread _readerThread;

        private Thread _writerThread;

        private bool _keepAliveEnabled;

        private int _keepAliveTimeout = 20000;

        private int _keepAliveInterval = 1000;

        private Thread _keepAliveThread;

        private Thread _callbackDispatcherThread;

        private EventWaitHandlePortable _commandQueueChangedEvent;

        private EventWaitHandlePortable _callbacksQueueChangedEvent;

        private EventWaitHandlePortable _threadExitEvent;

        private ConnectionState _connectionState;

        private bool _expectReadStringAsBinaryResponse;

        public ISystemConnector Connector
        {
            get
            {
                return this._connector;
            }
        }

        public int DefaultTimeout
        {
            get;
            set;
        }

        public System.Text.Encoding Encoding
        {
            get;
            set;
        }

        public bool ExpectReadStringAsBinaryResponse
        {
            get
            {
                return this._expectReadStringAsBinaryResponse;
            }
            set
            {
                this._expectReadStringAsBinaryResponse = this.ExpectReadStringAsBinaryResponse;
                if (this._parser != null)
                {
                    this._parser.ExpectReadStringAsBinaryResponse = this._expectReadStringAsBinaryResponse;
                }
            }
        }

        public string ExtraHeaderData
        {
            get;
            set;
        }

        public string FirmwareVersion
        {
            get;
            private set;
        }

        public Cognex.DataMan.SDK.ResultTypes ResultTypes
        {
            get;
            private set;
        }

        public ConnectionState State
        {
            get
            {
                return this._connectionState;
            }
        }

        static DataManSystem()
        {
            DataManSystem._nextCommandId = 100;
        }

        public DataManSystem(ISystemConnector connector)
        {
            this._commandQueueChangedEvent = new EventWaitHandlePortable(false);
            this._callbacksQueueChangedEvent = new EventWaitHandlePortable(false);
            this._threadExitEvent = new EventWaitHandlePortable(true);
            this._connector = connector;
            this.ResultTypes = Cognex.DataMan.SDK.ResultTypes.None;
            this.DefaultTimeout = 30000;
            this.ExpectReadStringAsBinaryResponse = false;
            this.Encoding = System.Text.Encoding.Default;
        }

        [Conditional("DEBUG_INPUT_BYTES")]
        private void AddInputByteForDebug(byte b)
        {
        }

        public void Backup(string fileName)
        {
            this.EndBackup(this.BeginBackup(fileName, null, null));
        }

        public IAsyncResult BeginBackup(string fileName, AsyncCallback callback, object state)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException("fileName is null");
            }
            return this.BeginSendCommandImpl("DEVICE.BACKUP", null, this.DefaultTimeout, true, fileName, callback, state);
        }

        public IAsyncResult BeginGetBufferedImage(int index, ImageFormat imageFormat, ImageSize imageSize, ImageQuality imageQuality, AsyncCallback callback, object state)
        {
            object[] objArray = new object[] { "IMAGE.SEND-BUFFER ", index, " ", imageSize.GetHashCode(), " ", imageFormat.GetHashCode(), " ", imageQuality.GetHashCode() };
            string str = string.Concat(objArray);
            return this.BeginSendCommandImpl(str, null, this.DefaultTimeout, true, null, callback, state);
        }

        public IAsyncResult BeginGetConfig(string fileName, AsyncCallback callback, object state)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException("fileName is null");
            }
            return this.BeginSendCommandImpl("CONFIG.SEND", null, this.DefaultTimeout, true, fileName, callback, state);
        }

        public IAsyncResult BeginGetLastReadImage(AsyncCallback callback, object state)
        {
            string str = "IMAGE.SEND";
            return this.BeginSendCommandImpl(str, null, this.DefaultTimeout, true, null, callback, state);
        }

        public IAsyncResult BeginGetLiveImage(ImageFormat imageFormat, ImageSize imageSize, ImageQuality imageQuality, AsyncCallback callback, object state)
        {
            object[] hashCode = new object[] { "LIVEIMG.SEND ", imageSize.GetHashCode(), " ", imageFormat.GetHashCode(), " ", imageQuality.GetHashCode() };
            string str = string.Concat(hashCode);
            return this.BeginSendCommandImpl(str, null, this.DefaultTimeout, true, null, callback, state);
        }

        public IAsyncResult BeginRestore(string fileName, AsyncCallback callback, object state)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException("fileName is null");
            }
            if (!File.Exists(fileName))
            {
                throw new IOException("File does not exist");
            }
            byte[] numArray = new byte[(int)checked((IntPtr)(new FileInfo(fileName)).Length)];
            int num = 0;
            using (FileStream fileStream = File.OpenRead(fileName))
            {
                while (true)
                {
                    int num1 = fileStream.Read(numArray, num, (int)fileStream.Length - num);
                    if (num1 == 0)
                    {
                        break;
                    }
                    num += num1;
                }
            }
            string str = string.Format("DEVICE.RESTORE {0}", (int)numArray.Length);
            return this.BeginSendCommandImpl(str, numArray, this.DefaultTimeout, false, null, callback, state);
        }

        public IAsyncResult BeginSendCommand(string command, AsyncCallback callback, object state)
        {
            return this.BeginSendCommandImpl(command, null, this.DefaultTimeout, false, null, callback, state);
        }

        private IAsyncResult BeginSendCommandImpl(string command, byte[] binaryData, int timeout, bool expectBinaryResponse, object commandState, AsyncCallback userCallback, object userState)
        {
            CommandInfo commandInfo = new CommandInfo()
            {
                Command = command,
                CommandId = this.GetNextCommandId(),
                BinaryData = binaryData,
                Timeout = timeout,
                CommandState = commandState,
                UserCallback = userCallback,
                UserState = userState,
                ExpectBinaryResponseForCommand = expectBinaryResponse,
                StartTime = DateTime.Now,
                CompletionTime = DateTime.MinValue
            };
            this.Log("BeginSendCommandImpl", string.Format("Queuing command ({0}) for execution ({1})", commandInfo.CommandId, command));
            lock (this._queuedCommands)
            {
                this._queuedCommands.Enqueue(commandInfo);
            }
            this._commandQueueChangedEvent.Set();
            return commandInfo;
        }

        public IAsyncResult BeginSetConfig(string fileName, AsyncCallback callback, object state)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException("fileName is null");
            }
            if (!File.Exists(fileName))
            {
                throw new IOException("File does not exist");
            }
            byte[] numArray = new byte[(int)checked((IntPtr)(new FileInfo(fileName)).Length)];
            int num = 0;
            using (FileStream fileStream = File.OpenRead(fileName))
            {
                while (true)
                {
                    int num1 = fileStream.Read(numArray, num, (int)fileStream.Length - num);
                    if (num1 == 0)
                    {
                        break;
                    }
                    num += num1;
                }
            }
            string str = string.Format("CONFIG.LOAD {0}", (int)numArray.Length);
            return this.BeginSendCommandImpl(str, numArray, this.DefaultTimeout, false, null, callback, state);
        }

        public IAsyncResult BeginUpdateFirmware(string strippedFileName, Stream fwFileStream, bool reboot, AsyncCallback callback, object state, int timeout)
        {
            if (this._connectionState != ConnectionState.Connected)
            {
                throw new SystemDisconnectedException();
            }
            string str = null;
            if (!this.CheckFirmware(fwFileStream, out str))
            {
                throw new InvalidFirmwareFileException(str);
            }
            fwFileStream.Seek((long)0, SeekOrigin.Begin);
            string str1 = fwFileStream.Length.ToString();
            DateTime now = DateTime.Now;
            string str2 = this.EscapeDmccPayload(now.ToString("dd.MM.yyyy HH:mm:ss"));
            byte[] numArray = new byte[(int)checked((IntPtr)fwFileStream.Length)];
            fwFileStream.Read(numArray, 0, (int)fwFileStream.Length);
            fwFileStream.Close();
            string str3 = this.EscapeDmccPayload(string.Concat(WindowsIdentity.GetCurrent().Name.ToString(), "@", Dns.GetHostEntry(Dns.GetHostName()).HostName));
            string fileName = Path.GetFileName(strippedFileName);
            string[] strArrays = new string[] { (reboot ? "FIRMWARE.LOAD " : "FIRMWARE.LOAD-NOREBOOT "), str1, " \"", str2, "\" \"", str3, "\" \"", fileName, "\"" };
            string str4 = string.Concat(strArrays);
            return this.BeginSendCommandImpl(str4, numArray, timeout, false, null, callback, state);
        }

        public IAsyncResult BeginUploadFeatureKey(string fileName, AsyncCallback callback, object state)
        {
            string end;
            if (fileName == null)
            {
                throw new NullReferenceException("File name is null");
            }
            if (!File.Exists(fileName))
            {
                throw new Exception("File does not exist");
            }
            XmlDocument xmlDocument = new XmlDocument();
            using (StreamReader streamReader = new StreamReader(fileName))
            {
                end = streamReader.ReadToEnd();
            }
            xmlDocument.LoadXml(end);
            string innerText = "";
            foreach (XmlNode childNode in xmlDocument.DocumentElement.ChildNodes)
            {
                if (!childNode.Name.Equals("LicenseKey"))
                {
                    continue;
                }
                innerText = childNode.InnerText;
                break;
            }
            char[] charArray = innerText.ToCharArray();
            int length = (int)charArray.Length;
            string str = string.Concat("||>FKEY.LOAD ", length.ToString());
            byte[] numArray = new byte[(int)charArray.Length];
            for (int i = 0; i < (int)charArray.Length; i++)
            {
                numArray[i] = (byte)charArray[i];
            }
            return this.BeginSendCommandImpl(str, numArray, this.DefaultTimeout, false, null, callback, state);
        }

        private void CallbackDispatcherThread()
        {
            EventWaitHandlePortable[] eventWaitHandlePortableArray = new EventWaitHandlePortable[] { this._threadExitEvent, this._callbacksQueueChangedEvent };
            this.Log("CallbackDispatcherThread", "Starting main loop...");
        Label0:
            while (1 == EventWaitHandlePortable.WaitAny(eventWaitHandlePortableArray))
            {
                int num = 1;
                while (true)
                {
                    DataManSystem.GenericCallback genericCallback = this.DequeueCallback();
                    if (genericCallback == null)
                    {
                        goto Label0;
                    }
                    genericCallback();
                    if (num > 30)
                    {
                        num = 0;
                        if (this._threadExitEvent.WaitOne(0))
                        {
                            break;
                        }
                    }
                    num++;
                }
                this.Log("CallbackDispatcherThread", "Thread exit event signaled.");
            }
            this.Log("CallbackDispatcherThread", "Finished.");
        }

        public bool Cancel(IAsyncResult asyncResult)
        {
            bool flag;
            CommandInfo commandInfo = asyncResult as CommandInfo;
            if (commandInfo == null)
            {
                throw new ArgumentException();
            }
            lock (this._pendingCommands)
            {
                if (!this._pendingCommands.RemoveCommand(commandInfo.CommandId))
                {
                    return false;
                }
                else
                {
                    commandInfo.SetError(new Cognex.DataMan.SDK.OperationCanceledException(commandInfo.Command));
                    flag = true;
                }
            }
            return flag;
        }

        public bool CheckFirmware(Stream fwFileStream, out string errorMessage)
        {
            string[] strArrays;
            string payLoad;
            int num;
            int num1;
            int num2;
            int num3;
            bool flag;
            MatchCollection matchCollections = null;
            Regex regex = null;
            BinaryReader binaryReader = new BinaryReader(fwFileStream);
            byte[] numArray = binaryReader.ReadBytes(8);
            byte[] numArray1 = new byte[] { 78, 74, 67, 85 };
            bool flag1 = false;
            int num4 = 0;
            while (numArray[num4] == numArray1[num4])
            {
                num4++;
                if (num4 != 4)
                {
                    continue;
                }
                flag1 = true;
                break;
            }
            bool flag2 = true;
            if (!flag1)
            {
                binaryReader.BaseStream.Position = (long)0;
                long length = binaryReader.BaseStream.Length;
            }
            else
            {
                ref byte numPointer = ref numArray[4];
                numPointer = (byte)(numPointer ^ 13);
                ref byte numPointer1 = ref numArray[5];
                numPointer1 = (byte)(numPointer1 ^ 13);
                ref byte numPointer2 = ref numArray[6];
                numPointer2 = (byte)(numPointer2 ^ 13);
                ref byte numPointer3 = ref numArray[7];
                numPointer3 = (byte)(numPointer3 ^ 13);
                StreamReader streamReader = new StreamReader(new MemoryStream(numArray, 4, 4));
                int num5 = int.Parse(streamReader.ReadToEnd(), NumberStyles.HexNumber);
                byte[] numArray2 = new byte[num5 - 1];
                binaryReader.ReadByte();
                for (int i = 0; i < num5 - 1; i++)
                {
                    numArray2[i] = binaryReader.ReadByte();
                    ref byte numPointer4 = ref numArray2[i];
                    numPointer4 = (byte)(numPointer4 ^ 13);
                }
                long length1 = binaryReader.BaseStream.Length;
                string end = (new StreamReader(new MemoryStream(numArray2))).ReadToEnd();
                regex = new Regex("\\|");
                string[] strArrays1 = regex.Split(end);
                if ((int)strArrays1.Length < 3)
                {
                    throw new InvalidFirmwareFileException("File format is incorrect.");
                }
                strArrays = strArrays1[0].Split(new char[] { ';' });
                string str = strArrays1[1];
                string str1 = strArrays1[2];
                string payLoad1 = null;
                try
                {
                    payLoad1 = this.SendCommand("GET DEVICE.SHIPPEDAS-VER").PayLoad;
                }
                catch
                {
                }
                string payLoad2 = null;
                try
                {
                    payLoad2 = this.SendCommand("GET DEVICE.FIRMWARE-VER").PayLoad;
                }
                catch
                {
                    try
                    {
                        payLoad2 = this.SendCommand("GET PDT.FIRMWARE-VER").PayLoad;
                    }
                    catch
                    {
                    }
                }
                string str2 = null;
                if (!string.IsNullOrEmpty(payLoad2))
                {
                    string[] strArrays2 = payLoad2.Split(new char[] { '.' });
                    if ((int)strArrays2.Length > 1)
                    {
                        str2 = string.Concat(strArrays2[0], ".", strArrays2[1]);
                    }
                }
                payLoad = null;
                try
                {
                    payLoad = this.SendCommand("GET DEVICE.ID").PayLoad;
                }
                catch
                {
                }
                if (payLoad1 == null)
                {
                    payLoad1 = "2.1";
                }
                if (str2 == null)
                {
                    str2 = "2.0";
                }
                if (payLoad == null)
                {
                    string payLoad3 = "";
                    try
                    {
                        payLoad3 = this.SendCommand("GET DEVICE.TYPE").PayLoad;
                    }
                    catch
                    {
                    }
                    payLoad = DataManSystem.GetDeviceIDForLegacySystems(payLoad3);
                    if (payLoad == null)
                    {
                        errorMessage = "Connected device unknown.";
                        return false;
                    }
                }
                regex = new Regex("\\d+");
                matchCollections = regex.Matches(str1);
                int.Parse(matchCollections[0].Value);
                int.Parse(matchCollections[1].Value);
                matchCollections = regex.Matches(payLoad1);
                num = 0;
                try
                {
                    num = int.Parse(matchCollections[0].Value);
                }
                catch (Exception exception)
                {
                    num = 0;
                }
                num1 = 0;
                try
                {
                    num1 = int.Parse(matchCollections[1].Value);
                }
                catch (Exception exception1)
                {
                    num1 = 0;
                }
                matchCollections = regex.Matches(str2);
                num2 = 0;
                num3 = 0;
                try
                {
                    num2 = int.Parse(matchCollections[0].Value);
                    num3 = int.Parse(matchCollections[1].Value);
                    goto Label0;
                }
                catch (Exception exception2)
                {
                    errorMessage = "Connected device unknown.";
                    flag = false;
                }
                return flag;
            }
            if (flag2)
            {
                errorMessage = "Connected device and firmware don't match.";
                return false;
            }
            errorMessage = null;
            return true;
        Label0:
            if (num2 <= num && num2 >= num)
            {
                Math.Max(num3, num1);
            }
            string[] strArrays3 = strArrays;
            for (int j = 0; j < (int)strArrays3.Length; j++)
            {
                if (strArrays3[j].Equals(payLoad) || payLoad.Equals("0"))
                {
                    flag2 = false;
                }
            }
            int num6 = 0;
            try
            {
                num6 = int.Parse(payLoad);
            }
            catch (Exception exception3)
            {
                num6 = 0;
            }
            if ((num6 == 12 || num6 == 13 || num6 == 17 || num6 == 18) && (num2 > 2 || num2 > 1 && num3 > 2) || num6 == 14 && (num2 > 1 || num2 > 0 && num3 > 1) || num6 == 15 || num6 == 16 || num6 > 18)
            {
                binaryReader.BaseStream.Position = (long)0;
                long length2 = binaryReader.BaseStream.Length;
                if (flag2)
                {
                    errorMessage = "Connected device and firmware don't match.";
                    return false;
                }
                errorMessage = null;
                return true;
            }
            else
            {
                if (flag2)
                {
                    errorMessage = "Connected device and firmware don't match.";
                    return false;
                }
                errorMessage = null;
                return true;
            }
        }

        private void CleanupConnection()
        {
            lock (this._queuedCommands)
            {
                foreach (CommandInfo _queuedCommand in this._queuedCommands)
                {
                    _queuedCommand.SetError(new Cognex.DataMan.SDK.OperationCanceledException(_queuedCommand.Command));
                }
                this._queuedCommands.Clear();
            }
            lock (this._pendingCommands)
            {
                foreach (CommandInfo commandInfo in this._pendingCommands.TakeAll())
                {
                    commandInfo.SetError(new Cognex.DataMan.SDK.OperationCanceledException(commandInfo.Command));
                }
            }
            this._threadExitEvent.Set();
            if (this._keepAliveThread != null)
            {
                if (this._keepAliveThread != Thread.CurrentThread)
                {
                    this._keepAliveThread.Join();
                }
                this._keepAliveThread = null;
            }
            if (this._writerThread != null)
            {
                if (this._writerThread != Thread.CurrentThread)
                {
                    this._writerThread.Join();
                }
                this._writerThread = null;
            }
            if (this._readerThread != null)
            {
                if (this._readerThread != Thread.CurrentThread)
                {
                    this._readerThread.Join();
                }
                this._readerThread = null;
            }
            if (this._callbackDispatcherThread != null)
            {
                if (this._callbackDispatcherThread != Thread.CurrentThread)
                {
                    this._callbackDispatcherThread.Join();
                }
                this._callbackDispatcherThread = null;
            }
        }

        public void Connect()
        {
            this.Connect(-1);
        }

        public void Connect(int timeout)
        {
            lock (this)
            {
                if (this._connectionState != ConnectionState.Connected)
                {
                    this._connectionState = ConnectionState.Connecting;
                    try
                    {
                        this._connector.Connect(timeout);
                    }
                    catch (Exception exception1)
                    {
                        Exception exception = exception1;
                        this._connectionState = ConnectionState.Disconnected;
                        throw exception;
                    }
                    this._threadExitEvent.Reset();
                    this._writerThread = new Thread(new ThreadStart(this.WriterThread))
                    {
                        IsBackground = true,
                        Name = "DataMan SDK Writer Thread"
                    };
                    this._writerThread.Start();
                    this._readerThread = new Thread(new ThreadStart(this.ReaderThreadFunc))
                    {
                        IsBackground = true,
                        Name = "DataMan SDK Reader Thread"
                    };
                    this._readerThread.Start();
                    this._callbackDispatcherThread = new Thread(new ThreadStart(this.CallbackDispatcherThread))
                    {
                        IsBackground = true,
                        Name = "DataMan SDK CallbackDispatcher Thread"
                    };
                    this._callbackDispatcherThread.Start();
                    try
                    {
                        try
                        {
                            this.SendCommandInternal("SET COM.DMCC-RESPONSE 1", null, 5000);
                        }
                        catch (Exception exception3)
                        {
                            Exception exception2 = exception3;
                            this.Log("Connect", string.Format("Basic connection command failed: SET COM.DMCC-RESPONSE 1 ({0})", exception2.Message));
                            throw;
                        }
                        try
                        {
                            this.SendCommandInternal("SET COM.DMCC-HEADER 1", null, 5000);
                        }
                        catch (Exception exception5)
                        {
                            Exception exception4 = exception5;
                            this.Log("Connect", string.Format("Basic connection command failed: SET COM.DMCC-HEADER 1 ({0})", exception4.Message));
                            throw;
                        }
                        try
                        {
                            this.SetResultTypesInternal(this.ResultTypes);
                        }
                        catch (Exception exception7)
                        {
                            Exception exception6 = exception7;
                            this.Log("Connect", string.Format("Command failed: SetResultTypes ({0})", exception6.Message));
                        }
                        try
                        {
                            this.SendCommandInternal("SET COM.DMCC-EVENT 255", null, 5000);
                        }
                        catch (Exception exception9)
                        {
                            Exception exception8 = exception9;
                            this.Log("Connect", string.Format("Command failed: SET COM.DMCC-EVENT 255 ({0})", exception8.Message));
                        }
                        try
                        {
                            DmccResponse dmccResponse = this.SendCommandInternal("GET DEVICE.FIRMWARE-VER", null, 5000);
                            if (dmccResponse != null && dmccResponse.PayLoad != null)
                            {
                                this.FirmwareVersion = dmccResponse.PayLoad.Trim();
                            }
                        }
                        catch (Exception exception11)
                        {
                            Exception exception10 = exception11;
                            this.Log("Connect", string.Format("Command failed: GET DEVICE.FIRMWARE-VER ({0})", exception10.Message));
                        }
                    }
                    catch (Exception exception13)
                    {
                        Exception exception12 = exception13;
                        this._connectionState = ConnectionState.Disconnecting;
                        this._connector.Disconnect();
                        this._connectionState = ConnectionState.Disconnected;
                        this.CleanupConnection();
                        throw exception12;
                    }
                    if (this._keepAliveEnabled)
                    {
                        this._keepAliveThread = new Thread(new ThreadStart(this.KeepAliveThread))
                        {
                            IsBackground = true,
                            Name = "DataMan SDK KeepAlive Thread"
                        };
                        this._keepAliveThread.Start();
                    }
                    this._connectionState = ConnectionState.Connected;
                }
                else
                {
                    return;
                }
            }
            this.RaiseSystemConnectedEvent();
        }

        private DataManSystem.GenericCallback DequeueCallback()
        {
            DataManSystem.GenericCallback genericCallback;
            lock (this._queuedCallbacks)
            {
                if (this._queuedCallbacks.Count != 0)
                {
                    genericCallback = this._queuedCallbacks.Dequeue();
                }
                else
                {
                    genericCallback = null;
                }
            }
            return genericCallback;
        }

        private CommandInfo DequeueWritableCommand()
        {
            CommandInfo commandInfo;
            lock (this._queuedCommands)
            {
                if (this._queuedCommands.Count != 0)
                {
                    commandInfo = this._queuedCommands.Dequeue();
                }
                else
                {
                    commandInfo = null;
                }
            }
            return commandInfo;
        }

        private static Cognex.DataMan.SDK.ResultTypes DetermineAutomaticResponseDataType(int responseStatus)
        {
            Cognex.DataMan.SDK.ResultTypes resultType = Cognex.DataMan.SDK.ResultTypes.None;
            switch (responseStatus)
            {
                case 1:
                    {
                        resultType = Cognex.DataMan.SDK.ResultTypes.ReadString;
                        return resultType;
                    }
                case 2:
                case 8:
                case 9:
                case 11:
                case 12:
                    {
                        return resultType;
                    }
                case 3:
                    {
                        resultType = Cognex.DataMan.SDK.ResultTypes.ReadXml;
                        return resultType;
                    }
                case 4:
                    {
                        resultType = Cognex.DataMan.SDK.ResultTypes.XmlStatistics;
                        return resultType;
                    }
                case 5:
                    {
                        resultType = Cognex.DataMan.SDK.ResultTypes.Image;
                        return resultType;
                    }
                case 6:
                    {
                        resultType = Cognex.DataMan.SDK.ResultTypes.ImageGraphics;
                        return resultType;
                    }
                case 7:
                    {
                        resultType = Cognex.DataMan.SDK.ResultTypes.TrainingResults;
                        return resultType;
                    }
                case 10:
                    {
                        resultType = Cognex.DataMan.SDK.ResultTypes.CodeQualityData;
                        return resultType;
                    }
                case 13:
                    {
                        resultType = Cognex.DataMan.SDK.ResultTypes.InputEvent;
                        return resultType;
                    }
                case 14:
                    {
                        resultType = Cognex.DataMan.SDK.ResultTypes.GroupTriggering;
                        return resultType;
                    }
                case 15:
                    {
                        resultType = Cognex.DataMan.SDK.ResultTypes.ProcessControlMetricsReport;
                        return resultType;
                    }
                default:
                    {
                        return resultType;
                    }
            }
        }

        public void Disconnect()
        {
            bool flag = false;
            lock (this)
            {
                if (this._connectionState == ConnectionState.Connected)
                {
                    this._connectionState = ConnectionState.Disconnecting;
                    if (!this._connector.Disconnect())
                    {
                        this._connectionState = ConnectionState.Disconnected;
                    }
                    else
                    {
                        this._connectionState = ConnectionState.Disconnected;
                        this.CleanupConnection();
                        flag = true;
                    }
                }
                else
                {
                    return;
                }
            }
            if (flag)
            {
                this.RaiseSystemDisconnectedEvent();
            }
        }

        public void Dispose()
        {
            this.DoDispose(true);
            GC.SuppressFinalize(this);
        }

        private void DoDispose(bool disposeManagedResources)
        {
            this.Disconnect();
            if (this._commandQueueChangedEvent != null)
            {
                this._commandQueueChangedEvent.Close();
            }
            if (this._callbacksQueueChangedEvent != null)
            {
                this._callbacksQueueChangedEvent.Close();
            }
            if (this._threadExitEvent != null)
            {
                this._threadExitEvent.Close();
            }
        }

        public void EndBackup(IAsyncResult asyncResult)
        {
            CommandInfo commandInfo = (CommandInfo)asyncResult;
            if (commandInfo == null)
            {
                throw new ArgumentException();
            }
            DmccResponse dmccResponse = this.EndSendCommandImpl(asyncResult);
            if (dmccResponse.Status != 0)
            {
                throw new InvalidResponseException(commandInfo.Command);
            }
            using (FileStream fileStream = File.Open((string)commandInfo.CommandState, FileMode.Create, FileAccess.Write))
            {
                byte[] array = dmccResponse.BinaryData.ToArray();
                fileStream.Write(array, 0, (int)array.Length);
            }
        }

        public Image EndGetBufferedImage(IAsyncResult asyncResult)
        {
            CommandInfo commandInfo = (CommandInfo)asyncResult;
            if (commandInfo == null)
            {
                throw new ArgumentException();
            }
            DmccResponse dmccResponse = this.EndSendCommandImpl(asyncResult);
            if (dmccResponse.Status != 0)
            {
                throw new InvalidResponseException(commandInfo.Command);
            }
            Image imageFromDmccResponse = this.GetImageFromDmccResponse(dmccResponse);
            if (imageFromDmccResponse == null)
            {
                throw new InvalidResponseException(commandInfo.Command);
            }
            return imageFromDmccResponse;
        }

        public void EndGetConfig(IAsyncResult asyncResult)
        {
            CommandInfo commandInfo = (CommandInfo)asyncResult;
            if (commandInfo == null)
            {
                throw new ArgumentException();
            }
            DmccResponse dmccResponse = this.EndSendCommandImpl(asyncResult);
            if (dmccResponse.Status != 0)
            {
                throw new InvalidResponseException(commandInfo.Command);
            }
            using (FileStream fileStream = File.Open((string)commandInfo.CommandState, FileMode.Create, FileAccess.Write))
            {
                byte[] array = dmccResponse.BinaryData.ToArray();
                fileStream.Write(array, 0, (int)array.Length);
            }
        }

        public Image EndGetLastReadImage(IAsyncResult asyncResult)
        {
            CommandInfo commandInfo = (CommandInfo)asyncResult;
            if (commandInfo == null)
            {
                throw new ArgumentException();
            }
            DmccResponse dmccResponse = this.EndSendCommandImpl(asyncResult);
            if (dmccResponse.Status != 0)
            {
                throw new InvalidResponseException(commandInfo.Command);
            }
            Image imageFromDmccResponse = this.GetImageFromDmccResponse(dmccResponse);
            if (imageFromDmccResponse == null)
            {
                throw new InvalidResponseException(commandInfo.Command);
            }
            return imageFromDmccResponse;
        }

        public Image EndGetLiveImage(IAsyncResult asyncResult)
        {
            CommandInfo commandInfo = (CommandInfo)asyncResult;
            if (commandInfo == null)
            {
                throw new ArgumentException();
            }
            DmccResponse dmccResponse = this.EndSendCommandImpl(asyncResult);
            if (dmccResponse.Status != 0)
            {
                throw new InvalidResponseException(commandInfo.Command);
            }
            Image imageFromDmccResponse = this.GetImageFromDmccResponse(dmccResponse);
            if (imageFromDmccResponse == null)
            {
                throw new InvalidResponseException(commandInfo.Command);
            }
            return imageFromDmccResponse;
        }

        public void EndRestore(IAsyncResult asyncResult)
        {
            CommandInfo commandInfo = (CommandInfo)asyncResult;
            if (commandInfo == null)
            {
                throw new ArgumentException();
            }
            if (this.EndSendCommandImpl(asyncResult).Status != 0)
            {
                throw new InvalidResponseException(commandInfo.Command);
            }
        }

        public DmccResponse EndSendCommand(IAsyncResult asyncResult)
        {
            return this.EndSendCommandImpl(asyncResult);
        }

        private DmccResponse EndSendCommandImpl(IAsyncResult asyncResult)
        {
            CommandInfo commandInfo = (CommandInfo)asyncResult;
            bool flag = false;
            if (commandInfo == null)
            {
                throw new ArgumentException();
            }
            if (!commandInfo.SendCompleteWaitHandle.WaitOne(commandInfo.Timeout, false))
            {
                this.Log("EndSendCommandImpl", string.Format("Command ({0}) sending timed out.", commandInfo.CommandId));
                throw new TimeoutException();
            }
            if (commandInfo.Error != null)
            {
                this.Log("EndSendCommandImpl", string.Format("Command ({0}) could not be sent.", commandInfo.CommandId));
                throw commandInfo.Error;
            }
            commandInfo.AsyncWaitHandle.WaitOne(commandInfo.Timeout, false);
            lock (this._pendingCommands)
            {
                flag = this._pendingCommands.RemoveCommand(commandInfo.CommandId);
            }
            if (flag)
            {
                this.Log("EndSendCommandImpl", string.Concat("Command (", commandInfo.CommandId, ") timed out."));
                throw new TimeoutException();
            }
            if (commandInfo.Error != null)
            {
                this.Log("EndSendCommandImpl", string.Format("Command ({0}) completed with error ({1})", commandInfo.CommandId, commandInfo.Error.Message));
                throw commandInfo.Error;
            }
            this.Log("EndSendCommandImpl", string.Concat("Command (", commandInfo.CommandId, ") completed."));
            return commandInfo.Response;
        }

        public void EndSetConfig(IAsyncResult asyncResult)
        {
            CommandInfo commandInfo = (CommandInfo)asyncResult;
            if (commandInfo == null)
            {
                throw new ArgumentException();
            }
            if (this.EndSendCommandImpl(asyncResult).Status != 0)
            {
                throw new InvalidResponseException(commandInfo.Command);
            }
        }

        public void EndUpdateFirmware(IAsyncResult asyncResult)
        {
            CommandInfo commandInfo = (CommandInfo)asyncResult;
            if (commandInfo == null)
            {
                throw new ArgumentException();
            }
            if (this.EndSendCommandImpl(asyncResult).Status != 0)
            {
                throw new InvalidResponseException(commandInfo.Command);
            }
        }

        public void EndUploadFeatureKey(IAsyncResult asyncResult)
        {
            CommandInfo commandInfo = (CommandInfo)asyncResult;
            if (commandInfo == null)
            {
                throw new ArgumentException();
            }
            if (this.EndSendCommandImpl(asyncResult).Status != 0)
            {
                throw new InvalidResponseException(commandInfo.Command);
            }
        }

        private void EnqueueCallback(DataManSystem.GenericCallback callback)
        {
            lock (this._queuedCallbacks)
            {
                this._queuedCallbacks.Enqueue(callback);
                this._callbacksQueueChangedEvent.Set();
            }
        }

        private string EscapeDmccPayload(string input)
        {
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(input);
            return System.Text.Encoding.ASCII.GetString(this.EscapeDmccPayloadBytes(bytes), 0, (int)bytes.Length);
        }

        private byte[] EscapeDmccPayloadBytes(byte[] aBytes)
        {
            int num = 0;
            for (int i = 0; i < (int)aBytes.Length; i++)
            {
                byte num1 = aBytes[i];
                if (num1 <= 34)
                {
                    switch (num1)
                    {
                        case 9:
                        case 10:
                        case 13:
                            {
                                break;
                            }
                        case 11:
                        case 12:
                            {
                                goto Label0;
                            }
                        default:
                            {
                                if (num1 == 34)
                                {
                                    break;
                                }
                                goto Label0;
                            }
                    }
                }
                else if (num1 != 92 && num1 != 124)
                {
                    goto Label0;
                }
                num++;
            Label0:
                _ = 0;
            }
            if (num == 0)
            {
                return aBytes;
            }
            byte[] numArray = new byte[(int)aBytes.Length + num];
            int num2 = 0;
            int num3 = 0;
            while (num2 < (int)aBytes.Length)
            {
                byte num4 = aBytes[num2];
                if (num4 <= 34)
                {
                    switch (num4)
                    {
                        case 9:
                            {
                                int num5 = num3;
                                num3 = num5 + 1;
                                numArray[num5] = 92;
                                int num6 = num3;
                                num3 = num6 + 1;
                                numArray[num6] = 116;
                                break;
                            }
                        case 10:
                            {
                                int num7 = num3;
                                num3 = num7 + 1;
                                numArray[num7] = 92;
                                int num8 = num3;
                                num3 = num8 + 1;
                                numArray[num8] = 110;
                                break;
                            }
                        case 11:
                        case 12:
                            {
                                goto Label1;
                            }
                        case 13:
                            {
                                int num9 = num3;
                                num3 = num9 + 1;
                                numArray[num9] = 92;
                                int num10 = num3;
                                num3 = num10 + 1;
                                numArray[num10] = 114;
                                break;
                            }
                        default:
                            {
                                if (num4 == 34)
                                {
                                    int num11 = num3;
                                    num3 = num11 + 1;
                                    numArray[num11] = 92;
                                    int num12 = num3;
                                    num3 = num12 + 1;
                                    numArray[num12] = 34;
                                    break;
                                }
                                else
                                {
                                    goto Label1;
                                }
                            }
                    }
                }
                else if (num4 == 92)
                {
                    int num13 = num3;
                    num3 = num13 + 1;
                    numArray[num13] = 92;
                    int num14 = num3;
                    num3 = num14 + 1;
                    numArray[num14] = 92;
                }
                else
                {
                    if (num4 != 124)
                    {
                        goto Label1;
                    }
                    int num15 = num3;
                    num3 = num15 + 1;
                    numArray[num15] = 92;
                    int num16 = num3;
                    num3 = num16 + 1;
                    numArray[num16] = 124;
                }
            Label3:
                num2++;

                return numArray;
            Label1:
                int num17 = num3;
                num3 = num17 + 1;
                numArray[num17] = aBytes[num2];
                goto Label3;
            }

            return numArray;
        }

        ~DataManSystem()
        {
            this.DoDispose(false);
        }

        private CommandInfo FindPendingCommand(int commandId)
        {
            CommandInfo commandInfo;
            lock (this._pendingCommands)
            {
                commandInfo = this._pendingCommands.FindPendingCommand(commandId) as CommandInfo;
            }
            return commandInfo;
        }

        public Image GetBufferedImage(int index, ImageFormat imageFormat, ImageSize imageSize, ImageQuality imageQuality)
        {
            return this.EndGetBufferedImage(this.BeginGetBufferedImage(index, imageFormat, imageSize, imageQuality, null, null));
        }

        public void GetConfig(string fileName)
        {
            this.EndGetConfig(this.BeginGetConfig(fileName, null, null));
        }

        private static string GetDeviceIDForLegacySystems(string modelName)
        {
            if (string.IsNullOrEmpty(modelName))
            {
                return null;
            }
            string str = modelName;
            string str1 = str;
            if (str != null)
            {
                switch (str1)
                {
                    case "VS2300":
                        {
                            return "1";
                        }
                    case "VS6300":
                        {
                            return "2";
                        }
                    case "VS1300":
                        {
                            return "3";
                        }
                    case "VS2300M":
                        {
                            return "4";
                        }
                    case "VS2300Semi":
                        {
                            return "5";
                        }
                    case "VS6300Semi":
                        {
                            return "6";
                        }
                    case "VS6200":
                        {
                            return "7";
                        }
                    case "VS2300MCT":
                        {
                            return "8";
                        }
                    case "DM6400":
                        {
                            return "9";
                        }
                    case "DM6500":
                        {
                            return "10";
                        }
                    case "DM6500SHD":
                        {
                            return "11";
                        }
                    case "DM7550":
                        {
                            return "12";
                        }
                    case "6320":
                        {
                            return "12";
                        }
                    case "DM7500":
                        {
                            return "13";
                        }
                    case "6300":
                        {
                            return "13";
                        }
                    case "DM100":
                        {
                            return "14";
                        }
                    case "DM7100":
                        {
                            return "15";
                        }
                    case "DM7150":
                        {
                            return "16";
                        }
                    case "DM7550LR":
                        {
                            return "17";
                        }
                    case "6320ILR":
                        {
                            return "17";
                        }
                    case "DM7500LR":
                        {
                            return "18";
                        }
                    case "6300ILR":
                        {
                            return "18";
                        }
                    case "DM700":
                        {
                            return "19";
                        }
                    case "DMPC":
                        {
                            return "20";
                        }
                    case "DM700G2":
                        {
                            return "21";
                        }
                    case "DM200":
                        {
                            return "22";
                        }
                    case "DM6437EVM":
                        {
                            return "23";
                        }
                    case "DM8100":
                        {
                            return "24";
                        }
                    case "DM8120":
                        {
                            return "25";
                        }
                    case "DM8150":
                        {
                            return "26";
                        }
                    case "DM8500":
                        {
                            return "27";
                        }
                    case "DM8520":
                        {
                            return "28";
                        }
                    case "DM8550":
                        {
                            return "29";
                        }
                    case "DMAE50":
                        {
                            return "30";
                        }
                    case "DM8000Base":
                        {
                            return "31";
                        }
                    case "DM500":
                        {
                            return "32";
                        }
                    case "DM9500":
                        {
                            return "33";
                        }
                    case "DM300":
                        {
                            return "34";
                        }
                    case "DM302":
                        {
                            return "34";
                        }
                    case "DM303":
                        {
                            return "34";
                        }
                    case "DM503":
                        {
                            return "35";
                        }
                    case "AT70":
                        {
                            return "36";
                        }
                    case "DM50":
                        {
                            return "37";
                        }
                    case "DM60":
                        {
                            return "38";
                        }
                    case "DM8050":
                        {
                            return "39";
                        }
                    case "DM8000BaseBT":
                        {
                            return "41";
                        }
                    case "DM8600":
                        {
                            return "40";
                        }
                }
            }
            return "0";
        }

        private Image GetImageFromDmccResponse(DmccResponse response)
        {
            try
            {
                if (response != null && response.BinaryData != null)
                {
                    return ImageArrivedEventArgs.GetImageFromImageBytes(response.BinaryData.ToArray());
                }
            }
            catch
            {
            }
            return null;
        }

        private long GetInactivityTimeSpanMS()
        {
            TimeSpan now = DateTime.Now - this._connector.LastOperationTime;
            return now.Ticks / (long)10000;
        }

        public Image GetLastReadImage()
        {
            return this.EndGetLastReadImage(this.BeginGetLastReadImage(null, null));
        }

        public Image GetLiveImage(ImageFormat imageFormat, ImageSize imageSize, ImageQuality imageQuality)
        {
            return this.EndGetLiveImage(this.BeginGetLiveImage(imageFormat, imageSize, imageQuality, null, null));
        }

        private int GetNextCommandId()
        {
            Interlocked.CompareExchange(ref DataManSystem._nextCommandId, 100, 32768);
            return Interlocked.Increment(ref DataManSystem._nextCommandId);
        }

        private void InitiateDisconnect()
        {
            if (this._connectionState == ConnectionState.Connected)
            {
                ThreadPool.QueueUserWorkItem((object param0) => this.Disconnect(), null);
            }
        }

        private bool IsAutomaticResponse(int status)
        {
            if (status <= 0)
            {
                return false;
            }
            if (status >= 100)
            {
                return false;
            }
            return true;
        }

        private void KeepAliveThread()
        {
            this.Log("KeepAliveThread", "Starting main loop...");
            while (this._keepAliveEnabled)
            {
                if (!this._threadExitEvent.WaitOne(100))
                {
                    if (this.GetInactivityTimeSpanMS() < (long)this._keepAliveTimeout)
                    {
                        continue;
                    }
                    this.Log("KeepAliveThread", string.Concat("No activity for ", this._keepAliveTimeout, "ms, sending keep alive command."));
                    try
                    {
                        this.SendCommand("GET DEVICE.TYPE", this._keepAliveInterval);
                        if (this._threadExitEvent.WaitOne(this._keepAliveInterval))
                        {
                            this.Log("KeepAliveThread", "Thread exit event signaled.");
                            break;
                        }
                    }
                    catch (SystemDisconnectedException systemDisconnectedException)
                    {
                        this.Log("KeepAliveThread", "Caught SystemDisconnectedException. Thread now terminates.");
                        break;
                    }
                    catch
                    {
                        if (this.GetInactivityTimeSpanMS() <= (long)this._keepAliveInterval)
                        {
                            this.Log("KeepAliveThread", "No response to keep alive command, but there were recent activity.");
                        }
                        else
                        {
                            this.Log("KeepAliveThread", "No response to keep alive command.");
                            this.RaiseKeepAliveResponseMissedEvent();
                        }
                    }
                }
                else
                {
                    this.Log("KeepAliveThread", "Thread exit event signaled.");
                    break;
                }
            }
            this.Log("KeepAliveThread", "Finished.");
        }

        private void Log(string function, string message)
        {
            if (this.Connector != null && this.Connector.Logger != null && this.Connector.Logger.Enabled)
            {
                this.Connector.Logger.Log(function, message);
            }
        }

        private void ProcessAutoResponse(DmccResponse response)
        {
            try
            {
                Cognex.DataMan.SDK.ResultTypes resultType = DataManSystem.DetermineAutomaticResponseDataType(response.Status);
                if (resultType != Cognex.DataMan.SDK.ResultTypes.None)
                {
                    if (resultType != Cognex.DataMan.SDK.ResultTypes.ReadString || this.ExpectReadStringAsBinaryResponse)
                    {
                        this.ProcessIncomingAutomaticResponse(response.ResponseId, resultType, response.BinaryData.ToArray());
                    }
                    else
                    {
                        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(response.PayLoad);
                        this.ProcessIncomingAutomaticResponse(response.ResponseId, resultType, bytes);
                    }
                }
                switch (response.Status)
                {
                    case 1:
                        {
                            this.ProcessIncomingReadString(response);
                            goto case 11;
                        }
                    case 2:
                    case 8:
                    case 9:
                    case 11:
                        {
                            break;
                        }
                    case 3:
                        {
                            this.ProcessIncomingXmlResult(response);
                            goto case 11;
                        }
                    case 4:
                        {
                            this.ProcessIncomingXmlStatistics(response);
                            goto case 11;
                        }
                    case 5:
                        {
                            this.ProcessIncomingImage(response);
                            goto case 11;
                        }
                    case 6:
                        {
                            this.ProcessIncomingImageGraphics(response);
                            goto case 11;
                        }
                    case 7:
                        {
                            this.ProcessIncomingTrainingResult(response);
                            goto case 11;
                        }
                    case 10:
                        {
                            this.ProcessIncomingCodeQualityData(response);
                            goto case 11;
                        }
                    case 12:
                        {
                            this.ProcessIncomingStatusEvent(response);
                            goto case 11;
                        }
                    default:
                        {
                            goto case 11;
                        }
                }
            }
            catch (Exception exception1)
            {
                Exception exception = exception1;
                string empty = string.Empty;
                try
                {
                    response.BinaryData.Seek((long)0, SeekOrigin.Begin);
                    empty = Convert.ToBase64String(response.BinaryData.ToArray());
                }
                catch
                {
                }
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(string.Format("Error during processing auto-response - CID={0}, ST={1}, Message={2})", response.CommandId, response.Status, exception.Message));
                stringBuilder.AppendLine("Stack trace:");
                stringBuilder.AppendLine(exception.StackTrace);
                if (!string.IsNullOrEmpty(empty))
                {
                    stringBuilder.AppendLine("Base64 decoded DmccResponse:");
                    stringBuilder.AppendLine(empty);
                }
                this.Log("ProcessAutoResponse", stringBuilder.ToString());
            }
            response.Dispose();
        }

        private void ProcessCommandResponse(DmccResponse response)
        {
            CommandInfo commandInfo = null;
            lock (this._pendingCommands)
            {
                commandInfo = (CommandInfo)this._pendingCommands.FindPendingCommand(response.CommandId);
                if (commandInfo == null)
                {
                    this.Log("ProcessCommandResponse", string.Concat("Response to command (", response.CommandId, ") arrived too late, ignoring response."));
                }
                else
                {
                    this._pendingCommands.RemoveCommand(response.CommandId);
                    int status = response.Status;
                    if (status == 0)
                    {
                        commandInfo.SetComplete(response);
                    }
                    else
                    {
                        switch (status)
                        {
                            case 100:
                                {
                                    commandInfo.SetError(new UnknownErrorException(commandInfo.Command));
                                    break;
                                }
                            case 101:
                                {
                                    commandInfo.SetError(new InvalidCommandException(commandInfo.Command));
                                    break;
                                }
                            case 102:
                                {
                                    commandInfo.SetError(new InvalidParameterException(commandInfo.Command));
                                    break;
                                }
                            case 103:
                                {
                                    commandInfo.SetError(new IncorrectChecksumException(commandInfo.Command));
                                    break;
                                }
                            case 104:
                                {
                                    commandInfo.SetError(new ParameterRejectedException(commandInfo.Command));
                                    break;
                                }
                            case 105:
                                {
                                    commandInfo.SetError(new SystemOfflineException());
                                    break;
                                }
                            default:
                                {
                                    goto case 100;
                                }
                        }
                    }
                }
            }
        }

        private void ProcessIncomingAutomaticResponse(int responseId, Cognex.DataMan.SDK.ResultTypes dataType, byte[] data)
        {
            this.RaiseAutomaticResponseArrivedEvent(responseId, dataType, data);
        }

        private void ProcessIncomingCodeQualityData(DmccResponse response)
        {
            this.RaiseCodeQualityDataArrivedEvent(response.PayLoad);
        }

        private void ProcessIncomingImage(DmccResponse response)
        {
            this.RaiseImageArrivedEvent(response.ResponseId, response.BinaryData.ToArray());
        }

        private void ProcessIncomingImageGraphics(DmccResponse response)
        {
            byte[] array = response.BinaryData.ToArray();
            string str = System.Text.Encoding.ASCII.GetString(array, 0, (int)array.Length);
            this.RaiseImageGraphicsArrivedEvent(response.ResponseId, str);
        }

        private void ProcessIncomingMessage(DmccMessage dmccMessage)
        {
            DmccResponse dmccResponse = new DmccResponse()
            {
                CommandId = dmccMessage.CommandId,
                ResponseId = dmccMessage.ResponseId,
                Status = dmccMessage.ResponseStatusCode,
                PayLoad = dmccMessage.PayLoad,
                BinaryData = dmccMessage.BinaryData
            };
            dmccResponse.BinaryData.Seek((long)0, SeekOrigin.Begin);
            if (dmccResponse.CommandId == -1)
            {
                this.EnqueueCallback(() => this.ProcessAutoResponse(dmccResponse));
                return;
            }
            this.EnqueueCallback(() => this.ProcessCommandResponse(dmccResponse));
        }

        private void ProcessIncomingReadString(DmccResponse response)
        {
            if (!this.ExpectReadStringAsBinaryResponse)
            {
                this.RaiseReadStringArrivedEvent(response.ResponseId, response.PayLoad);
                return;
            }
            byte[] array = response.BinaryData.ToArray();
            string str = System.Text.Encoding.ASCII.GetString(array, 0, (int)array.Length);
            this.RaiseReadStringArrivedEvent(response.ResponseId, str);
        }

        private void ProcessIncomingStatusEvent(DmccResponse response)
        {
            byte[] array = response.BinaryData.ToArray();
            string str = System.Text.Encoding.ASCII.GetString(array, 0, (int)array.Length);
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(str);
                XmlNode xmlNodes = xmlDocument.SelectSingleNode("event/connection/status");
                if (xmlNodes != null)
                {
                    if (string.Equals(xmlNodes.InnerText, "online", StringComparison.OrdinalIgnoreCase))
                    {
                        this.RaiseSystemWentOnlineEvent();
                        return;
                    }
                    else if (string.Equals(xmlNodes.InnerText, "offline", StringComparison.OrdinalIgnoreCase))
                    {
                        this.RaiseSystemWentOfflineEvent();
                        return;
                    }
                }
                this.RaiseStatusEventArrivedEvent(str);
                return;
            }
            catch
            {
            }
        }

        private void ProcessIncomingTrainingResult(DmccResponse response)
        {
            this.RaiseTrainingResultArrivedEvent(response.PayLoad);
        }

        private void ProcessIncomingXmlResult(DmccResponse response)
        {
            byte[] array = response.BinaryData.ToArray();
            string str = System.Text.Encoding.ASCII.GetString(array, 0, (int)array.Length);
            this.RaiseXmlResultArrivedEvent(response.ResponseId, str);
        }

        private void ProcessIncomingXmlStatistics(DmccResponse response)
        {
            byte[] array = response.BinaryData.ToArray();
            string str = System.Text.Encoding.ASCII.GetString(array, 0, (int)array.Length);
            this.RaiseXmlStatisticsArrivedEvent(str);
        }

        private void RaiseAutomaticResponseArrivedEvent(int responseId, Cognex.DataMan.SDK.ResultTypes dataType, byte[] data)
        {
            AutomaticResponseArrivedHandler automaticResponseArrivedHandler = this.AutomaticResponseArrived;
            if (automaticResponseArrivedHandler != null)
            {
                automaticResponseArrivedHandler(this, new AutomaticResponseArrivedEventArgs(responseId, dataType, data));
            }
        }

        private void RaiseBinaryDataTransferProgressEvent(TransferDirection direction, int totalDataSize, int bytesTransferred, Cognex.DataMan.SDK.ResultTypes resultType, int responseId)
        {
            BinaryDataTransferProgressHandler binaryDataTransferProgressHandler = this.BinaryDataTransferProgress;
            if (binaryDataTransferProgressHandler != null)
            {
                binaryDataTransferProgressHandler(this, new BinaryDataTransferProgressEventArgs(direction, totalDataSize, bytesTransferred, resultType, responseId));
            }
        }

        private void RaiseCodeQualityDataArrivedEvent(string data)
        {
            CodeQualityDataArrivedHandler codeQualityDataArrivedHandler = this.CodeQualityDataArrived;
            if (codeQualityDataArrivedHandler != null)
            {
                codeQualityDataArrivedHandler(this, new CodeQualityDataArrivedEventArgs(data));
            }
        }

        private void RaiseImageArrivedEvent(int resultId, byte[] imageBytes)
        {
            ImageArrivedHandler imageArrivedHandler = this.ImageArrived;
            if (imageArrivedHandler != null)
            {
                imageArrivedHandler(this, new ImageArrivedEventArgs(resultId, imageBytes));
            }
        }

        private void RaiseImageGraphicsArrivedEvent(int resultId, string xml)
        {
            ImageGraphicsArrivedHandler imageGraphicsArrivedHandler = this.ImageGraphicsArrived;
            if (imageGraphicsArrivedHandler != null)
            {
                imageGraphicsArrivedHandler(this, new ImageGraphicsArrivedEventArgs(resultId, xml));
            }
        }

        private void RaiseKeepAliveResponseMissedEvent()
        {
            KeepAliveResponseMissedHandler keepAliveResponseMissedHandler = this.KeepAliveResponseMissed;
            if (keepAliveResponseMissedHandler != null)
            {
                keepAliveResponseMissedHandler(this, new EventArgs());
            }
        }

        private void RaiseOffProtocolByteReceivedEvent(byte offProtocolByte)
        {
            OffProtocolByteReceivedHandler offProtocolByteReceivedHandler = this.OffProtocolByteReceived;
            if (offProtocolByteReceivedHandler != null)
            {
                offProtocolByteReceivedHandler(this, new OffProtocolByteReceivedEventArgs(offProtocolByte));
            }
        }

        private void RaiseReadStringArrivedEvent(int resultId, string readString)
        {
            ReadStringArrivedHandler readStringArrivedHandler = this.ReadStringArrived;
            if (readStringArrivedHandler != null)
            {
                readStringArrivedHandler(this, new ReadStringArrivedEventArgs(resultId, readString));
            }
        }

        private void RaiseStatusEventArrivedEvent(string xml)
        {
            StatusEventArrivedHandler statusEventArrivedHandler = this.StatusEventArrived;
            if (statusEventArrivedHandler != null)
            {
                statusEventArrivedHandler(this, new StatusEventArrivedEventArgs(xml));
            }
        }

        private void RaiseSystemConnectedEvent()
        {
            SystemConnectedHandler systemConnectedHandler = this.SystemConnected;
            if (systemConnectedHandler != null)
            {
                systemConnectedHandler(this, new EventArgs());
            }
        }

        private void RaiseSystemDisconnectedEvent()
        {
            SystemDisconnectedHandler systemDisconnectedHandler = this.SystemDisconnected;
            if (systemDisconnectedHandler != null)
            {
                systemDisconnectedHandler(this, new EventArgs());
            }
        }

        private void RaiseSystemWentOfflineEvent()
        {
            SystemWentOfflineHandler systemWentOfflineHandler = this.SystemWentOffline;
            if (systemWentOfflineHandler != null)
            {
                systemWentOfflineHandler(this, new EventArgs());
            }
        }

        private void RaiseSystemWentOnlineEvent()
        {
            SystemWentOnlineHandler systemWentOnlineHandler = this.SystemWentOnline;
            if (systemWentOnlineHandler != null)
            {
                systemWentOnlineHandler(this, new EventArgs());
            }
        }

        private void RaiseTrainingResultArrivedEvent(string trainingResult)
        {
            TrainingResultArrivedHandler trainingResultArrivedHandler = this.TrainingResultArrived;
            if (trainingResultArrivedHandler != null)
            {
                trainingResultArrivedHandler(this, new TrainingResultArrivedEventArgs(trainingResult));
            }
        }

        private void RaiseXmlResultArrivedEvent(int resultId, string xml)
        {
            XmlResultArrivedHandler xmlResultArrivedHandler = this.XmlResultArrived;
            if (xmlResultArrivedHandler != null)
            {
                xmlResultArrivedHandler(this, new XmlResultArrivedEventArgs(resultId, xml));
            }
        }

        private void RaiseXmlStatisticsArrivedEvent(string xml)
        {
            XmlStatisticsArrivedHandler xmlStatisticsArrivedHandler = this.XmlStatisticsArrived;
            if (xmlStatisticsArrivedHandler != null)
            {
                xmlStatisticsArrivedHandler(this, new XmlStatisticsArrivedEvent(xml));
            }
        }

        private void ReaderThreadFunc()
        {
            int num;
            int num1 = 0;
            byte[] numArray = new byte[262144];
            this._parser = new DmccParser(this._connector.Logger, this._pendingCommands, this.Encoding, this.ExpectReadStringAsBinaryResponse);
            this.Log("ReaderThread", "Starting main loop...");
            while (true)
            {
                try
                {
                    if (this._connector.State != ConnectionState.Connected)
                    {
                        num = -1;
                        this.Log("ReaderThread", string.Format("Error reading from connector: connector state is {0}", this._connector.State.ToString()));
                    }
                    else
                    {
                        num = this._connector.Read(numArray, 0, (int)numArray.Length, -1);
                    }
                }
                catch (Exception exception1)
                {
                    Exception exception = exception1;
                    num = -1;
                    this.Log("ReaderThread", string.Concat("Error reading from connector:", exception.Message));
                }
                if (-1 == num)
                {
                    break;
                }
                if (num != 0)
                {
                    int num2 = 0;
                    while (num2 < num)
                    {
                        if (!this._parser.IsReadingBinaryData)
                        {
                            this._parser.ParseByte(numArray[num2]);
                            if (this._parser.IsParserInErrorState)
                            {
                                this.RaiseOffProtocolByteReceivedEvent(numArray[num2]);
                            }
                            if (this._parser.IsAtEndOfDmccMessage)
                            {
                                this.ProcessIncomingMessage(this._parser.LastDmccMessage);
                                this._parser.Reset();
                            }
                            num2++;
                        }
                        else
                        {
                            Cognex.DataMan.SDK.ResultTypes resultType = Cognex.DataMan.SDK.ResultTypes.None;
                            int responseId = -1;
                            if (this._parser.LastDmccMessage != null && this._parser.LastDmccMessage.Type == DmccMessageType.Response && this._parser.LastDmccMessage.ResponseStatusCode != 0)
                            {
                                resultType = DataManSystem.DetermineAutomaticResponseDataType(this._parser.LastDmccMessage.ResponseStatusCode);
                                responseId = this._parser.LastDmccMessage.ResponseId;
                            }
                            if (this._parser.RemainingBinaryDataBytes == this._parser.TotalBinaryDataBytes)
                            {
                                this.RaiseBinaryDataTransferProgressEvent(TransferDirection.Incoming, this._parser.TotalBinaryDataBytes, 0, resultType, responseId);
                                num1 = 0;
                            }
                            int num3 = num - num2;
                            int num4 = Math.Min(this._parser.RemainingBinaryDataBytes, num3);
                            int totalBinaryDataBytes = this._parser.TotalBinaryDataBytes - this._parser.RemainingBinaryDataBytes + num4;
                            if (this._parser.TotalBinaryDataBytes > 0)
                            {
                                double totalBinaryDataBytes1 = (double)totalBinaryDataBytes * 100 / (double)this._parser.TotalBinaryDataBytes;
                            }
                            this._parser.StoreBinaryDataBytes(numArray, num2, num4);
                            if (this._parser.RemainingBinaryDataBytes > 0)
                            {
                                int num5 = num1;
                                num1 = totalBinaryDataBytes / 262144;
                                if (num5 != num1)
                                {
                                    this.RaiseBinaryDataTransferProgressEvent(TransferDirection.Incoming, this._parser.TotalBinaryDataBytes, totalBinaryDataBytes, resultType, responseId);
                                }
                            }
                            else
                            {
                                this.RaiseBinaryDataTransferProgressEvent(TransferDirection.Incoming, this._parser.TotalBinaryDataBytes, this._parser.TotalBinaryDataBytes, resultType, responseId);
                                this.ProcessIncomingMessage(this._parser.LastDmccMessage);
                                this._parser.Reset();
                            }
                            num2 += num4;
                        }
                    }
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
            this.Log("ReaderThread", "-1 bytes returned from connector, disconnecting...");
            this.InitiateDisconnect();
            this.Log("ReaderThread", "Finished.");
        }

        public void Restore(string fileName)
        {
            this.EndRestore(this.BeginRestore(fileName, null, null));
        }

        public DmccResponse SendCommand(string command)
        {
            return this.SendCommandImpl(command, null, this.DefaultTimeout, false);
        }

        public DmccResponse SendCommand(string command, int timeout)
        {
            return this.SendCommandImpl(command, null, timeout, false);
        }

        public DmccResponse SendCommand(string command, byte[] data)
        {
            return this.SendCommandImpl(command, data, this.DefaultTimeout, false);
        }

        public DmccResponse SendCommand(string command, byte[] data, int timeout)
        {
            return this.SendCommandImpl(command, data, timeout, false);
        }

        private DmccResponse SendCommandImpl(string command, byte[] binaryData, int timeout, bool expectBinaryResponse)
        {
            if (this._connectionState != ConnectionState.Connected)
            {
                throw new SystemDisconnectedException();
            }
            IAsyncResult asyncResult = this.BeginSendCommandImpl(command, binaryData, timeout, expectBinaryResponse, null, null, null);
            return this.EndSendCommand(asyncResult);
        }

        private DmccResponse SendCommandInternal(string command, byte[] data, int timeout)
        {
            IAsyncResult asyncResult = this.BeginSendCommandImpl(command, data, timeout, false, null, null, null);
            return this.EndSendCommand(asyncResult);
        }

        public DmccResponse SendCommandWithExpectedBinaryResult(string command)
        {
            return this.SendCommandImpl(command, null, this.DefaultTimeout, true);
        }

        public DmccResponse SendCommandWithExpectedBinaryResult(string command, int timeout)
        {
            return this.SendCommandImpl(command, null, timeout, true);
        }

        public DmccResponse SendCommandWithExpectedBinaryResult(string command, byte[] data)
        {
            return this.SendCommandImpl(command, data, this.DefaultTimeout, true);
        }

        public DmccResponse SendCommandWithExpectedBinaryResult(string command, byte[] data, int timeout)
        {
            return this.SendCommandImpl(command, data, timeout, true);
        }

        public void SetConfig(string fileName)
        {
            this.EndSetConfig(this.BeginSetConfig(fileName, null, null));
        }

        public void SetKeepAliveOptions(bool enabled, int timeout, int interval)
        {
            if (timeout < 0)
            {
                throw new ArgumentException("must be zero or more", "timeout");
            }
            if (interval < 1)
            {
                throw new ArgumentException("must be 1 or more", "interval");
            }
            bool flag = this._keepAliveEnabled;
            this._keepAliveEnabled = enabled;
            this._keepAliveTimeout = timeout;
            this._keepAliveInterval = interval;
            lock (this)
            {
                if (this._connectionState == ConnectionState.Connected && !flag && this._keepAliveEnabled)
                {
                    this._keepAliveThread = new Thread(new ThreadStart(this.KeepAliveThread))
                    {
                        IsBackground = true,
                        Name = "DataMan SDK KeepAlive Thread"
                    };
                    this._keepAliveThread.Start();
                }
            }
        }

        public void SetResultTypes(Cognex.DataMan.SDK.ResultTypes resultTypes)
        {
            if (this._connectionState == ConnectionState.Connected)
            {
                this.SendCommand(string.Concat("SET DATA.RESULT-TYPE ", (int)resultTypes));
            }
            this.ResultTypes = resultTypes;
        }

        private void SetResultTypesInternal(Cognex.DataMan.SDK.ResultTypes resultTypes)
        {
            this.SendCommandInternal(string.Concat("SET DATA.RESULT-TYPE ", (int)resultTypes), null, 5000);
            this.ResultTypes = resultTypes;
        }

        public void UpdateFirmware(string strippedFileName, Stream fwFileStream, bool reboot, int timeout)
        {
            this.EndUpdateFirmware(this.BeginUpdateFirmware(strippedFileName, fwFileStream, reboot, null, null, timeout));
        }

        public void UploadFeatureKey(string fileName)
        {
            this.EndUploadFeatureKey(this.BeginUploadFeatureKey(fileName, null, null));
        }

        private bool WriteCommand(string command, byte[] binaryData, int timeout, int commandId)
        {
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(command);
            if (!this._connector.Write(bytes, 0, (int)bytes.Length, timeout))
            {
                return false;
            }
            if (binaryData != null)
            {
                int num = 0;
                this.EnqueueCallback(() => this.RaiseBinaryDataTransferProgressEvent(TransferDirection.Outgoing, (int)binaryData.Length, 0, Cognex.DataMan.SDK.ResultTypes.None, commandId));
                while (num < (int)binaryData.Length)
                {
                    int num1 = Math.Min(46720, (int)binaryData.Length - num);
                    if (!this._connector.Write(binaryData, num, num1, timeout))
                    {
                        return false;
                    }
                    num += num1;
                    this.EnqueueCallback(() => this.RaiseBinaryDataTransferProgressEvent(TransferDirection.Outgoing, (int)binaryData.Length, num, Cognex.DataMan.SDK.ResultTypes.None, commandId));
                }
                this.EnqueueCallback(() => this.RaiseBinaryDataTransferProgressEvent(TransferDirection.Outgoing, (int)binaryData.Length, (int)binaryData.Length, Cognex.DataMan.SDK.ResultTypes.None, commandId));
            }
            return true;
        }

        private void WriterThread()
        {
            EventWaitHandlePortable[] eventWaitHandlePortableArray = new EventWaitHandlePortable[] { this._threadExitEvent, this._commandQueueChangedEvent };
            this.Log("WriterThread", "Starting main loop...");
        Label0:
            while (1 == EventWaitHandlePortable.WaitAny(eventWaitHandlePortableArray))
            {
                while (true)
                {
                    CommandInfo commandInfo = this.DequeueWritableCommand();
                    if (commandInfo == null)
                    {
                        goto Label0;
                    }
                    object[] commandId = new object[] { "||0:", commandInfo.CommandId, ";1", null, null, null, null };
                    commandId[3] = (string.IsNullOrEmpty(this.ExtraHeaderData) ? "" : string.Concat(";", this.ExtraHeaderData));
                    commandId[4] = ">";
                    commandId[5] = commandInfo.Command;
                    commandId[6] = "\r\n";
                    string str = string.Concat(commandId);
                    lock (this._pendingCommands)
                    {
                        this._pendingCommands.Add(commandInfo.CommandId, commandInfo);
                    }
                    if (this.WriteCommand(str, commandInfo.BinaryData, commandInfo.Timeout, commandInfo.CommandId))
                    {
                        commandInfo.SetSendCompleted();
                    }
                    else
                    {
                        commandInfo.SetSendError(new SystemDisconnectedException());
                    }
                }
            }
            this.Log("WriterThread", "Finished.");
        }

        public event AutomaticResponseArrivedHandler AutomaticResponseArrived;

        public event BinaryDataTransferProgressHandler BinaryDataTransferProgress;

        public event CodeQualityDataArrivedHandler CodeQualityDataArrived;

        public event ImageArrivedHandler ImageArrived;

        public event ImageGraphicsArrivedHandler ImageGraphicsArrived;

        public event KeepAliveResponseMissedHandler KeepAliveResponseMissed;

        public event OffProtocolByteReceivedHandler OffProtocolByteReceived;

        public event ReadStringArrivedHandler ReadStringArrived;

        public event StatusEventArrivedHandler StatusEventArrived;

        public event SystemConnectedHandler SystemConnected;

        public event SystemDisconnectedHandler SystemDisconnected;

        public event SystemWentOfflineHandler SystemWentOffline;

        public event SystemWentOnlineHandler SystemWentOnline;

        public event TrainingResultArrivedHandler TrainingResultArrived;

        public event XmlResultArrivedHandler XmlResultArrived;

        public event XmlStatisticsArrivedHandler XmlStatisticsArrived;

        private delegate void GenericCallback();
    }
}
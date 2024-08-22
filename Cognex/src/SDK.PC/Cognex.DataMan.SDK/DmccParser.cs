using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Cognex.DataMan.SDK
{
    internal class DmccParser : IDmccParser
    {
        private DmccMessage m_LastDmccMessage;

        private DmccParser.DmccParserState m_State;

        private DmccMessageChecksumType m_ChecksumType;

        private byte m_ChecksumByteInCommand;

        private StringBuilder m_HdrNum;

        private bool m_InPayloadEscape;

        private MemoryStream m_Payload;

        private int m_TotalBinaryDataBytes;

        private int m_RemainingBinaryDataBytes;

        private ILogger Logger;

        private IDmccPendingCommandContainer _dmccPendingCommandsContainer;

        public System.Text.Encoding Encoding
        {
            get;
            set;
        }

        public bool ExpectReadStringAsBinaryResponse
        {
            get;
            set;
        }

        public bool IsAtEndOfDmccHeader
        {
            get
            {
                return this.m_State == DmccParser.DmccParserState.FOOTER_LF;
            }
        }

        public bool IsAtEndOfDmccMessage
        {
            get
            {
                return this.m_State == DmccParser.DmccParserState.AT_MESSAGE_END;
            }
        }

        public bool IsParserInErrorState
        {
            get
            {
                return this.m_State == DmccParser.DmccParserState.ERROR;
            }
        }

        public bool IsReadingBinaryData
        {
            get
            {
                return this.m_State == DmccParser.DmccParserState.BINARY_DATA;
            }
        }

        public DmccMessage LastDmccMessage
        {
            get
            {
                return this.m_LastDmccMessage;
            }
        }

        public int RemainingBinaryDataBytes
        {
            get
            {
                return this.m_RemainingBinaryDataBytes;
            }
        }

        protected DmccParser.DmccParserState State
        {
            get
            {
                return this.m_State;
            }
        }

        public int TotalBinaryDataBytes
        {
            get
            {
                return this.m_TotalBinaryDataBytes;
            }
        }

        public DmccParser(ILogger logger, IDmccPendingCommandContainer dmccPendingCommandsContainer, System.Text.Encoding encoding, bool expectReadStringAsBinaryResponse)
        {
            this.Init();
            this.Reset();
            this.Logger = logger;
            this._dmccPendingCommandsContainer = dmccPendingCommandsContainer;
            this.Encoding = encoding;
            this.ExpectReadStringAsBinaryResponse = expectReadStringAsBinaryResponse;
        }

        private void EmitOffProtocolByteError(bool firstByte, byte invalidByte)
        {
            this.ParseError(string.Format("unexpected byte: {0} ({1} | is expected)", (invalidByte < 32 ? string.Format("<0x{0:02}>", invalidByte) : ((char)invalidByte).ToString()), (firstByte ? "first" : "second")));
        }

        private bool EvalChecksumTypeString(string aChecksumTypeStr)
        {
            if (aChecksumTypeStr != "0")
            {
                if (aChecksumTypeStr != "1")
                {
                    this.ParseError(string.Format("invalid checksum type: {0}", aChecksumTypeStr));
                    return false;
                }
                this.m_ChecksumType = DmccMessageChecksumType.XorAtFooter;
            }
            else
            {
                this.m_ChecksumType = DmccMessageChecksumType.NoChecksum;
            }
            return true;
        }

        private bool EvalCommandIdString(string aCommandIdString)
        {
            bool flag;
            try
            {
                this.m_LastDmccMessage.CommandId = int.Parse(aCommandIdString);
                return true;
            }
            catch (Exception exception)
            {
                this.ParseError(string.Format("invalid command id: {0}", aCommandIdString));
                flag = false;
            }
            return flag;
        }

        private bool EvalResponseIdString(string aResponseIdString)
        {
            bool flag;
            try
            {
                this.m_LastDmccMessage.ResponseId = int.Parse(aResponseIdString);
                return true;
            }
            catch (Exception exception)
            {
                this.ParseError(string.Format("invalid response id: {0}", aResponseIdString));
                flag = false;
            }
            return flag;
        }

        private bool EvalStatusCodeString(string aStatusCodeString)
        {
            bool flag;
            try
            {
                this.m_LastDmccMessage.ResponseStatusCode = int.Parse(aStatusCodeString);
                return true;
            }
            catch (Exception exception)
            {
                this.ParseError(string.Format("invalid status code: {0}", aStatusCodeString));
                flag = false;
            }
            return flag;
        }

        public void Init()
        {
            this.m_HdrNum = new StringBuilder();
            this.m_Payload = new MemoryStream();
            this.m_LastDmccMessage = new DmccMessage();
        }

        private void Log(string function, string msg)
        {
            try
            {
                if (this.Logger != null)
                {
                    this.Logger.Log(function, msg);
                }
            }
            catch (Exception exception)
            {
            }
        }

        public void ParseByte(byte aNextByte)
        {
            this.ResetStateOnInput();
            char chr = (char)aNextByte;
            switch (this.m_State)
            {
                case DmccParser.DmccParserState.EMPTY:
                    {
                        if (aNextByte != 124)
                        {
                            this.EmitOffProtocolByteError(true, aNextByte);
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                        else
                        {
                            this.StateChange(DmccParser.DmccParserState.HEADER_BAR1);
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                    }
                case DmccParser.DmccParserState.ERROR:
                case DmccParser.DmccParserState.AT_HEADER_END:
                case DmccParser.DmccParserState.HEADER_CHECKSUM_TYPE:
                case DmccParser.DmccParserState.CHECKSUM:
                    {
                        this.ParseError(string.Format("invalid parser state: {0}", this.m_State));
                        goto case DmccParser.DmccParserState.FOOTER_LF;
                    }
                case DmccParser.DmccParserState.AT_MESSAGE_END:
                case DmccParser.DmccParserState.FOOTER_LF:
                    {
                        if (this.m_State == DmccParser.DmccParserState.FOOTER_LF)
                        {
                            ICommandInfo commandInfo = null;
                            this.m_TotalBinaryDataBytes = 0;
                            this.m_RemainingBinaryDataBytes = 0;
                            bool flag = false;
                            switch (this.LastDmccMessage.ResponseStatusCode)
                            {
                                case 1:
                                    {
                                        if (!this.ExpectReadStringAsBinaryResponse)
                                        {
                                            break;
                                        }
                                        flag = true;
                                        break;
                                    }
                                case 2:
                                case 7:
                                case 8:
                                case 9:
                                case 11:
                                    {
                                        if (flag)
                                        {
                                            try
                                            {
                                                this.m_TotalBinaryDataBytes = int.Parse(this.LastDmccMessage.PayLoad);
                                            }
                                            catch
                                            {
                                                this.ParseError(string.Format("invalid number of binary bytes: {0}", (string.IsNullOrEmpty(this.LastDmccMessage.PayLoad) ? "" : this.LastDmccMessage.PayLoad.Substring(0, Math.Min(50, this.LastDmccMessage.PayLoad.Length)))));
                                            }
                                        }
                                        else if (this.LastDmccMessage.ResponseStatusCode == 0)
                                        {
                                            commandInfo = this._dmccPendingCommandsContainer.FindPendingCommand(this.LastDmccMessage.CommandId);
                                            if (commandInfo != null && commandInfo.ExpectBinaryResponseForCommand)
                                            {
                                                try
                                                {
                                                    this.m_TotalBinaryDataBytes = int.Parse(this.LastDmccMessage.PayLoad);
                                                }
                                                catch
                                                {
                                                    commandInfo.SetError();
                                                }
                                            }
                                        }
                                        if (this.m_TotalBinaryDataBytes > 0)
                                        {
                                            this.m_RemainingBinaryDataBytes = this.m_TotalBinaryDataBytes;
                                            this.m_LastDmccMessage.BinaryData.SetLength((long)0);
                                            this.m_LastDmccMessage.BinaryData.Capacity = this.m_RemainingBinaryDataBytes;
                                            this.StateChange(DmccParser.DmccParserState.BINARY_DATA);
                                            return;
                                        }
                                        this.StateChange(DmccParser.DmccParserState.AT_MESSAGE_END);
                                        break;
                                    }
                                case 3:
                                case 4:
                                case 5:
                                case 6:
                                case 10:
                                case 12:
                                case 13:
                                case 14:
                                case 15:
                                    {
                                        flag = true;
                                        break;
                                    }
                                default:
                                    {
                                        break;
                                    }
                            }
                        }
                        return;
                    }
                case DmccParser.DmccParserState.HEADER_BAR1:
                    {
                        if (aNextByte != 124)
                        {
                            this.EmitOffProtocolByteError(false, aNextByte);
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                        else
                        {
                            this.m_HdrNum.Length = 0;
                            this.StateChange(DmccParser.DmccParserState.HEADER_BAR2);
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                    }
                case DmccParser.DmccParserState.HEADER_BAR2:
                    {
                        if (char.IsNumber(chr))
                        {
                            this.m_HdrNum.Append(chr);
                            this.StateChange(DmccParser.DmccParserState.HEADER_FIRST_NUM);
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                        else if (chr == ':')
                        {
                            this.StateChange(DmccParser.DmccParserState.HEADER_COLON1);
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                        else if (chr == ';')
                        {
                            this.StateChange(DmccParser.DmccParserState.HEADER_SEMICOLON);
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                        else if (chr == '>')
                        {
                            this.m_LastDmccMessage.Type = DmccMessageType.Command;
                            this.StateChange(DmccParser.DmccParserState.HEADER_COMMAND_END);
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                        else if (chr != '[')
                        {
                            this.ParseError(string.Format("invalid character after second bar: 0x{0:X2}", aNextByte));
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                        else
                        {
                            this.m_LastDmccMessage.Type = DmccMessageType.Response;
                            this.StateChange(DmccParser.DmccParserState.STATUS_OPEN_BRACKET);
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                    }
                case DmccParser.DmccParserState.HEADER_COLON1:
                    {
                        if (char.IsNumber(chr))
                        {
                            this.m_HdrNum.Length = 0;
                            this.m_HdrNum.Append(chr);
                            this.StateChange(DmccParser.DmccParserState.HEADER_COMMAND_ID);
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                        else if (chr == ':')
                        {
                            this.StateChange(DmccParser.DmccParserState.HEADER_COLON2);
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                        else if (chr == ';')
                        {
                            this.StateChange(DmccParser.DmccParserState.HEADER_SEMICOLON);
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                        else if (chr == '>')
                        {
                            this.m_LastDmccMessage.Type = DmccMessageType.Command;
                            this.StateChange(DmccParser.DmccParserState.HEADER_COMMAND_END);
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                        else if (chr != '[')
                        {
                            this.ParseError(string.Format("invalid character after header colon: 0x{0:X2}", aNextByte));
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                        else
                        {
                            this.m_LastDmccMessage.Type = DmccMessageType.Response;
                            this.StateChange(DmccParser.DmccParserState.STATUS_OPEN_BRACKET);
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                    }
                case DmccParser.DmccParserState.HEADER_COMMAND_ID:
                    {
                        if (!char.IsNumber(chr))
                        {
                            if (!this.EvalCommandIdString(this.m_HdrNum.ToString()))
                            {
                                goto case DmccParser.DmccParserState.FOOTER_LF;
                            }
                            if (chr == ';')
                            {
                                this.StateChange(DmccParser.DmccParserState.HEADER_SEMICOLON);
                                goto case DmccParser.DmccParserState.FOOTER_LF;
                            }
                            else if (chr == '>')
                            {
                                this.m_LastDmccMessage.Type = DmccMessageType.Command;
                                this.StateChange(DmccParser.DmccParserState.HEADER_COMMAND_END);
                                goto case DmccParser.DmccParserState.FOOTER_LF;
                            }
                            else if (chr != '[')
                            {
                                this.ParseError(string.Format("invalid character after command id: 0x{0:X2}", aNextByte));
                                goto case DmccParser.DmccParserState.FOOTER_LF;
                            }
                            else
                            {
                                this.m_LastDmccMessage.Type = DmccMessageType.Response;
                                this.StateChange(DmccParser.DmccParserState.STATUS_OPEN_BRACKET);
                                goto case DmccParser.DmccParserState.FOOTER_LF;
                            }
                        }
                        else
                        {
                            this.m_HdrNum.Append(chr);
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                    }
                case DmccParser.DmccParserState.HEADER_SEMICOLON:
                    {
                        if (chr == '1')
                        {
                            this.m_LastDmccMessage.StatusCodeRequestType = DmccMessageStatusCodeRequestType.Send;
                            this.StateChange(DmccParser.DmccParserState.HEADER_RESP_MODE_FLAG);
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                        else if (chr != '0')
                        {
                            this.ParseError(string.Format("invalid character after command id: 0x{0:X2}", aNextByte));
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                        else
                        {
                            this.m_LastDmccMessage.StatusCodeRequestType = DmccMessageStatusCodeRequestType.DoNotSend;
                            this.StateChange(DmccParser.DmccParserState.HEADER_RESP_MODE_FLAG);
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                    }
                case DmccParser.DmccParserState.HEADER_RESP_MODE_FLAG:
                    {
                        if (chr != '>')
                        {
                            this.ParseError(string.Format("invalid character response mode flag: 0x{0:X2}", aNextByte));
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                        else
                        {
                            this.m_LastDmccMessage.Type = DmccMessageType.Command;
                            this.StateChange(DmccParser.DmccParserState.HEADER_COMMAND_END);
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                    }
                case DmccParser.DmccParserState.HEADER_COMMAND_END:
                    {
                        this.StateChange(DmccParser.DmccParserState.PAYLOAD);
                        this.StorePayloadByte(aNextByte);
                        goto case DmccParser.DmccParserState.FOOTER_LF;
                    }
                case DmccParser.DmccParserState.PAYLOAD:
                case DmccParser.DmccParserState.FOOTER_CR:
                    {
                        this.StorePayloadByte(aNextByte);
                        goto case DmccParser.DmccParserState.FOOTER_LF;
                    }
                case DmccParser.DmccParserState.HEADER_COLON2:
                    {
                        if (char.IsNumber(chr))
                        {
                            this.m_HdrNum.Length = 0;
                            this.m_HdrNum.Append(chr);
                            this.StateChange(DmccParser.DmccParserState.HEADER_RESPONSE_ID);
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                        else if (chr == ';')
                        {
                            this.StateChange(DmccParser.DmccParserState.HEADER_SEMICOLON);
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                        else if (chr == '>')
                        {
                            this.m_LastDmccMessage.Type = DmccMessageType.Command;
                            this.StateChange(DmccParser.DmccParserState.HEADER_COMMAND_END);
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                        else if (chr != '[')
                        {
                            this.ParseError(string.Format("invalid character after header colon: 0x{0:X2}", aNextByte));
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                        else
                        {
                            this.m_LastDmccMessage.Type = DmccMessageType.Response;
                            this.StateChange(DmccParser.DmccParserState.STATUS_OPEN_BRACKET);
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                    }
                case DmccParser.DmccParserState.HEADER_RESPONSE_ID:
                    {
                        if (!char.IsNumber(chr))
                        {
                            if (!this.EvalResponseIdString(this.m_HdrNum.ToString()))
                            {
                                goto case DmccParser.DmccParserState.FOOTER_LF;
                            }
                            if (chr == ';')
                            {
                                this.StateChange(DmccParser.DmccParserState.HEADER_SEMICOLON);
                                goto case DmccParser.DmccParserState.FOOTER_LF;
                            }
                            else if (chr == '>')
                            {
                                this.m_LastDmccMessage.Type = DmccMessageType.Command;
                                this.StateChange(DmccParser.DmccParserState.HEADER_COMMAND_END);
                                goto case DmccParser.DmccParserState.FOOTER_LF;
                            }
                            else if (chr != '[')
                            {
                                this.ParseError(string.Format("invalid character after command id: 0x{0:X2}", aNextByte));
                                goto case DmccParser.DmccParserState.FOOTER_LF;
                            }
                            else
                            {
                                this.m_LastDmccMessage.Type = DmccMessageType.Response;
                                this.StateChange(DmccParser.DmccParserState.STATUS_OPEN_BRACKET);
                                goto case DmccParser.DmccParserState.FOOTER_LF;
                            }
                        }
                        else
                        {
                            this.m_HdrNum.Append(chr);
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                    }
                case DmccParser.DmccParserState.STATUS_OPEN_BRACKET:
                    {
                        if (!char.IsNumber(chr))
                        {
                            this.ParseError(string.Format("invalid character after opening bracket: 0x{0:X2}", aNextByte));
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                        else
                        {
                            this.m_HdrNum.Length = 0;
                            this.m_HdrNum.Append(chr);
                            this.StateChange(DmccParser.DmccParserState.STATUS_CODE);
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                    }
                case DmccParser.DmccParserState.STATUS_CODE:
                    {
                        if (!char.IsNumber(chr))
                        {
                            if (!this.EvalStatusCodeString(this.m_HdrNum.ToString()))
                            {
                                goto case DmccParser.DmccParserState.FOOTER_LF;
                            }
                            if (chr != ']')
                            {
                                this.ParseError(string.Format("invalid character in status code: 0x{0:X2}", aNextByte));
                                goto case DmccParser.DmccParserState.FOOTER_LF;
                            }
                            else
                            {
                                this.StateChange(DmccParser.DmccParserState.STATUS_CLOSE_BRACKET);
                                goto case DmccParser.DmccParserState.FOOTER_LF;
                            }
                        }
                        else
                        {
                            this.m_HdrNum.Append(chr);
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                    }
                case DmccParser.DmccParserState.STATUS_CLOSE_BRACKET:
                    {
                        this.StateChange(DmccParser.DmccParserState.PAYLOAD);
                        this.StorePayloadByte(aNextByte);
                        goto case DmccParser.DmccParserState.FOOTER_LF;
                    }
                case DmccParser.DmccParserState.BINARY_DATA:
                    {
                        this.StoreBinaryDataByte(aNextByte);
                        goto case DmccParser.DmccParserState.FOOTER_LF;
                    }
                case DmccParser.DmccParserState.HEADER_FIRST_NUM:
                    {
                        if (!char.IsNumber(chr))
                        {
                            if (!this.EvalChecksumTypeString(this.m_HdrNum.ToString()))
                            {
                                goto case DmccParser.DmccParserState.FOOTER_LF;
                            }
                            if (chr == ':')
                            {
                                this.StateChange(DmccParser.DmccParserState.HEADER_COLON1);
                                goto case DmccParser.DmccParserState.FOOTER_LF;
                            }
                            else if (chr == ';')
                            {
                                this.StateChange(DmccParser.DmccParserState.HEADER_SEMICOLON);
                                goto case DmccParser.DmccParserState.FOOTER_LF;
                            }
                            else if (chr == '>')
                            {
                                this.m_LastDmccMessage.Type = DmccMessageType.Command;
                                this.StateChange(DmccParser.DmccParserState.HEADER_COMMAND_END);
                                goto case DmccParser.DmccParserState.FOOTER_LF;
                            }
                            else if (chr != '[')
                            {
                                this.ParseError(string.Format("invalid character after header number: 0x{0:X2}", aNextByte));
                                goto case DmccParser.DmccParserState.FOOTER_LF;
                            }
                            else
                            {
                                this.m_LastDmccMessage.Type = DmccMessageType.Response;
                                this.StateChange(DmccParser.DmccParserState.STATUS_OPEN_BRACKET);
                                goto case DmccParser.DmccParserState.FOOTER_LF;
                            }
                        }
                        else
                        {
                            this.m_HdrNum.Append(chr);
                            goto case DmccParser.DmccParserState.FOOTER_LF;
                        }
                    }
                default:
                    {
                        goto case DmccParser.DmccParserState.CHECKSUM;
                    }
            }
        }

        public void ParseError(string aMsg)
        {
            this.Log("DMCC parser", string.Format("parse error: {0}", aMsg));
            this.StateChange(DmccParser.DmccParserState.ERROR);
        }

        public void Reset()
        {
            this.m_ChecksumType = DmccMessageChecksumType.NoChecksum;
            this.m_LastDmccMessage = new DmccMessage();
            this.StateChange(DmccParser.DmccParserState.EMPTY);
            this.m_HdrNum.Length = 0;
            this.m_InPayloadEscape = false;
            this.m_Payload.SetLength((long)0);
            this.m_ChecksumByteInCommand = 0;
            this.m_TotalBinaryDataBytes = 0;
            this.m_RemainingBinaryDataBytes = 0;
        }

        private void ResetStateOnInput()
        {
            if (this.m_State == DmccParser.DmccParserState.EMPTY || this.m_State == DmccParser.DmccParserState.AT_MESSAGE_END)
            {
                this.Reset();
            }
            if (this.m_State == DmccParser.DmccParserState.ERROR)
            {
                this.Reset();
            }
        }

        protected void StateChange(DmccParser.DmccParserState aNewState)
        {
            this.m_State = aNewState;
        }

        private void StoreBinaryDataByte(byte aBinaryByte)
        {
            this.m_LastDmccMessage.BinaryData.WriteByte(aBinaryByte);
            this.m_RemainingBinaryDataBytes--;
            if (this.m_RemainingBinaryDataBytes == 0)
            {
                this.StateChange(DmccParser.DmccParserState.AT_MESSAGE_END);
            }
        }

        public void StoreBinaryDataBytes(byte[] aBinaryBytes, int startIndex, int bytesToStore)
        {
            if (this.m_RemainingBinaryDataBytes < bytesToStore)
            {
                this.StateChange(DmccParser.DmccParserState.ERROR);
                return;
            }
            this.m_LastDmccMessage.BinaryData.Write(aBinaryBytes, startIndex, bytesToStore);
            this.m_RemainingBinaryDataBytes -= bytesToStore;
            if (this.m_RemainingBinaryDataBytes == 0)
            {
                this.StateChange(DmccParser.DmccParserState.AT_MESSAGE_END);
            }
        }

        private void StorePayloadByte(byte aPayloadByte)
        {
            if (aPayloadByte == 13)
            {
                if (this.m_State == DmccParser.DmccParserState.FOOTER_CR)
                {
                    this.m_Payload.WriteByte(aPayloadByte);
                    return;
                }
                if (this.m_InPayloadEscape)
                {
                    this.m_Payload.WriteByte(92);
                    this.m_InPayloadEscape = false;
                }
                this.StateChange(DmccParser.DmccParserState.FOOTER_CR);
                return;
            }
            if (aPayloadByte != 10)
            {
                if (aPayloadByte == 92)
                {
                    if (!this.m_InPayloadEscape)
                    {
                        this.m_InPayloadEscape = true;
                        return;
                    }
                    this.m_Payload.WriteByte(92);
                    this.m_InPayloadEscape = false;
                    return;
                }
                if (this.m_InPayloadEscape)
                {
                    char chr = (char)aPayloadByte;
                    if (chr > 'n')
                    {
                        switch (chr)
                        {
                            case 'r':
                                {
                                    this.m_Payload.WriteByte(13);
                                    break;
                                }
                            case 's':
                                {
                                    this.m_Payload.WriteByte(92);
                                    this.m_Payload.WriteByte(aPayloadByte);
                                    this.Log("DMCC parser", string.Format("invalid escaped character: '{0}', accepted as '\\{1}'", (char)aPayloadByte, (char)aPayloadByte));
                                    this.m_InPayloadEscape = false;
                                    return;
                                }
                            case 't':
                                {
                                    this.m_Payload.WriteByte(9);
                                    break;
                                }
                            default:
                                {
                                    if (chr == '|')
                                    {
                                        this.m_Payload.WriteByte(124);
                                        break;
                                    }
                                    else
                                    {
                                        this.m_Payload.WriteByte(92);
                                        this.m_Payload.WriteByte(aPayloadByte);
                                        this.Log("DMCC parser", string.Format("invalid escaped character: '{0}', accepted as '\\{1}'", (char)aPayloadByte, (char)aPayloadByte));
                                        this.m_InPayloadEscape = false;
                                        return;
                                    }
                                }
                        }
                    }
                    else if (chr == '\"')
                    {
                        this.m_Payload.WriteByte(34);
                    }
                    else
                    {
                        if (chr != 'n')
                        {
                            this.m_Payload.WriteByte(92);
                            this.m_Payload.WriteByte(aPayloadByte);
                            this.Log("DMCC parser", string.Format("invalid escaped character: '{0}', accepted as '\\{1}'", (char)aPayloadByte, (char)aPayloadByte));
                            this.m_InPayloadEscape = false;
                            return;
                        }
                        this.m_Payload.WriteByte(10);
                    }
                    this.m_InPayloadEscape = false;
                    return;
                }
                this.m_Payload.WriteByte(aPayloadByte);
            }
            else
            {
                if (this.m_State != DmccParser.DmccParserState.FOOTER_CR)
                {
                    this.m_Payload.WriteByte(aPayloadByte);
                    return;
                }
                this.StateChange(DmccParser.DmccParserState.FOOTER_LF);
                if (this.m_ChecksumType == DmccMessageChecksumType.XorAtFooter)
                {
                    if (this.m_Payload.Length >= (long)1)
                    {
                        this.m_Payload.Seek(this.m_Payload.Length - (long)1, SeekOrigin.Begin);
                        int num = this.m_Payload.ReadByte();
                        if (num < 0 || num > 255)
                        {
                            this.ParseError(string.Format("could not read checksum byte", new object[0]));
                        }
                        else
                        {
                            this.m_ChecksumByteInCommand = (byte)num;
                        }
                        this.m_Payload.SetLength(this.m_Payload.Length - (long)1);
                    }
                    else
                    {
                        this.ParseError(string.Format("missing checksum byte", new object[0]));
                    }
                }
                this.m_Payload.Seek((long)0, SeekOrigin.Begin);
                if (this.m_State != DmccParser.DmccParserState.ERROR)
                {
                    byte[] array = this.m_Payload.ToArray();
                    this.m_LastDmccMessage.PayLoad = this.Encoding.GetString(array, 0, (int)array.Length);
                    this.m_Payload.SetLength((long)0);
                    return;
                }
            }
        }

        protected enum DmccParserState
        {
            EMPTY,
            ERROR,
            AT_HEADER_END,
            AT_MESSAGE_END,
            HEADER_BAR1,
            HEADER_BAR2,
            HEADER_CHECKSUM_TYPE,
            HEADER_COLON1,
            HEADER_COMMAND_ID,
            HEADER_SEMICOLON,
            HEADER_RESP_MODE_FLAG,
            HEADER_COMMAND_END,
            PAYLOAD,
            CHECKSUM,
            FOOTER_CR,
            FOOTER_LF,
            HEADER_COLON2,
            HEADER_RESPONSE_ID,
            STATUS_OPEN_BRACKET,
            STATUS_CODE,
            STATUS_CLOSE_BRACKET,
            BINARY_DATA,
            HEADER_FIRST_NUM
        }
    }
}
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Cognex.DataMan.SDK
{
	internal class CommandInfo : ICommandInfo, IAsyncResult
	{
		private ManualResetEvent _sentEvent = new ManualResetEvent(false);

		private ManualResetEvent _completionEvent = new ManualResetEvent(false);

		private bool _completed;

		public string Command;

		public int CommandId;

		public byte[] BinaryData;

		public int Timeout;

		public AsyncCallback UserCallback;

		public object UserState;

		public object CommandState;

		public DateTime StartTime;

		public DateTime CompletionTime;

		public DmccResponse Response;

		public Exception Error;

		public object AsyncState
		{
			get
			{
				return this.UserState;
			}
		}

		public WaitHandle AsyncWaitHandle
		{
			get
			{
				return this._completionEvent;
			}
		}

		public bool CompletedSynchronously
		{
			get
			{
				return false;
			}
		}

		public bool ExpectBinaryResponseForCommand
		{
			get
			{
				return JustDecompileGenerated_get_ExpectBinaryResponseForCommand();
			}
			set
			{
				JustDecompileGenerated_set_ExpectBinaryResponseForCommand(value);
			}
		}

		private bool JustDecompileGenerated_ExpectBinaryResponseForCommand_k__BackingField;

		public bool JustDecompileGenerated_get_ExpectBinaryResponseForCommand()
		{
			return this.JustDecompileGenerated_ExpectBinaryResponseForCommand_k__BackingField;
		}

		public void JustDecompileGenerated_set_ExpectBinaryResponseForCommand(bool value)
		{
			this.JustDecompileGenerated_ExpectBinaryResponseForCommand_k__BackingField = value;
		}

		public bool IsCompleted
		{
			get
			{
				return this._completed;
			}
		}

		public WaitHandle SendCompleteWaitHandle
		{
			get
			{
				return this._sentEvent;
			}
		}

		public CommandInfo()
		{
		}

		public void SetComplete(DmccResponse response)
		{
			this.CompletionTime = DateTime.Now;
			this.Response = response;
			this.Response.CommandRoundtrip = this.CompletionTime - this.StartTime;
			this._completed = true;
			this._completionEvent.Set();
			if (this.UserCallback != null)
			{
				this.UserCallback(this);
			}
		}

		public void SetError()
		{
			this.SetError(new InvalidCommandException(this.Command));
		}

		public void SetError(Exception exception)
		{
			this.CompletionTime = DateTime.Now;
			this.Error = exception;
			this._completed = true;
			this._completionEvent.Set();
			if (this.UserCallback != null)
			{
				this.UserCallback(this);
			}
		}

		public void SetSendCompleted()
		{
			this._sentEvent.Set();
		}

		public void SetSendError(Exception exception)
		{
			this.Error = exception;
			this._sentEvent.Set();
		}
	}
}
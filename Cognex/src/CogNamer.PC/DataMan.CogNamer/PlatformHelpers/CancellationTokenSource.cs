using System;

namespace Cognex.DataMan.CogNamer.PlatformHelpers
{
	internal class CancellationTokenSource : IDisposable
	{
		private bool _disposed;

		private EventWaitHandlePortable _cancellationEvent;

		public bool IsCancellationRequested
		{
			get
			{
				if (this._cancellationEvent == null)
				{
					return false;
				}
				return this._cancellationEvent.WaitOne(0);
			}
		}

		public CancellationToken Token
		{
			get
			{
				return new CancellationToken(this._cancellationEvent);
			}
		}

		public CancellationTokenSource()
		{
			this._disposed = false;
			this._cancellationEvent = new EventWaitHandlePortable(true);
		}

		public void Cancel()
		{
			if (this._disposed)
			{
				return;
			}
			if (this._cancellationEvent != null)
			{
				this._cancellationEvent.Set();
			}
		}

		public void Dispose()
		{
			this.DoDispose(true);
			GC.SuppressFinalize(this);
		}

		private void DoDispose(bool disposeManagedResources)
		{
			if (this._disposed)
			{
				return;
			}
			this._disposed = true;
			if (disposeManagedResources)
			{
				EventWaitHandlePortable eventWaitHandlePortable = this._cancellationEvent;
				if (eventWaitHandlePortable != null)
				{
					eventWaitHandlePortable.Set();
					eventWaitHandlePortable.Close();
				}
			}
		}

		~CancellationTokenSource()
		{
			this.DoDispose(false);
		}
	}
}
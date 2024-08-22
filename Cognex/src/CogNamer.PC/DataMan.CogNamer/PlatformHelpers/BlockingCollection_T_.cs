using System;
using System.Collections.Generic;

namespace Cognex.DataMan.CogNamer.PlatformHelpers
{
	internal class BlockingCollection<T> : IDisposable
	{
		private bool _disposed;

		private Queue<T> _queue;

		private object _queueLock;

		private EventWaitHandlePortable _queueElementAddedEvent;

		private CancellationToken _cancellationToken;

		public BlockingCollection()
		{
			this._disposed = false;
			this._queueElementAddedEvent = new EventWaitHandlePortable(false);
			this._queueLock = new object();
			this._queue = new Queue<T>();
		}

		internal void Add(T item, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}
			lock (this._queueLock)
			{
				this._queue.Enqueue(item);
			}
			this._queueElementAddedEvent.Set();
		}

		public T DequeueWithCancellation(CancellationToken cancellationToken)
		{
			T t;
			this._cancellationToken = cancellationToken;
			try
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					lock (this._queueLock)
					{
						if (this._queue.Count > 0)
						{
							t = this._queue.Dequeue();
							return t;
						}
					}
					EventWaitHandlePortable[] waitHandle = new EventWaitHandlePortable[] { cancellationToken.WaitHandle, this._queueElementAddedEvent };
					EventWaitHandlePortable.WaitAny(waitHandle);
				}
				t = default(T);
			}
			finally
			{
				this._cancellationToken = null;
			}
			return t;
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
			CancellationToken cancellationToken = this._cancellationToken;
			if (cancellationToken != null)
			{
				cancellationToken.Cancel();
			}
		}

		~BlockingCollection()
		{
			this.DoDispose(false);
		}

		public bool TryDequeue(out T result)
		{
			bool flag;
			lock (this._queueLock)
			{
				if (this._queue.Count != 0)
				{
					result = this._queue.Dequeue();
					flag = true;
				}
				else
				{
					result = default(T);
					flag = false;
				}
			}
			return flag;
		}
	}
}
using Cognex.DataMan.SDK;
using Cognex.DataMan.SDK.Utils.PlatformHelpers;
using System;
using System.Collections.Generic;

namespace Cognex.DataMan.SDK.Utils
{
	internal class SimpleResultEntry
	{
		public SimpleResult Result;

		public int NumRaisedCompletedEvents;

		public HashSetPortable<int> ContainedResultIds;

		public HashSetPortable<int> ContainedImageIds;

		public HashSetPortable<int> ReferredResultIds;

		public HashSetPortable<int> ReferredImageIds;

		public HashSetPortable<SimpleResultId> ExpectedSimpleResults;

		public SimpleResultEntry()
		{
			this.NumRaisedCompletedEvents = 0;
			this.ContainedResultIds = new HashSetPortable<int>();
			this.ContainedImageIds = new HashSetPortable<int>();
			this.ReferredResultIds = new HashSetPortable<int>();
			this.ReferredImageIds = new HashSetPortable<int>();
			this.ExpectedSimpleResults = new HashSetPortable<SimpleResultId>();
		}

		public SimpleResultEntry(SimpleResult result) : this()
		{
			this.Result = result;
		}

		public SimpleResultEntry(SimpleResultId id) : this(new SimpleResult(id))
		{
		}

		public SimpleResultEntry(ResultTypes type, int id) : this(new SimpleResultId(type, id))
		{
		}

		public SimpleResultEntry(ResultTypes type, int id, byte[] data, DateTime arrivedAtUtc) : this(new SimpleResult(new SimpleResultId(type, id), data, arrivedAtUtc))
		{
		}

		public SimpleResultEntry(SimpleResultId id, byte[] data, DateTime arrivedAtUtc) : this(new SimpleResult(id, data, arrivedAtUtc))
		{
		}

		public void AddContainedImageId(int imageId)
		{
			this.ContainedImageIds.Add(imageId);
		}

		public void AddContainedResultId(int resultId)
		{
			this.ContainedResultIds.Add(resultId);
		}

		public void AddContainedResultIds(IEnumerable<int> resultIds)
		{
			foreach (int resultId in resultIds)
			{
				this.ContainedResultIds.Add(resultId);
			}
		}

		public void AddExpectedSimpleResult(SimpleResultId id)
		{
			this.ExpectedSimpleResults.Add(id);
		}

		public void AddReferredImageId(int imageId)
		{
			this.ReferredImageIds.Add(imageId);
		}

		public void AddReferredImageIds(IEnumerable<int> imageIds)
		{
			foreach (int imageId in imageIds)
			{
				this.ReferredImageIds.Add(imageId);
			}
		}

		public void AddReferredResultId(int resultId)
		{
			this.ReferredResultIds.Add(resultId);
		}

		public void AddReferredResultIds(IEnumerable<int> resultIds)
		{
			foreach (int resultId in resultIds)
			{
				this.ReferredResultIds.Add(resultId);
			}
		}
	}
}
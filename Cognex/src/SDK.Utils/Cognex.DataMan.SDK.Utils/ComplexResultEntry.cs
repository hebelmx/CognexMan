using Cognex.DataMan.SDK;
using Cognex.DataMan.SDK.Utils.PlatformHelpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cognex.DataMan.SDK.Utils
{
	internal class ComplexResultEntry
	{
		private HashSetPortable<int> ContainedResultIds;

		private HashSetPortable<int> ReferredResultIds;

		private HashSetPortable<int> ContainedImageIds;

		private HashSetPortable<int> ReferredImageIds;

		private HashSetPortable<SimpleResultId> ExpectedSimpleResults;

		public ResultTypes CollectedResultTypes;

		public Dictionary<SimpleResultId, SimpleResultEntry> SimpleResults;

		public string ContainedImageIdsAsString
		{
			get
			{
				return ComplexResultEntry.GetIdsAsString(this.ContainedImageIds);
			}
		}

		public string ContainedResultIdsAsString
		{
			get
			{
				return ComplexResultEntry.GetIdsAsString(this.ContainedResultIds);
			}
		}

		public string ReferredImageIdsAsString
		{
			get
			{
				return ComplexResultEntry.GetIdsAsString(this.ReferredImageIds);
			}
		}

		public string ReferredResultIdsAsString
		{
			get
			{
				return ComplexResultEntry.GetIdsAsString(this.ReferredResultIds);
			}
		}

		internal string SimpleResultsAsString
		{
			get
			{
				StringBuilder stringBuilder = new StringBuilder();
				foreach (KeyValuePair<SimpleResultId, SimpleResultEntry> simpleResult in this.SimpleResults)
				{
					stringBuilder.AppendFormat("{0}{1}[{2}]", (stringBuilder.Length > 0 ? "," : ""), simpleResult.Key.Type, simpleResult.Key.Id);
				}
				return stringBuilder.ToString();
			}
		}

		public ComplexResultEntry()
		{
			this.ContainedResultIds = new HashSetPortable<int>();
			this.ReferredResultIds = new HashSetPortable<int>();
			this.ContainedImageIds = new HashSetPortable<int>();
			this.ReferredImageIds = new HashSetPortable<int>();
			this.ExpectedSimpleResults = new HashSetPortable<SimpleResultId>();
			this.CollectedResultTypes = ResultTypes.None;
			this.SimpleResults = new Dictionary<SimpleResultId, SimpleResultEntry>();
		}

		public void AddSimpleResult(SimpleResultEntry simpleResultEntry)
		{
			this.SimpleResults[simpleResultEntry.Result.Id] = simpleResultEntry;
			this.CollectedResultTypes |= simpleResultEntry.Result.Id.Type;
			foreach (int containedResultId in simpleResultEntry.ContainedResultIds)
			{
				this.ContainedResultIds.Add(containedResultId);
			}
			foreach (int containedImageId in simpleResultEntry.ContainedImageIds)
			{
				this.ContainedImageIds.Add(containedImageId);
			}
			foreach (int referredResultId in simpleResultEntry.ReferredResultIds)
			{
				this.ReferredResultIds.Add(referredResultId);
			}
			foreach (int referredImageId in simpleResultEntry.ReferredImageIds)
			{
				this.ReferredImageIds.Add(referredImageId);
			}
			foreach (SimpleResultId expectedSimpleResult in simpleResultEntry.ExpectedSimpleResults)
			{
				this.ExpectedSimpleResults.Add(expectedSimpleResult);
			}
		}

		public static ComplexResult ConvertToComplexResult(ComplexResultEntry result_entry, bool onlyEntriesAlreadyArrived)
		{
			int num;
			ComplexResult complexResult = new ComplexResult();
			Dictionary<SimpleResultId, int> simpleResultIds = new Dictionary<SimpleResultId, int>(result_entry.SimpleResults.Count);
			foreach (KeyValuePair<SimpleResultId, SimpleResultEntry> simpleResult in result_entry.SimpleResults)
			{
				if (simpleResultIds.TryGetValue(simpleResult.Key, out num) || !simpleResult.Value.Result.IsArrived && onlyEntriesAlreadyArrived)
				{
					continue;
				}
				complexResult.SimpleResults.Add(simpleResult.Value.Result);
			}
			return complexResult;
		}

		internal static string GetIdsAsString(IEnumerable<int> ids)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (int id in ids)
			{
				stringBuilder.AppendFormat("{0}{1}", (stringBuilder.Length > 0 ? "," : ""), id);
			}
			return stringBuilder.ToString();
		}

		public bool IsComplete(ResultTypes collectedResultTypes)
		{
			bool flag;
			if (this.CollectedResultTypes != collectedResultTypes)
			{
				return false;
			}
			if (ComplexResultEntry.IsImageCollected(this.CollectedResultTypes))
			{
				foreach (int referredImageId in this.ReferredImageIds)
				{
					if (this.ContainedImageIds.Contains(referredImageId))
					{
						continue;
					}
					flag = false;
					return flag;
				}
			}
			if (ComplexResultEntry.IsResultIdCollected(this.CollectedResultTypes))
			{
				foreach (int referredResultId in this.ReferredResultIds)
				{
					if (this.ContainedResultIds.Contains(referredResultId))
					{
						continue;
					}
					flag = false;
					return flag;
				}
			}
			HashSet<SimpleResultId>.Enumerator enumerator = this.ExpectedSimpleResults.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					SimpleResultId current = enumerator.Current;
					if (this.SimpleResults.ContainsKey(current))
					{
						continue;
					}
					flag = false;
					return flag;
				}
				return true;
			}
			finally
			{
				((IDisposable)enumerator).Dispose();
			}
			return flag;
		}

		public static bool IsImageCollected(ResultTypes collectedResultTypes)
		{
			return (collectedResultTypes & ResultTypes.Image) != ResultTypes.None;
		}

		public static bool IsImageGraphicsCollected(ResultTypes collectedResultTypes)
		{
			return (collectedResultTypes & ResultTypes.ImageGraphics) != ResultTypes.None;
		}

		public static bool IsResultIdCollected(ResultTypes collectedResultTypes)
		{
			return (collectedResultTypes & (ResultTypes.ReadXml | ResultTypes.ReadXmlExtended)) != ResultTypes.None;
		}

		public bool JoinInto(ComplexResultEntry other)
		{
			if ((this.ContainedResultIds.Overlaps(other.ReferredResultIds) || this.ContainedImageIds.Overlaps(other.ReferredImageIds) || other.ContainedResultIds.Overlaps(this.ReferredResultIds) || other.ContainedImageIds.Overlaps(this.ReferredImageIds) ? false : !this.ContainedResultIds.Overlaps(other.ContainedResultIds)))
			{
				return false;
			}
			foreach (SimpleResultEntry value in this.SimpleResults.Values)
			{
				other.AddSimpleResult(value);
			}
			return true;
		}

		public override string ToString()
		{
			object[] simpleResultsAsString = new object[] { this.SimpleResultsAsString, this.ContainedResultIdsAsString, this.ContainedImageIdsAsString, this.ReferredResultIdsAsString, this.ReferredImageIdsAsString };
			return string.Format("SimpleResults=({0}), Contains: Results={1}, Images={2}; RefersTo: Results={3}, Images={4}", simpleResultsAsString);
		}
	}
}
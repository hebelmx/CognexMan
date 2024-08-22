using Cognex.DataMan.SDK;
using Cognex.DataMan.SDK.Utils.PlatformHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Cognex.DataMan.SDK.Utils
{
	public class ResultCollector : IDisposable
	{
		private DataManSystem _dmSystem;

		private int _resultCacheLength = 1024;

		private TimeSpan _resultTimeOut = TimeSpan.FromSeconds(10);

		private ResultTypes _collectedResultTypes;

		private List<ResultTypes> _collectedResultTypeList;

		private Timer _resultTimeOutTimer;

		private readonly int _purgeOldResultsFrequency = 1000;

		private float _firmwareVersion;

		private object _results_lock = new object();

		private LinkedList<SimpleResultEntry> _collectedSimpleResults = new LinkedList<SimpleResultEntry>();

		private Dictionary<SimpleResultId, bool> _collectedSimpleResultIds = new Dictionary<SimpleResultId, bool>();

		private int _isDisposed;

		public int ResultCacheLength
		{
			get
			{
				return this._resultCacheLength;
			}
			set
			{
				this._resultCacheLength = value;
				this.RemoveAllExpiredCacheItems();
			}
		}

		public TimeSpan ResultTimeOut
		{
			get
			{
				return this._resultTimeOut;
			}
			set
			{
				this._resultTimeOut = value;
				this.RemoveAllExpiredCacheItems();
				this.UpdateResultTimeOutTimer();
			}
		}

		public ResultCollector(DataManSystem system, ResultTypes expectedResultTypes)
		{
			if (expectedResultTypes == ResultTypes.None)
			{
				throw new InvalidResultTypeException("No result type was set for the Result Collector.");
			}
			this._dmSystem = system;
			this._collectedResultTypes = expectedResultTypes;
			this.DetermineNumResultTypesCollected();
			this.ResetFirmwareVersion();
			this._resultTimeOutTimer = new Timer(new TimerCallback(this.OnResultTimeOutTimerTicked), null, -1, -1);
			this.UpdateResultTimeOutTimer();
			this._dmSystem.AutomaticResponseArrived += new AutomaticResponseArrivedHandler(this.OnAutomaticResponseArrived);
			this._dmSystem.SystemConnected += new SystemConnectedHandler(this.OnSystemConnected);
		}

		private void addCollectedSimpleResult_NonLocked(SimpleResultEntry simpleResultEntry)
		{
			this._collectedSimpleResults.AddLast(simpleResultEntry);
			this._collectedSimpleResultIds[simpleResultEntry.Result.Id] = true;
		}

		private void AddExpectedMultipleResults(SimpleResultEntry simple_result, HashSetPortable<int> referred_image_ids)
		{
			foreach (int referredImageId in referred_image_ids)
			{
				if (ComplexResultEntry.IsImageCollected(this._collectedResultTypes))
				{
					simple_result.AddExpectedSimpleResult(new SimpleResultId(ResultTypes.Image, referredImageId));
				}
				if (!ComplexResultEntry.IsImageGraphicsCollected(this._collectedResultTypes))
				{
					continue;
				}
				simple_result.AddExpectedSimpleResult(new SimpleResultId(ResultTypes.ImageGraphics, referredImageId));
			}
		}

		private List<ComplexResultEntry> buildCompletedComplexResultsList()
		{
			bool flag;
			LinkedList<ComplexResultEntry> complexResultEntries = new LinkedList<ComplexResultEntry>();
			foreach (SimpleResultEntry _collectedSimpleResult in this._collectedSimpleResults)
			{
				ComplexResultEntry complexResultEntry = new ComplexResultEntry();
				complexResultEntry.AddSimpleResult(_collectedSimpleResult);
				bool flag1 = false;
				if (complexResultEntries.Count > 0)
				{
					flag1 = complexResultEntry.JoinInto(complexResultEntries.Last.Value);
				}
				if (flag1)
				{
					continue;
				}
				complexResultEntries.AddLast(complexResultEntry);
			}
			do
			{
				if (complexResultEntries.Count < 2)
				{
					break;
				}
				flag = false;
				for (LinkedListNode<ComplexResultEntry> i = complexResultEntries.First; !flag && i != null; i = i.Next)
				{
					ComplexResultEntry value = i.Value;
					for (LinkedListNode<ComplexResultEntry> j = i.Next; !flag && j != null; j = j.Next)
					{
						flag = value.JoinInto(j.Value);
						if (flag)
						{
							complexResultEntries.Remove(i);
						}
					}
				}
			}
			while (flag);
			List<ComplexResultEntry> complexResultEntries1 = new List<ComplexResultEntry>();
			foreach (ComplexResultEntry complexResultEntry1 in complexResultEntries)
			{
				if (!complexResultEntry1.IsComplete(this._collectedResultTypes))
				{
					continue;
				}
				complexResultEntries1.Add(complexResultEntry1);
			}
			return complexResultEntries1;
		}

		public void ClearCachedResults()
		{
			this.RemoveAllCacheItems();
		}

		[Conditional("DEBUG")]
		private void DebugLog(string msg)
		{
			try
			{
				if (this._dmSystem != null && this._dmSystem.Connector != null && this._dmSystem.Connector.Logger != null)
				{
					this._dmSystem.Connector.Logger.Log("ResultCollector", msg);
				}
			}
			catch
			{
			}
		}

		private void DetermineNumResultTypesCollected()
		{
			this._collectedResultTypeList = new List<ResultTypes>();
			int num = 1;
			int num1 = 0;
			while (num1 < 31)
			{
				if (((int)this._collectedResultTypes & num) != (int)ResultTypes.None)
				{
					this._collectedResultTypeList.Add((ResultTypes)num);
				}
				num1++;
				num <<= 1;
			}
		}

		private void Dispose(bool disposeManagedResources)
		{
			if (Interlocked.CompareExchange(ref this._isDisposed, 1, 0) != 0)
			{
				return;
			}
			if (disposeManagedResources)
			{
				this.UpdateResultTimeOutTimer();
				this.ClearCachedResults();
				DataManSystem dataManSystem = this._dmSystem;
				this._dmSystem = null;
				if (dataManSystem != null)
				{
					dataManSystem.AutomaticResponseArrived -= new AutomaticResponseArrivedHandler(this.OnAutomaticResponseArrived);
					dataManSystem.SystemConnected -= new SystemConnectedHandler(this.OnSystemConnected);
				}
			}
		}

		public void Dispose()
		{
			this.Dispose(true);
		}

		public void EmptyResultQueue()
		{
			this.RemoveAllCacheItems();
		}

		private bool IsResultTypeCollected(ResultTypes resultTypes)
		{
			return (this._collectedResultTypes & resultTypes) == resultTypes;
		}

		private ResultInfo Legacy_ConvertComplexResultToResultInfo(ComplexResult completed_result)
		{
			int num = -1;
			int num1 = -1;
			string str = null;
			string str1 = null;
			string str2 = null;
			Image image = null;
			byte[] numArray = null;
			foreach (SimpleResult simpleResult in completed_result.SimpleResults)
			{
				this.Legacy_ExtractSimpleResultFields(simpleResult, ref num, ref num1, ref str, ref str1, ref str2, ref numArray);
			}
			return new ResultInfo(num, num1, str, str1, str2, image, numArray);
		}

		private ResultInfo Legacy_ConvertSimpleResultToResultInfo(SimpleResult simple_result)
		{
			int num = -1;
			int num1 = -1;
			string str = null;
			string str1 = null;
			string str2 = null;
			Image image = null;
			byte[] numArray = null;
			this.Legacy_ExtractSimpleResultFields(simple_result, ref num, ref num1, ref str, ref str1, ref str2, ref numArray);
			return new ResultInfo(num, num1, str, str1, str2, image, numArray);
		}

		private void Legacy_ExtractSimpleResultFields(SimpleResult simple_result, ref int result_id, ref int image_id, ref string graphics, ref string read_string, ref string read_xml, ref byte[] image_bytes)
		{
			ResultTypes type = simple_result.Id.Type;
			switch (type)
			{
				case ResultTypes.ReadString:
				{
					if (read_string != null)
					{
						break;
					}
					read_string = Encoding.UTF8.GetString(simple_result.Data, 0, (int)simple_result.Data.Length);
					return;
				}
				case ResultTypes.ReadXml:
				{
					if (result_id <= 0)
					{
						result_id = simple_result.Id.Id;
					}
					if (read_xml != null)
					{
						break;
					}
					read_xml = Encoding.UTF8.GetString(simple_result.Data, 0, (int)simple_result.Data.Length);
					return;
				}
				default:
				{
					if (type == ResultTypes.Image)
					{
						if (image_id <= 0)
						{
							image_id = simple_result.Id.Id;
						}
						if (read_xml != null)
						{
							break;
						}
						image_bytes = simple_result.Data;
						return;
					}
					else
					{
						if (type != ResultTypes.ImageGraphics)
						{
							return;
						}
						if (graphics != null)
						{
							break;
						}
						graphics = Encoding.UTF8.GetString(simple_result.Data, 0, (int)simple_result.Data.Length);
						break;
					}
				}
			}
		}

		private void Log(string msg)
		{
			try
			{
				if (this._dmSystem != null && this._dmSystem.Connector != null && this._dmSystem.Connector.Logger != null)
				{
					this._dmSystem.Connector.Logger.Log("ResultCollector", msg);
				}
			}
			catch
			{
			}
		}

		private void OnAutomaticResponseArrived(object sender, AutomaticResponseArrivedEventArgs args)
		{
			try
			{
				ResultTypes dataType = args.DataType;
				if (!this.IsResultTypeCollected(dataType))
				{
					if (dataType != ResultTypes.ReadString || !this.IsResultTypeCollected(ResultTypes.ReadXmlExtended))
					{
						this.RaiseDropped(new SimpleResult(new SimpleResultId(args.DataType, args.ResponseId), args.Data, DateTime.UtcNow));
						return;
					}
					else
					{
						dataType = ResultTypes.ReadXmlExtended;
					}
				}
				this.ParseAndStoreResponse(dataType, args.ResponseId, args.Data);
			}
			catch (Exception exception)
			{
			}
		}

		private void OnResultTimeOutTimerTicked(object state)
		{
			this.RemoveAllExpiredCacheItems();
		}

		private void OnSystemConnected(object sender, EventArgs args)
		{
			try
			{
				this.ResetFirmwareVersion();
				Regex regex = new Regex("^\\d+\\.\\d+");
				Match match = regex.Match(this._dmSystem.FirmwareVersion);
				if (match.Success)
				{
					this._firmwareVersion = float.Parse(match.Groups[0].Value);
				}
			}
			catch (Exception exception)
			{
			}
		}

		public void ParseAndStoreResponse(ResultTypes dataType, int responseId, byte[] data)
		{
			SimpleResultEntry simpleResultEntry = this.parseResponse(dataType, responseId, data);
			if (simpleResultEntry == null)
			{
				return;
			}
			if (this._collectedResultTypeList.Count == 1)
			{
				ComplexResult complexResult = new ComplexResult();
				complexResult.SimpleResults.Add(simpleResultEntry.Result);
				this.RaiseCompleted(complexResult);
				return;
			}
			List<SimpleResult> simpleResults = new List<SimpleResult>();
			List<ComplexResultEntry> complexResultEntries = new List<ComplexResultEntry>();
			lock (this._results_lock)
			{
				DateTime utcNow = DateTime.UtcNow;
				LinkedListNode<SimpleResultEntry> linkedListNode = this.searchCollectedSimpleResult_NonLocked(simpleResultEntry.Result.Id);
				if (linkedListNode != null)
				{
					if (linkedListNode.Value.NumRaisedCompletedEvents < 1)
					{
						simpleResults.Add(linkedListNode.Value.Result);
					}
					this.removeCollectedSimpleResult_NonLocked(linkedListNode);
				}
				simpleResultEntry.NumRaisedCompletedEvents = 0;
				this.addCollectedSimpleResult_NonLocked(simpleResultEntry);
				complexResultEntries = this.buildCompletedComplexResultsList();
				foreach (ComplexResultEntry complexResultEntry in complexResultEntries)
				{
					this.removeCollectedSimpleResults_NonLocked(complexResultEntry.SimpleResults);
				}
				DateTime dateTime = DateTime.UtcNow;
			}
			foreach (SimpleResult simpleResult in simpleResults)
			{
				this.RaiseDropped(simpleResult);
			}
			foreach (ComplexResultEntry complexResultEntry1 in complexResultEntries)
			{
				this.RaiseCompleted(complexResultEntry1);
			}
		}

		private SimpleResultEntry parseResponse(ResultTypes dataType, int responseId, byte[] data)
		{
			int num;
			HashSetPortable<int> hashSetPortable;
			HashSetPortable<int> hashSetPortable1;
			HashSetPortable<int> hashSetPortable2;
			HashSetPortable<int> hashSetPortable3;
			int num1;
			HashSetPortable<int> hashSetPortable4;
			SimpleResultEntry simpleResultEntry = null;
			DateTime utcNow = DateTime.UtcNow;
			if (dataType == ResultTypes.ReadString && (int)data.Length > 50 && Encoding.UTF8.GetString(data, 0, 50).StartsWith("<?xml version=\"1.0\"?><results>"))
			{
				dataType = ResultTypes.ReadXmlExtended;
			}
			ResultTypes resultType = dataType;
			if (resultType <= ResultTypes.CodeQualityData)
			{
				if (resultType <= ResultTypes.ImageGraphics)
				{
					switch (resultType)
					{
						case ResultTypes.None:
						case ResultTypes.ReadString | ResultTypes.ReadXml:
						case ResultTypes.ReadString | ResultTypes.XmlStatistics:
						case ResultTypes.ReadXml | ResultTypes.XmlStatistics:
						case ResultTypes.ReadString | ResultTypes.ReadXml | ResultTypes.XmlStatistics:
						{
							break;
						}
						case ResultTypes.ReadString:
						{
							simpleResultEntry = new SimpleResultEntry(ResultTypes.ReadString, responseId, data, utcNow);
							simpleResultEntry.AddContainedResultId(responseId);
							break;
						}
						case ResultTypes.ReadXml:
						{
							simpleResultEntry = new SimpleResultEntry(ResultTypes.ReadXml, responseId, data, utcNow);
							DmccResponseParserUtils.ExtractIdsFromReadXml(simpleResultEntry.Result.GetDataAsString(), out hashSetPortable, out hashSetPortable1, out num);
							simpleResultEntry.AddContainedResultIds(hashSetPortable);
							simpleResultEntry.AddReferredImageIds(hashSetPortable1);
							this.AddExpectedMultipleResults(simpleResultEntry, hashSetPortable1);
							break;
						}
						case ResultTypes.XmlStatistics:
						{
							simpleResultEntry = new SimpleResultEntry(ResultTypes.XmlStatistics, responseId, data, utcNow);
							simpleResultEntry.AddContainedResultId(responseId);
							break;
						}
						case ResultTypes.Image:
						{
							simpleResultEntry = new SimpleResultEntry(ResultTypes.Image, responseId, data, utcNow);
							simpleResultEntry.AddContainedImageId(responseId);
							break;
						}
						default:
						{
							if (resultType == ResultTypes.ImageGraphics)
							{
								simpleResultEntry = new SimpleResultEntry(ResultTypes.ImageGraphics, responseId, data, utcNow);
								DmccResponseParserUtils.ExtractIdsFromSvg(this._firmwareVersion, simpleResultEntry.Result.GetDataAsString(), out hashSetPortable4, out num1);
								if (num1 > 0)
								{
									simpleResultEntry.AddReferredImageId(num1);
								}
								simpleResultEntry.AddReferredResultIds(hashSetPortable4);
								break;
							}
							else
							{
								break;
							}
						}
					}
				}
				else if (resultType == ResultTypes.TrainingResults)
				{
					simpleResultEntry = new SimpleResultEntry(ResultTypes.TrainingResults, responseId, data, utcNow);
					simpleResultEntry.AddContainedResultId(responseId);
				}
				else if (resultType == ResultTypes.CodeQualityData)
				{
					simpleResultEntry = new SimpleResultEntry(ResultTypes.CodeQualityData, responseId, data, utcNow);
					simpleResultEntry.AddContainedResultId(responseId);
				}
			}
			else if (resultType <= ResultTypes.InputEvent)
			{
				if (resultType == ResultTypes.ReadXmlExtended)
				{
					simpleResultEntry = new SimpleResultEntry(ResultTypes.ReadXmlExtended, responseId, data, utcNow);
					int num2 = -1;
					DmccResponseParserUtils.ExtractIdsFromReadXmlExtended(simpleResultEntry.Result.GetDataAsString(), out hashSetPortable2, out hashSetPortable3, out num2);
					simpleResultEntry.AddContainedResultIds(hashSetPortable2);
					simpleResultEntry.AddReferredImageIds(hashSetPortable3);
					this.AddExpectedMultipleResults(simpleResultEntry, hashSetPortable3);
				}
				else if (resultType == ResultTypes.InputEvent)
				{
					simpleResultEntry = new SimpleResultEntry(ResultTypes.InputEvent, responseId, data, utcNow);
					simpleResultEntry.AddContainedResultId(responseId);
				}
			}
			else if (resultType == ResultTypes.GroupTriggering)
			{
				simpleResultEntry = new SimpleResultEntry(ResultTypes.GroupTriggering, responseId, data, utcNow);
				simpleResultEntry.AddContainedResultId(responseId);
			}
			else if (resultType == ResultTypes.ProcessControlMetricsReport)
			{
				simpleResultEntry = new SimpleResultEntry(ResultTypes.ProcessControlMetricsReport, responseId, data, utcNow);
				simpleResultEntry.AddContainedResultId(responseId);
			}
			return simpleResultEntry;
		}

		private void RaiseCompleted(ComplexResultEntry completed_result_entry)
		{
			this.RaiseCompleted(ComplexResultEntry.ConvertToComplexResult(completed_result_entry, false));
		}

		private void RaiseCompleted(ComplexResult completed_result)
		{
			ComplexResultCompletedEventHandler complexResultCompletedEventHandler = this.ComplexResultCompleted;
			if (complexResultCompletedEventHandler != null)
			{
				complexResultCompletedEventHandler(this, completed_result);
			}
			ComplexResultArrivedEventHandler complexResultArrivedEventHandler = this.ComplexResultArrived;
			if (complexResultArrivedEventHandler != null)
			{
				complexResultArrivedEventHandler(this, this.Legacy_ConvertComplexResultToResultInfo(completed_result));
			}
		}

		private void RaiseDropped(SimpleResult dropped_simple_result)
		{
			SimpleResultDroppedEventHandler simpleResultDroppedEventHandler = this.SimpleResultDropped;
			if (simpleResultDroppedEventHandler != null)
			{
				simpleResultDroppedEventHandler(this, dropped_simple_result);
			}
			PartialResultDroppedEventHandler partialResultDroppedEventHandler = this.PartialResultDropped;
			if (partialResultDroppedEventHandler != null)
			{
				partialResultDroppedEventHandler(this, this.Legacy_ConvertSimpleResultToResultInfo(dropped_simple_result));
			}
		}

		private void RaiseDroppedIfUnused(SimpleResultEntry droppedSimpleResult)
		{
			if (droppedSimpleResult.NumRaisedCompletedEvents >= 1)
			{
				return;
			}
			this.RaiseDropped(droppedSimpleResult.Result);
		}

		private void RemoveAllCacheItems()
		{
			this.RemoveAllExpiredCacheItems(true, DateTime.MaxValue, 0);
		}

		private void RemoveAllExpiredCacheItems()
		{
			DateTime utcNow = DateTime.UtcNow - this._resultTimeOut;
			this.RemoveAllExpiredCacheItems(false, utcNow, this._resultCacheLength);
		}

		private void RemoveAllExpiredCacheItems(bool unconditionallyRemoveAll, DateTime expiryDateUtc, int maxNumItems)
		{
			LinkedListNode<SimpleResultEntry> next = null;
			List<SimpleResult> simpleResults = new List<SimpleResult>();
			lock (this._results_lock)
			{
				for (LinkedListNode<SimpleResultEntry> i = this._collectedSimpleResults.First; i != null; i = next)
				{
					next = i.Next;
					SimpleResultEntry value = i.Value;
					bool arrivedAtUtc = value.Result.ArrivedAtUtc < expiryDateUtc;
					bool count = this._collectedSimpleResults.Count > maxNumItems;
					if (unconditionallyRemoveAll || arrivedAtUtc || count)
					{
						if (value.NumRaisedCompletedEvents < 1)
						{
							simpleResults.Add(value.Result);
						}
						this.removeCollectedSimpleResult_NonLocked(i);
					}
				}
			}
			foreach (SimpleResult simpleResult in simpleResults)
			{
				this.RaiseDropped(simpleResult);
			}
		}

		private void removeCollectedSimpleResult_NonLocked(LinkedListNode<SimpleResultEntry> simpleResultNode)
		{
			if (simpleResultNode == null)
			{
				return;
			}
			SimpleResultId id = simpleResultNode.Value.Result.Id;
			this._collectedSimpleResultIds.Remove(id);
			int count = this._collectedSimpleResults.Count;
			this._collectedSimpleResults.Remove(simpleResultNode);
		}

		private void removeCollectedSimpleResults_NonLocked(Dictionary<SimpleResultId, SimpleResultEntry> resultsToRemove)
		{
			LinkedListNode<SimpleResultEntry> next = null;
			for (LinkedListNode<SimpleResultEntry> i = this._collectedSimpleResults.First; i != null; i = next)
			{
				next = i.Next;
				if (resultsToRemove.ContainsKey(i.Value.Result.Id))
				{
					this.removeCollectedSimpleResult_NonLocked(i);
				}
			}
		}

		private void ResetFirmwareVersion()
		{
			this._firmwareVersion = 5.7f;
		}

		private LinkedListNode<SimpleResultEntry> searchCollectedSimpleResult_NonLocked(SimpleResultId searchedResultId)
		{
			if (!this._collectedSimpleResultIds.ContainsKey(searchedResultId))
			{
				return null;
			}
			for (LinkedListNode<SimpleResultEntry> i = this._collectedSimpleResults.First; i != null; i = i.Next)
			{
				if (i.Value.Result.Id.Equals(searchedResultId))
				{
					return i;
				}
			}
			return null;
		}

		private void UpdateResultTimeOutTimer()
		{
			if (this._isDisposed == 1)
			{
				this._resultTimeOutTimer.Change(-1, -1);
				return;
			}
			this._resultTimeOutTimer.Change((int)this._resultTimeOut.TotalMilliseconds, this._purgeOldResultsFrequency);
		}

		[Obsolete("Use event ComplexResultCompleted instead")]
		public event ComplexResultArrivedEventHandler ComplexResultArrived;

		public event ComplexResultCompletedEventHandler ComplexResultCompleted;

		[Obsolete("Use event SimpleResultDropped instead")]
		public event PartialResultDroppedEventHandler PartialResultDropped;

		public event SimpleResultDroppedEventHandler SimpleResultDropped;
	}
}
using Cognex.DataMan.SDK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Xml;

namespace Cognex.DataMan.SDK.Utils
{
	public class ResultCollectorLegacy : IDisposable
	{
		private int _isDisposed;

		private DataManSystem _dmSystem;

		private object _currentResultInfoSyncLock = new object();

		private Dictionary<int, ResultInfo> _complexResultCache = new Dictionary<int, ResultInfo>();

		private Dictionary<int, ResultInfo> _imageCache = new Dictionary<int, ResultInfo>();

		private Dictionary<int, ResultInfo> _imageGraphicsCache = new Dictionary<int, ResultInfo>();

		private Dictionary<int, ResultInfo> _readStringCache = new Dictionary<int, ResultInfo>();

		private int _resultCacheLength = 24;

		private TimeSpan _resultTimeOut = TimeSpan.FromSeconds(10);

		private ResultTypes _resultTypes;

		public int ResultCacheLength
		{
			get
			{
				return this._resultCacheLength;
			}
			set
			{
				this._resultCacheLength = value;
				this.ProcessResultQueue(false);
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
			}
		}

		public ResultCollectorLegacy(DataManSystem system, ResultTypes expectedResultTypes)
		{
			this._dmSystem = system;
			if (expectedResultTypes == ResultTypes.None)
			{
				throw new InvalidResultTypeException("No result type was set for the Result Collector.");
			}
			if ((expectedResultTypes & ResultTypes.CodeQualityData) != ResultTypes.None || (expectedResultTypes & ResultTypes.TrainingResults) != ResultTypes.None || (expectedResultTypes & ResultTypes.XmlStatistics) != ResultTypes.None)
			{
				throw new InvalidResultTypeException("Result Collector can only process Readstring, ReadXml, Image and ImageGraphics results!");
			}
			this._resultTypes = expectedResultTypes;
			this._dmSystem.ReadStringArrived += new ReadStringArrivedHandler(this.OnReadStringArrived);
			this._dmSystem.ImageGraphicsArrived += new ImageGraphicsArrivedHandler(this.OnImageGraphicsArrived);
			this._dmSystem.ImageArrived += new ImageArrivedHandler(this.OnImageArrived);
			this._dmSystem.XmlResultArrived += new XmlResultArrivedHandler(this.OnXmlResultArrived);
		}

		public void ClearCachedResults()
		{
			lock (this._currentResultInfoSyncLock)
			{
				this.RemoveAllExpiredCacheItems(DateTime.MaxValue, 0);
			}
		}

		public void Dispose()
		{
			this.DoDispose(true);
			GC.SuppressFinalize(this);
		}

		private void DoDispose(bool disposeManagedResources)
		{
			if (Interlocked.CompareExchange(ref this._isDisposed, 1, 0) == 0)
			{
				if (this._dmSystem != null)
				{
					this._dmSystem.ReadStringArrived -= new ReadStringArrivedHandler(this.OnReadStringArrived);
					this._dmSystem.ImageGraphicsArrived -= new ImageGraphicsArrivedHandler(this.OnImageGraphicsArrived);
					this._dmSystem.ImageArrived -= new ImageArrivedHandler(this.OnImageArrived);
					this._dmSystem.XmlResultArrived -= new XmlResultArrivedHandler(this.OnXmlResultArrived);
				}
				this._dmSystem = null;
			}
		}

		private void DropImage(int resultId, int imageId, byte[] imageBytes)
		{
			if (this.PartialResultDropped != null)
			{
				this.PartialResultDropped(this, new ResultInfo(resultId, imageId, null, null, null, null, imageBytes));
			}
		}

		private void DropImageGraphics(int resultId, int imageGraphicsId, string imageGraphics)
		{
			if (this.PartialResultDropped != null)
			{
				this.PartialResultDropped(this, new ResultInfo(resultId, imageGraphicsId, imageGraphics, null, null, null, null));
			}
		}

		private void DropReadString(int resultId, string readString)
		{
			if (this.PartialResultDropped != null)
			{
				this.PartialResultDropped(this, new ResultInfo(resultId, -1, null, readString, null, null, null));
			}
		}

		private void DropXmlResult(int resultId, string xmlResult)
		{
			if (this.PartialResultDropped != null)
			{
				this.PartialResultDropped(this, new ResultInfo(resultId, -1, null, null, xmlResult, null, null));
			}
		}

		public void EmptyResultQueue()
		{
			lock (this._currentResultInfoSyncLock)
			{
				this.RemoveAllExpiredCacheItems(DateTime.MaxValue, 0);
			}
		}

		~ResultCollectorLegacy()
		{
			this.DoDispose(false);
		}

		private void MergeCachesIntoComplexCache(bool createMissingEntry)
		{
			Dictionary<int, int> nums = null;
			nums = null;
			foreach (KeyValuePair<int, ResultInfo> value in this._readStringCache)
			{
				if (nums == null)
				{
					nums = new Dictionary<int, int>();
				}
				if (!createMissingEntry || this._complexResultCache.ContainsKey(value.Value.ResultId))
				{
					if (!this.StoreReadString(value.Value.ResultId, value.Value.ReadString, this._complexResultCache))
					{
						continue;
					}
					nums.Add(value.Key, 1);
				}
				else
				{
					this._complexResultCache[value.Value.ResultId] = value.Value;
					nums.Add(value.Key, 1);
				}
			}
			if (nums != null)
			{
				foreach (KeyValuePair<int, int> num in nums)
				{
					this._readStringCache.Remove(num.Key);
				}
			}
			nums = null;
			foreach (KeyValuePair<int, ResultInfo> keyValuePair in this._imageCache)
			{
				if (nums == null)
				{
					nums = new Dictionary<int, int>();
				}
				if (!createMissingEntry || this._complexResultCache.ContainsKey(keyValuePair.Value.ImageId))
				{
					if (!this.StoreImage(keyValuePair.Value.ImageId, keyValuePair.Value.ImageBytes, this._complexResultCache))
					{
						continue;
					}
					nums.Add(keyValuePair.Key, 1);
				}
				else
				{
					this._complexResultCache[keyValuePair.Value.ImageId] = keyValuePair.Value;
					nums.Add(keyValuePair.Key, 1);
				}
			}
			if (nums != null)
			{
				foreach (KeyValuePair<int, int> num1 in nums)
				{
					this._imageCache.Remove(num1.Key);
				}
			}
			nums = null;
			foreach (KeyValuePair<int, ResultInfo> value1 in this._imageGraphicsCache)
			{
				if (nums == null)
				{
					nums = new Dictionary<int, int>();
				}
				if (!createMissingEntry || this._complexResultCache.ContainsKey(value1.Value.ResultId))
				{
					if (!this.StoreImageGraphics(value1.Value.ResultId, value1.Value.ImageGraphics, this._complexResultCache))
					{
						continue;
					}
					nums.Add(value1.Key, 1);
				}
				else
				{
					this._complexResultCache[value1.Value.ResultId] = value1.Value;
					nums.Add(value1.Key, 1);
				}
			}
			if (nums != null)
			{
				foreach (KeyValuePair<int, int> keyValuePair1 in nums)
				{
					this._imageGraphicsCache.Remove(keyValuePair1.Key);
				}
			}
		}

		private void OnImageArrived(object sender, ImageArrivedEventArgs args)
		{
			int num;
			int resultInfo;
			ResultInfo resultInfo1;
			Image image;
			lock (this._currentResultInfoSyncLock)
			{
				this.ParseImage(args, out num, out resultInfo);
				if ((this._resultTypes & ResultTypes.Image) != ResultTypes.None)
				{
					if (!this.StoreImage(num, args.ImageBytes, this._complexResultCache))
					{
						if (this._imageCache.TryGetValue(resultInfo, out resultInfo1))
						{
							this.DropImage(num, resultInfo, resultInfo1.ImageBytes);
						}
						if (args.IsImageCreatedFromImageBytes)
						{
							image = args.Image;
						}
						else
						{
							image = null;
						}
						Image image1 = image;
						this._imageCache[resultInfo] = new ResultInfo(num, resultInfo, null, null, null, image1, args.ImageBytes);
					}
					this.ProcessResultQueue(false);
				}
				else
				{
					this.DropImage(num, resultInfo, args.ImageBytes);
				}
			}
		}

		private void OnImageGraphicsArrived(object sender, ImageGraphicsArrivedEventArgs args)
		{
			int num;
			int resultInfo;
			lock (this._currentResultInfoSyncLock)
			{
				this.ParseImageGraphics(args, out num, out resultInfo);
				if ((this._resultTypes & ResultTypes.ImageGraphics) != ResultTypes.None)
				{
					if (!this.StoreImageGraphics(num, args.ImageGraphics, this._complexResultCache))
					{
						if (this._imageGraphicsCache.ContainsKey(resultInfo))
						{
							this.DropImageGraphics(num, resultInfo, this._imageGraphicsCache[resultInfo].ImageGraphics);
						}
						this._imageGraphicsCache[resultInfo] = new ResultInfo(num, resultInfo, args.ImageGraphics, null, null, null, null);
					}
					this.ProcessResultQueue(false);
				}
				else
				{
					this.DropImageGraphics(num, resultInfo, args.ImageGraphics);
				}
			}
		}

		private void OnReadStringArrived(object sender, ReadStringArrivedEventArgs args)
		{
			int resultInfo;
			lock (this._currentResultInfoSyncLock)
			{
				this.ParseReadString(args, out resultInfo);
				if ((this._resultTypes & ResultTypes.ReadString) != ResultTypes.None)
				{
					if (!this.StoreReadString(resultInfo, args.ReadString, this._complexResultCache))
					{
						if (this._readStringCache.ContainsKey(resultInfo))
						{
							this.DropReadString(resultInfo, this._readStringCache[resultInfo].ReadString);
						}
						this._readStringCache[resultInfo] = new ResultInfo(resultInfo, -1, null, args.ReadString, null, null, null);
					}
					this.ProcessResultQueue(false);
				}
				else
				{
					this.DropReadString(resultInfo, args.ReadString);
				}
			}
		}

		private void OnXmlResultArrived(object sender, XmlResultArrivedEventArgs args)
		{
			lock (this._currentResultInfoSyncLock)
			{
				if ((this._resultTypes & ResultTypes.ReadXml) != ResultTypes.None)
				{
					ResultInfo resultInfo = this.ParseXmlResult(args);
					if (resultInfo != null)
					{
						if (this._complexResultCache.ContainsKey(args.ResultId))
						{
							this.DropXmlResult(args.ResultId, this._complexResultCache[args.ResultId].XmlResult);
						}
						this._complexResultCache[args.ResultId] = resultInfo;
						this.ProcessResultQueue(true);
					}
					else
					{
						this.DropXmlResult(args.ResultId, args.XmlResult);
					}
				}
				else
				{
					this.DropXmlResult(args.ResultId, args.XmlResult);
				}
			}
		}

		private void ParseImage(ImageArrivedEventArgs args, out int resultId, out int imageId)
		{
			int num = args.ResultId;
			int num1 = num;
			resultId = num;
			imageId = num1;
		}

		private void ParseImageGraphics(ImageGraphicsArrivedEventArgs args, out int resultId, out int imageId)
		{
			int num = args.ResultId;
			int num1 = num;
			resultId = num;
			imageId = num1;
		}

		private void ParseReadString(ReadStringArrivedEventArgs args, out int resultId)
		{
			resultId = args.ResultId;
		}

		private void ParseResultNode(XmlNode resultNode, ref ResultInfo result)
		{
			int num;
			int num1;
			XmlAttribute itemOf = resultNode.Attributes["id"];
			if (itemOf != null && this.TryParse(itemOf.InnerText, out num))
			{
				result.ResultId = num;
			}
			XmlAttribute xmlAttribute = resultNode.Attributes["image_id"];
			if (xmlAttribute != null && this.TryParse(xmlAttribute.InnerText, out num1))
			{
				result.ImageId = num1;
			}
		}

		private ResultInfo ParseXmlResult(XmlResultArrivedEventArgs args)
		{
			ResultInfo resultInfo;
			XmlDocument xmlDocument = new XmlDocument();
			try
			{
				xmlDocument.LoadXml(args.XmlResult);
				XmlNode xmlNodes = xmlDocument.SelectSingleNode("result");
				if (xmlNodes != null)
				{
					ResultInfo resultInfos = new ResultInfo(-1, -1, null, null, args.XmlResult, null, null);
					this.ParseResultNode(xmlNodes, ref resultInfos);
					foreach (XmlNode xmlNodes1 in xmlNodes.SelectNodes("/result/result"))
					{
						try
						{
							ResultInfo resultInfo1 = new ResultInfo(-1, -1, null, null, xmlNodes1.OuterXml, null, null);
							this.ParseResultNode(xmlNodes1, ref resultInfo1);
							if (resultInfo1 != null && resultInfo1.ResultId >= 1)
							{
								if (resultInfos.SubResults == null)
								{
									resultInfos.SubResults = new List<ResultInfo>();
								}
								resultInfos.SubResults.Add(resultInfo1);
							}
						}
						catch
						{
						}
					}
					resultInfo = resultInfos;
				}
				else
				{
					resultInfo = null;
				}
			}
			finally
			{
				xmlDocument = null;
			}
			return resultInfo;
		}

		public void ProcessResultQueue()
		{
			this.ProcessResultQueue(true);
		}

		private void ProcessResultQueue(bool complexCacheChanged)
		{
			lock (this._currentResultInfoSyncLock)
			{
				DateTime now = DateTime.Now;
				int key = -1;
				if (complexCacheChanged)
				{
					this.MergeCachesIntoComplexCache(false);
				}
				else if ((this._resultTypes & ResultTypes.ReadXml) == ResultTypes.None)
				{
					this.MergeCachesIntoComplexCache(true);
				}
				foreach (KeyValuePair<int, ResultInfo> keyValuePair in this._complexResultCache)
				{
					if (!keyValuePair.Value.IsResultComplete(this._resultTypes))
					{
						continue;
					}
					key = keyValuePair.Key;
					if (this.ComplexResultArrived == null)
					{
						break;
					}
					this.ComplexResultArrived(this, keyValuePair.Value);
					break;
				}
				if (key >= 0)
				{
					this._complexResultCache.Remove(key);
				}
				this.RemoveAllExpiredCacheItems(now - this._resultTimeOut, this._resultCacheLength);
			}
		}

		private void RemoveAllExpiredCacheItems(DateTime expiryDate, int maxNumItems)
		{
			Dictionary<int, ResultInfo>[] dictionaryArrays = new Dictionary<int, ResultInfo>[] { this._readStringCache, this._imageCache, this._imageGraphicsCache, this._complexResultCache };
			Dictionary<int, ResultInfo>[] dictionaryArrays1 = dictionaryArrays;
			for (int i = 0; i < (int)dictionaryArrays1.Length; i++)
			{
				Dictionary<int, ResultInfo> nums = dictionaryArrays1[i];
				List<ResultInfo> resultInfos = ResultCollectorLegacy.RemoveExpiredCacheItems(nums, expiryDate, this._resultCacheLength);
				if (resultInfos.Count > 0)
				{
					foreach (ResultInfo resultInfo in resultInfos)
					{
						if (this.PartialResultDropped == null)
						{
							continue;
						}
						this.PartialResultDropped(this, resultInfo);
					}
				}
			}
		}

		private static List<ResultInfo> RemoveExpiredCacheItems(Dictionary<int, ResultInfo> cache, DateTime expiryDate, int maxNumItems)
		{
			List<ResultInfo> resultInfos = new List<ResultInfo>();
			if (cache.Count > 0)
			{
				List<KeyValuePair<DateTime, int>> keyValuePairs = new List<KeyValuePair<DateTime, int>>(cache.Count);
				foreach (KeyValuePair<int, ResultInfo> keyValuePair in cache)
				{
					keyValuePairs.Add(new KeyValuePair<DateTime, int>(keyValuePair.Value.ResultArrivedAt, keyValuePair.Key));
				}
				keyValuePairs.Sort((KeyValuePair<DateTime, int> kv1, KeyValuePair<DateTime, int> kv2) => kv1.Key.CompareTo(kv2.Key));
				int count = keyValuePairs.Count - maxNumItems;
				for (int i = 0; i < keyValuePairs.Count; i++)
				{
					if (i < count || keyValuePairs[i].Key < expiryDate)
					{
						int value = keyValuePairs[i].Value;
						resultInfos.Add(cache[value]);
						cache.Remove(value);
					}
				}
			}
			return resultInfos;
		}

		private bool StoreImage(int imageId, byte[] imageBytes, Dictionary<int, ResultInfo> destination)
		{
			bool flag = false;
			foreach (KeyValuePair<int, ResultInfo> keyValuePair in destination)
			{
				if (!this.StoreImage(imageId, imageBytes, keyValuePair.Value))
				{
					continue;
				}
				flag = true;
			}
			return flag;
		}

		private bool StoreImage(int imageId, byte[] imageBytes, ResultInfo destination)
		{
			bool flag = false;
			if (imageBytes == null || destination == null)
			{
				return false;
			}
			if (destination.HasImageId)
			{
				if (destination.ImageId == imageId)
				{
					byte[] numArray = destination.ImageBytes;
					destination.Image = null;
					destination.ImageBytes = imageBytes;
					flag = true;
					if (numArray != null)
					{
						this.DropImage(destination.ResultId, imageId, numArray);
					}
				}
				if (destination.SubResults != null)
				{
					foreach (ResultInfo subResult in destination.SubResults)
					{
						if (!this.StoreImage(imageId, imageBytes, subResult))
						{
							continue;
						}
						flag = true;
					}
				}
			}
			else if (destination.ResultId == imageId)
			{
				destination.Image = null;
				destination.ImageBytes = imageBytes;
				flag = true;
			}
			return flag;
		}

		private bool StoreImageGraphics(int imageGraphicsId, string imageGraphics, Dictionary<int, ResultInfo> destination)
		{
			bool flag = false;
			foreach (KeyValuePair<int, ResultInfo> keyValuePair in destination)
			{
				if (!this.StoreImageGraphics(imageGraphicsId, imageGraphics, keyValuePair.Value))
				{
					continue;
				}
				flag = true;
			}
			return flag;
		}

		private bool StoreImageGraphics(int imageGraphicsId, string imageGraphics, ResultInfo destination)
		{
			bool flag = false;
			if (imageGraphics == null || destination == null)
			{
				return false;
			}
			if (destination.HasImageId)
			{
				if (destination.ImageId == imageGraphicsId)
				{
					string str = destination.ImageGraphics;
					destination.ImageGraphics = imageGraphics;
					flag = true;
					if (str != null)
					{
						this.DropImageGraphics(destination.ResultId, imageGraphicsId, str);
					}
				}
				if (destination.SubResults != null)
				{
					foreach (ResultInfo subResult in destination.SubResults)
					{
						if (!this.StoreImageGraphics(imageGraphicsId, imageGraphics, subResult))
						{
							continue;
						}
						flag = true;
					}
				}
			}
			else if (destination.ResultId == imageGraphicsId)
			{
				destination.ImageGraphics = imageGraphics;
				flag = true;
			}
			return flag;
		}

		private bool StoreReadString(int resultId, string readString, Dictionary<int, ResultInfo> destination)
		{
			bool flag = false;
			foreach (KeyValuePair<int, ResultInfo> keyValuePair in destination)
			{
				if (!this.StoreReadString(resultId, readString, keyValuePair.Value))
				{
					continue;
				}
				flag = true;
			}
			return flag;
		}

		private bool StoreReadString(int resultId, string readString, ResultInfo destination)
		{
			bool flag = false;
			if (readString == null || destination == null)
			{
				return false;
			}
			if (destination.ResultId == resultId)
			{
				string str = destination.ReadString;
				destination.ReadString = readString;
				flag = true;
				if (str != null)
				{
					this.DropReadString(destination.ResultId, str);
				}
			}
			if (destination.SubResults != null)
			{
				foreach (ResultInfo subResult in destination.SubResults)
				{
					if (!this.StoreReadString(resultId, readString, subResult))
					{
						continue;
					}
					flag = true;
				}
			}
			return flag;
		}

		private bool TryParse(string input, out int result)
		{
			return int.TryParse(input, out result);
		}

		public event ComplexResultArrivedEventHandler ComplexResultArrived;

		public event PartialResultDroppedEventHandler PartialResultDropped;
	}
}
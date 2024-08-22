using System;
using System.Runtime.CompilerServices;

namespace Cognex.DataMan.SDK
{
	public class XmlStatisticsArrivedEvent : EventArgs
	{
		public string XmlStatistics
		{
			get;
			private set;
		}

		internal XmlStatisticsArrivedEvent(string xmlStatistics)
		{
			this.XmlStatistics = xmlStatistics;
		}
	}
}
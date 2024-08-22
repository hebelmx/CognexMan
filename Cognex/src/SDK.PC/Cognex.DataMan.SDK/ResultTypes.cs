using System;

namespace Cognex.DataMan.SDK
{
	[Flags]
	public enum ResultTypes
	{
		None = 0,
		ReadString = 1,
		ReadXml = 2,
		XmlStatistics = 4,
		Image = 8,
		ImageGraphics = 16,
		TrainingResults = 32,
		CodeQualityData = 64,
		ReadXmlExtended = 128,
		InputEvent = 256,
		GroupTriggering = 512,
		ProcessControlMetricsReport = 1024
	}
}
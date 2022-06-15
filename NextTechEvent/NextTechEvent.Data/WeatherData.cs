using Raven.Client.Documents.Session.TimeSeries;

namespace NextTechEvent.Data;

public class WeatherData
{
    [TimeSeriesValue(0)]
    public double Minimum { get; set; }
    [TimeSeriesValue(1)]
    public double Maximum { get; set; }
    [TimeSeriesValue(2)]
    public double Average { get; set; }
}

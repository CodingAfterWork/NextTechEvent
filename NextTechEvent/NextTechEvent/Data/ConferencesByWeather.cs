using Raven.Client.Documents.Indexes.TimeSeries;

namespace NextTechEvent.Data
{
    public class ConferencesByWeather : AbstractMultiMapTimeSeriesIndexCreationTask
    {
        public ConferencesByWeather()
        {

            var currentDate = DateOnly.FromDateTime(DateTime.Now);
            AddMap<Conference>(
                "WeatherDatas",
                timeSeries => from ts in timeSeries
                              let conference = LoadDocument<Conference>(ts.DocumentId)
                              from entry in ts.Entries
                              select new
                              {
                                  Minimum = entry.Values[0],
                                  Maximum = entry.Values[1],
                                  Average = entry.Values[2],
                                  entry.Timestamp,
                                  ConferenceId = ts.DocumentId,
                                  Name = conference.Name,
                                  conference.CfpStartDate,
                                  conference.CfpEndDate,
                                  conference.EventStart,
                                  conference.EventEnd
                              });
        }
    }
}

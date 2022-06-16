using Raven.Client.Documents.Indexes.TimeSeries;

namespace NextTechEvent.Data.Index
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
                              select new ConferenceWeather()
                              {
                                  Minimum = entry.Values[0],
                                  Maximum = entry.Values[1],
                                  Average = entry.Values[2],
                                  Timestamp = entry.Timestamp,
                                  ConferenceId = ts.DocumentId,
                                  Name = conference.Name,
                                  CfpStartDate = conference.CfpStartDate,
                                  CfpEndDate = conference.CfpEndDate,
                                  EventStart = conference.EventStart,
                                  EventEnd = conference.EventEnd
                              });
        }
    }
}

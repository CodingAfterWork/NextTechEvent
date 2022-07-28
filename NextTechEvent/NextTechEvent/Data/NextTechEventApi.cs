using Microsoft.AspNetCore.Components.Web.Virtualization;
using NextTechEvent.Data.Index;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Client.Documents.Session.TimeSeries;
using InterfaceGenerator;
namespace NextTechEvent.Data
{
    [GenerateAutoInterface]
    public class NextTechEventApi : INextTechEventApi
    {
        IDocumentStore _store;
        public NextTechEventApi(IDocumentStore store)
        {
            _store = store;
        }

        public async Task<Conference> SaveConferenceAsync(Conference conference)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            await session.StoreAsync(conference);
            await session.SaveChangesAsync();
            return conference;
        }

        public async Task<Conference> GetConferenceAsync(string id)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            return await session.Query<Conference>().Where(c => c.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<Conference>> GetConferencesAsync()
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            return await session.Query<Conference>().Where(c => c.EventEnd > DateOnly.FromDateTime(DateTime.Now)).ToListAsync();
        }

        public async Task<List<ConferenceCountByDate>> GetConferenceCountByDate(DateOnly start, DateOnly end)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            return await session.Query<ConferenceCountByDate>("ConferenceCountByDates").Where(c => c.Date >= start && c.Date <= end).ToListAsync();
        }

        public async ValueTask<ItemsProviderResult<Conference>> GetConferencesWithOpenCfpAsync(ItemsProviderRequest request)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            var confs =
                await session.Query<Conference>()
                .Statistics(out var stats)
                .Where(c => c.CfpEndDate > DateTime.Now)
                .OrderBy(c => c.CfpEndDate)
                .Skip(request.StartIndex)
                .Take(request.Count).ToListAsync();

            return new ItemsProviderResult<Conference>(confs, stats.TotalResults);
        }

        public async ValueTask<ItemsProviderResult<Conference>> GetConferencesAsync(ItemsProviderRequest request)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            var confs =
                await session.Query<Conference>()
                .Statistics(out var stats)
                .Where(c => c.EventStart > DateOnly.FromDateTime(DateTime.Now))
                .OrderBy(c => c.EventStart)
                .Skip(request.StartIndex)
                .Take(request.Count).ToListAsync();

            return new ItemsProviderResult<Conference>(confs, stats.TotalResults);
        }

        public async Task<List<ConferenceWeather>> GetConferencesByWeatherAsync(double averageTemp)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();

            return await session.Query<ConferenceWeather>("ConferencesByWeather").Where(c => c.Average > averageTemp).ToListAsync();
        }

        public async Task<TimeSeriesEntry<WeatherData>[]> GetWeatherTimeSeriesAsync(string conferenceId)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();

            TimeSeriesEntry<WeatherData>[] val = await session.TimeSeriesFor<WeatherData>(conferenceId).GetAsync();
            return val;
        }
    }
}

using Microsoft.AspNetCore.Components.Web.Virtualization;
using NextTechEvent.Data.Index;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Client.Documents.Session.TimeSeries;
using InterfaceGenerator;
using NextTechEvent.Components;
using Raven.Client.Documents.Queries;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using NPOI.SS.Formula.Functions;

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
        
        public async Task<Status> SaveStatusAsync(Status status)
        {
            var savedstatus = await GetStatusAsync(status.ConferenceId, status.UserId);
            if (savedstatus != null)
            {
                status.Id = savedstatus.Id;
            }

            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            await session.StoreAsync(status);
            await session.SaveChangesAsync();
            return status;
        }

        public async Task<Status> GetStatusAsync(string conferenceId,string userId)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            return await session.Query<Status>().Where(c => c.ConferenceId == conferenceId && c.UserId==userId).FirstOrDefaultAsync();
        }

        public async Task<Ical.Net.Calendar> GetUserCalendarAsync(string userId)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            var data=await session.Query<Status>()
                .Include(c => c.ConferenceId)
                .Where(c => c.UserId == userId && c.State != StateEnum.NotSet && c.State != StateEnum.Rejected)
                .ToListAsync();
            
            var calendar = new Ical.Net.Calendar();
            calendar.Name = "NextTechEvent";
            foreach (var s in data)
            {
                var conference = await session.LoadAsync<Conference>(s.ConferenceId);
                var e = new CalendarEvent
                {
                    Start = new CalDateTime(conference.EventStart.ToDateTime(new TimeOnly(0, 0))),
                    End = new CalDateTime(conference.EventEnd.ToDateTime(new TimeOnly(0, 0))),
                    IsAllDay = true,
                    Summary = $"{s.State} {conference.Name}",
                    Description = $"https://nexttechevent.azurewebsites.net/Conferences/{s.ConferenceId}"
                };


                calendar.Events.Add(e);
            }
            return calendar;
        }


        public async Task<Conference> GetConferenceAsync(string id)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            return await session.Query<Conference>().Where(c => c.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<Conference>> GetConferencesAsync()
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            return await session.Query<Conference>().Where(c =>c.NumberOfDays<10 && c.EventEnd > DateOnly.FromDateTime(DateTime.Now)).ToListAsync();
        }

        public async Task<List<Conference>> GetConferencesAsync(DateOnly startdate, DateOnly enddate)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            return await session.Query<Conference>()
                .Where(c => c.NumberOfDays < 10 &&
                (c.EventStart>=startdate && c.EventStart<=enddate) 
                || 
                ( c.EventEnd>=startdate && c.EventEnd <= enddate)
                ).ToListAsync();
        }

            public async Task<List<ConferenceSearchTerm>> SearchConferencesAsync(string searchterm)
            {
                using IAsyncDocumentSession session = _store.OpenAsyncSession();

            var query = session.Query<ConferenceSearchTerm>("ConferenceBySearchTerm")
            .Search(x => x.SearchTerm, searchterm, @operator: SearchOperator.And)
            .Where(c=>c.NumberOfDays < 10);
                return await query.ToListAsync();
            }

        public async Task<List<ConferenceCountByDate>> GetConferenceCountByDate(DateOnly start, DateOnly end, string searchterm)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            var query = session.Query<ConferenceCountByDate>("ConferenceCountByDates");
            if (!string.IsNullOrEmpty(searchterm))
            {
                query = query.Search(x => x.SearchTerm, searchterm, @operator: SearchOperator.And);
            }
            return await query.Where(c => c.Date >= start && c.Date <= end).ToListAsync();
        }

        public async ValueTask<ItemsProviderResult<Conference>> GetConferencesWithOpenCfpAsync(ItemsProviderRequest request)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            var confs =
                await session.Query<Conference>()
                .Statistics(out var stats)
                .Where(c => c.CfpEndDate > DateTime.Now && c.NumberOfDays < 10)
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
                .Where(c => c.EventStart > DateOnly.FromDateTime(DateTime.Now) && c.NumberOfDays < 10)
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

using Ical.Net;
using InterfaceGenerator;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using NextTechEvent.Components;
using NextTechEvent.Data;
using NextTechEvent.Data.Index;
using Raven.Client.Documents.Session.TimeSeries;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Telerik.SvgIcons;

namespace NextTechEvent.Client.Data;

[GenerateAutoInterface(Name = "INextTechEventApi")]
public class NextTechEventClient : INextTechEventApi
{
    private readonly HttpClient _http;

    public NextTechEventClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ConferenceCounts> GetConferenceCountsAsync()
    {
        return await _http.GetFromJsonAsync<ConferenceCounts>($"/api/conferences/count")??new ConferenceCounts(0, 0, 0, 0);
    }

    public async Task<Conference> GetConferenceAsync(string id)
    {
        var result = await _http.GetFromJsonAsync<Conference>($"/api/conferences/{id}");
        return result ?? throw new KeyNotFoundException($"Conference '{id}' not found");
    }

    public async Task<Conference> GetConferenceBySessionizeIdAsync(string sessionizeId)
    {
        throw new NotImplementedException();
        //var result = await _http.GetFromJsonAsync<Conference>($"/api/conferences/by-sessionize/{Uri.EscapeDataString(sessionizeId)}");
        //return result ?? throw new KeyNotFoundException($"Conference by sessionize id '{sessionizeId}' not found");
    }

    public Task<List<ConferenceCountByDate>> GetConferenceCountByDate(DateOnly start, DateOnly end, string searchterm)
    {
        var qs = new QueryStringBuilder()
        .Add("start", start.ToString("yyyy-MM-dd"))
        .Add("end", end.ToString("yyyy-MM-dd"))
        .Add("searchterm", searchterm)
        .Build();
        return _http.GetFromJsonAsync<List<ConferenceCountByDate>>($"/api/conferences/count-by-date{qs}")!;
    }

    public Task<List<Conference>> GetConferencesAsync()
    {
        return _http.GetFromJsonAsync<List<Conference>>("/api/conferences")!;
    }

    public Task<List<Conference>> GetConferencesAsync(DateOnly startdate, DateOnly enddate)
    {
        var qs = new QueryStringBuilder()
        .Add("startdate", startdate.ToString("yyyy-MM-dd"))
        .Add("enddate", enddate.ToString("yyyy-MM-dd"))
        .Build();
        return _http.GetFromJsonAsync<List<Conference>>($"/api/conferences/range{qs}")!;
    }

    public Task<List<Conference>> GetConferencesAsync(double latitude, double longitude, double radius, DateOnly startdate, DateOnly enddate)
    {
        var qs = new QueryStringBuilder()
        .Add("latitude", latitude.ToString(System.Globalization.CultureInfo.InvariantCulture))
        .Add("longitude", longitude.ToString(System.Globalization.CultureInfo.InvariantCulture))
        .Add("radius", radius.ToString(System.Globalization.CultureInfo.InvariantCulture))
        .Add("startdate", startdate.ToString("yyyy-MM-dd"))
        .Add("enddate", enddate.ToString("yyyy-MM-dd"))
        .Build();
        return _http.GetFromJsonAsync<List<Conference>>($"/api/conferences/near{qs}")!;
    }

    public async ValueTask<ItemsProviderResult<Conference>> GetConferencesAsync(ItemsProviderRequest request)
    {
        var qs = new QueryStringBuilder()
        .Add("startIndex", request.StartIndex.ToString())
        .Add("count", request.Count.ToString())
        .Build();
        var result = await _http.GetFromJsonAsync<ItemsProviderResult<Conference>>($"/api/conferences/virtualized{qs}", request.CancellationToken);
        return result;
    }

    public Task<List<Conference>> GetConferencesByUserAsync()
    {
        return _http.GetFromJsonAsync<List<Conference>>($"/api/conferences/by-user")!;
    }

    public Task<List<ConferenceWeather>> GetConferencesByWeatherAsync(double averageTemp)
    {
        var qs = new QueryStringBuilder()
        .Add("averageTemp", averageTemp.ToString(System.Globalization.CultureInfo.InvariantCulture))
        .Build();
        return _http.GetFromJsonAsync<List<ConferenceWeather>>($"/api/conferences/by-weather{qs}")!;
    }

    public async ValueTask<ItemsProviderResult<Conference>> GetConferencesWithOpenCfpAsync(ItemsProviderRequest request)
    {
        var qs = new QueryStringBuilder()
        .Add("startIndex", request.StartIndex.ToString())
        .Add("count", request.Count.ToString())
        .Build();
        var result = await _http.GetFromJsonAsync<ItemsProviderResult<Conference>>($"/api/conferences/open-cfp{qs}", request.CancellationToken);
        return result;
    }

    public Task<Settings?> GetSettingsAsync()
    {
        return _http.GetFromJsonAsync<Settings>($"/api/settings");
    }

    public Task<Status?> GetStatusAsync(string conferenceId)
    {
        return _http.GetFromJsonAsync<Status>($"/api/statuses/{conferenceId}");
    }

    public Task<List<Status>> GetStatusesAsync()
    {
        return _http.GetFromJsonAsync<List<Status>>($"/api/statuses/by-user/")!;
    }

    public Task<Ical.Net.Calendar> GetUserCalendarAsync(string userId)
    {
        return _http.GetFromJsonAsync<Ical.Net.Calendar>($"/api/users/{Uri.EscapeDataString(userId)}/calendar")!;
    }

    public Task<TimeSeriesEntry<WeatherData>[]> GetWeatherTimeSeriesAsync(string conferenceId)
    {
        return _http.GetFromJsonAsync<TimeSeriesEntry<WeatherData>[]>($"/api/conferences/weather/{conferenceId}")!;
    }

    public async Task<Conference> SaveConferenceAsync(Conference conference)
    {
        var resp = await _http.PostAsJsonAsync("/api/conferences", conference);
        resp.EnsureSuccessStatusCode();
        var saved = await resp.Content.ReadFromJsonAsync<Conference>();
        return saved!;
    }

    public async Task<Settings> SaveSettingsAsync(Settings item)
    {
        var resp = await _http.PostAsJsonAsync("/api/settings", item);
        resp.EnsureSuccessStatusCode();
        var saved = await resp.Content.ReadFromJsonAsync<Settings>();
        return saved!;
    }

    public async Task<Status> SaveStatusAsync(Status status)
    {
        var resp = await _http.PostAsJsonAsync("/api/statuses", status);
        resp.EnsureSuccessStatusCode();
        var saved = await resp.Content.ReadFromJsonAsync<Status>();
        return saved!;
    }

    public Task<List<Conference>> SearchActiveConferencesAsync(bool hasOpenCallforPaper, string searchterm, int pagesize, int page)
    {
        var qs = new QueryStringBuilder()
        .Add("hasOpenCallforPaper", hasOpenCallforPaper.ToString())
        .Add("searchterm", searchterm)
        .Add("pagesize", pagesize.ToString())
        .Add("page", page.ToString())
        .Build();
        return _http.GetFromJsonAsync<List<Conference>>($"/api/conferences/search-active{qs}")!;
    }

    public Task<List<ConferenceSearchTerm>> SearchConferencesAsync(string searchterm)
    {
        var qs = new QueryStringBuilder()
        .Add("searchterm", searchterm)
        .Build();
        return _http.GetFromJsonAsync<List<ConferenceSearchTerm>>($"/api/conferences/search{qs}")!;
    }
    //This logic should be in the function API
    //public async Task UpdateStatusBasedOnSessionizeCalendarAsync(Settings settings)
    //{
    //    var resp = await _http.PostAsJsonAsync("/api/statuses/update-from-calendar", settings);
    //    resp.EnsureSuccessStatusCode();
    //}

    private sealed class QueryStringBuilder
    {
        private readonly StringBuilder _sb = new("?");
        private bool _first = true;
        public QueryStringBuilder Add(string name, string? value)
        {
            if (value is null) return this;
            if (!_first) _sb.Append('&');
            _first = false;
            _sb.Append(Uri.EscapeDataString(name));
            _sb.Append('=');
            _sb.Append(Uri.EscapeDataString(value));
            return this;
        }
        public string Build() => _first ? string.Empty : _sb.ToString();
    }
}

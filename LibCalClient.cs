using Flurl.Http;
using LibCalTypes;
using Newtonsoft.Json.Linq;

class LibCalClient
{
    public LibCalClient()
    {
        Client = new FlurlClient("https://pitt.libcal.com");
    }

    IFlurlClient Client { get; }

    /// <summary>
    /// Send a client id and secret to get an access token and save it to the client.
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="clientSecret"></param>
    /// <exception cref="Exception"></exception>
    public async Task Authorize(string clientId, string clientSecret)
    {
        var response = await Client.Request("/1.1/oauth/token")
            .PostUrlEncodedAsync(new
            {
                client_id = clientId,
                client_secret = clientSecret,
                grant_type = "client_credentials"
            }).ReceiveJson<JObject>();
        var accessToken = response["access_token"]?.ToString() ??
                          throw new Exception("Auth response missing access token.");
        Client.WithOAuthBearerToken(accessToken);
    }

    /// <summary>
    /// Get all calendar ids.
    /// </summary>
    /// <returns></returns>
    public async Task<List<int>> GetCalendarIds()
    {
        var response = await Client.Request("/1.1/calendars")
            .GetJsonAsync<JObject>();
        return response["calendars"].Select(cal => (int)cal["calid"]).ToList();
    }

    /// <summary>
    /// Get events from a calendar between two given dates, inclusive.
    /// </summary>
    /// <param name="calendarId"></param>
    /// <param name="fromDate"></param>
    /// <param name="toDate"></param>
    /// <returns></returns>
    public async Task<List<Event>> GetEvents(int calendarId, DateTime fromDate, DateTime toDate)
    {
        return await GetInDateInterval<EventsResponse, Event>(
            Client.Request("1.1/events").SetQueryParam("cal_id", calendarId),
            fromDate,
            toDate,
            resp => resp.Events);
    }

    /// <summary>
    /// Given a list of event ids, gets registrant info for those events.
    /// </summary>
    /// <param name="eventIds">
    /// List of event ids.
    /// Unclear from the API docs what the upper limit is on the number of ids per call.
    /// </param>
    /// <returns></returns>
    public async Task<List<RegistrationsResponse>> GetRegistrations(IEnumerable<long> eventIds)
    {
        return await Client.Request("api/1.1/events", string.Join(',', eventIds), "registrations")
            .GetJsonAsync<List<RegistrationsResponse>>();
    }

    /// <summary>
    /// Get details for an Appointments user.
    /// </summary>
    /// <returns></returns>
    public async Task<AppointmentUser> GetAppointmentUser(long userId)
    {
        return await Client.Request("/1.1/appointments")
            .SetQueryParam("user_id", userId)
            .GetJsonAsync<AppointmentUser>();
    }

    /// <summary>
    /// Gets all appointment bookings between two dates, inclusive.
    /// </summary>
    /// <param name="fromDate"></param>
    /// <param name="toDate"></param>
    /// <returns></returns>
    public Task<List<AppointmentBooking>> GetAppointmentBookings(DateTime fromDate, DateTime toDate)
    {
        return GetInDateInterval<AppointmentBooking>(Client.Request("/1.1/appointments/bookings"), fromDate, toDate);
    }

    /// <summary>
    /// Get the questions associated with an appointment
    /// </summary>
    /// <param name="questionIds"></param>
    /// <returns></returns>
    public Task<List<AppointmentQuestion>> GetAppointmentQuestions(IEnumerable<long> questionIds)
    {
        return Client.Request("/api/1.1/appointments/question", string.Join(',', questionIds))
            .GetJsonAsync<List<AppointmentQuestion>>();
    }

    /// <summary>
    /// Get the space bookings for today. T
    /// </summary>
    /// <returns></returns>
    public Task<List<SpaceBooking>> GetSpaceBookings()
    {
        return Client.Request("/1.1/space/bookings")
            .SetQueryParam("limit", 500)
            .GetJsonAsync<List<SpaceBooking>>();
    }

    /// <summary>
    /// Get all data between two given dates by splitting them into periods.
    /// Each individual call has a limit of 500, so if you are hitting that limit, try reducing the period.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="fromDate"></param>
    /// <param name="toDate"></param>
    /// <param name="mapFunc"></param>
    /// <param name="periodLengthInDays"></param>
    /// <typeparam name="TResponse"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    static async Task<List<TResult>> GetInDateInterval<TResponse, TResult>(IFlurlRequest request, DateTime fromDate,
        DateTime toDate, Func<TResponse, IEnumerable<TResult>> mapFunc, int periodLengthInDays = 30)
    {
        var results = new List<TResult>();
        Console.WriteLine($"Interval from {fromDate:d} to {toDate:d}");
        for (var currentDate = fromDate; currentDate < toDate; currentDate = currentDate.AddDays(periodLengthInDays))
        {
            var days = Math.Min((toDate - currentDate).Days, periodLengthInDays - 1);
            Console.WriteLine($"Step: {currentDate:d}, {days} days");
            var response = await request
                .SetQueryParams(new
                {
                    date = currentDate.ToString("yyyy-M-d"),
                    days,
                    limit = 500
                })
                .GetJsonAsync<TResponse>();
            results.AddRange(mapFunc(response));
        }

        return results;
    }

    /// <summary>
    /// Get all data between two given dates by splitting them into periods.
    /// Each individual call has a limit of 500, so if you are hitting that limit, try reducing the period.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="fromDate"></param>
    /// <param name="toDate"></param>
    /// <param name="periodLengthInDays"></param>
    /// <typeparam name="TResponse"></typeparam>
    /// <returns></returns>
    static Task<List<TResponse>> GetInDateInterval<TResponse>(IFlurlRequest request, DateTime fromDate, DateTime toDate,
        int periodLengthInDays = 30)
    {
        return GetInDateInterval<List<TResponse>, TResponse>(request, fromDate, toDate, x => x, periodLengthInDays);
    }
}
using System.Net.Http;
using System.Net.Http.Headers;

namespace Formula1Api
{
    public sealed class F1ApiOptions
    {
        public string ApiUrl { get; init; } = "https://f1api.dev/api";
        public int DefaultLimit { get; init; } = 30;
        public int MaxLimit { get; init; } = 100;
    }

    public sealed class F1ApiException : Exception
    {
        public System.Net.HttpStatusCode StatusCode { get; }

        public F1ApiException(System.Net.HttpStatusCode statusCode, string message)
            : base(message) => StatusCode = statusCode;
    }

    public sealed class F1Api : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly F1ApiOptions _options;
        private bool _disposed;

        public F1Api(HttpClient httpClient, F1ApiOptions? options = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options ?? new F1ApiOptions();

            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public Task<string> GetDrivers(CancellationToken ct = default)
            => Get("/drivers", ct);

        public Task<string> SearchDriver(string query, int offset = 0, int? limit = null, CancellationToken ct = default)
            => Get($"/drivers/search{BuildSearchQuery(query, offset, limit)}", ct);

        public Task<string> GetDriver(string driverId, CancellationToken ct = default)
            => Get($"/drivers/{Encode(driverId)}", ct);

        public Task<string> GetDriversByYear(int year, int offset = 0, int? limit = null, CancellationToken ct = default)
            => Get($"/{year}/drivers{BuildPagingQuery(offset, limit)}", ct);

        public Task<string> GetCurrentDrivers(CancellationToken ct = default)
            => Get("/current/drivers", ct);

        public Task<string> GetTeams(CancellationToken ct = default)
            => Get("/teams", ct);

        public Task<string> SearchTeam(string query, int offset = 0, int? limit = null, CancellationToken ct = default)
            => Get($"/teams/search{BuildSearchQuery(query, offset, limit)}", ct);

        public Task<string> GetTeam(string teamId, CancellationToken ct = default)
            => Get($"/teams/{Encode(teamId)}", ct);

        public Task<string> GetTeamsByYear(int year, int offset = 0, int? limit = null, CancellationToken ct = default)
            => Get($"/{year}/teams{BuildPagingQuery(offset, limit)}", ct);

        public Task<string> GetCurrentTeams(CancellationToken ct = default)
            => Get("/current/teams", ct);

        public Task<string> GetCircuits(CancellationToken ct = default)
            => Get("/circuits", ct);

        public Task<string> SearchCircuit(string query, int offset = 0, int? limit = null, CancellationToken ct = default)
            => Get($"/circuits/search{BuildSearchQuery(query, offset, limit)}", ct);

        public Task<string> GetCircuit(string circuitId, CancellationToken ct = default)
            => Get($"/circuits/{Encode(circuitId)}", ct);

        public Task<string> GetSeasons(CancellationToken ct = default)
            => Get("/seasons", ct);

        public Task<string> GetRace(int year, CancellationToken ct = default)
            => Get($"/{year}", ct);

        public Task<string> GetCurrentRace(CancellationToken ct = default)
            => Get("/current", ct);

        public Task<string> GetLastRace(CancellationToken ct = default)
            => Get("/current/last", ct);

        public Task<string> GetNextRace(CancellationToken ct = default)
            => Get("/current/next", ct);

        public Task<string> GetFp1Results(int year, int round, CancellationToken ct = default)
            => Get($"/{year}/{round}/fp1", ct);

        public Task<string> GetFp2Results(int year, int round, CancellationToken ct = default)
            => Get($"/{year}/{round}/fp2", ct);

        public Task<string> GetFp3Results(int year, int round, CancellationToken ct = default)
            => Get($"/{year}/{round}/fp3", ct);

        public Task<string> GetQualifyingResults(int year, int round, CancellationToken ct = default)
            => Get($"/{year}/{round}/qualy", ct);

        public Task<string> GetRaceResults(int year, int round, CancellationToken ct = default)
            => Get($"/{year}/{round}/race", ct);

        public Task<string> GetSprintQualifyingResults(int year, int round, CancellationToken ct = default)
            => Get($"/{year}/{round}/sprint/qualy", ct);

        public Task<string> GetSprintRaceResults(int year, int round, CancellationToken ct = default)
            => Get($"/{year}/{round}/sprint/race", ct);

        public Task<string> GetLastFp1Results(CancellationToken ct = default)
            => Get("/last/fp1", ct);

        public Task<string> GetLastFp2Results(CancellationToken ct = default)
            => Get("/last/fp2", ct);

        public Task<string> GetLastFp3Results(CancellationToken ct = default)
            => Get("/last/fp3", ct);

        public Task<string> GetLastQualifyingResults(CancellationToken ct = default)
            => Get("/last/qualy", ct);

        public Task<string> GetLastRaceResults(CancellationToken ct = default)
            => Get("/last/race", ct);

        public Task<string> GetLastSprintQualifyingResults(CancellationToken ct = default)
            => Get("/last/sprint/qualy", ct);

        public Task<string> GetLastSprintRaceResults(CancellationToken ct = default)
            => Get("/last/sprint/race", ct);


        public Task<string> GetDriverStandings(int year, CancellationToken ct = default)
            => Get($"/{year}/drivers-championship", ct);

        public Task<string> GetCurrentDriverStandings(CancellationToken ct = default)
            => Get("/current/drivers-championship", ct);

        public Task<string> GetConstructorStandings(int year, CancellationToken ct = default)
            => Get($"/{year}/constructor-championship", ct);

        public Task<string> GetCurrentConstructorStandings(CancellationToken ct = default)
            => Get("/current/constructor-championship", ct);

        private async Task<string> Get(string endpoint, CancellationToken ct)
        {
            var url = $"{_options.ApiUrl.TrimEnd('/')}{endpoint}";
            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                throw new F1ApiException(response.StatusCode,
                    $"Request to '{endpoint}' failed with {(int)response.StatusCode}: {body}");
            }

            return await response.Content.ReadAsStringAsync(ct);
        }

        private string BuildSearchQuery(string query, int offset, int? limit)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(query);
            var effectiveLimit = Math.Min(limit ?? _options.DefaultLimit, _options.MaxLimit);
            return $"?q={Encode(query)}&offset={offset}&limit={effectiveLimit}";
        }

        private string BuildPagingQuery(int offset, int? limit)
        {
            var effectiveLimit = Math.Min(limit ?? _options.DefaultLimit, _options.MaxLimit);
            return $"?offset={offset}&limit={effectiveLimit}";
        }

        private static string Encode(string value) => Uri.EscapeDataString(value);

        public void Dispose()
        {
            if (_disposed) return;
            _httpClient.Dispose();
            _disposed = true;
        }
    }
}

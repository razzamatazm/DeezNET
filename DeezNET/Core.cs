[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DeezNET.Tests")]

namespace DeezNET;

public class DeezerClient
{
    /// <summary>
    /// Creates a new DeezerClient.
    /// </summary>
    public DeezerClient()
    {
        _arl = "";

        _clientHandler = new() { CookieContainer = new() };
        _client = new HttpClient(_clientHandler);
        _client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
        // A missing User-Agent is a textbook abuse-detection tell, especially
        // on Akamai-fronted endpoints. Match a current Edge build so requests
        // look like a normal desktop browser session.
        _client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36 Edg/124.0.0.0");

        _gwApi = new(_client, _arl);
        _publicApi = new(_client);
        _downloader = new(_client, _gwApi, _publicApi);
    }

    /// <summary>
    /// Sets the internal ARL and refreshes the API token.
    /// Passing a null, empty, or whitespace string will remove the ARL and API token from the GWApi.
    /// </summary>
    /// <param name="arl">A Deezer account access token.</param>
    public async Task SetARL(string arl)
    {
        if (string.IsNullOrWhiteSpace(arl))
        {
            _arl = "";
            _gwApi._arl = "";
            _gwApi._apiToken = "null";
        }

        _arl = arl;
        _gwApi._arl = arl;

        // Seed the cookie jar with the ARL so every subsequent request
        // carries it automatically. Previously only deezer.getUserData
        // attached `Cookie: arl=...` manually; every other gw-light call
        // relied on the response cookie being kept in the jar by the
        // handler. If any response ever cleared the arl cookie (Set-Cookie
        // max-age=0, expiry, error path) subsequent calls silently went
        // anonymous and Deezer would mint a fresh anonymous sid, kicking
        // the user out of their session. Re-asserting it on every SetARL
        // overwrites any cleared state.
        if (!string.IsNullOrWhiteSpace(arl))
        {
            _clientHandler.CookieContainer.Add(new System.Net.Cookie("arl", arl, "/", ".deezer.com"));
        }

        await _gwApi.SetToken();
    }

    public Downloader Downloader { get => _downloader; }
    public GWApi GWApi { get => _gwApi; }
    public PublicApi PublicApi { get => _publicApi; }
    public string ActiveARL { get => _arl; }
    public string SID { get => _clientHandler.CookieContainer.GetCookies(_deezerUri).FirstOrDefault(c => c.Name == "sid")?.Value ?? ""; }

    private Downloader _downloader;
    private HttpClient _client;
    private GWApi _gwApi;
    private PublicApi _publicApi;
    private string _arl;
    private HttpClientHandler _clientHandler;
    private readonly Uri _deezerUri = new("https://deezer.com");
}
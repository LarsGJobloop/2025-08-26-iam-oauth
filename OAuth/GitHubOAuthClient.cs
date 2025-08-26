
using System.Net;
using System.Text.Json;

class GitHubOAuthClient : IOAuthClient
{
  // These are gotten from the IdP (GitHub)
  private static readonly string clientId = "Ov23liuZkpx9rTE4jQ89";
  private static readonly string clientSecret = "1e46f6893616983f5e213ec1bd66a842981a798e";
  // This is you configuring your registered OAuth app with the IdP
  private static readonly string redirectUri = "http://127.0.0.1:20000/callback/";

  // These are rather static IdP, and specific to the IdP
  private static readonly string authorizeUrl = "https://github.com/login/oauth/authorize";
  private static readonly string tokenUrl = "https://github.com/login/oauth/access_token";

  private string? userToken = null;

  public async Task<string> GetProfileInfo()
  {
    // Setup HTTP request
    string userApiUrl = "https://api.github.com/user";
    var request = new HttpRequestMessage(HttpMethod.Get, userApiUrl);
    request.Headers.Add("Authorization", "Bearer " + userToken);
    request.Headers.Add("User-Agent", "OAuthDemoApp");

    // Make request
    var http = new HttpClient();
    var response = await http.SendAsync(request);

    // Parse and return info
    string userJson = await response.Content.ReadAsStringAsync();
    return userJson;
  }

  public async Task Login()
  {
    // Construct URL
    string state = Guid.NewGuid().ToString("N");
    string scopes = "read:user";
    string url = $"{authorizeUrl}?client_id={clientId}&redirect_uri={redirectUri}&scope={scopes}&state={state}";
    // Have user navigate to URL
    Console.WriteLine($"Navigate to: {url}");

    // Setup callback listner
    using var listener = new HttpListener();
    listener.Prefixes.Add(redirectUri);
    listener.Start();
    // Await callback from browser
    var context = await listener.GetContextAsync();
    var request = context.Request;
    // If these are wrong, GitHub is at fault. We can do nothing, but crash
    string code = request.QueryString["code"]!;
    string gotState = request.QueryString["state"]!;

    // Inform the user that they can close the browser
    var response = context.Response;
    string body = "You may now close this window.";
    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(body);
    response.OutputStream.Write(buffer, 0, buffer.Length);
    response.Close();
    listener.Stop();

    // Verify that the state is correct.
    if (gotState != state) throw new Exception("State mismatch!");

    // Exchange code for an actual API token
    using var http = new HttpClient();
    var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
    tokenRequest.Headers.Add("Accept", "application/json");
    tokenRequest.Content = new FormUrlEncodedContent(
    [
      new KeyValuePair<string,string>("client_id", clientId),
      new KeyValuePair<string,string>("client_secret", clientSecret),
      new KeyValuePair<string,string>("code", code),
      new KeyValuePair<string,string>("redirect_uri", redirectUri)
    ]);
    var tokenResponse = await http.SendAsync(tokenRequest);
    string tokenJson = await tokenResponse.Content.ReadAsStringAsync();
    // WARNING! Never expose the token it's a password!
    // Console.WriteLine($"Returned token: {tokenJson}");
    // We assume that the GitHub API is stable.
    string accessToken = JsonDocument.Parse(tokenJson).RootElement.GetProperty("access_token").GetString()!;

    // Store token for later use
    userToken = accessToken;
  }
}

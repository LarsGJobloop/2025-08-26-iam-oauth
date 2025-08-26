using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

class GitHubDeviceFlow(string clientId, List<string> scopes) : IOAuthClient
{
  private readonly HttpClient http = new();
  private readonly string clientId = clientId;
  private readonly List<string> scopes = scopes;
  private string? accessToken = null;

  public async Task Login()
  {
    // Generate PKCE values
    var codeVerifier = GenerateCodeVerifier();
    var codeChallenge = GenerateCodeChallenge(codeVerifier);

    // Step 1: Request device & user codes
    var deviceResp = await http.PostAsync(
      "https://github.com/login/device/code",
      new FormUrlEncodedContent(
      [
        new KeyValuePair<string,string>("client_id", clientId),
        new KeyValuePair<string,string>("scope", string.Join(" ", scopes)),
        new KeyValuePair<string,string>("code_challenge", codeChallenge),
        new KeyValuePair<string,string>("code_challenge_method", "S256")
      ])
    );

    deviceResp.EnsureSuccessStatusCode();
    var deviceContent = await deviceResp.Content.ReadAsStringAsync();
    var deviceParsed = System.Web.HttpUtility.ParseQueryString(deviceContent);

    var userCode = deviceParsed["user_code"];
    var verificationUri = deviceParsed["verification_uri"];
    var deviceCode = deviceParsed["device_code"];
    var interval = int.Parse(deviceParsed["interval"] ?? "5");

    Console.WriteLine($"Please open {verificationUri} and enter the code: {userCode}");

    // Step 2: Poll for access token
    while (accessToken == null)
    {
      await Task.Delay(interval * 1000);

      var tokenResp = await http.PostAsync(
        "https://github.com/login/oauth/access_token",
        new FormUrlEncodedContent(
        [
          new KeyValuePair<string,string>("client_id", clientId),
          new KeyValuePair<string,string>("device_code", deviceCode!),
          new KeyValuePair<string,string>("grant_type", "urn:ietf:params:oauth:grant-type:device_code"),
          new KeyValuePair<string,string>("code_verifier", codeVerifier)
        ])
      );

      var tokenContent = await tokenResp.Content.ReadAsStringAsync();
      var tokenParsed = System.Web.HttpUtility.ParseQueryString(tokenContent);

      if (tokenParsed["error"] == "authorization_pending")
        continue;

      if (tokenParsed["access_token"] != null)
      {
        accessToken = tokenParsed["access_token"];
      }
      else if (tokenParsed["error"] != null)
      {
        throw new Exception($"OAuth error: {tokenParsed["error"]}");
      }
    }

    Console.WriteLine("Login successful!");
  }

  public async Task<string> GetProfileInfo()
  {
    if (accessToken == null)
      throw new InvalidOperationException("Not logged in");

    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    http.DefaultRequestHeaders.UserAgent.ParseAdd("DemoOAuthApp/1.0");
    var resp = await http.GetStringAsync("https://api.github.com/user");
    return resp;
  }

  /// <summary>
  /// Generic API GET wrapper with scope-aware error handling.
  /// </summary>
  public async Task<string> GetAsync(string url)
  {
    if (accessToken == null)
      throw new InvalidOperationException("Not logged in");

    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    var resp = await http.GetAsync(url);

    if (resp.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      // GitHub adds headers describing what scopes are required vs granted
      var requiredScopes = resp.Headers.Contains("X-Accepted-OAuth-Scopes")
        ? string.Join(", ", resp.Headers.GetValues("X-Accepted-OAuth-Scopes"))
        : "(unknown)";

      var grantedScopes = resp.Headers.Contains("X-OAuth-Scopes")
        ? string.Join(", ", resp.Headers.GetValues("X-OAuth-Scopes"))
        : "(none)";

      throw new Exception(
        $"403 Forbidden: This endpoint requires scopes: {requiredScopes}. " +
        $"Your token has: {grantedScopes}. " +
        $"Please update the scope list when constructing the client."
      );
    }

    resp.EnsureSuccessStatusCode();
    return await resp.Content.ReadAsStringAsync();
  }

  // Helpers for PKCE
  private static string GenerateCodeVerifier()
  {
    var bytes = new byte[32];
    RandomNumberGenerator.Fill(bytes);
    return Base64UrlEncode(bytes);
  }

  private static string GenerateCodeChallenge(string verifier)
  {
    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(verifier));
    return Base64UrlEncode(hash);
  }

  private static string Base64UrlEncode(byte[] bytes)
  {
    return Convert.ToBase64String(bytes)
      .Replace("+", "-")
      .Replace("/", "_")
      .Replace("=", "");
  }
}

Got it — you want something terse and didactic so students see the OAuth 2.0 dance (authorization code flow) without all the SDK abstraction. Here’s a minimal C# implementation of a GitHub OAuth client using only `HttpListener` and `HttpClient` — no external packages beyond the standard library:

```csharp
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

class GitHubOAuthDemo
{
    static readonly string clientId     = "<your-client-id>";
    static readonly string clientSecret = "<your-client-secret>";
    static readonly string redirectUri  = "http://localhost:5000/callback";
    static readonly string authorizeUrl = "https://github.com/login/oauth/authorize";
    static readonly string tokenUrl     = "https://github.com/login/oauth/access_token";
    static readonly string userApiUrl   = "https://api.github.com/user";

    static async Task Main()
    {
        // Step 1: Open GitHub authorize URL in browser
        string state = Guid.NewGuid().ToString("N");
        string authUrl = $"{authorizeUrl}?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&state={state}&scope=read:user";
        Console.WriteLine("Open this URL in browser:\n" + authUrl);

        // Step 2: Listen for callback
        using var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:5000/callback/");
        listener.Start();
        var context = await listener.GetContextAsync();
        var request = context.Request;
        string code  = request.QueryString["code"];
        string gotState = request.QueryString["state"];

        var response = context.Response;
        string body = "You may now close this window.";
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(body);
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.Close();
        listener.Stop();

        if (gotState != state) throw new Exception("State mismatch!");

        // Step 3: Exchange code for access token
        using var http = new HttpClient();
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
        tokenRequest.Headers.Add("Accept", "application/json");
        tokenRequest.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("client_id", clientId),
            new KeyValuePair<string,string>("client_secret", clientSecret),
            new KeyValuePair<string,string>("code", code),
            new KeyValuePair<string,string>("redirect_uri", redirectUri)
        });
        var tokenResponse = await http.SendAsync(tokenRequest);
        string tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        string accessToken = JsonDocument.Parse(tokenJson).RootElement.GetProperty("access_token").GetString();

        // Step 4: Call GitHub API
        var apiRequest = new HttpRequestMessage(HttpMethod.Get, userApiUrl);
        apiRequest.Headers.Add("Authorization", "Bearer " + accessToken);
        apiRequest.Headers.Add("User-Agent", "OAuthDemoApp");
        var apiResponse = await http.SendAsync(apiRequest);
        string userJson = await apiResponse.Content.ReadAsStringAsync();
        Console.WriteLine("GitHub user info:\n" + userJson);
    }
}
```

---

### Why this is good for teaching

* **Shows the flow explicitly**: Browser redirect → callback handler → code exchange → API call.
* **Minimal dependencies**: Only `HttpListener` and `HttpClient`.
* **Checks state**: Demonstrates CSRF protection (many demos skip this).
* **Plain JSON parsing**: No heavy models or SDKs — students see raw OAuth responses.

### Possible classroom extensions

* Break it at each step and have students *manually* exchange codes using curl before wiring it into C#.
* Add error handling (expired codes, bad client secret).
* Compare with using Octokit or ASP.NET middleware after this exercise.

---

Do you want me to also sketch a **sequence diagram** for students, showing the redirect/code/token exchange visually? That could help cement the flow before diving into code.

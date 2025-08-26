
class GitHubOAuthClient : IOAuthClient
{
  // These are gotten from the IdP (GitHub)
  private static readonly string clientId = "Ov23liuZkpx9rTE4jQ89";
  private static readonly string clientSecret = "1e46f6893616983f5e213ec1bd66a842981a798e";
  // This is you configuring your registered OAuth app with the IdP
  private static readonly string redirectUri = "http://127.0.0.1:20000/callback";

  // These are rather static IdP, and specific to the IdP
  private static readonly string authorizeUrl = "https://github.com/login/oauth/authorize";
  private static readonly string tokenUrl = "https://github.com/login/oauth/access_token";

  public Task<string> GetProfileInfo()
  {
    throw new NotImplementedException();
  }

  public Task Login()
  {
    throw new NotImplementedException();
  }
}

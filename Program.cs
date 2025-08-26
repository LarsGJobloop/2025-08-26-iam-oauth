// Can be loaded from configuration, keeping it simple here
string clientId = "Ov23liuZkpx9rTE4jQ89";

// Login use via GitHub
Console.WriteLine("Loging in via GitHub OAuth");

// IOAuthClient client = new GitHubOAuthClient();
var client = new GitHubDeviceFlow(clientId, [
  "read:user",
  "repo",
]);
await client.Login();

// Get GitHub profile info
Console.WriteLine("Printing GitHub profile info");
var profileInfo = await client.GetProfileInfo();
Console.WriteLine(profileInfo);

// Get repositories, it might be too long so write result to file
List<string> queryParameters = [
  "visibility=private",
];
var repos = await client.GetAsync($"https://api.github.com/user/repos?{string.Join('&', queryParameters)}");
File.WriteAllText("repos.json", repos);

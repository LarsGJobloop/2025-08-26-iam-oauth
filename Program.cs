// Login use via GitHub
Console.WriteLine("Loging in via GitHub OAuth");

IOAuthClient client = new GitHubOAuthClient();
await client.Login();

// Console log User Info
Console.WriteLine("Printing GitHub profile info");
var profileInfo = await client.GetProfileInfo();
Console.WriteLine(profileInfo);

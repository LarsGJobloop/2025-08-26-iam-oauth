interface IOAuthClient
{
  Task Login();
  Task<string> GetProfileInfo();
}

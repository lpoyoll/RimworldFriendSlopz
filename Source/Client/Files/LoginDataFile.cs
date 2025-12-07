using System;

namespace GameClient.Files
{
    [Serializable]
    public class LoginDataFile
    {
        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}

using System;

namespace DataMasking.Utils
{
    /// <summary>
    /// Manages user session and credentials caching
    /// </summary>
    public static class SessionManager
    {
        private static string cachedUsername;
        private static string cachedPassword;
        private static bool isLoggedIn = false;

        public static bool IsLoggedIn => isLoggedIn;
        public static string Username => cachedUsername;
        public static string Password => cachedPassword;

        /// <summary>
        /// Cache user credentials after successful login
        /// </summary>
        public static void Login(string username, string password)
        {
            cachedUsername = username;
            cachedPassword = password;
            isLoggedIn = true;
            
            Console.WriteLine($"[SessionManager] User logged in: {username}");
        }

        /// <summary>
        /// Clear cached credentials and logout
        /// </summary>
        public static void Logout()
        {
            Console.WriteLine($"[SessionManager] User logged out: {cachedUsername}");
            
            cachedUsername = null;
            cachedPassword = null;
            isLoggedIn = false;
        }

        /// <summary>
        /// Check if credentials are cached
        /// </summary>
        public static bool HasCachedCredentials()
        {
            return isLoggedIn && !string.IsNullOrEmpty(cachedUsername) && !string.IsNullOrEmpty(cachedPassword);
        }
    }
}

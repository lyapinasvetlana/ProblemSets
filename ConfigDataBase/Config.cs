using System;

namespace ProblemSets.ConfigDataBase
{
    public class Config
    {
        public static string UserId { get; private set; }
        public static string Password { get; private set; }
        public static string Host { get; private set; } 
        public static string Port { get; private set; } 
        public static string Database { get; private set; }

        public static string SetConfig()
        {
            Config.UserId = Environment.GetEnvironmentVariable("POSTGRES_USER_ID");
            Config.Password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
            Config.Host = Environment.GetEnvironmentVariable("POSTGRES_HOST");
            Config.Port = Environment.GetEnvironmentVariable("POSTGRES_PORT");
            Config.Database = Environment.GetEnvironmentVariable("POSTGRES_DB");

            return $"Server={Host};Port={Port};User Id={UserId};Password={Password};Database={Database}; Pooling=true; {Environment.GetEnvironmentVariable("SSL_ENABLE")}";
        }
        
    }
    
}
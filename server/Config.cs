using System;

class Config
{
    private static Config instance = null;
    private static readonly object padlock = new object();

    private string rabbitMqHost;
    private string eventQueueName;
    private string postgresConnectionString;

    private Config()
    {
        // Get config from ENV
        rabbitMqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST");
        eventQueueName = Environment.GetEnvironmentVariable("EVENT_QUEUE_NAME");
        string pgHost = Environment.GetEnvironmentVariable("POSTGRES_HOST");
        string pgUser = Environment.GetEnvironmentVariable("POSTGRES_USER");
        string pgPass = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
        string pgDb = Environment.GetEnvironmentVariable("POSTGRES_DB");
        postgresConnectionString = $"Host={pgHost};Username={pgUser};Password={pgPass};Database={pgDb}";
    }

    public static Config Instance
    {
        get
        {
            lock (padlock)
            {
                if (instance == null)
                {
                    instance = new Config();
                }
                return instance;
            }
        }
    }

    public string GetRabbitMqHost()
    {
        return rabbitMqHost;
    }

    public string GetEventQueueName()
    {
        return eventQueueName;
    }

    public string GetPostgresConnectionString()
    {
        return postgresConnectionString;
    }
}

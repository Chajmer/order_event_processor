using DotNetEnv;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

class Program
{
    static void Main(string[] args)
    {
        var (connection, channel, queueName) = SetupRabbitMq();

        HandleEvents(channel, queueName);

        Console.WriteLine("Client program exited.");
    }

    static (IConnection connection, IModel channel, string queueName) SetupRabbitMq()
    {
        DotNetEnv.Env.Load();

        var factory = new ConnectionFactory { HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") };

        var connection = factory.CreateConnection();
        var channel = connection.CreateModel();

        string queueName = Environment.GetEnvironmentVariable("QUEUE_NAME");
        channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);

        return (connection, channel, queueName);
    }

    static void HandleEvents(IModel channel, string queueName)
    {
        Console.WriteLine("To send OrderEvent or PaymentEvent, type '-o' or '-p'. Type 'exit' or empty string to quit.");

        var orderedSwitches = new List<string> { "-o", "-p" };
        var switchTypes = new Dictionary<string, string>
        {
            { orderedSwitches[0], "OrderEvent" },
            { orderedSwitches[1], "PaymentEvent" }
        };

        while (EventLooop(channel, queueName, orderedSwitches, switchTypes)) { }
    }

    static bool EventLooop(IModel channel, string queueName, List<string> orderedSwitches, Dictionary<string, string> switchTypes)
    {
        Console.WriteLine("Choose action...");
        var action = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(action) || action.Equals("exit", StringComparison.OrdinalIgnoreCase))
            return false;

        string json;
        IBasicProperties props;

        if (action == orderedSwitches[0])
        {
            if (!BuildOrderEvent(channel, out json, out props))
                return true;
        }
        else if (action == orderedSwitches[1])
        {
            if (!BuildPaymentEvent(channel, out json, out props))
                return true;
        }
        else
        {
            Console.WriteLine("Invalid input. Please enter '-o', '-p', or 'exit'.");
            return true;
        }

        SendMessage(channel, queueName, json, props, switchTypes[action]);
        return true;
    }

    static bool BuildOrderEvent(IModel channel, out string json, out IBasicProperties props)
    {
        Console.WriteLine("Enter OrderEvent data: <id> <product> <total> <currency>");
        var input = Console.ReadLine()?.Trim();
    
        json = null;
        props = channel.CreateBasicProperties();
        props.Headers = new Dictionary<string, object>();

        var parts = input?.Split(" ");
        if (parts?.Length != 4 || !decimal.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal price))
        {
            Console.WriteLine("Invalid order format.");
            return false;
        }

        var order = new
        {
            id = parts[0],
            product = parts[1],
            total = price,
            currency = parts[3]
        };

        json = JsonSerializer.Serialize(order);
        props.Headers["X-MsgType"] = Encoding.UTF8.GetBytes("OrderEvent");
        return true;
    }

    static bool BuildPaymentEvent(IModel channel, out string json, out IBasicProperties props)
    {
        Console.WriteLine("Enter PaymentEvent data: <orderId> <amount>");
        var input = Console.ReadLine()?.Trim();
        
        json = null;
        props = channel.CreateBasicProperties();
        props.Headers = new Dictionary<string, object>();

        var parts = input?.Split(" ");
        if (parts?.Length != 2 || !decimal.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
        {
            Console.WriteLine("Invalid payment format.");
            return false;
        }

        var payment = new
        {
            orderId = parts[0],
            amount = amount
        };

        json = JsonSerializer.Serialize(payment);
        props.Headers["X-MsgType"] = Encoding.UTF8.GetBytes("PaymentEvent");
        return true;
    }

    static void SendMessage(IModel channel, string queueName, string json, IBasicProperties props, string eventType)
    {
        var body = Encoding.UTF8.GetBytes(json);
        channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: props, body: body);
        Console.WriteLine($"Sent {eventType}: {json}");
    }
}

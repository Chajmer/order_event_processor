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
        DotNetEnv.Env.Load();

        var factory = new ConnectionFactory() { HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        string queueName = Environment.GetEnvironmentVariable("QUEUE_NAME");
        channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);

        Console.WriteLine("To send OrderEvent or PaymentEvent, type '-o' or '-p'. Type 'exit' or empty string to quit.");

        var orderedSwitches = new List<string> { "-o", "-p" };
        var switchTypes = new Dictionary<string, string>
        {
            { orderedSwitches[0], "OrderEvent" },
            { orderedSwitches[1], "PaymentEvent" }
        };

        while (true)
        {
            Console.WriteLine("Choose action...");
            var action = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(action) || action.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            IBasicProperties props = channel.CreateBasicProperties();
            props.Headers = new System.Collections.Generic.Dictionary<string, object>();

            string json = "";

            if (action == orderedSwitches[0])
            {
                Console.WriteLine("Enter OrderEvent data: <id> <product> <total> <currency>");
                var input = Console.ReadLine()?.Trim();

                var parts = input.Split(" "); // todo decompose for order and payment with better invalidation message
                if (parts.Length != 4 || !decimal.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal price))
                {
                    Console.WriteLine("Invalid order format.");
                    continue;
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
            }
            else if (action == orderedSwitches[1])
            {
                Console.WriteLine("Enter PaymentEvent data: <orderId> <amount>");
                var input = Console.ReadLine()?.Trim();

                var parts = input.Split(" ");
                if (parts.Length != 2 || !decimal.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal price))
                {
                    Console.WriteLine("Invalid payment format.");
                    continue;
                }

                var payment = new
                {
                    orderId = parts[0],
                    amount = price
                };

                json = JsonSerializer.Serialize(payment);
                props.Headers["X-MsgType"] = Encoding.UTF8.GetBytes("PaymentEvent");
            }
            else
            {
                Console.WriteLine("Invalid input. Please enter '-o', '-p', or 'exit'.");
                continue;
            }

            var body = Encoding.UTF8.GetBytes(json);

            channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: props, body: body);

            Console.WriteLine($"Sent {switchTypes[action]}: {json}");
        }

        Console.WriteLine("Client program exited.");
    }
}

using System;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

class Program
{
    static void Main(string[] args)
    {
        var factory = new ConnectionFactory() { HostName = "localhost" }; // todo make hostname scalable 

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        string queueName = "order_events";
        channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);

        Console.WriteLine("Send OrderEvent or PaymentEvent?");
        var type = Console.ReadLine();

        IBasicProperties props = channel.CreateBasicProperties();
        props.Headers = new System.Collections.Generic.Dictionary<string, object>();

        string json = "";

        if (type == "OrderEvent")
        {
            var order = new
            {
                id = "O-99",
                product = "PR-ABC",
                total = 12.34,
                currency = "USD"
            };

            json = JsonSerializer.Serialize(order);
            props.Headers["X-MsgType"] = Encoding.UTF8.GetBytes("OrderEvent");
        }
        else if (type == "PaymentEvent")
        {
            var payment = new
            {
                orderId = "O-123",
                amount = 12.34
            };

            json = JsonSerializer.Serialize(payment);
            props.Headers["X-MsgType"] = Encoding.UTF8.GetBytes("PaymentEvent");
        }
        else
        {
            Console.WriteLine("Invalid input.");
            return;
        }

        var body = Encoding.UTF8.GetBytes(json);

        channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: props, body: body);

        Console.WriteLine($"Sent {type}:\n{json}");
    }
}

using System;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Starting Order Event Processor...");

        var factory = new ConnectionFactory() { HostName = "localhost" };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        string queueName = "order_events";
        channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var msgType = ea.BasicProperties.Headers["X-MsgType"];
            var msgTypeStr = Encoding.UTF8.GetString((byte[])msgType);

            Console.WriteLine($"[x] Received message of type: {msgTypeStr}");

            try
            {
                if (msgTypeStr == "OrderEvent")
                {
                    var order = JsonSerializer.Deserialize<OrderEvent>(message, options);
                    Console.WriteLine($"    → Order: {order.Id}, Product: {order.Product}, Total: {order.Total} {order.Currency}");
                }
                else if (msgTypeStr == "PaymentEvent")
                {
                    var payment = JsonSerializer.Deserialize<PaymentEvent>(message, options);
                    Console.WriteLine($"    → Payment for Order: {payment.OrderId}, Amount: {payment.Amount}");
                }
                else
                {
                    Console.WriteLine("    → Unknown message type");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    → Error processing message: {ex.Message}");
            }
        };

        channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

        Console.WriteLine("Waiting for messages. Press [enter] to exit.");
        Console.ReadLine();
    }
}

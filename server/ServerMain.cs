using System;
using System.Text;
using System.Text.Json;
using Npgsql;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Starting Order Event Processor...");

        // Connect to database
        var config = Config.Instance;
        using var conn = new NpgsqlConnection(config.GetPostgresConnectionString());
        conn.Open();

        // RabbitMQ setup
        var (connection, channel, queueName) = SetupRabbitMq(config);
        if (connection == null) return;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Message consumer - waiting loop
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var msgTypeStr = Encoding.UTF8.GetString((byte[])ea.BasicProperties.Headers["X-MsgType"]);

            Console.WriteLine($"\n[_] Received message of type: {msgTypeStr}");

            try
            {
                if (msgTypeStr == "OrderEvent")
                {
                    var order = JsonSerializer.Deserialize<OrderEvent>(message, options);
                    Console.WriteLine($"    → Order: {order.Id}, Product: {order.Product}, Total: {order.Total} {order.Currency}");

                    using var cmd = new NpgsqlCommand("INSERT INTO orders (id, product, total, currency) VALUES (@id, @product, @total, @currency) ON CONFLICT DO NOTHING", conn);
                    cmd.Parameters.AddWithValue("id", order.Id);
                    cmd.Parameters.AddWithValue("product", order.Product);
                    cmd.Parameters.AddWithValue("total", order.Total);
                    cmd.Parameters.AddWithValue("currency", order.Currency);
                    cmd.ExecuteNonQuery();

                    CheckAndPrintPaymentStatus(conn, order.Id);
                }
                else if (msgTypeStr == "PaymentEvent")
                {
                    var payment = JsonSerializer.Deserialize<PaymentEvent>(message, options);
                    Console.WriteLine($"    → Payment for Order: {payment.OrderId}, Amount: {payment.Amount}");

                    using var cmd = new NpgsqlCommand("INSERT INTO payments (order_id, amount) VALUES (@order_id, @amount)", conn);
                    cmd.Parameters.AddWithValue("order_id", payment.OrderId);
                    cmd.Parameters.AddWithValue("amount", payment.Amount);
                    cmd.ExecuteNonQuery();

                    CheckAndPrintPaymentStatus(conn, payment.OrderId);
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
        System.Threading.Tasks.Task.Delay(-1).Wait();
    }

    static (IConnection connection, IModel channel, string queueName) SetupRabbitMq(Config config)
    {
        var factory = new ConnectionFactory { HostName = config.GetRabbitMqHost() };

        IConnection connection = null;
        for (int i = 0; i < 10; i++)
        {
            try
            {
                connection = factory.CreateConnection();
                break;
            }
            catch
            {
                Console.WriteLine("Waiting for RabbitMQ...");
                System.Threading.Thread.Sleep(3000);
            }
        }

        if (connection == null)
        {
            Console.WriteLine("Cannot connect to RabbitMQ, exiting.");
            return (null, null, null);
        }
    
        using var channel = connection.CreateModel();

        string queueName = config.GetEventQueueName();
        channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);

        return (connection, channel, queueName);
    }

    static void CheckAndPrintPaymentStatus(NpgsqlConnection conn, string orderId)
    {
        using var cmd = new NpgsqlCommand(@"
            SELECT o.id, o.product, o.total, o.currency, COALESCE(SUM(p.amount), 0)
            FROM orders o
            LEFT JOIN payments p ON p.order_id = o.id
            WHERE o.id = @id
            GROUP BY o.id", conn);
        cmd.Parameters.AddWithValue("id", orderId);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            var total = reader.GetDecimal(2);
            var paid = reader.GetDecimal(4);
            if (paid >= total)
            {
                Console.WriteLine($"✅ Order {orderId} PAID in full ({paid} / {total})");
            }
        }
    }
}

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Globalization;
using System.Text;

namespace VertiportNexus.MqTestClient.Services
{
    internal sealed class MqTestClient : IDisposable
    {
        public const string StatusRequestQueue = "q.status.req";
        public const string StatusResponseQueue = "q.status.res";
        public const string CommandRequestQueue = "q.command.req";
        public const string CommandResponseQueue = "q.command.res";

        private readonly IConnection _connection;
        private readonly IModel _channel;
        private string _statusResponseConsumerTag;
        private string _commandResponseConsumerTag;
        private bool _disposed;

        public MqTestClient(string hostName, int port)
        {
            ConnectionFactory connectionFactory = new ConnectionFactory
            {
                HostName = hostName,
                Port = port,
                UserName = "vertiport_GS",
                Password = "rmffhqjf1!",
                RequestedConnectionTimeout = TimeSpan.FromSeconds(2),
                SocketReadTimeout = TimeSpan.FromSeconds(2),
                SocketWriteTimeout = TimeSpan.FromSeconds(2),
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(2)
            };

            _connection = connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();

            DeclareQueue(StatusRequestQueue);
            DeclareQueue(StatusResponseQueue);
            DeclareQueue(CommandRequestQueue);
            DeclareQueue(CommandResponseQueue);
        }

        public void StartResponseConsumers()
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(_statusResponseConsumerTag))
                _statusResponseConsumerTag = StartConsumer(StatusResponseQueue);

            if (string.IsNullOrWhiteSpace(_commandResponseConsumerTag))
                _commandResponseConsumerTag = StartConsumer(CommandResponseQueue);

            Console.WriteLine("[RabbitMQ] Consumer Started: " + StatusResponseQueue);
            Console.WriteLine("[RabbitMQ] Consumer Started: " + CommandResponseQueue);
        }

        public void PublishGetState(string msgId, int frequency)
        {
            ThrowIfDisposed();

            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss'Z'");
            string json = "{\"msg_type\":\"get_state\",\"msg_id\":\"" + JsonEscape(msgId) + "\",\"timestamp\":\"" + timestamp + "\",\"reply_to\":\"" + StatusResponseQueue + "\",\"payload\":{\"frequency\":" + frequency + "}}";
            Publish(StatusRequestQueue, json);
        }

        public void PublishDetectOn(string msgId)
        {
            string payloadJson = "{\"frame_id\":1,\"x1\":1918,\"y1\":530,\"x2\":1920,\"y2\":550,\"class_id\":1,\"confidence\":0.95}";
            PublishCommand("detect_on", msgId, payloadJson);
        }

        public void PublishDetectCont(string msgId)
        {
            string payloadJson = "{\"frame_id\":2,\"x1\":110,\"y1\":105,\"x2\":310,\"y2\":305,\"class_id\":1,\"confidence\":0.96}";
            PublishCommand("detect_cont", msgId, payloadJson);
        }

        public void PublishDetectOff(string msgId)
        {
            PublishCommand("detect_off", msgId, "{}");
        }

        public void PublishUnsupportedCommand(string msgId)
        {
            PublishCommand("unsupported_test", msgId, "{}");
        }

        public void PublishPtzControlMode(string msgId, string mode)
        {
            string payloadJson = "{\"mode\":\"" + JsonEscape(mode) + "\"}";
            PublishCommand("ptz_move", msgId, payloadJson);
        }

        public void PublishPtzAbsolute(string msgId, float pan, float tilt)
        {
            string payloadJson = "{\"mode\":\"absolute\",\"pan\":" + pan.ToString(CultureInfo.InvariantCulture) + ",\"tilt\":" + tilt.ToString(CultureInfo.InvariantCulture) + "}";
            PublishCommand("ptz_move", msgId, payloadJson);
        }

        public void PublishPtzContinuous(string msgId, string command)
        {
            string payloadJson = "{\"mode\":\"continuous\",\"command\":\"" + JsonEscape(command) + "\"}";
            PublishCommand("ptz_move", msgId, payloadJson);
        }

        public void PublishPtzZoom(string msgId, float zoom)
        {
            string payloadJson = "{\"mode\":\"zoom\",\"zoom\":" + zoom.ToString(CultureInfo.InvariantCulture) + "}";
            PublishCommand("ptz_move", msgId, payloadJson);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                if (_channel != null && _channel.IsOpen && !string.IsNullOrWhiteSpace(_statusResponseConsumerTag))
                    _channel.BasicCancel(_statusResponseConsumerTag);

                if (_channel != null && _channel.IsOpen && !string.IsNullOrWhiteSpace(_commandResponseConsumerTag))
                    _channel.BasicCancel(_commandResponseConsumerTag);
            }
            catch
            {
            }

            try
            {
                if (_channel != null && _channel.IsOpen)
                    _channel.Close();
            }
            catch
            {
            }

            try
            {
                if (_connection != null && _connection.IsOpen)
                    _connection.Close();
            }
            catch
            {
            }

            _channel?.Dispose();
            _connection?.Dispose();
        }

        private void DeclareQueue(string queueName)
        {
            _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        }

        private string StartConsumer(string queueName)
        {
            EventingBasicConsumer consumer = new EventingBasicConsumer(_channel);
            consumer.Received += OnResponseReceived;
            return _channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
        }

        private void PublishCommand(string msgType, string msgId, string payloadJson)
        {
            ThrowIfDisposed();

            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss'Z'");
            string json = "{\"msg_type\":\"" + JsonEscape(msgType) + "\",\"msg_id\":\"" + JsonEscape(msgId) + "\",\"timestamp\":\"" + timestamp + "\",\"reply_to\":\"" + CommandResponseQueue + "\",\"payload\":" + payloadJson + "}";

            Publish(CommandRequestQueue, json);
        }

        private void Publish(string queueName, string json)
        {
            ThrowIfDisposed();

            byte[] body = Encoding.UTF8.GetBytes(json);
            _channel.BasicPublish(exchange: string.Empty, routingKey: queueName, basicProperties: null, body: body);

            Console.WriteLine();
            Console.WriteLine("[TX] " + queueName);
            Console.WriteLine(json);
        }

        private void OnResponseReceived(object sender, BasicDeliverEventArgs eventArgs)
        {
            string json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

            Console.WriteLine();
            Console.WriteLine("[RX] " + eventArgs.RoutingKey);
            Console.WriteLine(json);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MqTestClient));
        }

        private static string JsonEscape(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}

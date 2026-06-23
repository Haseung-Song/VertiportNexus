using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using VertiportNexus.Common;
using VertiportNexus.Models.Vertiport;

namespace VertiportNexus.Services.Communication.MQ
{
    /// <summary>
    /// [RabbitMQ] 메시지 수신 서비스
    /// 
    /// [q.command.req] Queue에서 [JSON] 메시지를 수신한다.
    /// </summary>
    internal class RabbitMqReceiver : IMqReceiver
    {
        #region [Fields]

        /// <summary>
        /// [RabbitMQ] 연결 정보
        /// </summary>
        private readonly ConnectionFactory _connectionFactory;

        /// <summary>
        /// [RabbitMQ] 연결 객체
        /// </summary>
        private IConnection _connection;

        /// <summary>
        /// [RabbitMQ] Channel 객체
        /// </summary>
        private IModel _channel;

        #endregion

        #region [Events]

        /// <summary>
        /// [RabbitMQ] 메시지 수신 이벤트
        /// </summary>
        public event Action<string, string> MessageReceived;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [RabbitMqReceiver] 생성자
        /// </summary>
        public RabbitMqReceiver()
        {
            _connectionFactory =
                new ConnectionFactory
                {
                    HostName = "localhost",
                    Port = 5672,
                    UserName = "guest",
                    Password = "guest"
                };

        }

        #endregion

        #region [Public Methods]

        /// <summary>
        /// [RabbitMQ] 메시지 수신 시작
        /// </summary>
        public void StartReceive()
        {
            _connection =
                _connectionFactory.CreateConnection();

            _channel =
                _connection.CreateModel();

            _channel.QueueDeclare(
                queue: CseMqQueue.CommandRequest,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            EventingBasicConsumer consumer =
                new EventingBasicConsumer(
                    _channel);

            consumer.Received +=
                OnMessageReceived;

            _channel.BasicConsume(
                queue: CseMqQueue.CommandRequest,
                autoAck: true,
                consumer: consumer);

            Console.WriteLine("[RabbitMQ][RECV] Receive Start");
            Console.WriteLine("[RabbitMQ][RECV] Queue : " + CseMqQueue.CommandRequest);
        }

        /// <summary>
        /// [RabbitMQ] 메시지 수신 중지
        /// </summary>
        public void StopReceive()
        {
            _channel?.Close();
            _connection?.Close();

            _channel?.Dispose();
            _connection?.Dispose();

            _channel =
                null;

            _connection =
                null;

            Console.WriteLine("[RabbitMQ][RECV] Receive Stop");
        }

        #endregion

        #region [Event Methods]

        /// <summary>
        /// [RabbitMQ] 메시지 수신 이벤트 처리
        /// </summary>
        private void OnMessageReceived(
            object sender,
            BasicDeliverEventArgs eventArgs)
        {
            string message =
                Encoding.UTF8.GetString(
                    eventArgs.Body.ToArray());

            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[RabbitMQ][RECV] Message Received");
            Console.WriteLine("[RabbitMQ][RECV] Queue : " + CseMqQueue.CommandRequest);
            Console.WriteLine("[RabbitMQ][RECV] JSON");
            Console.WriteLine(message);
            ConsoleLogHelper.PrintLine();

            MessageReceived?.Invoke(
                CseMqQueue.CommandRequest,
                message);
        }
        #endregion
    }

}

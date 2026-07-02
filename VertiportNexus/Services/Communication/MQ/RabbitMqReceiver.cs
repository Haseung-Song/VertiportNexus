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
    /// [q.command.req] / [q.status.req] Queue에서
    /// [JSON] 메시지를 수신한다.
    /// </summary>
    internal class RabbitMqReceiver : IMqReceiver
    {
        #region [Constants]

        /// <summary>
        /// [RabbitMQ] 기본 연결 대상 [Host]
        /// </summary>
        private const string DEFAULT_RABBITMQ_HOST_NAME = "127.0.0.1";

        /// <summary>
        /// [RabbitMQ] 기본 연결 대상 [Port]
        /// </summary>
        private const int DEFAULT_RABBITMQ_PORT = 5672;

        /// <summary>
        /// [RabbitMQ] 연결 제한 시간 [초]
        /// </summary>
        private const int RABBITMQ_CONNECTION_TIMEOUT_SECONDS = 2;

        #endregion

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

        /// <summary>
        /// [RabbitMQ] Command Request Consumer 식별값
        /// </summary>
        private string _commandRequestConsumerTag;

        /// <summary>
        /// [RabbitMQ] Status Request Consumer 식별값
        /// </summary>
        private string _statusRequestConsumerTag;

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
            : this(
                  DEFAULT_RABBITMQ_HOST_NAME,
                  DEFAULT_RABBITMQ_PORT)
        {
        }

        /// <summary>
        /// [RabbitMqReceiver] 생성자
        /// </summary>
        /// <param name="hostName">
        /// [RabbitMQ] 연결 대상 [Host]
        /// </param>
        /// <param name="port">
        /// [RabbitMQ] 연결 대상 [Port]
        /// </param>
        public RabbitMqReceiver(
            string hostName,
            int port)
        {
            _connectionFactory =
                new ConnectionFactory
                {
                    HostName =
                        hostName,

                    Port =
                        port,

                    UserName =
                        "vertiport_GS",

                    Password =
                        "rmffhqjf1!",

                    RequestedConnectionTimeout =
                        TimeSpan.FromSeconds(
                            RABBITMQ_CONNECTION_TIMEOUT_SECONDS),

                    SocketReadTimeout =
                        TimeSpan.FromSeconds(
                            RABBITMQ_CONNECTION_TIMEOUT_SECONDS),

                    SocketWriteTimeout =
                        TimeSpan.FromSeconds(
                            RABBITMQ_CONNECTION_TIMEOUT_SECONDS)
                };

        }

        #endregion

        #region [Public Methods]

        /// <summary>
        /// [RabbitMQ] 메시지 수신 시작
        /// </summary>
        public void StartReceive()
        {
            if (_channel != null &&
                _channel.IsOpen)
            {
                Console.WriteLine("[RabbitMQ][RECV] Receive Start Ignored : Already Running");
                return;
            }

            try
            {
                _connection =
                    _connectionFactory.CreateConnection();

                _channel =
                    _connection.CreateModel();

                // [Command Request] Queue 선언
                //
                // [IF-GUIS-CSE-001] ~ [IF-GUIS-CSE-004] 명령은
                // [q.command.req] Queue로 수신한다.
                _channel.QueueDeclare(
                    queue: CseMqQueue.CommandRequest,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                // [Status Request] Queue 선언
                //
                // [IF-GUIS-CSE-005] 카메라 상태 조회 요청은
                // [q.status.req] Queue로 수신한다.
                _channel.QueueDeclare(
                    queue: CseMqQueue.StatusRequest,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                EventingBasicConsumer consumer =
                    new EventingBasicConsumer(
                        _channel);

                consumer.Received +=
                    OnMessageReceived;

                // [Command Request] Queue 수신 시작
                _commandRequestConsumerTag =
                    _channel.BasicConsume(
                        queue: CseMqQueue.CommandRequest,
                        autoAck: true,
                        consumer: consumer);

                // [Status Request] Queue 수신 시작
                _statusRequestConsumerTag =
                    _channel.BasicConsume(
                        queue: CseMqQueue.StatusRequest,
                        autoAck: true,
                        consumer: consumer);

                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[RabbitMQ][RECV] Receive Start");
                Console.WriteLine("[RabbitMQ][RECV] Host : " + _connectionFactory.HostName);
                Console.WriteLine("[RabbitMQ][RECV] Port : " + _connectionFactory.Port);
                Console.WriteLine("[RabbitMQ][RECV] Queue : " + CseMqQueue.CommandRequest);
                Console.WriteLine("[RabbitMQ][RECV] Queue : " + CseMqQueue.StatusRequest);
                ConsoleLogHelper.PrintLine();
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[RabbitMQ][RECV] Receive Start Failed");
                Console.WriteLine("[RabbitMQ][RECV] RabbitMQ Server Not Connected");
                Console.WriteLine("[RabbitMQ][RECV] Error : " + ex.Message);
                ConsoleLogHelper.PrintLine();

                ReleaseResources();

                throw;
            }

        }

        /// <summary>
        /// [RabbitMQ] 메시지 수신 중지
        /// </summary>
        public void StopReceive()
        {
            try
            {
                if (_channel != null &&
                    _channel.IsOpen &&
                    !string.IsNullOrWhiteSpace(
                        _commandRequestConsumerTag))
                {
                    _channel.BasicCancel(
                        _commandRequestConsumerTag);
                }

                if (_channel != null &&
                    _channel.IsOpen &&
                    !string.IsNullOrWhiteSpace(
                        _statusRequestConsumerTag))
                {
                    _channel.BasicCancel(
                        _statusRequestConsumerTag);
                }

                if (_channel != null &&
                    _channel.IsOpen)
                {
                    _channel.Close();
                }

                if (_connection != null &&
                    _connection.IsOpen)
                {
                    _connection.Close();
                }

                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[RabbitMQ][RECV] Receive Stop");
                ConsoleLogHelper.PrintLine();
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[RabbitMQ][RECV] Receive Stop Failed");
                Console.WriteLine("[RabbitMQ][RECV] Error : " + ex.Message);
                ConsoleLogHelper.PrintLine();
            }
            finally
            {
                ReleaseResources();
            }

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
            string queueName =
                eventArgs.RoutingKey;

            string message =
                Encoding.UTF8.GetString(
                    eventArgs.Body.ToArray());

            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[RabbitMQ][RECV] Message Received");
            Console.WriteLine("[RabbitMQ][RECV] Queue : " + queueName);
            Console.WriteLine("[RabbitMQ][RECV] JSON");
            Console.WriteLine(message);
            ConsoleLogHelper.PrintLine();

            MessageReceived?.Invoke(
                queueName,
                message);
        }

        #endregion

        #region [Private Methods]

        /// <summary>
        /// [RabbitMQ] 수신 리소스 정리
        /// </summary>
        private void ReleaseResources()
        {
            _channel?.Dispose();
            _connection?.Dispose();

            _channel =
                null;

            _connection =
                null;

            _commandRequestConsumerTag =
                null;

            _statusRequestConsumerTag =
                null;
        }
        #endregion
    }

}

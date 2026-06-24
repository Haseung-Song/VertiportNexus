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
        #region [Constants]

        /// <summary>
        /// [RabbitMQ] 연결 제한 시간 [초]
        /// 
        /// RabbitMQ 서버가 실행 중이 아니어도
        /// 프로그램 실행이 오래 지연되지 않도록 제한한다.
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
        /// [RabbitMQ] Consumer 식별값
        /// 
        /// 수신 중지 시 [BasicCancel] 처리에 사용한다.
        /// </summary>
        private string _consumerTag;

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
                    UserName = "vertiport_GS",
                    Password = "rmffhqjf1!",

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

                _consumerTag =
                    _channel.BasicConsume(
                        queue: CseMqQueue.CommandRequest,
                        autoAck: true,
                        consumer: consumer);

                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[RabbitMQ][RECV] Receive Start");
                Console.WriteLine("[RabbitMQ][RECV] Queue : " + CseMqQueue.CommandRequest);
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
                    !string.IsNullOrWhiteSpace(_consumerTag))
                {
                    _channel.BasicCancel(
                        _consumerTag);
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
                Console.WriteLine("[RabbitMQ][RECV] Receive Stop Failed");
                Console.WriteLine("[RabbitMQ][RECV] Error : " + ex.Message);
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

            _consumerTag =
                null;
        }
        #endregion
    }

}

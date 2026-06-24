using RabbitMQ.Client;
using System;
using System.Text;
using VertiportNexus.Common;

namespace VertiportNexus.Services.Communication.MQ
{
    /// <summary>
    /// [RabbitMQ] 메시지 송신 서비스
    /// 
    /// 지정된 [Queue]로 [JSON] 메시지를 송신한다.
    /// </summary>
    internal class RabbitMqSender : IMqSender
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

        #endregion

        #region [Constructor]

        /// <summary>
        /// [RabbitMqSender] 생성자
        /// </summary>
        public RabbitMqSender()
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
        /// [RabbitMQ] 메시지 송신
        /// </summary>
        /// <param name="queueName">
        /// 송신 대상 [Queue] 이름
        /// </param>
        /// <param name="message">
        /// 송신 [JSON] 메시지
        /// </param>
        public void Send(
            string queueName,
            string message)
        {
            if (!CanSend(
                queueName,
                message))
            {
                return;
            }

            try
            {
                using (IConnection connection =
                       _connectionFactory.CreateConnection())
                using (IModel channel =
                       connection.CreateModel())
                {
                    channel.QueueDeclare(
                        queue: queueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);

                    byte[] body =
                        Encoding.UTF8.GetBytes(
                            message);

                    channel.BasicPublish(
                        exchange: string.Empty,
                        routingKey: queueName,
                        basicProperties: null,
                        body: body);
                }

                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[RabbitMQ][SEND] Send Start");
                Console.WriteLine("[RabbitMQ][SEND] Queue : " + queueName);
                Console.WriteLine("[RabbitMQ][SEND] Message");
                Console.WriteLine(message);
                Console.WriteLine("[RabbitMQ][SEND] Send End");
                ConsoleLogHelper.PrintLine();
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.PrintLine();
                Console.WriteLine("[RabbitMQ][SEND] Send Failed");
                Console.WriteLine("[RabbitMQ][SEND] Queue : " + queueName);
                Console.WriteLine("[RabbitMQ][SEND] RabbitMQ Server Not Connected");
                Console.WriteLine("[RabbitMQ][SEND] Error : " + ex.Message);
                ConsoleLogHelper.PrintLine();
            }

        }

        #endregion

        #region [Private Methods]

        /// <summary>
        /// [RabbitMQ] 메시지 송신 가능 여부 확인
        /// </summary>
        /// <param name="queueName">
        /// 송신 대상 [Queue] 이름
        /// </param>
        /// <param name="message">
        /// 송신 [JSON] 메시지
        /// </param>
        /// <returns>
        /// 송신 가능 여부
        /// </returns>
        private bool CanSend(
            string queueName,
            string message)
        {
            if (string.IsNullOrWhiteSpace(
                queueName))
            {
                Console.WriteLine("[RabbitMQ][SEND] Send Failed : Queue is empty");
                return false;
            }

            if (string.IsNullOrWhiteSpace(
                message))
            {
                Console.WriteLine("[RabbitMQ][SEND] Send Failed : Message is empty");
                return false;
            }

            return true;
        }
        #endregion
    }

}

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
                    UserName = "guest",
                    Password = "guest"
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
        #endregion
    }

}

using System;
using VertiportNexus.Common;

namespace VertiportNexus.Services.Communication.MQ
{
    /// <summary>
    /// [Mock] [MQ] 송신 서비스
    /// 
    /// 실제 [MQ] 서버 연결 전,
    /// 송신 대상 [Queue]와 [JSON] 메시지를 Console 로그로 확인한다.
    /// </summary>
    internal class MockMqSender : IMqSender
    {
        #region [Public Methods]

        /// <summary>
        /// [Mock] [MQ] 메시지 송신
        /// </summary>
        /// <param name="queueName">
        /// 송신 [Queue] 이름
        /// </param>
        /// <param name="message">
        /// 송신 [JSON] 메시지
        /// </param>
        public void Send(
            string queueName,
            string message)
        {
            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[MQ][MOCK] Send");
            Console.WriteLine("[MQ][MOCK] Queue : " + queueName);
            Console.WriteLine("[MQ][MOCK] Message");
            Console.WriteLine(message);
            ConsoleLogHelper.PrintLine();
        }
        #endregion
    }

}

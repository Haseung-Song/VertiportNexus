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
            if (!CanSend(
                queueName,
                message))
            {
                return;
            }

            ConsoleLogHelper.PrintLine();
            ConsoleLogHelper.WriteLine("[MQ][MOCK][SEND] Send");
            ConsoleLogHelper.WriteLine("[MQ][MOCK][SEND] Queue : " + queueName);
            ConsoleLogHelper.WriteLine("[MQ][MOCK][SEND] Message");
            ConsoleLogHelper.WriteLine(message);
            ConsoleLogHelper.PrintLine();
        }

        #endregion

        #region [Private Methods]

        /// <summary>
        /// [Mock] [MQ] 메시지 송신 가능 여부 확인
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
            if (string.IsNullOrWhiteSpace(queueName))
            {
                ConsoleLogHelper.WriteLine("[MQ][MOCK][SEND] Send Failed : Queue is empty");
                return false;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                ConsoleLogHelper.WriteLine("[MQ][MOCK][SEND] Send Failed : Message is empty");
                return false;
            }
            return true;
        }
        #endregion
    }

}

namespace VertiportNexus.Services.Communication.MQ
{
    /// <summary>
    /// [MQ] 송신 인터페이스
    /// 
    /// 실제 [RabbitMQ] / [ZeroMQ] 구현 전,
    /// [MQ] 송신 구조를 공통화하기 위한 인터페이스이다.
    /// </summary>
    internal interface IMqSender
    {
        #region [Methods]

        /// <summary>
        /// [MQ] 메시지 송신
        /// </summary>
        /// <param name="queueName">
        /// 송신 [Queue] 이름
        /// </param>
        /// <param name="message">
        /// 송신 메시지
        /// </param>
        void Send(
            string queueName,
            string message);

        #endregion
    }

}

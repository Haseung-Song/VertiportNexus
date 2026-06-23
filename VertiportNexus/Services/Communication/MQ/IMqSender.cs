namespace VertiportNexus.Services.Communication.MQ
{
    /// <summary>
    /// [MQ] 송신 서비스 인터페이스
    /// 
    /// [Mock MQ] / [RabbitMQ] 송신 서비스를
    /// 동일한 구조로 사용하기 위한 공통 인터페이스이다.
    /// </summary>
    internal interface IMqSender
    {
        /// <summary>
        /// [MQ] 메시지 송신
        /// </summary>
        /// <param name="queueName">
        /// 송신 대상 [Queue] 이름
        /// </param>
        /// <param name="message">
        /// 송신 메시지
        /// </param>
        void Send(
            string queueName,
            string message);
    }

}

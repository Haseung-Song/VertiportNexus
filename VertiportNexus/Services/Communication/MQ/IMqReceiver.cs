using System;

namespace VertiportNexus.Services.Communication.MQ
{
    /// <summary>
    /// [MQ] 수신 인터페이스
    /// 
    /// 실제 [RabbitMQ] / [ZeroMQ] 구현 전,
    /// [MQ] 수신 구조를 공통화하기 위한 인터페이스이다.
    /// </summary>
    internal interface IMqReceiver
    {
        #region [Events]

        /// <summary>
        /// [MQ] 메시지 수신 이벤트
        /// </summary>
        event Action<string, string> MessageReceived;

        #endregion

        #region [Methods]

        /// <summary>
        /// [MQ] 수신 시작
        /// </summary>
        /// <param name="queueName">
        /// 수신 [Queue] 이름
        /// </param>
        void StartReceive(
            string queueName);

        /// <summary>
        /// [MQ] 수신 중지
        /// </summary>
        void StopReceive();

        #endregion
    }

}

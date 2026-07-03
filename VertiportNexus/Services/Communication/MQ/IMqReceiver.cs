using System;

namespace VertiportNexus.Services.Communication.MQ
{
    /// <summary>
    /// [MQ] 수신 서비스 인터페이스
    /// 
    /// [MockMQ] / [RabbitMQ] 수신 서비스를
    /// 동일한 구조로 사용하기 위한 공통 인터페이스이다.
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
        /// [MQ] 메시지 수신 시작
        /// </summary>
        void StartReceive();

        /// <summary>
        /// [MQ] 메시지 수신 중지
        /// </summary>
        void StopReceive();

        #endregion
    }

}

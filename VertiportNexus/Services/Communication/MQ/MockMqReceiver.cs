using System;
using VertiportNexus.Common;
using VertiportNexus.Models.Vertiport;

namespace VertiportNexus.Services.Communication.MQ
{
    /// <summary>
    /// [Mock] [MQ] 수신 서비스
    /// 
    /// 실제 [RabbitMQ] 서버 연결 전,
    /// 테스트 [JSON] 문자열을 수동으로 수신 처리하기 위한
    /// 개발용 수신기이다.
    /// </summary>
    internal class MockMqReceiver : IMqReceiver
    {
        #region [Fields]

        /// <summary>
        /// 현재 수신 중인 [Queue] 이름
        /// 
        /// 기본값은 [CSE] 명령 요청 [Queue]이다.
        /// </summary>
        private string _queueName =
            CseMqQueue.CommandRequest;

        /// <summary>
        /// 수신 시작 여부
        /// </summary>
        private bool _isReceiving;

        #endregion

        #region [Events]

        /// <summary>
        /// [MQ] 메시지 수신 이벤트
        /// </summary>
        public event Action<string, string> MessageReceived;

        #endregion

        #region [Public Methods]

        /// <summary>
        /// [Mock] [MQ] 수신 시작
        /// 
        /// 실제 [MQ] 연결 없이
        /// 테스트 메시지 수신 가능 상태로 전환한다.
        /// </summary>
        public void StartReceive()
        {
            if (_isReceiving)
            {
                ConsoleLogHelper.WriteLine("[MQ][MOCK][RECV] Receive Start Ignored : Already Running");
                return;
            }

            _queueName =
                CseMqQueue.CommandRequest;

            _isReceiving =
                true;

            ConsoleLogHelper.PrintLine();
            ConsoleLogHelper.WriteLine("[MQ][MOCK][RECV] Receive Start");
            ConsoleLogHelper.WriteLine("[MQ][MOCK][RECV] Queue : " + _queueName);
            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [Mock] [MQ] 수신 중지
        /// </summary>
        public void StopReceive()
        {
            if (!_isReceiving)
            {
                ConsoleLogHelper.WriteLine("[MQ][MOCK][RECV] Receive Stop Ignored : Already Stopped");
                return;
            }

            _isReceiving =
                false;

            ConsoleLogHelper.PrintLine();
            ConsoleLogHelper.WriteLine("[MQ][MOCK][RECV] Receive Stop");
            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// 테스트 [JSON] 메시지 수신 처리
        /// 
        /// 실제 [MQ] 수신 대신 개발자가 직접 [JSON] 문자열을 넣어
        /// 수신 이벤트를 발생시킨다.
        /// </summary>
        /// <param name="json">
        /// 테스트 [JSON] 문자열
        /// </param>
        public void InjectMessage(
            string json)
        {
            if (!_isReceiving)
            {
                ConsoleLogHelper.WriteLine("[MQ][MOCK][RECV] Inject Failed : Receiver Not Running");
                return;
            }

            if (string.IsNullOrWhiteSpace(
                json))
            {
                ConsoleLogHelper.WriteLine("[MQ][MOCK][RECV] Inject Failed : Message is empty");
                return;
            }

            ConsoleLogHelper.PrintLine();
            ConsoleLogHelper.WriteLine("[MQ][MOCK][RECV] Message Received");
            ConsoleLogHelper.WriteLine("[MQ][MOCK][RECV] Queue : " + _queueName);
            ConsoleLogHelper.WriteLine("[MQ][MOCK][RECV] JSON");

            ConsoleLogHelper.WriteLine(json);
            ConsoleLogHelper.PrintLine();

            MessageReceived?.Invoke(
                _queueName,
                json);
        }
        #endregion
    }

}

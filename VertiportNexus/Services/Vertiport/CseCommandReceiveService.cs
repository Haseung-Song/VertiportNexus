using System;
using VertiportNexus.Common;
using VertiportNexus.Models.Vertiport;
using VertiportNexus.Services.Communication.MQ;

namespace VertiportNexus.Services.Vertiport
{
    /// <summary>
    /// [CSE] 명령 수신 서비스
    /// 
    /// [MQ]에서 수신한 [JSON] 문자열을 파싱하고,
    /// 파싱된 [CseCommandMessage]를 상위 처리부로 전달한다.
    /// </summary>
    internal class CseCommandReceiveService
    {
        #region [Fields]

        /// <summary>
        /// [MQ] 수신 서비스
        /// </summary>
        private readonly IMqReceiver _mqReceiver;

        /// <summary>
        /// [CSE] 메시지 파서
        /// </summary>
        private readonly CseMessageParser _messageParser;

        #endregion

        #region [Events]

        /// <summary>
        /// [CSE] 명령 수신 이벤트
        /// 
        /// [JSON] 파싱에 성공한 [CseCommandMessage]를
        /// 상위 명령 처리 서비스로 전달한다.
        /// </summary>
        public event Action<CseCommandMessage> CommandReceived;

        /// <summary>
        /// 마지막 수신 [JSON] 메시지 변경 이벤트
        /// 
        /// 개발 / 테스트 화면에서 마지막 수신 메시지를 표시할 때 사용한다.
        /// </summary>
        public event Action<string> LastMessageChanged;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [CseCommandReceiveService] 생성자
        /// </summary>
        /// <param name="mqReceiver">
        /// [MQ] 수신 서비스
        /// </param>
        public CseCommandReceiveService(
            IMqReceiver mqReceiver)
        {
            _mqReceiver =
                mqReceiver;

            _messageParser =
                new CseMessageParser();

            _mqReceiver.MessageReceived +=
                OnMqMessageReceived;
        }

        #endregion

        #region [Public Methods]

        /// <summary>
        /// [CSE] 명령 수신 시작
        /// 
        /// [ICD] 기준 [q.command.req] Queue를 수신 대상으로 사용한다.
        /// </summary>
        public void StartReceive()
        {
            _mqReceiver.StartReceive(
                "q.command.req");
        }

        /// <summary>
        /// [CSE] 명령 수신 중지
        /// </summary>
        public void StopReceive()
        {
            _mqReceiver.StopReceive();
        }

        #endregion

        #region [Private Methods]

        /// <summary>
        /// [MQ] 메시지 수신 처리
        /// 
        /// [MQ] 수신부에서 전달된 [JSON] 문자열을
        /// [CseCommandMessage]로 변환한다.
        /// </summary>
        /// <param name="queueName">
        /// 수신 [Queue] 이름
        /// </param>
        /// <param name="json">
        /// 수신 [JSON] 문자열
        /// </param>
        private void OnMqMessageReceived(
            string queueName,
            string json)
        {
            LastMessageChanged?.Invoke(
                json);

            Console.WriteLine("[CSE][RECV] Queue : " + queueName);
            Console.WriteLine("[CSE][RECV] JSON Parse Start");

            CseCommandMessage message =
                _messageParser.Parse(
                    json);

            if (message == null)
            {
                Console.WriteLine("[CSE][RECV] JSON Parse Failed");
                return;
            }

            Console.WriteLine("[CSE][RECV] MsgType : " + message.MsgType);
            Console.WriteLine("[CSE][RECV] MsgId : " + message.MsgId);
            Console.WriteLine("[CSE][RECV] ReplyTo : " + message.ReplyTo);

            CommandReceived?.Invoke(message);
        }

        #endregion
    }

}

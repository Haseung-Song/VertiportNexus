using System;
using System.Threading;
using System.Threading.Tasks;
using VertiportSurveillanceGUI.Common;

namespace VertiportSurveillanceGUI.Services.Communication.MQ
{
    /// <summary>
    /// [MQ] 메시지 통신 서비스
    /// 
    /// 역할:
    /// 1. [RabbitMQ] 또는 [ZeroMQ] 기반 메시지 통신 구조 확장 예정
    /// 2. [MQ] 연결 / 해제 기본 틀 제공
    /// 3. 메시지 송신 / 수신 이벤트 구조 제공
    /// 4. 향후 [Publish] / [Subscribe] 구조 적용 예정
    /// </summary>
    internal class MqService
    {
        #region [Fields]

        /// <summary>
        /// [MQ] 연결 상태
        /// </summary>
        private bool _isConnected;

        /// <summary>
        /// [MQ] 수신 루프 종료 제어용 [Token]
        /// </summary>
        private CancellationTokenSource _cts;

        /// <summary>
        /// [Log] 표시용 서비스 이름
        /// </summary>
        private readonly string _serviceName;

        #endregion

        #region [Events]

        /// <summary>
        /// [MQ] 메시지 수신 이벤트
        /// 
        /// 향후 [RabbitMQ] / [ZeroMQ]에서 수신한 메시지를
        /// [ViewModel] 또는 상위 서비스로 전달할 때 사용한다.
        /// </summary>
        public event Action<string, DateTime> MessageReceived;

        #endregion

        #region [Properties]

        /// <summary>
        /// [MQ] 연결 상태
        /// </summary>
        public bool IsConnected => _isConnected;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [MqService] 생성자
        /// </summary>
        public MqService()
            : this("MQ")
        {
        }

        /// <summary>
        /// [MqService] 생성자
        /// </summary>
        /// <param name="serviceName">
        /// [Log]에 표시할 서비스 이름
        /// </param>
        public MqService(
            string serviceName)
        {
            _serviceName = serviceName;
        }

        #endregion

        #region [Connect]

        /// <summary>
        /// [MQ] 연결
        /// 
        /// 현재 단계에서는 실제 [RabbitMQ] / [ZeroMQ] 연결 전이므로,
        /// 기본 연결 상태와 로그만 처리한다.
        /// </summary>
        public Task<bool> ConnectAsync()
        {
            if (_isConnected)
            {
                Console.WriteLine("[MQ][" + _serviceName + "] Already Connected");
                return Task.FromResult(true);
            }

            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[MQ][" + _serviceName + "] Connect Try");
            Console.WriteLine();

            _cts = new CancellationTokenSource();

            _isConnected = true;

            Console.WriteLine("[MQ][" + _serviceName + "] Connect Success");
            ConsoleLogHelper.PrintLine();

            return Task.FromResult(true);
        }

        #endregion

        #region [Send]

        /// <summary>
        /// [MQ] 메시지 송신
        /// 
        /// 향후 [RabbitMQ] [Publish] 또는 [ZeroMQ] [Send] 구조로 확장한다.
        /// </summary>
        /// <param name="message">
        /// 송신 메시지
        /// </param>
        /// <returns>
        /// 송신 성공 여부
        /// </returns>
        public Task<bool> SendAsync(
            string message)
        {
            if (!CanSend(
                message))
            {
                return Task.FromResult(false);
            }

            Console.WriteLine("[MQ][" + _serviceName + "] SEND " + message);

            return Task.FromResult(true);
        }

        /// <summary>
        /// 송신 가능 상태 확인
        /// </summary>
        private bool CanSend(
            string message)
        {
            if (!_isConnected)
            {
                Console.WriteLine("[MQ][" + _serviceName + "] Send Failed : Not Connected");
                return false;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                Console.WriteLine("[MQ][" + _serviceName + "] Send Failed : Message is empty");
                return false;
            }
            return true;
        }

        #endregion

        #region [Receive]

        /// <summary>
        /// [MQ] 수신 시작
        /// 
        /// 향후 [Subscribe] 방식 수신 루프를 구성할 때 사용한다.
        /// </summary>
        public void StartReceive()
        {
            if (!_isConnected)
            {
                Console.WriteLine("[MQ][" + _serviceName + "] Receive Start Failed : Not Connected");
                return;
            }

            Console.WriteLine("[MQ][" + _serviceName + "] Receive Start");

            /// <summary>
            /// 현재 단계에서는 실제 [MQ] 수신 구현 전이므로,
            /// 수신 루프는 추후 [RabbitMQ] / [ZeroMQ] 확정 후 구현한다.
            /// </summary>


        }

        /// <summary>
        /// [MQ] 수신 중지
        /// </summary>
        public void StopReceive()
        {
            _cts?.Cancel();

            Console.WriteLine("[MQ][" + _serviceName + "] Receive Stopped");
        }

        /// <summary>
        /// [MQ] 수신 메시지 이벤트 전달
        /// 
        /// 향후 실제 [MQ] 수신부에서 메시지를 받은 뒤 호출한다.
        /// </summary>
        /// <param name="message">
        /// 수신 메시지
        /// </param>
        private void RaiseMessageReceived(
            string message)
        {
            MessageReceived?.Invoke(
                message,
                DateTime.Now);
        }

        #endregion

        #region [Disconnect]

        /// <summary>
        /// [MQ] 연결 해제 및 리소스 정리
        /// 
        /// 수신 루프 종료 요청 후,
        /// [MQ] 관련 리소스를 정리한다.
        /// </summary>
        public void Disconnect()
        {
            if (!_isConnected)
            {
                Console.WriteLine("[MQ][" + _serviceName + "] Already Disconnected");
                return;
            }

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            _isConnected = false;

            Console.WriteLine("[MQ][" + _serviceName + "] Disconnected");
        }
        #endregion
    }

}

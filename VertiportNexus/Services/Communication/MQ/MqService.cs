using System;
using System.Threading;
using System.Threading.Tasks;
using VertiportNexus.Common;

namespace VertiportNexus.Services.Communication.MQ
{
    /// <summary>
    /// [MQ] 메시지 통신 서비스
    /// 
    /// 역할:
    /// 1. [MQ] 연결 / 해제 기본 틀 제공
    /// 2. 메시지 송신 / 수신 이벤트 구조 제공
    /// 3. 향후 [RabbitMQ] / [ZeroMQ] 공통 구조 확장 시 사용
    /// 
    /// 현재 실제 운용은 [RabbitMqReceiver] / [RabbitMqSender]에서 처리한다.
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
        /// 향후 실제 [MQ] 수신부에서 받은 메시지를
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
            _serviceName =
                string.IsNullOrWhiteSpace(serviceName)
                    ? "MQ"
                    : serviceName;
        }

        #endregion

        #region [Connect]

        /// <summary>
        /// [MQ] 연결
        /// 
        /// 현재 클래스는 공통 구조 유지용이므로
        /// 실제 서버 연결 없이 상태값과 로그만 처리한다.
        /// </summary>
        /// <returns>
        /// 연결 성공 여부
        /// </returns>
        public Task<bool> ConnectAsync()
        {
            if (_isConnected)
            {
                Console.WriteLine("[MQ][" + _serviceName + "] Connect Ignored : Already Connected");
                return Task.FromResult(true);
            }

            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[MQ][" + _serviceName + "] Connect Try");

            _cts =
                new CancellationTokenSource();

            _isConnected =
                true;

            Console.WriteLine("[MQ][" + _serviceName + "] Connect Success");
            ConsoleLogHelper.PrintLine();

            return Task.FromResult(true);
        }

        #endregion

        #region [Send]

        /// <summary>
        /// [MQ] 메시지 송신
        /// 
        /// 현재 클래스는 공통 구조 유지용이므로
        /// 실제 [Publish] 없이 로그만 출력한다.
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

            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[MQ][" + _serviceName + "] Send");
            Console.WriteLine("[MQ][" + _serviceName + "] Message");
            Console.WriteLine(message);
            ConsoleLogHelper.PrintLine();

            return Task.FromResult(true);
        }

        #endregion

        #region [Receive]

        /// <summary>
        /// [MQ] 수신 시작
        /// 
        /// 현재 클래스는 공통 구조 유지용이므로
        /// 실제 [Subscribe] 루프 없이 로그만 출력한다.
        /// </summary>
        public void StartReceive()
        {
            if (!_isConnected)
            {
                Console.WriteLine("[MQ][" + _serviceName + "] Receive Start Failed : Not Connected");
                return;
            }

            if (_cts == null ||
                _cts.IsCancellationRequested)
            {
                _cts =
                    new CancellationTokenSource();
            }

            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[MQ][" + _serviceName + "] Receive Start");
            Console.WriteLine("[MQ][" + _serviceName + "] Receive Loop Not Implemented");
            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [MQ] 수신 중지
        /// </summary>
        public void StopReceive()
        {
            if (!_isConnected)
            {
                Console.WriteLine("[MQ][" + _serviceName + "] Receive Stop Ignored : Not Connected");
                return;
            }

            _cts?.Cancel();

            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[MQ][" + _serviceName + "] Receive Stop");
            ConsoleLogHelper.PrintLine();
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
            if (string.IsNullOrWhiteSpace(
                message))
            {
                return;
            }

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
                Console.WriteLine("[MQ][" + _serviceName + "] Disconnect Ignored : Already Disconnected");
                return;
            }

            StopReceive();
            ReleaseResources();

            _isConnected =
                false;

            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[MQ][" + _serviceName + "] Disconnect Complete");
            ConsoleLogHelper.PrintLine();
        }

        #endregion

        #region [Private Methods]

        /// <summary>
        /// [MQ] 메시지 송신 가능 여부 확인
        /// </summary>
        /// <param name="message">
        /// 송신 메시지
        /// </param>
        /// <returns>
        /// 송신 가능 여부
        /// </returns>
        private bool CanSend(
            string message)
        {
            if (!_isConnected)
            {
                Console.WriteLine("[MQ][" + _serviceName + "] Send Failed : Not Connected");
                return false;
            }

            if (string.IsNullOrWhiteSpace(
                message))
            {
                Console.WriteLine("[MQ][" + _serviceName + "] Send Failed : Message is empty");
                return false;
            }
            return true;
        }

        /// <summary>
        /// [MQ] 리소스 정리
        /// </summary>
        private void ReleaseResources()
        {
            _cts?.Dispose();

            _cts =
                null;
        }
        #endregion
    }

}

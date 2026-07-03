using System;
using System.Threading.Tasks;
using VertiportNexus.Common;
using VertiportNexus.Models.ADS1000;
using VertiportNexus.Services.Communication.TCP;

namespace VertiportNexus.Services.ADS1000
{
    /// <summary>
    /// [ADS1000] 장비 [TCP] 연결 서비스
    /// 
    /// [MCB] / [SCB] [TCP] 연결과 연결 해제를 담당한다.
    /// 
    /// [MainViewModel]은 연결 요청과 화면 상태 갱신만 처리하고,
    /// 실제 [TCP] 연결 처리는 본 서비스에서 수행한다.
    /// </summary>
    internal class Ads1000ConnectionService
    {
        #region [Fields]

        /// <summary>
        /// [MCB] [TCP] 통신 서비스
        /// </summary>
        private readonly TcpClientService _mcbTcpClientService;

        /// <summary>
        /// [SCB] [TCP] 통신 서비스
        /// </summary>
        private readonly TcpClientService _scbTcpClientService;

        #endregion

        #region [Events]

        /// <summary>
        /// [MCB] / [SCB] 연결 상태 변경 이벤트
        /// 
        /// 각 장비 연결 시도 결과를
        /// 화면에 즉시 반영하기 위해 사용한다.
        /// </summary>
        public event Action<bool?, bool?> ConnectionStateChanged;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [Ads1000ConnectionService] 생성자
        /// </summary>
        public Ads1000ConnectionService(
            TcpClientService mcbTcpClientService,
            TcpClientService scbTcpClientService)
        {
            _mcbTcpClientService =
                mcbTcpClientService
                ?? throw new ArgumentNullException(
                    nameof(mcbTcpClientService));

            _scbTcpClientService =
                scbTcpClientService
                ?? throw new ArgumentNullException(
                    nameof(scbTcpClientService));
        }

        #endregion

        #region [Connection Methods]

        /// <summary>
        /// [MCB] / [SCB] 장비 [TCP] 연결
        /// </summary>
        public async Task<Ads1000ConnectionResult> ConnectAsync(
            string mcbIpAddress,
            int mcbPort,
            string scbIpAddress,
            int scbPort)
        {
            Console.WriteLine("[DEVICE] Connect Start");
            Console.WriteLine();

            Console.WriteLine("[DEVICE] MCB Target : " + mcbIpAddress + ":" + mcbPort);
            Console.WriteLine("[DEVICE] SCB Target : " + scbIpAddress + ":" + scbPort);
            ConsoleLogHelper.PrintLine();

            // [MCB] 연결 전 대기
            //
            // 화면에서 [Connecting] 상태가 즉시 지나가지 않도록
            // [MCB] 연결 시도 전 짧은 대기 시간을 둔다.
            await Task.Delay(
                500);

            bool isMcbConnected =
                await _mcbTcpClientService.ConnectAsync(
                    mcbIpAddress,
                    mcbPort);

            // [MCB] 연결 결과 즉시 알림
            //
            // [SCB] 연결 완료를 기다리지 않고,
            // [MCB] 연결 상태를 화면에 먼저 반영한다.
            ConnectionStateChanged?.Invoke(
                isMcbConnected,
                null);

            // [SCB] 연결 전 대기
            //
            // 화면에서 [Connecting] 상태가 즉시 지나가지 않도록
            // [SCB] 연결 시도 전 짧은 대기 시간을 둔다.
            await Task.Delay(
                500);

            bool isScbConnected =
                await _scbTcpClientService.ConnectAsync(
                    scbIpAddress,
                    scbPort);

            // [SCB] 연결 결과 즉시 알림
            //
            // [MCB] 연결 결과는 유지하고,
            // [SCB] 연결 상태만 추가 반영한다.
            ConnectionStateChanged?.Invoke(
                isMcbConnected,
                isScbConnected);

            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[DEVICE] Connect Result");
            Console.WriteLine();

            Console.WriteLine("[DEVICE] MCB : " + isMcbConnected);
            Console.WriteLine("[DEVICE] SCB : " + isScbConnected);
            ConsoleLogHelper.PrintLine();

            return new Ads1000ConnectionResult(
                isMcbConnected,
                isScbConnected);
        }

        /// <summary>
        /// [MCB] / [SCB] 장비 [TCP] 연결 해제
        /// </summary>
        public void Disconnect()
        {
            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[DEVICE] Disconnect Start");
            Console.WriteLine();

            _mcbTcpClientService.Disconnect();
            _scbTcpClientService.Disconnect();

            Console.WriteLine();
            Console.WriteLine("[DEVICE] Disconnect Complete");
            ConsoleLogHelper.PrintLine();
        }
        #endregion
    }

}

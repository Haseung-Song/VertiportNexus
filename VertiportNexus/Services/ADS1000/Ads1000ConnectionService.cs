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

        #region [Public Methods]

        /// <summary>
        /// [MCB] / [SCB] 장비 [TCP] 연결
        /// </summary>
        public async Task<Ads1000ConnectionResult> ConnectAsync(
            string mcbIpAddress,
            int mcbPort,
            string scbIpAddress,
            int scbPort)
        {
            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[DEVICE] Connect Start");
            Console.WriteLine("[DEVICE] MCB Target : " + mcbIpAddress + ":" + mcbPort);
            Console.WriteLine("[DEVICE] SCB Target : " + scbIpAddress + ":" + scbPort);
            ConsoleLogHelper.PrintLine();

            bool isMcbConnected =
                await _mcbTcpClientService.ConnectAsync(
                    mcbIpAddress,
                    mcbPort);

            bool isScbConnected =
                await _scbTcpClientService.ConnectAsync(
                    scbIpAddress,
                    scbPort);

            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[DEVICE] Connect Result");
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

            _mcbTcpClientService.Disconnect();
            _scbTcpClientService.Disconnect();

            Console.WriteLine("[DEVICE] Disconnect Complete");
            ConsoleLogHelper.PrintLine();
        }
        #endregion
    }

}

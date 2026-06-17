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
        /// <param name="mcbTcpClientService">
        /// [MCB] [TCP] 통신 서비스
        /// </param>
        /// 
        /// <param name="scbTcpClientService">
        /// [SCB] [TCP] 통신 서비스
        /// </param>
        public Ads1000ConnectionService(
            TcpClientService mcbTcpClientService,
            TcpClientService scbTcpClientService)
        {
            _mcbTcpClientService =
                mcbTcpClientService;

            _scbTcpClientService =
                scbTcpClientService;
        }

        #endregion

        #region [Public Methods]

        /// <summary>
        /// [MCB] / [SCB] 장비 [TCP] 연결
        /// </summary>
        /// <param name="mcbIpAddress">
        /// [MCB] 연결 대상 [IP]
        /// </param>
        /// 
        /// <param name="mcbPort">
        /// [MCB] 연결 대상 [Port]
        /// </param>
        /// 
        /// <param name="scbIpAddress">
        /// [SCB] 연결 대상 [IP]
        /// </param>
        /// 
        /// <param name="scbPort">
        /// [SCB] 연결 대상 [Port]
        /// </param>
        /// 
        /// <returns>
        /// [MCB] / [SCB] 연결 결과
        /// </returns>
        public async Task<Ads1000ConnectionResult> ConnectAsync(
            string mcbIpAddress,
            int mcbPort,
            string scbIpAddress,
            int scbPort)
        {
            Console.WriteLine("[DEVICE] Connect Start");

            bool isMcbConnected =
                await _mcbTcpClientService.ConnectAsync(
                    mcbIpAddress,
                    mcbPort);

            bool isScbConnected =
                await _scbTcpClientService.ConnectAsync(
                    scbIpAddress,
                    scbPort);

            ConsoleLogHelper.PrintLine();
            Console.WriteLine($"[DEVICE] Connect Result : MCB={isMcbConnected}, SCB={isScbConnected}");
            ConsoleLogHelper.PrintLine();

            return new Ads1000ConnectionResult(isMcbConnected, isScbConnected);
        }

        /// <summary>
        /// [MCB] / [SCB] 장비 [TCP] 연결 해제
        /// </summary>
        public void Disconnect()
        {
            Console.WriteLine("[DEVICE] Disconnect Start");

            _mcbTcpClientService.Disconnect();
            _scbTcpClientService.Disconnect();

            ConsoleLogHelper.PrintLine();
            Console.WriteLine("[DEVICE] Disconnect Complete");
            ConsoleLogHelper.PrintLine();
        }
        #endregion
    }

}

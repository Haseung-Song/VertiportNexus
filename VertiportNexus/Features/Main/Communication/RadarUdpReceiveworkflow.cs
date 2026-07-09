using System;
using System.Threading.Tasks;
using VertiportNexus.Common;
using VertiportNexus.ViewModels.Main;
using VertiportNexus.ViewModels.Main.Composition;

namespace VertiportNexus.Features.Main.Communication
{
    /// <summary>
    /// [Radar] UDP 수신 Workflow
    /// 
    /// [MainViewModel]에 직접 포함되어 있던
    /// Radar UDP 수신 시작 / 중지 처리 흐름을 분리한다.
    /// 
    /// 화면 상태값 변경은 [MainViewModel]에서 수행하고,
    /// 본 Workflow는 Radar UDP Controller 호출 및
    /// 예외 처리 결과 반환만 담당한다.
    /// </summary>
    internal sealed class RadarUdpReceiveWorkflow
    {
        #region [Fields]

        /// <summary>
        /// [MainViewModel] 구성 객체
        /// </summary>
        private readonly MainViewModelContext _context;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [Radar] UDP 수신 Workflow 생성자
        /// </summary>
        /// <param name="context">
        /// [MainViewModel] 구성 객체
        /// </param>
        internal RadarUdpReceiveWorkflow(
            MainViewModelContext context)
        {
            _context =
                context;
        }

        #endregion

        #region [Receive Methods]

        /// <summary>
        /// [Radar] UDP 수신 시작
        /// </summary>
        /// <param name="localPort">
        /// [Radar] UDP 수신 [Port]
        /// </param>
        /// <returns>
        /// Controller 처리 결과
        /// </returns>
        internal async Task<ControllerResult> StartAsync(
            int localPort)
        {
            try
            {
                return await _context
                    .RadarUdpController
                    .StartReceiveAsync(
                        localPort);
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.PrintLine();

                Console.WriteLine(
                    "[RADAR][UDP] Start Failed");

                Console.WriteLine(
                    ex.Message);

                Console.WriteLine();

                return ControllerResult.Failed(
                    "Radar UDP Receive Failed : " + ex.Message);
            }

        }

        /// <summary>
        /// [Radar] UDP 수신 중지
        /// </summary>
        /// <returns>
        /// Controller 처리 결과
        /// </returns>
        internal ControllerResult Stop()
        {
            try
            {
                return _context
                    .RadarUdpController
                    .StopReceive();
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.PrintLine();

                Console.WriteLine(
                    "[RADAR][UDP] Stop Failed");

                Console.WriteLine(
                    ex.Message);

                Console.WriteLine();

                return ControllerResult.Failed(
                    "Radar UDP Receive Stop Failed : " + ex.Message);
            }

        }
        #endregion
    }

}

using System;
using System.Threading.Tasks;
using VertiportNexus.Services.Radar;
using VertiportNexus.Common;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [Radar] UDP Controller
    /// 
    /// Radar UDP 수신 시작 / 중지 기능만 담당한다.
    /// 화면 Binding 상태 변경은 수행하지 않고,
    /// 처리 결과만 [ControllerResult]로 반환한다.
    /// </summary>
    internal sealed class RadarUdpController
    {
        #region [Service Fields]

        /// <summary>
        /// [Radar] UDP 연동 서비스
        /// </summary>
        private readonly RadarUdpService _radarUdpService;

        #endregion

        #region [Status Fields]

        /// <summary>
        /// [Radar] UDP 수신 시작 여부
        /// </summary>
        private bool _isReceiveStarted;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [Radar] UDP Controller 생성
        /// </summary>
        internal RadarUdpController(
            RadarUdpService radarUdpService)
        {
            _radarUdpService =
                radarUdpService;
        }

        #endregion

        #region [Radar UDP Methods]

        /// <summary>
        /// [Radar] UDP 수신 시작
        /// </summary>
        internal async Task<ControllerResult> StartReceiveAsync(
            int localPort)
        {
            if (_isReceiveStarted)
            {
                ConsoleLogHelper.Warning(
                    "[RADAR][UDP] Receive Ignored : Already Started");

                return ControllerResult.Failed(
                    "Radar UDP Receive Already Started");
            }

            try
            {
                _isReceiveStarted =
                    true;

                await Task.Delay(
                    500);

                _radarUdpService
                    .StartReceive(
                        localPort);

                return ControllerResult.Success(
                    "Radar UDP Receive Started");
            }
            catch (Exception ex)
            {
                _isReceiveStarted =
                    false;

                ConsoleLogHelper.Error(
                    "[RADAR][UDP] Receive Failed : " + ex.Message);

                return ControllerResult.Failed(
                    "Radar UDP Receive Failed : " + ex.Message);
            }

        }

        /// <summary>
        /// [Radar] UDP 수신 중지
        /// </summary>
        internal ControllerResult StopReceive()
        {
            if (!_isReceiveStarted)
            {
                ConsoleLogHelper.Warning(
                    "[RADAR][UDP] Stop Ignored : Not Started");

                return ControllerResult.Failed(
                    "Radar UDP Receive Not Started");
            }

            try
            {
                _radarUdpService
                    .StopReceive();

                _isReceiveStarted =
                    false;

                return ControllerResult.Success(
                    "Radar UDP Receive Stopped");
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error(
                    "[RADAR][UDP] Receive Stop Failed : " + ex.Message);

                return ControllerResult.Failed(
                    "Radar UDP Receive Stop Failed : " + ex.Message);
            }

        }
        #endregion
    }

}

using System;
using System.Threading.Tasks;
using VertiportNexus.Models.ADS1000;
using VertiportNexus.Services.ADS1000;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [Device Connection] Controller
    /// 
    /// [MCB] / [SCB] 장비 연결 / 해제 기능만 담당하고,
    /// 화면 Binding 상태 변경은 수행하지 않는다.
    /// </summary>
    internal sealed class DeviceConnectionController
    {
        #region [Service Fields]

        /// <summary>
        /// [ADS1000] 장비 연결 서비스
        /// </summary>
        private readonly Ads1000ConnectionService _connectionService;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [Device Connection] Controller 생성
        /// </summary>
        internal DeviceConnectionController(
            Ads1000ConnectionService connectionService)
        {
            _connectionService =
                connectionService;
        }

        #endregion

        #region [Connection Methods]

        /// <summary>
        /// [MCB] / [SCB] 장비 연결
        /// </summary>
        internal async Task<DeviceConnectionControllerResult> ConnectAsync(
            string mcbIpAddress,
            int mcbPort,
            string scbIpAddress,
            int scbPort)
        {
            try
            {
                Ads1000ConnectionResult result =
                    await _connectionService
                        .ConnectAsync(
                            mcbIpAddress,
                            mcbPort,
                            scbIpAddress,
                            scbPort);

                bool isSuccess =
                    result != null &&
                    result.IsMcbConnected &&
                    result.IsScbConnected;

                if (isSuccess)
                {
                    return DeviceConnectionControllerResult.Success(
                        "MCB / SCB Connected",
                        result);
                }

                return DeviceConnectionControllerResult.Failed(
                    "MCB / SCB Connect Failed");
            }
            catch (Exception ex)
            {
                return DeviceConnectionControllerResult.Failed(
                    "MCB / SCB Connect Error : " + ex.Message);
            }
        }

        /// <summary>
        /// [MCB] / [SCB] 장비 연결 해제
        /// </summary>
        internal ControllerResult Disconnect()
        {
            try
            {
                _connectionService
                    .Disconnect();

                return ControllerResult.Success(
                    "MCB / SCB Disconnected");
            }
            catch (Exception ex)
            {
                return ControllerResult.Failed(
                    "MCB / SCB Disconnect Error : " + ex.Message);
            }
        }

        #endregion
    }
}

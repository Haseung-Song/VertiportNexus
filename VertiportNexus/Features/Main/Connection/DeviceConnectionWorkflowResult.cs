using VertiportNexus.ViewModels.Main;

namespace VertiportNexus.Features.Main.Connection
{
    /// <summary>
    /// [Device Connection] Workflow 처리 결과
    /// 
    /// 장비 연결 / 연결 해제 과정에서 발생한
    /// Controller 처리 결과를 [MainViewModel]로 전달한다.
    /// </summary>
    internal sealed class DeviceConnectionWorkflowResult
    {
        #region [Properties]

        /// <summary>
        /// [Device Connection] 연결 처리 결과
        /// </summary>
        internal DeviceConnectionControllerResult ConnectResult { get; private set; }

        /// <summary>
        /// [Radar] UDP 수신 중지 처리 결과
        /// </summary>
        internal ControllerResult RadarUdpStopResult { get; private set; }

        /// <summary>
        /// [RabbitMQ] 수신 중지 처리 결과
        /// </summary>
        internal ControllerResult RabbitMqStopResult { get; private set; }

        /// <summary>
        /// [Device Connection] 연결 해제 처리 결과
        /// </summary>
        internal ControllerResult DisconnectResult { get; private set; }

        #endregion

        #region [Constructor]

        /// <summary>
        /// [Device Connection] Workflow 처리 결과 생성자
        /// </summary>
        private DeviceConnectionWorkflowResult() { }

        #endregion

        #region [Factory Methods]

        /// <summary>
        /// 장비 연결 처리 결과 생성
        /// </summary>
        /// <param name="connectResult">
        /// 장비 연결 처리 결과
        /// </param>
        /// <returns>
        /// [Device Connection] Workflow 처리 결과
        /// </returns>
        internal static DeviceConnectionWorkflowResult ConnectCompleted(
            DeviceConnectionControllerResult connectResult)
        {
            return new DeviceConnectionWorkflowResult
            {
                ConnectResult =
                    connectResult
            };

        }

        /// <summary>
        /// 장비 연결 해제 처리 결과 생성
        /// </summary>
        /// <param name="radarUdpStopResult">
        /// [Radar] UDP 수신 중지 처리 결과
        /// </param>
        /// <param name="rabbitMqStopResult">
        /// [RabbitMQ] 수신 중지 처리 결과
        /// </param>
        /// <param name="disconnectResult">
        /// 장비 연결 해제 처리 결과
        /// </param>
        /// <returns>
        /// [Device Connection] Workflow 처리 결과
        /// </returns>
        internal static DeviceConnectionWorkflowResult DisconnectCompleted(
            ControllerResult radarUdpStopResult,
            ControllerResult rabbitMqStopResult,
            ControllerResult disconnectResult)
        {
            return new DeviceConnectionWorkflowResult
            {
                RadarUdpStopResult =
                    radarUdpStopResult,

                RabbitMqStopResult =
                    rabbitMqStopResult,

                DisconnectResult =
                    disconnectResult
            };

        }
        #endregion
    }

}

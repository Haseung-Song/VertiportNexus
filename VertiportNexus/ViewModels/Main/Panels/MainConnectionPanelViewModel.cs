using VertiportNexus.ViewModels.Main;

namespace VertiportNexus.ViewModels.Main.Panels
{
    /// <summary>
    /// [Main] 장비 / 통신 연결 상태
    /// </summary>
    internal sealed class MainConnectionPanelViewModel
    {
        #region [Busy State Properties]

        /// <summary>
        /// 장비 연결 진행 여부
        /// </summary>
        internal bool IsDeviceConnecting { get; set; }

        /// <summary>
        /// [Home Position] 이동 진행 여부
        /// </summary>
        internal bool IsHomePositionMoving { get; set; }

        /// <summary>
        /// 장비 연결 해제 진행 여부
        /// </summary>
        internal bool IsDeviceDisconnecting { get; set; }

        #endregion

        #region [Connection State Properties]

        /// <summary>
        /// [MCB] TCP 연결 상태
        /// </summary>
        internal MainViewModel.ConnectionState McbConnectionState { get; set; } =
            MainViewModel.ConnectionState.Disconnected;

        /// <summary>
        /// [SCB] TCP 연결 상태
        /// </summary>
        internal MainViewModel.ConnectionState ScbConnectionState { get; set; } =
            MainViewModel.ConnectionState.Disconnected;

        /// <summary>
        /// [Radar] UDP 수신 상태
        /// </summary>
        internal MainViewModel.ConnectionState RadarUdpConnectionState { get; set; } =
            MainViewModel.ConnectionState.Disconnected;

        /// <summary>
        /// [RabbitMQ] 수신 상태
        /// </summary>
        internal MainViewModel.ConnectionState RabbitMqConnectionState { get; set; } =
            MainViewModel.ConnectionState.Disconnected;

        #endregion
    }
}

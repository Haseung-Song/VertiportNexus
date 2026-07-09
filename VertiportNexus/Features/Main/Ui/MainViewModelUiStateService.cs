using System.Windows.Media;
using VertiportNexus.ViewModels.Main;

namespace VertiportNexus.Features.Main.Ui
{
    /// <summary>
    /// [MainViewModel] UI 상태 조회 서비스
    /// 
    /// 연결 상태 표시 문자열 / 표시 색상 / 버튼 활성 조건을 계산한다.
    /// [MainViewModel]은 현재 상태값만 전달하고,
    /// 반복되는 UI 조건 계산은 본 서비스에서 담당한다.
    /// </summary>
    internal sealed class MainViewModelUiStateService
    {
        #region [Status Display Methods]

        /// <summary>
        /// 연결 상태 표시 문자열 조회
        /// </summary>
        /// <param name="connectionState">
        /// 연결 상태
        /// </param>
        /// <returns>
        /// 연결 상태 표시 문자열
        /// </returns>
        internal string GetConnectionStatusText(
            MainViewModel.ConnectionState connectionState)
        {
            switch (connectionState)
            {
                case MainViewModel.ConnectionState.Connected:
                    return "● Connected";

                case MainViewModel.ConnectionState.Connecting:
                    return "● Connecting";

                default:
                    return "● Disconnected";
            }

        }

        /// <summary>
        /// 연결 상태 표시 색상 조회
        /// </summary>
        /// <param name="connectionState">
        /// 연결 상태
        /// </param>
        /// <returns>
        /// 연결 상태 표시 색상
        /// </returns>
        internal Brush GetConnectionStatusBrush(
            MainViewModel.ConnectionState connectionState)
        {
            switch (connectionState)
            {
                case MainViewModel.ConnectionState.Connected:
                    return Brushes.LimeGreen;

                case MainViewModel.ConnectionState.Connecting:
                    return Brushes.Gold;

                default:
                    return Brushes.IndianRed;
            }

        }

        #endregion

        #region [Device State Methods]

        /// <summary>
        /// 장비 제어 가능 여부 조회
        /// </summary>
        internal bool IsDeviceControlEnabled(
            MainViewModel.ConnectionState mcbConnectionState,
            MainViewModel.ConnectionState scbConnectionState,
            bool isDeviceConnecting,
            bool isDeviceDisconnecting,
            bool isHomePositionMoving)
        {
            return IsAnyDeviceConnected(
                       mcbConnectionState,
                       scbConnectionState) &&
                   !isDeviceConnecting &&
                   !isDeviceDisconnecting &&
                   !isHomePositionMoving;
        }

        /// <summary>
        /// 장비 통신 설정 입력 가능 여부 조회
        /// </summary>
        internal bool IsDeviceConnectionSettingEnabled(
            MainViewModel.ConnectionState mcbConnectionState,
            MainViewModel.ConnectionState scbConnectionState,
            bool isDeviceConnecting,
            bool isDeviceDisconnecting,
            bool isHomePositionMoving)
        {
            return mcbConnectionState == MainViewModel.ConnectionState.Disconnected &&
                   scbConnectionState == MainViewModel.ConnectionState.Disconnected &&
                   !isDeviceConnecting &&
                   !isDeviceDisconnecting &&
                   !isHomePositionMoving;
        }

        /// <summary>
        /// 장비 제어 탭 활성 여부 조회
        /// </summary>
        internal bool IsDeviceControlTabEnabled(
            bool isHomePositionMoving)
        {
            return !isHomePositionMoving;
        }

        /// <summary>
        /// Pan / Tilt 속도 설정 가능 여부 조회
        /// </summary>
        internal bool IsPanTiltSpeedEnabled(
            MainViewModel.ConnectionState mcbConnectionState,
            bool isHomePositionMoving)
        {
            return mcbConnectionState == MainViewModel.ConnectionState.Connected &&
                   !isHomePositionMoving;
        }

        /// <summary>
        /// 장비 연결 버튼 활성 여부 조회
        /// </summary>
        internal bool IsDeviceConnectButtonEnabled(
            bool isDeviceConnecting,
            bool isDeviceDisconnecting,
            bool isHomePositionMoving)
        {
            return !isDeviceConnecting &&
                   !isDeviceDisconnecting &&
                   !isHomePositionMoving;
        }

        /// <summary>
        /// 장비 연결 해제 버튼 활성 여부 조회
        /// </summary>
        internal bool IsDeviceDisconnectButtonEnabled(
            MainViewModel.ConnectionState mcbConnectionState,
            MainViewModel.ConnectionState scbConnectionState,
            bool isDeviceDisconnecting,
            bool isHomePositionMoving)
        {
            return IsAnyDeviceConnected(
                       mcbConnectionState,
                       scbConnectionState) &&
                   !isDeviceDisconnecting &&
                   !isHomePositionMoving;
        }

        #endregion

        #region [Radar UDP State Methods]

        /// <summary>
        /// Radar UDP 수신 시작 버튼 활성 여부 조회
        /// </summary>
        internal bool IsRadarUdpStartButtonEnabled(
            MainViewModel.ConnectionState mcbConnectionState,
            MainViewModel.ConnectionState scbConnectionState,
            MainViewModel.ConnectionState radarUdpConnectionState)
        {
            return IsDeviceFullyConnected(
                       mcbConnectionState,
                       scbConnectionState) &&
                   radarUdpConnectionState != MainViewModel.ConnectionState.Connected;
        }

        /// <summary>
        /// Radar UDP 수신 중지 버튼 활성 여부 조회
        /// </summary>
        internal bool IsRadarUdpStopButtonEnabled(
            MainViewModel.ConnectionState mcbConnectionState,
            MainViewModel.ConnectionState scbConnectionState,
            MainViewModel.ConnectionState radarUdpConnectionState)
        {
            return IsDeviceFullyConnected(
                       mcbConnectionState,
                       scbConnectionState) &&
                   radarUdpConnectionState == MainViewModel.ConnectionState.Connected;
        }

        /// <summary>
        /// Radar UDP 통신 설정 입력 가능 여부 조회
        /// </summary>
        internal bool IsRadarUdpConnectionSettingEnabled(
            MainViewModel.ConnectionState mcbConnectionState,
            MainViewModel.ConnectionState scbConnectionState,
            MainViewModel.ConnectionState radarUdpConnectionState)
        {
            return IsDeviceFullyConnected(
                       mcbConnectionState,
                       scbConnectionState) &&
                   radarUdpConnectionState == MainViewModel.ConnectionState.Disconnected;
        }

        #endregion

        #region [RabbitMQ State Methods]

        /// <summary>
        /// RabbitMQ 수신 시작 버튼 활성 여부 조회
        /// </summary>
        internal bool IsRabbitMqStartButtonEnabled(
            MainViewModel.ConnectionState mcbConnectionState,
            MainViewModel.ConnectionState scbConnectionState,
            MainViewModel.ConnectionState rabbitMqConnectionState)
        {
            return IsDeviceFullyConnected(
                       mcbConnectionState,
                       scbConnectionState) &&
                   rabbitMqConnectionState != MainViewModel.ConnectionState.Connected &&
                   rabbitMqConnectionState != MainViewModel.ConnectionState.Connecting;
        }

        /// <summary>
        /// RabbitMQ 수신 중지 버튼 활성 여부 조회
        /// </summary>
        internal bool IsRabbitMqStopButtonEnabled(
            MainViewModel.ConnectionState mcbConnectionState,
            MainViewModel.ConnectionState scbConnectionState,
            MainViewModel.ConnectionState rabbitMqConnectionState)
        {
            return IsDeviceFullyConnected(
                       mcbConnectionState,
                       scbConnectionState) &&
                   rabbitMqConnectionState == MainViewModel.ConnectionState.Connected;
        }

        /// <summary>
        /// RabbitMQ 통신 설정 입력 가능 여부 조회
        /// </summary>
        internal bool IsRabbitMqConnectionSettingEnabled(
            MainViewModel.ConnectionState mcbConnectionState,
            MainViewModel.ConnectionState scbConnectionState,
            MainViewModel.ConnectionState rabbitMqConnectionState)
        {
            return IsDeviceFullyConnected(
                       mcbConnectionState,
                       scbConnectionState) &&
                   rabbitMqConnectionState == MainViewModel.ConnectionState.Disconnected;
        }

        #endregion

        #region [Private Methods]

        /// <summary>
        /// 장비 전체 연결 여부 조회
        /// </summary>
        private bool IsDeviceFullyConnected(
            MainViewModel.ConnectionState mcbConnectionState,
            MainViewModel.ConnectionState scbConnectionState)
        {
            return mcbConnectionState == MainViewModel.ConnectionState.Connected &&
                   scbConnectionState == MainViewModel.ConnectionState.Connected;
        }

        /// <summary>
        /// 장비 일부 연결 여부 조회
        /// </summary>
        private bool IsAnyDeviceConnected(
            MainViewModel.ConnectionState mcbConnectionState,
            MainViewModel.ConnectionState scbConnectionState)
        {
            return mcbConnectionState == MainViewModel.ConnectionState.Connected ||
                   scbConnectionState == MainViewModel.ConnectionState.Connected;
        }
        #endregion
    }

}

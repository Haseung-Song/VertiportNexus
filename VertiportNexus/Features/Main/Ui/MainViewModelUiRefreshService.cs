using System;

namespace VertiportNexus.Features.Main.Ui
{
    /// <summary>
    /// [MainViewModel] UI 갱신 서비스
    /// 
    /// 연결 상태 / 통신 상태 / Home Position 상태 변경 시
    /// 반복적으로 호출되는 PropertyChanged 묶음을 담당한다.
    /// 
    /// [MainViewModel]은 상태값 저장과 화면 값 반영만 수행하고,
    /// 관련 Binding Property 갱신 목록은 본 서비스에 위임한다.
    /// </summary>
    internal sealed class MainViewModelUiRefreshService
    {
        #region [Home Position Refresh Methods]

        /// <summary>
        /// [Home Position] 이동 상태 변경 관련 UI 갱신
        /// </summary>
        /// <param name="notifyPropertyChanged">
        /// PropertyChanged 호출 함수
        /// </param>
        internal void NotifyHomePositionMovingStateChanged(
            Action<string> notifyPropertyChanged)
        {
            if (notifyPropertyChanged == null)
            {
                return;
            }

            // [장비 연결 버튼] 활성화 상태 갱신
            notifyPropertyChanged(
                "IsDeviceConnectButtonEnabled");

            // [장비 연결 해제 버튼] 활성화 상태 갱신
            notifyPropertyChanged(
                "IsDeviceDisconnectButtonEnabled");

            // [장비 통신 설정] 입력 가능 상태 갱신
            notifyPropertyChanged(
                "IsDeviceConnectionSettingEnabled");

            // [장비 제어] 활성화 상태 갱신
            notifyPropertyChanged(
                "IsDeviceControlEnabled");

            // [장비 제어 탭] 활성화 상태 갱신
            notifyPropertyChanged(
                "IsDeviceControlTabEnabled");

            // [Pan / Tilt Speed] 설정 가능 상태 갱신
            notifyPropertyChanged(
                "IsPanTiltSpeedEnabled");
        }

        #endregion

        #region [Device Connection Refresh Methods]

        /// <summary>
        /// [MCB] / [SCB] 연결 상태 변경 관련 UI 갱신
        /// </summary>
        /// <param name="notifyPropertyChanged">
        /// PropertyChanged 호출 함수
        /// </param>
        internal void NotifyDeviceConnectionStateChanged(
            Action<string> notifyPropertyChanged)
        {
            if (notifyPropertyChanged == null)
            {
                return;
            }

            // [MCB] 연결 상태 UI 갱신
            notifyPropertyChanged(
                "McbConnectionStatusText");
            notifyPropertyChanged(
                "McbConnectionStatusBrush");

            // [SCB] 연결 상태 UI 갱신
            notifyPropertyChanged(
                "ScbConnectionStatusText");
            notifyPropertyChanged(
                "ScbConnectionStatusBrush");

            // [장비 제어] 활성화 상태 갱신
            notifyPropertyChanged(
                "IsDeviceControlEnabled");

            // [장비 통신 설정] 입력 가능 상태 갱신
            notifyPropertyChanged(
                "IsDeviceConnectionSettingEnabled");

            // [장비 제어 탭] 활성화 상태 갱신
            notifyPropertyChanged(
                "IsDeviceControlTabEnabled");

            // [Pan / Tilt Speed] 설정 가능 상태 갱신
            notifyPropertyChanged(
                "IsPanTiltSpeedEnabled");

            // [장비 연결] 버튼 활성화 상태 갱신
            notifyPropertyChanged(
                "IsDeviceConnectButtonEnabled");

            // [장비 연결 해제 버튼] 활성화 상태 갱신
            notifyPropertyChanged(
                "IsDeviceDisconnectButtonEnabled");

            // [Radar UDP] 연결 관련 UI 갱신
            notifyPropertyChanged(
                "IsRadarUdpStartButtonEnabled");
            notifyPropertyChanged(
                "IsRadarUdpStopButtonEnabled");
            notifyPropertyChanged(
                "IsRadarUdpConnectionSettingEnabled");

            // [RabbitMQ] 연결 관련 UI 갱신
            notifyPropertyChanged(
                "IsRabbitMqStartButtonEnabled");
            notifyPropertyChanged(
                "IsRabbitMqStopButtonEnabled");
            notifyPropertyChanged(
                "IsRabbitMqConnectionSettingEnabled");
        }

        /// <summary>
        /// [장비 연결 / 해제 진행 상태] 관련 UI 갱신
        /// </summary>
        /// <param name="notifyPropertyChanged">
        /// PropertyChanged 호출 함수
        /// </param>
        internal void NotifyDeviceConnectionBusyStateChanged(
            Action<string> notifyPropertyChanged)
        {
            if (notifyPropertyChanged == null)
            {
                return;
            }

            // [장비 연결] 버튼 활성화 상태 갱신
            notifyPropertyChanged(
                "IsDeviceConnectButtonEnabled");

            // [장비 연결 해제 버튼] 활성화 상태 갱신
            notifyPropertyChanged(
                "IsDeviceDisconnectButtonEnabled");

            // [장비 통신 설정] 입력 가능 상태 갱신
            notifyPropertyChanged(
                "IsDeviceConnectionSettingEnabled");

            // [Radar UDP 통신 설정] 입력 가능 상태 갱신
            notifyPropertyChanged(
                "IsRadarUdpConnectionSettingEnabled");

            // [RabbitMQ 통신 설정] 입력 가능 상태 갱신
            notifyPropertyChanged(
                "IsRabbitMqConnectionSettingEnabled");
        }

        #endregion

        #region [Communication Refresh Methods]

        /// <summary>
        /// [RabbitMQ] 연결 상태 변경 관련 UI 갱신
        /// </summary>
        /// <param name="notifyPropertyChanged">
        /// PropertyChanged 호출 함수
        /// </param>
        internal void NotifyRabbitMqConnectionStateChanged(
            Action<string> notifyPropertyChanged)
        {
            if (notifyPropertyChanged == null)
            {
                return;
            }

            // [RabbitMQ] 연결 상태 UI 갱신
            notifyPropertyChanged(
                "RabbitMqConnectionStatusText");
            notifyPropertyChanged(
                "RabbitMqConnectionStatusBrush");

            // [RabbitMQ] 버튼 / 설정 입력 상태 갱신
            notifyPropertyChanged(
                "IsRabbitMqStartButtonEnabled");
            notifyPropertyChanged(
                "IsRabbitMqStopButtonEnabled");
            notifyPropertyChanged(
                "IsRabbitMqConnectionSettingEnabled");
        }

        /// <summary>
        /// [Radar UDP] 연결 상태 변경 관련 UI 갱신
        /// </summary>
        /// <param name="notifyPropertyChanged">
        /// PropertyChanged 호출 함수
        /// </param>
        internal void NotifyRadarUdpConnectionStateChanged(
            Action<string> notifyPropertyChanged)
        {
            if (notifyPropertyChanged == null)
            {
                return;
            }

            // [Radar UDP] 연결 상태 UI 갱신
            notifyPropertyChanged(
                "RadarUdpConnectionStatusText");
            notifyPropertyChanged(
                "RadarUdpConnectionStatusBrush");

            // [Radar UDP] 버튼 / 설정 입력 상태 갱신
            notifyPropertyChanged(
                "IsRadarUdpStartButtonEnabled");
            notifyPropertyChanged(
                "IsRadarUdpStopButtonEnabled");
            notifyPropertyChanged(
                "IsRadarUdpConnectionSettingEnabled");
        }
        #endregion
    }

}

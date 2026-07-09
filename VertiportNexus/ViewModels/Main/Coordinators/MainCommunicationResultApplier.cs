using System;
using VertiportNexus.Features.Main.Ui;
using VertiportNexus.ViewModels.Main.Panels;

namespace VertiportNexus.ViewModels.Main.Coordinators
{
    /// <summary>
    /// [Main Communication] 처리 결과 반영 객체
    ///
    /// RabbitMQ / Radar UDP 처리 결과를 연결 상태와 화면 상태에 반영한다.
    /// </summary>
    internal sealed class MainCommunicationResultApplier
    {
        #region [Fields]

        /// <summary>
        /// Main 화면 상태 표시 객체
        /// </summary>
        private readonly MainStatusPanelViewModel _statusPanel;

        /// <summary>
        /// 장비 / 통신 연결 상태 객체
        /// </summary>
        private readonly MainConnectionPanelViewModel _connectionPanel;

        /// <summary>
        /// UI 갱신 서비스
        /// </summary>
        private readonly MainViewModelUiRefreshService _uiRefreshService;

        /// <summary>
        /// Binding Property 갱신 알림 처리기
        /// </summary>
        private readonly Action<string> _notifyPropertyChanged;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [Main Communication] 처리 결과 반영 객체 생성자
        /// </summary>
        internal MainCommunicationResultApplier(
            MainStatusPanelViewModel statusPanel,
            MainConnectionPanelViewModel connectionPanel,
            MainViewModelUiRefreshService uiRefreshService,
            Action<string> notifyPropertyChanged)
        {
            _statusPanel =
                statusPanel;

            _connectionPanel =
                connectionPanel;

            _uiRefreshService =
                uiRefreshService;

            _notifyPropertyChanged =
                notifyPropertyChanged;
        }

        #endregion

        #region [Apply Methods]

        /// <summary>
        /// Communication 처리 결과 반영
        /// </summary>
        internal void Apply(
            MainCommunicationResult result)
        {
            if (result == null)
            {
                return;
            }

            if (result.RabbitMqConnectionState.HasValue)
            {
                _connectionPanel.RabbitMqConnectionState =
                    result.RabbitMqConnectionState.Value;

                _uiRefreshService
                    .NotifyRabbitMqConnectionStateChanged(
                        Notify);
            }

            if (result.RadarUdpConnectionState.HasValue)
            {
                _connectionPanel.RadarUdpConnectionState =
                    result.RadarUdpConnectionState.Value;

                _uiRefreshService
                    .NotifyRadarUdpConnectionStateChanged(
                        Notify);
            }

            if (!string.IsNullOrWhiteSpace(result.MqStatusText) &&
                _statusPanel.SetMqStatusText(result.MqStatusText))
            {
                Notify(
                    "MqStatusText");
            }

            if (!string.IsNullOrWhiteSpace(result.MainStatusText) &&
                _statusPanel.SetMainStatusText(result.MainStatusText))
            {
                Notify(
                    "MainStatusText");
            }

        }

        #endregion

        #region [Utility Methods]

        /// <summary>
        /// Binding Property 갱신 알림
        /// </summary>
        private void Notify(
            string propertyName)
        {
            _notifyPropertyChanged?.Invoke(
                propertyName);
        }
        #endregion
    }

}

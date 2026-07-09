using System.Threading.Tasks;
using VertiportNexus.ViewModels.Main.Panels;

namespace VertiportNexus.ViewModels.Main.Coordinators
{
    /// <summary>
    /// [Main Communication Command] Proxy
    ///
    /// RabbitMQ / Radar UDP Command 진입점을 대신 받아
    /// 통신 Coordinator 실행 후 결과 반영까지 처리한다.
    /// </summary>
    internal sealed class MainCommunicationCommandProxy
    {
        #region [Fields]

        /// <summary>
        /// 통신 실행 Coordinator
        /// </summary>
        private readonly MainCommunicationCoordinator _coordinator;

        /// <summary>
        /// 통신 결과 반영 객체
        /// </summary>
        private readonly MainCommunicationResultApplier _resultApplier;

        /// <summary>
        /// Network 입력 상태
        /// </summary>
        private readonly MainNetworkPanelViewModel _networkPanel;

        /// <summary>
        /// 연결 상태
        /// </summary>
        private readonly MainConnectionPanelViewModel _connectionPanel;

        /// <summary>
        /// [RabbitMQ] 연결 상태 변경 처리 함수
        /// </summary>
        private readonly System.Action<MainViewModel.ConnectionState> _rabbitMqConnectionStateChanged;

        /// <summary>
        /// [Radar UDP] 연결 상태 변경 처리 함수
        /// </summary>
        private readonly System.Action<MainViewModel.ConnectionState> _radarUdpConnectionStateChanged;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [Main Communication Command] Proxy 생성자
        /// </summary>
        internal MainCommunicationCommandProxy(
            MainCommunicationCoordinator coordinator,
            MainCommunicationResultApplier resultApplier,
            MainNetworkPanelViewModel networkPanel,
            MainConnectionPanelViewModel connectionPanel,
            System.Action<MainViewModel.ConnectionState> rabbitMqConnectionStateChanged,
            System.Action<MainViewModel.ConnectionState> radarUdpConnectionStateChanged)
        {
            _coordinator =
                coordinator;

            _resultApplier =
                resultApplier;

            _networkPanel =
                networkPanel;

            _connectionPanel =
                connectionPanel;

            _rabbitMqConnectionStateChanged =
                rabbitMqConnectionStateChanged;

            _radarUdpConnectionStateChanged =
                radarUdpConnectionStateChanged;
        }

        #endregion

        #region [RabbitMQ Methods]

        /// <summary>
        /// [RabbitMQ] 수신 시작
        /// </summary>
        internal async Task StartRabbitMqReceiveAsync()
        {
            MainCommunicationResult result =
                await _coordinator
                    .StartRabbitMqReceiveAsync(
                        _connectionPanel.RabbitMqConnectionState,
                        _rabbitMqConnectionStateChanged);

            Apply(
                result);
        }

        /// <summary>
        /// [RabbitMQ] 수신 중지
        /// </summary>
        internal void StopRabbitMqReceive()
        {
            MainCommunicationResult result =
                _coordinator
                    .StopRabbitMqReceive(
                        _connectionPanel.RabbitMqConnectionState,
                        _rabbitMqConnectionStateChanged);

            Apply(
                result);
        }

        #endregion

        #region [Radar UDP Methods]

        /// <summary>
        /// [Radar] UDP 수신 시작
        /// </summary>
        internal async Task StartRadarUdpReceiveAsync()
        {
            MainCommunicationResult result =
                await _coordinator
                    .StartRadarUdpReceiveAsync(
                        _connectionPanel.McbConnectionState,
                        _connectionPanel.ScbConnectionState,
                        _connectionPanel.RadarUdpConnectionState,
                        _networkPanel.RadarUdpLocalPort,
                        _radarUdpConnectionStateChanged);

            Apply(
                result);
        }

        /// <summary>
        /// [Radar] UDP 수신 중지
        /// </summary>
        internal void StopRadarUdpReceive()
        {
            MainCommunicationResult result =
                _coordinator
                    .StopRadarUdpReceive(
                        _connectionPanel.RadarUdpConnectionState,
                        _radarUdpConnectionStateChanged);

            Apply(
                result);
        }

        #endregion

        #region [Apply Methods]

        /// <summary>
        /// 통신 처리 결과 반영
        /// </summary>
        private void Apply(
            MainCommunicationResult result)
        {
            _resultApplier
                .Apply(
                    result);
        }
        #endregion
    }

}

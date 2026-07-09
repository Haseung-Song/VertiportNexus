namespace VertiportNexus.ViewModels.Main.Coordinators
{
    /// <summary>
    /// [Main Communication] 처리 결과
    /// </summary>
    internal sealed class MainCommunicationResult
    {
        #region [Properties]

        /// <summary>
        /// [RabbitMQ] 변경 연결 상태
        /// </summary>
        internal MainViewModel.ConnectionState? RabbitMqConnectionState { get; private set; }

        /// <summary>
        /// [Radar UDP] 변경 연결 상태
        /// </summary>
        internal MainViewModel.ConnectionState? RadarUdpConnectionState { get; private set; }

        /// <summary>
        /// [MQ] 상태 표시 문자열
        /// </summary>
        internal string MqStatusText { get; private set; }

        /// <summary>
        /// [Main] 상태 표시 문자열
        /// </summary>
        internal string MainStatusText { get; private set; }

        #endregion

        #region [Factory Methods]

        /// <summary>
        /// 처리 결과 생성
        /// </summary>
        internal static MainCommunicationResult Create(
            MainViewModel.ConnectionState? rabbitMqConnectionState = null,
            MainViewModel.ConnectionState? radarUdpConnectionState = null,
            string mqStatusText = null,
            string mainStatusText = null)
        {
            return new MainCommunicationResult
            {
                RabbitMqConnectionState =
                    rabbitMqConnectionState,

                RadarUdpConnectionState =
                    radarUdpConnectionState,

                MqStatusText =
                    mqStatusText,

                MainStatusText =
                    mainStatusText
            };
        }

        #endregion
    }
}

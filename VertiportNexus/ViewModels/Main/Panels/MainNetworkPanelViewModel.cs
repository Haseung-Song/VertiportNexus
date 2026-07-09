using System;

namespace VertiportNexus.ViewModels.Main.Panels
{
    /// <summary>
    /// [Main] Network / MQ / Radar UDP 설정 상태
    /// </summary>
    internal sealed class MainNetworkPanelViewModel
    {
        #region [Default Constants]

        /// <summary>
        /// [Radar] UDP 기본 수신 IP 주소
        /// </summary>
        private const string DEFAULT_RADAR_UDP_IP_ADDRESS =
            "127.0.0.1";

        /// <summary>
        /// [Radar] UDP 기본 Local Port
        /// </summary>
        private const int DEFAULT_RADAR_UDP_LOCAL_PORT =
            5000;

        /// <summary>
        /// [RabbitMQ] 기본 Host 주소
        /// </summary>
        private const string DEFAULT_MQ_HOST_NAME =
            "127.0.0.1";

        /// <summary>
        /// [RabbitMQ] 기본 Port
        /// </summary>
        private const int DEFAULT_MQ_PORT =
            5672;

        #endregion

        #region [Fields]

        /// <summary>
        /// [MCB] TCP 연결 IP 주소
        /// </summary>
        private string _mcbIpAddress;

        /// <summary>
        /// [MCB] TCP 연결 Port
        /// </summary>
        private int _mcbPort;

        /// <summary>
        /// [SCB] TCP 연결 IP 주소
        /// </summary>
        private string _scbIpAddress;

        /// <summary>
        /// [SCB] TCP 연결 Port
        /// </summary>
        private int _scbPort;

        /// <summary>
        /// [Radar] UDP 수신 IP 주소
        /// </summary>
        private string _radarUdpIpAddress;

        /// <summary>
        /// [Radar] UDP 수신 Local Port
        /// </summary>
        private int _radarUdpLocalPort;

        /// <summary>
        /// [RabbitMQ] Host 주소
        /// </summary>
        private string _mqHostName;

        /// <summary>
        /// [RabbitMQ] 연결 Port
        /// </summary>
        private int _mqPort;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [Main] Network 설정 상태 생성자
        /// </summary>
        /// <param name="defaultDeviceIpAddress">
        /// 기본 장비 IP 주소
        /// </param>
        /// <param name="defaultMcbPort">
        /// 기본 [MCB] Port
        /// </param>
        /// <param name="defaultScbPort">
        /// 기본 [SCB] Port
        /// </param>
        internal MainNetworkPanelViewModel(
            string defaultDeviceIpAddress,
            int defaultMcbPort,
            int defaultScbPort)
        {
            _mcbIpAddress =
                defaultDeviceIpAddress;

            _mcbPort =
                defaultMcbPort;

            _scbIpAddress =
                defaultDeviceIpAddress;

            _scbPort =
                defaultScbPort;

            _radarUdpIpAddress =
                DEFAULT_RADAR_UDP_IP_ADDRESS;

            _radarUdpLocalPort =
                DEFAULT_RADAR_UDP_LOCAL_PORT;

            _mqHostName =
                DEFAULT_MQ_HOST_NAME;

            _mqPort =
                DEFAULT_MQ_PORT;
        }

        #endregion

        #region [Properties]

        /// <summary>
        /// [MCB] TCP 연결 IP 주소
        /// </summary>
        internal string McbIpAddress =>
            _mcbIpAddress;

        /// <summary>
        /// [MCB] TCP 연결 Port
        /// </summary>
        internal int McbPort =>
            _mcbPort;

        /// <summary>
        /// [SCB] TCP 연결 IP 주소
        /// </summary>
        internal string ScbIpAddress =>
            _scbIpAddress;

        /// <summary>
        /// [SCB] TCP 연결 Port
        /// </summary>
        internal int ScbPort =>
            _scbPort;

        /// <summary>
        /// [Radar] UDP 수신 IP 주소
        /// </summary>
        internal string RadarUdpIpAddress =>
            _radarUdpIpAddress;

        /// <summary>
        /// [Radar] UDP 수신 Local Port
        /// </summary>
        internal int RadarUdpLocalPort =>
            _radarUdpLocalPort;

        /// <summary>
        /// [RabbitMQ] Host 주소
        /// </summary>
        internal string MqHostName =>
            _mqHostName;

        /// <summary>
        /// [RabbitMQ] 연결 Port
        /// </summary>
        internal int MqPort =>
            _mqPort;

        #endregion

        #region [Set Methods]

        /// <summary>
        /// [MCB] TCP 연결 IP 주소 저장
        /// </summary>
        internal bool SetMcbIpAddress(
            string value)
        {
            return SetValue(
                ref _mcbIpAddress,
                value);
        }

        /// <summary>
        /// [MCB] TCP 연결 Port 저장
        /// </summary>
        internal bool SetMcbPort(
            int value)
        {
            return SetValue(
                ref _mcbPort,
                value);
        }

        /// <summary>
        /// [SCB] TCP 연결 IP 주소 저장
        /// </summary>
        internal bool SetScbIpAddress(
            string value)
        {
            return SetValue(
                ref _scbIpAddress,
                value);
        }

        /// <summary>
        /// [SCB] TCP 연결 Port 저장
        /// </summary>
        internal bool SetScbPort(
            int value)
        {
            return SetValue(
                ref _scbPort,
                value);
        }

        /// <summary>
        /// [Radar] UDP 수신 IP 주소 저장
        /// </summary>
        internal bool SetRadarUdpIpAddress(
            string value)
        {
            return SetValue(
                ref _radarUdpIpAddress,
                value);
        }

        /// <summary>
        /// [Radar] UDP 수신 Local Port 저장
        /// </summary>
        internal bool SetRadarUdpLocalPort(
            int value)
        {
            return SetValue(
                ref _radarUdpLocalPort,
                value);
        }

        /// <summary>
        /// [RabbitMQ] Host 주소 저장
        /// </summary>
        internal bool SetMqHostName(
            string value)
        {
            return SetValue(
                ref _mqHostName,
                value);
        }

        /// <summary>
        /// [RabbitMQ] 연결 Port 저장
        /// </summary>
        internal bool SetMqPort(
            int value)
        {
            return SetValue(
                ref _mqPort,
                value);
        }

        #endregion

        #region [Utility Methods]

        /// <summary>
        /// 값 변경 처리
        /// </summary>
        private static bool SetValue<T>(
            ref T field,
            T value)
        {
            if (Equals(
                    field,
                    value))
            {
                return false;
            }

            field =
                value;

            return true;
        }

        #endregion
    }
}

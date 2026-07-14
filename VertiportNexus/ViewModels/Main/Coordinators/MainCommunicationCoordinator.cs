using System;
using System.Threading.Tasks;
using VertiportNexus.Common;
using VertiportNexus.Features.Main.Communication;

namespace VertiportNexus.ViewModels.Main.Coordinators
{
    /// <summary>
    /// [Main] RabbitMQ / Radar UDP 실행 Coordinator
    ///
    /// 통신 시작 / 중지 가능 여부 판단과 Workflow 호출을 담당한다.
    /// </summary>
    internal sealed class MainCommunicationCoordinator
    {
        #region [Fields]

        /// <summary>
        /// [RabbitMQ] 수신 Workflow
        /// </summary>
        private readonly RabbitMqReceiveWorkflow _rabbitMqReceiveWorkflow;

        /// <summary>
        /// [Radar] UDP 수신 Workflow
        /// </summary>
        private readonly RadarUdpReceiveWorkflow _radarUdpReceiveWorkflow;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [Main Communication] Coordinator 생성
        /// </summary>
        internal MainCommunicationCoordinator(
            RabbitMqReceiveWorkflow rabbitMqReceiveWorkflow,
            RadarUdpReceiveWorkflow radarUdpReceiveWorkflow)
        {
            _rabbitMqReceiveWorkflow =
                rabbitMqReceiveWorkflow ?? throw new ArgumentNullException(nameof(rabbitMqReceiveWorkflow));

            _radarUdpReceiveWorkflow =
                radarUdpReceiveWorkflow ?? throw new ArgumentNullException(nameof(radarUdpReceiveWorkflow));
        }

        #endregion

        #region [RabbitMQ Methods]

        /// <summary>
        /// [RabbitMQ] 수신 시작
        /// </summary>
        /// <param name="rabbitMqConnectionState">
        /// 현재 [RabbitMQ] 연결 상태
        /// </param>
        /// <param name="connectionStateChanged">
        /// [RabbitMQ] 연결 상태 변경 처리 함수
        /// </param>
        /// <returns>
        /// Communication 처리 결과
        /// </returns>
        internal async Task<MainCommunicationResult> StartRabbitMqReceiveAsync(
            MainViewModel.ConnectionState rabbitMqConnectionState,
            Action<MainViewModel.ConnectionState> connectionStateChanged)
        {
            if (rabbitMqConnectionState == MainViewModel.ConnectionState.Connected ||
                rabbitMqConnectionState == MainViewModel.ConnectionState.Connecting)
            {
                ConsoleLogHelper.PrintLine();

                ConsoleLogHelper.WriteLine(
                    "[CSE][MQ] Start Ignored : Already Started");

                ConsoleLogHelper.WriteLine();

                return MainCommunicationResult.Create();
            }

            connectionStateChanged?.Invoke(
                MainViewModel.ConnectionState.Connecting);

            ControllerResult result =
                await _rabbitMqReceiveWorkflow
                    .StartAsync();

            MainViewModel.ConnectionState nextState =
                result.IsSuccess
                    ? MainViewModel.ConnectionState.Connected
                    : MainViewModel.ConnectionState.Disconnected;

            connectionStateChanged?.Invoke(
                nextState);

            return MainCommunicationResult.Create(
                mqStatusText: result.Message);
        }

        /// <summary>
        /// [RabbitMQ] 수신 중지
        /// </summary>
        /// <param name="rabbitMqConnectionState">
        /// 현재 [RabbitMQ] 연결 상태
        /// </param>
        /// <param name="connectionStateChanged">
        /// [RabbitMQ] 연결 상태 변경 처리 함수
        /// </param>
        /// <returns>
        /// Communication 처리 결과
        /// </returns>
        internal MainCommunicationResult StopRabbitMqReceive(
            MainViewModel.ConnectionState rabbitMqConnectionState,
            Action<MainViewModel.ConnectionState> connectionStateChanged)
        {
            if (rabbitMqConnectionState != MainViewModel.ConnectionState.Connected)
            {
                ConsoleLogHelper.PrintLine();

                ConsoleLogHelper.WriteLine(
                    "[CSE][MQ] Stop Ignored : Not Started");

                ConsoleLogHelper.WriteLine();

                return MainCommunicationResult.Create();
            }

            ControllerResult result =
                _rabbitMqReceiveWorkflow
                    .Stop();

            connectionStateChanged?.Invoke(
                MainViewModel.ConnectionState.Disconnected);

            return MainCommunicationResult.Create(
                mqStatusText: result.Message);
        }

        #endregion

        #region [Radar UDP Methods]

        /// <summary>
        /// [Radar] UDP 수신 시작
        /// </summary>
        /// <param name="mcbConnectionState">
        /// 현재 [MCB] 연결 상태
        /// </param>
        /// <param name="scbConnectionState">
        /// 현재 [SCB] 연결 상태
        /// </param>
        /// <param name="radarUdpConnectionState">
        /// 현재 [Radar UDP] 연결 상태
        /// </param>
        /// <param name="localPort">
        /// [Radar UDP] Local Port
        /// </param>
        /// <param name="connectionStateChanged">
        /// [Radar UDP] 연결 상태 변경 처리 함수
        /// </param>
        /// <returns>
        /// Communication 처리 결과
        /// </returns>
        internal async Task<MainCommunicationResult> StartRadarUdpReceiveAsync(
            MainViewModel.ConnectionState mcbConnectionState,
            MainViewModel.ConnectionState scbConnectionState,
            MainViewModel.ConnectionState radarUdpConnectionState,
            int localPort,
            Action<MainViewModel.ConnectionState> connectionStateChanged)
        {
            if (mcbConnectionState != MainViewModel.ConnectionState.Connected ||
                scbConnectionState != MainViewModel.ConnectionState.Connected)
            {
                ConsoleLogHelper.PrintLine();

                ConsoleLogHelper.WriteLine(
                    "[RADAR][UDP] Start Failed : MCB / SCB Not Connected");

                ConsoleLogHelper.WriteLine();

                return MainCommunicationResult.Create();
            }

            if (radarUdpConnectionState == MainViewModel.ConnectionState.Connected ||
                radarUdpConnectionState == MainViewModel.ConnectionState.Connecting)
            {
                ConsoleLogHelper.PrintLine();

                ConsoleLogHelper.WriteLine(
                    "[RADAR][UDP] Start Ignored : Already Started");

                ConsoleLogHelper.WriteLine();

                return MainCommunicationResult.Create();
            }

            connectionStateChanged?.Invoke(
                MainViewModel.ConnectionState.Connecting);

            ControllerResult result =
                await _radarUdpReceiveWorkflow
                    .StartAsync(
                        localPort);

            MainViewModel.ConnectionState nextState =
                result.IsSuccess
                    ? MainViewModel.ConnectionState.Connected
                    : MainViewModel.ConnectionState.Disconnected;

            connectionStateChanged?.Invoke(
                nextState);

            return MainCommunicationResult.Create(
                mainStatusText: result.Message);
        }

        /// <summary>
        /// [Radar] UDP 수신 중지
        /// </summary>
        /// <param name="radarUdpConnectionState">
        /// 현재 [Radar UDP] 연결 상태
        /// </param>
        /// <param name="connectionStateChanged">
        /// [Radar UDP] 연결 상태 변경 처리 함수
        /// </param>
        /// <returns>
        /// Communication 처리 결과
        /// </returns>
        internal MainCommunicationResult StopRadarUdpReceive(
            MainViewModel.ConnectionState radarUdpConnectionState,
            Action<MainViewModel.ConnectionState> connectionStateChanged)
        {
            if (radarUdpConnectionState != MainViewModel.ConnectionState.Connected)
            {
                ConsoleLogHelper.PrintLine();
                ConsoleLogHelper.WriteLine(
                    "[RADAR][UDP] Stop Ignored : Not Started");
                ConsoleLogHelper.WriteLine();

                return MainCommunicationResult.Create();
            }

            ControllerResult result =
                _radarUdpReceiveWorkflow
                    .Stop();

            if (result.IsSuccess)
            {
                connectionStateChanged?.Invoke(
                    MainViewModel.ConnectionState.Disconnected);
            }

            return MainCommunicationResult.Create(
                mainStatusText: result.Message);
        }
        #endregion
    }

}

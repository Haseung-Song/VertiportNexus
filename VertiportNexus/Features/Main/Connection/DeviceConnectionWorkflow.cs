using System;
using System.Threading.Tasks;
using VertiportNexus.Common;
using VertiportNexus.Features.Main.Camera;
using VertiportNexus.Features.Main.Communication;
using VertiportNexus.Features.Main.Ptz;
using VertiportNexus.ViewModels.Main;
using VertiportNexus.ViewModels.Main.Composition;

namespace VertiportNexus.Features.Main.Connection
{
    /// <summary>
    /// [Device Connection] Workflow
    /// 
    /// [MainViewModel]에 직접 포함되어 있던
    /// MCB / SCB 장비 연결 / 연결 해제 흐름을 분리한다.
    /// 
    /// 화면 상태값 변경은 [MainViewModel]에서 수행하고,
    /// 본 Workflow는 Controller 호출 / EO Camera 연결 / Home Position 대기 흐름을 담당한다.
    /// </summary>
    internal sealed class DeviceConnectionWorkflow
    {
        #region [Fields]

        /// <summary>
        /// [MainViewModel] 구성 객체
        /// </summary>
        private readonly MainViewModelContext _context;

        /// <summary>
        /// [EO Camera] 연동 Workflow
        /// </summary>
        private readonly EoCameraWorkflow _eoCameraWorkflow;

        /// <summary>
        /// [RabbitMQ] 수신 Workflow
        /// </summary>
        private readonly RabbitMqReceiveWorkflow _rabbitMqReceiveWorkflow;

        /// <summary>
        /// [Radar] UDP 수신 Workflow
        /// </summary>
        private readonly RadarUdpReceiveWorkflow _radarUdpReceiveWorkflow;

        /// <summary>
        /// [PTZ Control] Workflow
        /// </summary>
        private readonly PtzControlWorkflow _ptzControlWorkflow;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [Device Connection] Workflow 생성자
        /// </summary>
        /// <param name="context">
        /// [MainViewModel] 구성 객체
        /// </param>
        /// <param name="eoCameraWorkflow">
        /// [EO Camera] 연동 Workflow
        /// </param>
        /// <param name="rabbitMqReceiveWorkflow">
        /// [RabbitMQ] 수신 Workflow
        /// </param>
        /// <param name="radarUdpReceiveWorkflow">
        /// [Radar] UDP 수신 Workflow
        /// </param>
        /// <param name="ptzControlWorkflow">
        /// [PTZ Control] Workflow
        /// </param>
        internal DeviceConnectionWorkflow(
            MainViewModelContext context,
            EoCameraWorkflow eoCameraWorkflow,
            RabbitMqReceiveWorkflow rabbitMqReceiveWorkflow,
            RadarUdpReceiveWorkflow radarUdpReceiveWorkflow,
            PtzControlWorkflow ptzControlWorkflow)
        {
            _context =
                context;

            _eoCameraWorkflow =
                eoCameraWorkflow;

            _rabbitMqReceiveWorkflow =
                rabbitMqReceiveWorkflow;

            _radarUdpReceiveWorkflow =
                radarUdpReceiveWorkflow;

            _ptzControlWorkflow =
                ptzControlWorkflow;
        }

        #endregion

        #region [Connection Methods]

        /// <summary>
        /// [MCB] / [SCB] 장비 연결
        /// </summary>
        /// <param name="mcbIpAddress">
        /// [MCB] 연결 대상 IP
        /// </param>
        /// <param name="mcbPort">
        /// [MCB] 연결 대상 Port
        /// </param>
        /// <param name="scbIpAddress">
        /// [SCB] 연결 대상 IP
        /// </param>
        /// <param name="scbPort">
        /// [SCB] 연결 대상 Port
        /// </param>
        /// <returns>
        /// [Device Connection] Workflow 처리 결과
        /// </returns>
        internal async Task<DeviceConnectionWorkflowResult> ConnectAsync(
            string mcbIpAddress,
            int mcbPort,
            string scbIpAddress,
            int scbPort)
        {
            DeviceConnectionControllerResult result =
                await _context
                    .DeviceConnectionController
                    .ConnectAsync(
                        mcbIpAddress,
                        mcbPort,
                        scbIpAddress,
                        scbPort);

            if (result.IsSuccess &&
                result.ConnectionResult != null)
            {
                _eoCameraWorkflow
                    .EnableVideoDisplay();

                _eoCameraWorkflow
                    .Connect();
            }

            return DeviceConnectionWorkflowResult.ConnectCompleted(
                result);
        }

        /// <summary>
        /// [MCB] / [SCB] 장비 연결 해제
        /// </summary>
        /// <param name="shouldStopRadarUdp">
        /// [Radar] UDP 수신 중지 필요 여부
        /// </param>
        /// <param name="shouldStopRabbitMq">
        /// [RabbitMQ] 수신 중지 필요 여부
        /// </param>
        /// <returns>
        /// [Device Connection] Workflow 처리 결과
        /// </returns>
        internal DeviceConnectionWorkflowResult Disconnect(
            bool shouldStopRadarUdp,
            bool shouldStopRabbitMq)
        {
            ControllerResult radarUdpStopResult =
                null;

            ControllerResult rabbitMqStopResult =
                null;

            if (shouldStopRadarUdp)
            {
                radarUdpStopResult =
                    _radarUdpReceiveWorkflow
                        .Stop();
            }

            if (shouldStopRabbitMq)
            {
                rabbitMqStopResult =
                    _rabbitMqReceiveWorkflow
                        .Stop();
            }

            _eoCameraWorkflow
                .Disconnect();

            ControllerResult disconnectResult =
                _context
                    .DeviceConnectionController
                    .Disconnect();

            return DeviceConnectionWorkflowResult.DisconnectCompleted(
                radarUdpStopResult,
                rabbitMqStopResult,
                disconnectResult);
        }

        #endregion

        #region [Home Position Methods]

        /// <summary>
        /// [장비 연결 후] EO RTSP 연결 성공 대기 및 Home Position 이동
        /// </summary>
        /// <param name="isHomePositionMoving">
        /// Home Position 이동 진행 여부
        /// </param>
        /// <param name="isDeviceFullyConnected">
        /// 장비 전체 연결 여부
        /// </param>
        /// <param name="isDeviceFullyConnectedProperty">
        /// 장비 전체 연결 여부 Binding 값
        /// </param>
        /// <param name="currentPanProvider">
        /// 현재 Pan 위치값 조회 함수
        /// </param>
        /// <param name="currentTiltProvider">
        /// 현재 Tilt 위치값 조회 함수
        /// </param>
        /// <param name="homePositionMovingStateChanged">
        /// Home Position 이동 상태 변경 처리기
        /// </param>
        /// <param name="mainStatusChanged">
        /// Main 상태 문자열 변경 처리기
        /// </param>
        /// <returns>
        /// [PTZ Control] Workflow 처리 결과
        /// </returns>
        internal async Task<PtzControlWorkflowResult> WaitEoRtspConnectedAndMoveHomePositionAsync(
            bool isHomePositionMoving,
            bool isDeviceFullyConnectedProperty,
            Func<bool> isDeviceFullyConnected,
            Func<double> currentPanProvider,
            Func<double> currentTiltProvider,
            Action<bool> homePositionMovingStateChanged,
            Action<string> mainStatusChanged)
        {
            const int CHECK_DELAY_MS =
                200;

            const int MAX_WAIT_MS =
                65000;

            int elapsedMs =
                0;

            Console.WriteLine(
                "[EO CAMERA] RTSP Connected Wait Start");

            ConsoleLogHelper.PrintLine();

            while (_eoCameraWorkflow.IsVideoDisplayEnabled &&
                   !_eoCameraWorkflow.IsRtspConnected &&
                   elapsedMs < MAX_WAIT_MS)
            {
                await Task.Delay(
                    CHECK_DELAY_MS);

                elapsedMs +=
                    CHECK_DELAY_MS;
            }

            if (!_eoCameraWorkflow.IsVideoDisplayEnabled)
            {
                ConsoleLogHelper.PrintLine();

                Console.WriteLine(
                    "[EO CAMERA] RTSP Connected Wait Canceled : Display Disabled");

                ConsoleLogHelper.PrintLine();

                return null;
            }

            if (!_eoCameraWorkflow.IsRtspConnected)
            {
                ConsoleLogHelper.PrintLine();

                Console.WriteLine(
                    "[EO CAMERA] RTSP Connected Wait Failed : Timeout");

                Console.WriteLine(
                    "[DEVICE] Home Position After Connect Skipped : EO RTSP Not Connected");

                ConsoleLogHelper.PrintLine();

                return null;
            }

            Console.WriteLine(
                "[EO CAMERA] RTSP Connected Wait Complete");

            ConsoleLogHelper.PrintLine();

            await Task.Delay(
                300);

            return await _ptzControlWorkflow
                .MoveHomePositionAsync(
                    "[DEVICE] Home Position After Connect",
                    isHomePositionMoving,
                    isDeviceFullyConnectedProperty,
                    isDeviceFullyConnected,
                    currentPanProvider,
                    currentTiltProvider,
                    homePositionMovingStateChanged,
                    mainStatusChanged);
        }
        #endregion
    }

}

using Serilog;
using System;
using System.Windows.Media.Imaging;
using VertiportNexus.Common;
using VertiportNexus.Models.ADS1000;
using VertiportNexus.Models.Vertiport;
using VertiportNexus.Services.ADS1000;
using VertiportNexus.Services.Camera;
using VertiportNexus.Services.Command;
using VertiportNexus.Services.Communication.MQ;
using VertiportNexus.Services.Communication.TCP;
using VertiportNexus.Services.Communication.UDP;
using VertiportNexus.Services.Radar;
using VertiportNexus.Services.Vertiport;

namespace VertiportNexus.ViewModels.Main.Composition
{
    /// <summary>
    /// [MainViewModel] 구성 초기화 처리
    /// 
    /// [MainViewModel] 생성자에 직접 포함되어 있던
    /// Service / Controller 생성 및 Event 연결 책임을 분리한다.
    /// 
    /// [MainViewModel]은 화면 Binding / Command / UI 상태 반영에 집중하고,
    /// 본 클래스는 화면 구동에 필요한 내부 구성 객체 생성만 담당한다.
    /// </summary>
    internal sealed class MainViewModelBootstrapper
    {
        #region [Create Methods]

        /// <summary>
        /// [MainViewModel] 구성 객체 생성
        /// </summary>
        /// <param name="mqHostName">
        /// [MQ] 연결 대상 [Host]
        /// </param>
        /// <param name="mqPort">
        /// [MQ] 연결 대상 [Port]
        /// </param>
        /// <param name="eventHandlers">
        /// [MainViewModel] 이벤트 처리기 목록
        /// </param>
        /// <returns>
        /// [MainViewModel] 구성 객체
        /// </returns>
        internal MainViewModelContext Create(
            string mqHostName,
            int mqPort,
            MainViewModelEventHandlerSet eventHandlers)
        {
            MainViewModelContext context =
                new MainViewModelContext();

            InitializeTcpServices(
                context,
                eventHandlers);

            InitializeAds1000Services(
                context,
                eventHandlers);

            InitializeStateServices(
                context,
                eventHandlers);

            InitializeMqServices(
                context,
                mqHostName,
                mqPort,
                eventHandlers);

            InitializeRadarServices(
                context);

            InitializeControllers(
                context);

            return context;
        }

        #endregion

        #region [TCP Initialize Methods]

        /// <summary>
        /// [TCP] 통신 서비스 초기화
        /// </summary>
        /// <param name="context">
        /// [MainViewModel] 구성 객체
        /// </param>
        /// <param name="eventHandlers">
        /// [MainViewModel] 이벤트 처리기 목록
        /// </param>
        private void InitializeTcpServices(
            MainViewModelContext context,
            MainViewModelEventHandlerSet eventHandlers)
        {
            // [MCB] [TCP] 통신 서비스 생성
            context.McbTcpClientService =
                new TcpClientService(
                    "MCB");

            // [SCB] [TCP] 통신 서비스 생성
            context.ScbTcpClientService =
                new TcpClientService(
                    "SCB");

            if (eventHandlers != null)
            {
                // [MCB] 수신 이벤트 연결
                //
                // [TCP] 수신 데이터는 기존 [MainViewModel]의
                // [OnMcbMessageReceived] 흐름을 그대로 사용한다.
                context.McbTcpClientService.MessageReceived +=
                    eventHandlers.OnMcbMessageReceived;

                // [SCB] 수신 이벤트 연결
                //
                // [TCP] 수신 데이터는 기존 [MainViewModel]의
                // [OnScbMessageReceived] 흐름을 그대로 사용한다.
                context.ScbTcpClientService.MessageReceived +=
                    eventHandlers.OnScbMessageReceived;
            }

            // [ADS1000] 장비 [TCP] 연결 서비스 생성
            //
            // [MCB] / [SCB] TCP Client를 조합하여
            // 장비 연결 / 해제 흐름을 처리한다.
            context.Ads1000ConnectionService =
                new Ads1000ConnectionService(
                    context.McbTcpClientService,
                    context.ScbTcpClientService);

            if (eventHandlers != null)
            {
                // [ADS1000] 장비 연결 상태 변경 이벤트 연결
                //
                // [MCB] / [SCB] 연결 상태 변화는
                // 화면 연결 상태 표시값에 즉시 반영한다.
                context.Ads1000ConnectionService.ConnectionStateChanged +=
                    eventHandlers.OnAds1000ConnectionStateChanged;
            }

        }

        #endregion

        #region [ADS1000 Initialize Methods]

        /// <summary>
        /// [ADS1000] 제어 / EO 영상 서비스 초기화
        /// </summary>
        /// <param name="context">
        /// [MainViewModel] 구성 객체
        /// </param>
        /// <param name="eventHandlers">
        /// [MainViewModel] 이벤트 처리기 목록
        /// </param>
        private void InitializeAds1000Services(
            MainViewModelContext context,
            MainViewModelEventHandlerSet eventHandlers)
        {
            // [EO] [Camera] 영상 서비스 생성
            context.EoCameraService =
                new EoCameraService();

            if (eventHandlers != null)
            {
                // [EO] 영상 [Frame] 수신 이벤트 연결
                //
                // RTSP 수신 Frame을 화면 Image Binding 속성에 반영한다.
                context.EoCameraService.FrameReceived +=
                    eventHandlers.OnEoCameraFrameReceived;

                // [EO] 영상 상태 변경 이벤트 연결
                //
                // RTSP 연결 / 실패 / Error 상태를
                // 화면 운용 상태 및 재연결 흐름에 반영한다.
                context.EoCameraService.StatusChanged +=
                    eventHandlers.OnEoCameraStatusChanged;
            }

            // [MCB] [Packet Builder] 생성
            //
            // Pan / Tilt / Zoom / Focus 명령 중
            // [MCB] 대상 명령 Packet 생성에 사용한다.
            Ads1000McbPacketBuilder mcbPacketBuilder =
                new Ads1000McbPacketBuilder();

            // [SCB] [Packet Builder] 생성
            //
            // Pan / Tilt / Zoom / Focus 명령 중
            // [SCB] 대상 명령 Packet 생성에 사용한다.
            Ads1000ScbPacketBuilder scbPacketBuilder =
                new Ads1000ScbPacketBuilder();

            // [ADS1000] [Camera] 제어 서비스 생성
            //
            // [MCB] / [SCB] TCP Client와 Packet Builder를 조합하여
            // 실제 ADS1000 제어 명령 송신을 담당한다.
            context.Ads1000CameraControlService =
                new Ads1000CameraControlService(
                    context.McbTcpClientService,
                    context.ScbTcpClientService,
                    mcbPacketBuilder,
                    scbPacketBuilder);

            if (eventHandlers != null)
            {
                // [ADS1000] [Packet] 송신 결과 이벤트 연결
                //
                // 장비 제어 Packet 송신 성공 / 실패 결과를
                // 화면 상태 문자열 또는 로그에 반영한다.
                context.Ads1000CameraControlService.SendResultChanged +=
                    eventHandlers.OnAds1000SendResultChanged;
            }

            // [ADS1000] 상태 [Packet] 처리 서비스 생성
            //
            // 장비에서 수신한 Raw Packet을
            // 상태 Packet 모델로 파싱하기 위해 사용한다.
            context.Ads1000StatusService =
                new Ads1000StatusService();

            Log.Information(
                "[ADS1000] Encoder Resolution : Pan={PanResolution}, Tilt={TiltResolution}",
                Ads1000Constants.PAN_MOTOR_ENCODER_RESOLUTION,
                Ads1000Constants.TILT_MOTOR_ENCODER_RESOLUTION);
        }

        #endregion

        #region [State Initialize Methods]

        /// <summary>
        /// 상태 저장 / 내부 명령 서비스 초기화
        /// </summary>
        /// <param name="context">
        /// [MainViewModel] 구성 객체
        /// </param>
        /// <param name="eventHandlers">
        /// [MainViewModel] 이벤트 처리기 목록
        /// </param>
        private void InitializeStateServices(
            MainViewModelContext context,
            MainViewModelEventHandlerSet eventHandlers)
        {
            // [Camera] 상태 저장 서비스 생성
            //
            // ADS1000 상태 Packet에서 파싱된
            // Pan / Tilt / Zoom / Focus / PTZ Mode 값을 보관한다.
            context.CameraStateProvider =
                new CameraStateProvider();

            // [Detection] 상태 저장 서비스 생성
            //
            // [CSE] 탐지 / 추적 명령 상태와
            // Tracking 제어에 사용할 마지막 탐지 객체 정보를 보관한다.
            context.DetectionStateProvider =
                new DetectionStateProvider();

            // [Radar] 상태 저장 서비스 생성
            //
            // Radar Tracking 활성 상태와
            // Radar 우선 제어 여부를 보관한다.
            context.RadarStateProvider =
                new RadarStateProvider();

            // 내부 [Camera] 명령 처리 서비스 생성
            //
            // [CSE] 명령 처리 결과로 생성된 내부 Camera Command를
            // 실제 ADS1000 장비 제어 명령으로 변환 / 송신한다.
            context.CameraCommandService =
                new CameraCommandService(
                    context.Ads1000CameraControlService,
                    context.CameraStateProvider);

            // [Tracking] 자동 추적 제어 서비스 생성
            //
            // 탐지 객체 중심점과 영상 중심점을 비교하여
            // Pan / Tilt 보정 명령을 계산하고 송신한다.
            context.TrackingControlService =
                new TrackingControlService(
                    context.Ads1000CameraControlService);

            if (eventHandlers != null)
            {
                // [PTZ] 제어 모드 변경 이벤트 연결
                //
                // [MQ] 수신 또는 화면 버튼 조작으로
                // PTZ Mode가 변경된 경우 화면 표시값을 갱신한다.
                context.CameraStateProvider.PtzControlModeChanged +=
                    eventHandlers.OnPtzControlModeChanged;
            }

        }

        #endregion

        #region [MQ Initialize Methods]

        /// <summary>
        /// [MQ] / [CSE] 서비스 초기화
        /// </summary>
        /// <param name="context">
        /// [MainViewModel] 구성 객체
        /// </param>
        /// <param name="mqHostName">
        /// [MQ] 연결 대상 [Host]
        /// </param>
        /// <param name="mqPort">
        /// [MQ] 연결 대상 [Port]
        /// </param>
        /// <param name="eventHandlers">
        /// [MainViewModel] 이벤트 처리기 목록
        /// </param>
        private void InitializeMqServices(
            MainViewModelContext context,
            string mqHostName,
            int mqPort,
            MainViewModelEventHandlerSet eventHandlers)
        {
            // [MQ] 수신 서비스 생성
            //
            // 실제 RabbitMQ [q.command.req] / [q.status.req] Queue에서
            // [CSE] 명령 JSON을 수신한다.
            context.MqReceiver =
                new RabbitMqReceiver(
                    mqHostName,
                    mqPort);

            // [MQ] 송신 서비스 생성
            //
            // [CSE] 명령 처리 결과를
            // RabbitMQ 응답 Queue로 송신한다.
            context.MqSender =
                new RabbitMqSender(
                    mqHostName,
                    mqPort);

            // [CSE] 명령 수신 서비스 생성
            //
            // MQ Receiver에서 수신한 JSON 메시지를
            // CSE Command 모델로 변환하여 이벤트로 전달한다.
            context.CseCommandReceiveService =
                new CseCommandReceiveService(
                    context.MqReceiver);

            // [CSE] 명령 응답 송신 서비스 생성
            //
            // 명령 처리 결과 / 상태 조회 결과를
            // [q.command.res] / [q.status.res] Queue로 송신한다.
            context.CseCommandResponseService =
                new CseCommandResponseService(
                    context.MqSender);

            // [CSE] 명령 처리 서비스 생성
            //
            // detect_on / detect_off / detect_cont /
            // ptz_move / get_state 명령을 처리한다.
            context.CseCommandHandler =
                new CseCommandHandler(
                    context.CameraCommandService,
                    context.CseCommandResponseService,
                    context.CameraStateProvider,
                    context.DetectionStateProvider,
                    context.TrackingControlService,
                    context.RadarStateProvider);

            if (eventHandlers != null)
            {
                // [CSE] 명령 수신 이벤트 연결
                //
                // CSE Command 모델 변환 완료 후
                // 기존 [MainViewModel] 명령 처리 흐름으로 전달한다.
                context.CseCommandReceiveService.CommandReceived +=
                    eventHandlers.OnCseCommandReceived;
            }

        }

        #endregion

        #region [Radar Initialize Methods]

        /// <summary>
        /// [Radar] UDP 연동 서비스 초기화
        /// </summary>
        /// <param name="context">
        /// [MainViewModel] 구성 객체
        /// </param>
        private void InitializeRadarServices(
            MainViewModelContext context)
        {
            // [Radar] UDP 통신 서비스 생성
            //
            // [CSR]에서 CSE로 전달되는 Radar Packet을
            // UDP로 수신하기 위해 사용한다.
            context.RadarUdpClientService =
                new UdpClientService(
                    "RADAR");

            // [Radar] Packet Parser 생성
            //
            // 수신 byte[] 데이터를 Header / SubData / Tail 구조로 분리하고,
            // Command별 Payload 모델로 변환한다.
            context.RadarPacketParser =
                new RadarPacketParser();

            // [Radar] Packet Builder 생성
            //
            // CSE에서 CSR로 송신할 응답 Packet을 생성한다.
            context.RadarPacketBuilder =
                new RadarPacketBuilder();

            // [Radar] 추적 제어 서비스 생성
            //
            // Radar Tracking Request에서 수신한
            // 방위각 / 고각 정보를 ADS1000 Pan / Tilt 제어로 연결한다.
            context.RadarTrackingControlService =
                new RadarTrackingControlService(
                    context.Ads1000CameraControlService);

            // [Radar] Command 처리 서비스 생성
            //
            // Radar Packet의 Command를 기준으로
            // Tracking Request / BIST Request를 분기 처리하고,
            // Tracking Request 수신 시 Radar 우선 제어 상태를 활성화한다.
            context.RadarCommandHandler =
                new RadarCommandHandler(
                    context.RadarPacketParser,
                    context.RadarPacketBuilder,
                    context.RadarStateProvider,
                    context.RadarTrackingControlService);

            // [Radar] UDP 연동 서비스 생성
            //
            // UDP 수신 Packet을 Handler로 전달하고,
            // 처리 결과 응답 Packet을 송신자에게 반환한다.
            context.RadarUdpService =
                new RadarUdpService(
                    context.RadarUdpClientService,
                    context.RadarCommandHandler);
        }

        #endregion

        #region [Controller Initialize Methods]

        /// <summary>
        /// Controller 초기화
        /// </summary>
        /// <param name="context">
        /// [MainViewModel] 구성 객체
        /// </param>
        private void InitializeControllers(
            MainViewModelContext context)
        {
            // [RabbitMQ] 수신 Controller 생성
            context.RabbitMqController =
                new RabbitMqController(
                    context.CseCommandReceiveService,
                    context.CseCommandHandler,
                    context.MqReceiver);

            // [Radar] UDP 수신 Controller 생성
            context.RadarUdpController =
                new RadarUdpController(
                    context.RadarUdpService);

            // [Device Connection] Controller 생성
            context.DeviceConnectionController =
                new DeviceConnectionController(
                    context.Ads1000ConnectionService);

            // [EO Camera] Controller 생성
            context.EoCameraController =
                new EoCameraController(
                    context.EoCameraService);

            // [ADS1000 Receive] Controller 생성
            context.Ads1000ReceiveController =
                new Ads1000ReceiveController(
                    context.Ads1000StatusService);

            // [ADS1000 Status Apply] Controller 생성
            context.Ads1000StatusApplyController =
                new Ads1000StatusApplyController(
                    context.CameraStateProvider);

            // [PTZ Absolute] Controller 생성
            context.PtzAbsoluteController =
                new PtzAbsoluteController(
                    context.Ads1000CameraControlService);

            // [PTZ Relative] Controller 생성
            context.PtzRelativeController =
                new PtzRelativeController(
                    context.Ads1000CameraControlService);

            // [PTZ Continuous] Controller 생성
            context.PtzContinuousController =
                new PtzContinuousController(
                    context.Ads1000CameraControlService);

            // [PTZ Home / Zero] Controller 생성
            context.PtzHomeZeroController =
                new PtzHomeZeroController(
                    context.Ads1000CameraControlService);

            // [Keyboard PTZ] Controller 생성
            context.KeyboardPtzController =
                new KeyboardPtzController();

            // [Zoom / Focus Position] Controller 생성
            context.ZoomFocusPositionController =
                new ZoomFocusPositionController(
                    context.Ads1000CameraControlService);

            // [PTZ Mode] Controller 생성
            context.PtzModeController =
                new PtzModeController();
        }
        #endregion
    }

    /// <summary>
    /// [MainViewModel] 이벤트 처리기 목록
    /// 
    /// [MainViewModelBootstrapper]에서 생성한 Service 이벤트를
    /// 기존 [MainViewModel] 이벤트 처리 메서드와 연결하기 위해 사용한다.
    /// 
    /// 현재 단계에서는 이벤트 처리 로직 자체는 [MainViewModel]에 남겨두고,
    /// 이벤트 연결 책임만 [Bootstrapper]로 분리한다.
    /// </summary>
    internal sealed class MainViewModelEventHandlerSet
    {
        #region [TCP Event Handler Properties]

        /// <summary>
        /// [MCB] 수신 이벤트 처리기
        /// </summary>
        internal Action<byte[], DateTime> OnMcbMessageReceived { get; set; }

        /// <summary>
        /// [SCB] 수신 이벤트 처리기
        /// </summary>
        internal Action<byte[], DateTime> OnScbMessageReceived { get; set; }

        /// <summary>
        /// [ADS1000] 장비 연결 상태 변경 이벤트 처리기
        /// </summary>
        internal Action<bool?, bool?> OnAds1000ConnectionStateChanged { get; set; }

        #endregion

        #region [Camera Event Handler Properties]

        /// <summary>
        /// [EO Camera] Frame 수신 이벤트 처리기
        /// </summary>
        internal Action<BitmapSource> OnEoCameraFrameReceived { get; set; }

        /// <summary>
        /// [EO Camera] 상태 변경 이벤트 처리기
        /// </summary>
        internal Action<string> OnEoCameraStatusChanged { get; set; }

        /// <summary>
        /// [ADS1000] Packet 송신 결과 이벤트 처리기
        /// </summary>
        internal Action<Ads1000SendResult> OnAds1000SendResultChanged { get; set; }

        /// <summary>
        /// [PTZ] 제어 모드 변경 이벤트 처리기
        /// </summary>
        internal Action<string> OnPtzControlModeChanged { get; set; }

        #endregion

        #region [CSE Event Handler Properties]

        /// <summary>
        /// [CSE] 명령 수신 이벤트 처리기
        /// </summary>
        internal Action<CseCommandMessage> OnCseCommandReceived { get; set; }

        #endregion
    }

}

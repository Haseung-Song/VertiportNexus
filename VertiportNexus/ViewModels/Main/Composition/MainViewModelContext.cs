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
    /// [MainViewModel] 구성 객체
    /// 
    /// [MainViewModel] 생성자에서 직접 생성하던
    /// Service / Controller 인스턴스를 보관한다.
    /// 
    /// 현재 단계에서는 기존 [MainViewModel] 필드 구조를 유지하기 위해
    /// 생성된 객체를 [Context]에 담아 전달하고,
    /// 이후 단계에서 [MainViewModel]이 세부 Service / Controller를
    /// 직접 참조하지 않도록 점진적으로 줄여나간다.
    /// </summary>
    internal sealed class MainViewModelContext
    {
        #region [TCP Service Properties]

        /// <summary>
        /// [MCB] [TCP] 통신 서비스
        /// </summary>
        internal TcpClientService McbTcpClientService { get; set; }

        /// <summary>
        /// [SCB] [TCP] 통신 서비스
        /// </summary>
        internal TcpClientService ScbTcpClientService { get; set; }

        #endregion

        #region [ADS1000 Service Properties]

        /// <summary>
        /// [ADS1000] 장비 [TCP] 연결 서비스
        /// </summary>
        internal Ads1000ConnectionService Ads1000ConnectionService { get; set; }

        /// <summary>
        /// [ADS1000] [Camera] 제어 서비스
        /// </summary>
        internal Ads1000CameraControlService Ads1000CameraControlService { get; set; }

        /// <summary>
        /// [ADS1000] 상태 [Packet] 처리 서비스
        /// </summary>
        internal Ads1000StatusService Ads1000StatusService { get; set; }

        #endregion

        #region [State Provider Properties]

        /// <summary>
        /// [Camera] 상태 저장 서비스
        /// </summary>
        internal CameraStateProvider CameraStateProvider { get; set; }

        /// <summary>
        /// [Detection] 상태 저장 서비스
        /// </summary>
        internal DetectionStateProvider DetectionStateProvider { get; set; }

        /// <summary>
        /// [Radar] 상태 저장 서비스
        /// </summary>
        internal RadarStateProvider RadarStateProvider { get; set; }

        #endregion

        #region [Camera Service Properties]

        /// <summary>
        /// [EO] [Camera] 영상 서비스
        /// </summary>
        internal EoCameraService EoCameraService { get; set; }

        /// <summary>
        /// [Camera] 내부 명령 처리 서비스
        /// </summary>
        internal CameraCommandService CameraCommandService { get; set; }

        /// <summary>
        /// [Tracking] 자동 추적 제어 서비스
        /// </summary>
        internal TrackingControlService TrackingControlService { get; set; }

        #endregion

        #region [MQ / CSE Service Properties]

        /// <summary>
        /// [MQ] 수신 서비스
        /// </summary>
        internal IMqReceiver MqReceiver { get; set; }

        /// <summary>
        /// [MQ] 송신 서비스
        /// </summary>
        internal IMqSender MqSender { get; set; }

        /// <summary>
        /// [CSE] 명령 수신 서비스
        /// </summary>
        internal CseCommandReceiveService CseCommandReceiveService { get; set; }

        /// <summary>
        /// [CSE] 명령 응답 송신 서비스
        /// </summary>
        internal CseCommandResponseService CseCommandResponseService { get; set; }

        /// <summary>
        /// [CSE] 명령 처리 서비스
        /// </summary>
        internal CseCommandHandler CseCommandHandler { get; set; }

        #endregion

        #region [Radar Service Properties]

        /// <summary>
        /// [Radar] UDP 통신 서비스
        /// </summary>
        internal UdpClientService RadarUdpClientService { get; set; }

        /// <summary>
        /// [Radar] Packet Parser
        /// </summary>
        internal RadarPacketParser RadarPacketParser { get; set; }

        /// <summary>
        /// [Radar] Packet Builder
        /// </summary>
        internal RadarPacketBuilder RadarPacketBuilder { get; set; }

        /// <summary>
        /// [Radar] 추적 제어 서비스
        /// </summary>
        internal RadarTrackingControlService RadarTrackingControlService { get; set; }

        /// <summary>
        /// [Radar] Command 처리 서비스
        /// </summary>
        internal RadarCommandHandler RadarCommandHandler { get; set; }

        /// <summary>
        /// [Radar] UDP 연동 서비스
        /// </summary>
        internal RadarUdpService RadarUdpService { get; set; }

        #endregion

        #region [Communication Controller Properties]

        /// <summary>
        /// [RabbitMQ] 수신 Controller
        /// </summary>
        internal RabbitMqController RabbitMqController { get; set; }

        /// <summary>
        /// [Radar] UDP 수신 Controller
        /// </summary>
        internal RadarUdpController RadarUdpController { get; set; }

        #endregion

        #region [Connection Controller Properties]

        /// <summary>
        /// [Device Connection] Controller
        /// </summary>
        internal DeviceConnectionController DeviceConnectionController { get; set; }

        #endregion

        #region [Camera Controller Properties]

        /// <summary>
        /// [EO Camera] Controller
        /// </summary>
        internal EoCameraController EoCameraController { get; set; }

        /// <summary>
        /// [ADS1000 Receive] Controller
        /// </summary>
        internal Ads1000ReceiveController Ads1000ReceiveController { get; set; }

        /// <summary>
        /// [ADS1000 Status Apply] Controller
        /// </summary>
        internal Ads1000StatusApplyController Ads1000StatusApplyController { get; set; }

        #endregion

        #region [PTZ Controller Properties]

        /// <summary>
        /// [PTZ Absolute] Controller
        /// </summary>
        internal PtzAbsoluteController PtzAbsoluteController { get; set; }

        /// <summary>
        /// [PTZ Relative] Controller
        /// </summary>
        internal PtzRelativeController PtzRelativeController { get; set; }

        /// <summary>
        /// [PTZ Continuous] Controller
        /// </summary>
        internal PtzContinuousController PtzContinuousController { get; set; }

        /// <summary>
        /// [PTZ Home / Zero] Controller
        /// </summary>
        internal PtzHomeZeroController PtzHomeZeroController { get; set; }

        /// <summary>
        /// [Keyboard PTZ] Controller
        /// </summary>
        internal KeyboardPtzController KeyboardPtzController { get; set; }

        /// <summary>
        /// [Zoom / Focus Position] Controller
        /// </summary>
        internal ZoomFocusPositionController ZoomFocusPositionController { get; set; }

        /// <summary>
        /// [PTZ Mode] Controller
        /// </summary>
        internal PtzModeController PtzModeController { get; set; }

        #endregion
    }

}

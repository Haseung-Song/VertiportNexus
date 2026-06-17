using System;
using VertiportNexus.Models.ADS1000;
using VertiportNexus.Services.Communication.TCP;

namespace VertiportNexus.Services.ADS1000
{
    /// <summary>
    /// [ADS1000] [Camera] 제어 서비스
    /// 
    /// [MCB] [Pan] / [Tilt] 제어와
    /// [SCB] [Zoom] / [Focus] 제어 [Packet] 송신을 담당한다.
    /// 
    /// [MainViewModel]은 [Command] 연결과 화면 상태 갱신만 처리하고,
    /// 실제 [Packet] 생성 및 송신은 본 서비스에서 수행한다.
    /// </summary>
    internal class Ads1000CameraControlService
    {
        #region [Enum Type]

        /// <summary>
        /// 현재 진행 중인 [연속 제어] 종류
        /// </summary>
        private enum ContinuousMoveType
        {
            None,
            PanTilt,
            Zoom,
            Focus
        }

        #endregion

        #region [Constants]

        /// <summary>
        /// [Pan] / [Tilt] 기본 각속도
        /// </summary>
        private const double DEFAULT_PAN_TILT_SPEED = 50;

        #endregion

        #region [Fields]

        /// <summary>
        /// [MCB] [TCP] 통신 서비스
        /// </summary>
        private readonly TcpClientService _mcbTcpClientService;

        /// <summary>
        /// [SCB] [TCP] 통신 서비스
        /// </summary>
        private readonly TcpClientService _scbTcpClientService;

        /// <summary>
        /// [MCB] [Pan] / [Tilt] [Packet] 생성 객체
        /// </summary>
        private readonly Ads1000McbPacketBuilder _mcbPacketBuilder;

        /// <summary>
        /// [SCB] [Zoom] / [Focus] [Packet] 생성 객체
        /// </summary>
        private readonly Ads1000ScbPacketBuilder _scbPacketBuilder;

        /// <summary>
        /// 현재 어떤 [연속 제어]가 동작 중인지
        /// 
        /// 현재 [XAML] 바인딩에는 사용하지 않지만,
        /// [MouseDown] / [MouseUp] 기반 제어 상태를 내부적으로 구분하기 위해 유지한다.
        /// </summary>
        private ContinuousMoveType _currentMoveType =
            ContinuousMoveType.None;

        #endregion

        #region [Events]

        /// <summary>
        /// [Packet] 송신 결과 이벤트
        /// 
        /// [MainViewModel]의 송신 [Packet] 표시와 상태 표시 갱신에 사용한다.
        /// </summary>
        public event Action<Ads1000SendResult> SendResultChanged;

        #endregion

        #region [Properties]

        /// <summary>
        /// [PAN / TILT] 현재 속도
        /// </summary>
        public double PanTiltSpeedLevel =>
            DEFAULT_PAN_TILT_SPEED;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [Ads1000CameraControlService] 생성자
        /// </summary>
        /// <param name="mcbTcpClientService">
        /// [MCB] [TCP] 통신 서비스
        /// </param>
        /// <param name="scbTcpClientService">
        /// [SCB] [TCP] 통신 서비스
        /// </param>
        /// <param name="mcbPacketBuilder">
        /// [MCB] [Packet Builder]
        /// </param>
        /// <param name="scbPacketBuilder">
        /// [SCB] [Packet Builder]
        /// </param>
        public Ads1000CameraControlService(
            TcpClientService mcbTcpClientService,
            TcpClientService scbTcpClientService,
            Ads1000McbPacketBuilder mcbPacketBuilder,
            Ads1000ScbPacketBuilder scbPacketBuilder)
        {
            _mcbTcpClientService =
                mcbTcpClientService;

            _scbTcpClientService =
                scbTcpClientService;

            _mcbPacketBuilder =
                mcbPacketBuilder;

            _scbPacketBuilder =
                scbPacketBuilder;
        }

        #endregion

        #region [Pan / Tilt Methods]

        /// <summary>
        /// [Pan] 왼쪽 이동
        /// </summary>
        public void PanLeft()
        {
            SetContinuousMoveType(
                ContinuousMoveType.PanTilt);

            SendMcbPacket(
                _mcbPacketBuilder.BuildPanSpeedPacket(
                    DEFAULT_PAN_TILT_SPEED),
                "Pan Left");
        }

        /// <summary>
        /// [Pan] 오른쪽 이동
        /// </summary>
        public void PanRight()
        {
            SetContinuousMoveType(
                ContinuousMoveType.PanTilt);

            SendMcbPacket(
                _mcbPacketBuilder.BuildPanSpeedPacket(
                    -DEFAULT_PAN_TILT_SPEED),
                "Pan Right");
        }

        /// <summary>
        /// [Tilt] 위쪽 이동
        /// </summary>
        public void TiltUp()
        {
            SetContinuousMoveType(
                ContinuousMoveType.PanTilt);

            SendMcbPacket(
                _mcbPacketBuilder.BuildTiltSpeedPacket(
                    DEFAULT_PAN_TILT_SPEED),
                "Tilt Up");
        }

        /// <summary>
        /// [Tilt] 아래쪽 이동
        /// </summary>
        public void TiltDown()
        {
            SetContinuousMoveType(
                ContinuousMoveType.PanTilt);

            SendMcbPacket(
                _mcbPacketBuilder.BuildTiltSpeedPacket(
                    -DEFAULT_PAN_TILT_SPEED),
                "Tilt Down");
        }

        #endregion

        #region [Zoom Methods]

        /// <summary>
        /// [Zoom] 확대
        /// </summary>
        public void ZoomIn()
        {
            SetContinuousMoveType(
                ContinuousMoveType.Zoom);

            SendScbPacket(
                _scbPacketBuilder.BuildZoomTelePacket(),
                "Zoom In");
        }

        /// <summary>
        /// [Zoom] 축소
        /// </summary>
        public void ZoomOut()
        {
            SetContinuousMoveType(
                ContinuousMoveType.Zoom);

            SendScbPacket(
                _scbPacketBuilder.BuildZoomWidePacket(),
                "Zoom Out");
        }

        #endregion

        #region [Focus Methods]

        /// <summary>
        /// [Focus] Near
        /// </summary>
        public void FocusNear()
        {
            SetContinuousMoveType(
                ContinuousMoveType.Focus);

            SendScbPacket(
                _scbPacketBuilder.BuildFocusNearPacket(),
                "Focus Near");
        }

        /// <summary>
        /// [Focus] Far
        /// </summary>
        public void FocusFar()
        {
            SetContinuousMoveType(
                ContinuousMoveType.Focus);

            SendScbPacket(
                _scbPacketBuilder.BuildFocusFarPacket(),
                "Focus Far");
        }

        /// <summary>
        /// [Auto Focus] 실행
        /// </summary>
        public void AutoFocus()
        {
            SendScbPacket(
                _scbPacketBuilder.BuildAutoFocusPacket(),
                "Auto Focus");
        }

        #endregion

        #region [Stop / Request Methods]

        /// <summary>
        /// [Pan] / [Tilt] / [Zoom] / [Focus] 정지
        /// </summary>
        public void StopMove()
        {
            /// <summary>
            /// 연결되지 않은 상태에서
            /// [Stop Packet] 송신 시,
            /// 불필요한 [Send Failed] 로그가 발생하므로
            /// 연결 여부를 먼저 확인한다.
            /// </summary>
            if (!_mcbTcpClientService.IsConnected &&
                !_scbTcpClientService.IsConnected)
            {
                return;
            }

            SendMcbPacket(
                _mcbPacketBuilder.BuildPanStopPacket(),
                "Pan Stop");

            SendMcbPacket(
                _mcbPacketBuilder.BuildTiltStopPacket(),
                "Tilt Stop");

            SendScbPacket(
                _scbPacketBuilder.BuildZoomStopPacket(),
                "Zoom Stop");

            SendScbPacket(
                _scbPacketBuilder.BuildFocusStopPacket(),
                "Focus Stop");

            SetContinuousMoveType(
                ContinuousMoveType.None);

            SendResultChanged?.Invoke(
                new Ads1000SendResult(
                    string.Empty,
                    null,
                    "Stop Move",
                    true));
        }

        /// <summary>
        /// [SCB] 펌웨어 버전 조회
        /// </summary>
        public void SendVersionQuery()
        {
            SendScbPacket(
                _scbPacketBuilder.BuildVersionQueryPacket(),
                "SCB Version Query");
        }

        #endregion

        #region [Private Methods]

        /// <summary>
        /// [MCB] [Packet] 송신
        /// </summary>
        /// <param name="packet">
        /// 송신 [Packet]
        /// </param>
        /// <param name="commandName">
        /// 제어 명령 이름
        /// </param>
        private void SendMcbPacket(
            byte[] packet,
            string commandName)
        {
            bool result =
                _mcbTcpClientService.Send(
                    packet);

            PublishSendResult(
                "MCB",
                packet,
                commandName,
                result);
        }

        /// <summary>
        /// [SCB] [Packet] 송신
        /// </summary>
        /// <param name="packet">
        /// 송신 [Packet]
        /// </param>
        /// <param name="commandName">
        /// 제어 명령 이름
        /// </param>
        private void SendScbPacket(
            byte[] packet,
            string commandName)
        {
            bool result =
                _scbTcpClientService.Send(
                    packet);

            PublishSendResult(
                "SCB",
                packet,
                commandName,
                result);
        }

        /// <summary>
        /// [Packet] 송신 결과 이벤트 전달
        /// </summary>
        /// <param name="deviceName">
        /// 송신 장비 이름
        /// </param>
        /// <param name="packet">
        /// 송신 [Packet]
        /// </param>
        /// <param name="commandName">
        /// 제어 명령 이름
        /// </param>
        /// <param name="isSuccess">
        /// 송신 성공 여부
        /// </param>
        private void PublishSendResult(
            string deviceName,
            byte[] packet,
            string commandName,
            bool isSuccess)
        {
            SendResultChanged?.Invoke(
                new Ads1000SendResult(
                    deviceName,
                    packet,
                    commandName,
                    isSuccess));
        }

        /// <summary>
        /// [연속 제어] 종류 반영
        /// 
        /// 현재 [XAML] 바인딩에는 사용하지 않지만,
        /// 내부적으로 마지막 제어 동작 상태를 관리한다.
        /// </summary>
        /// <param name="moveType">
        /// 현재 진행 중인 [연속 제어] 종류
        /// </param>
        private void SetContinuousMoveType(
            ContinuousMoveType moveType)
        {
            _currentMoveType =
                moveType;
        }
        #endregion
    }

}

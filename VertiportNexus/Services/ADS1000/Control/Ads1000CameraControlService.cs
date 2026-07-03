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
        #region [Constants]

        /// <summary>
        /// [Pan] / [Tilt] 연속 이동 기본 속도
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
        /// [Pan] / [Tilt] 현재 제어 속도
        /// </summary>
        public double PanTiltSpeedLevel
        {
            get;
            set;
        }

        #endregion

        #region [Constructor]

        /// <summary>
        /// [Ads1000CameraControlService] 생성자
        /// </summary>
        public Ads1000CameraControlService(
            TcpClientService mcbTcpClientService,
            TcpClientService scbTcpClientService,
            Ads1000McbPacketBuilder mcbPacketBuilder,
            Ads1000ScbPacketBuilder scbPacketBuilder)
        {
            _mcbTcpClientService =
                mcbTcpClientService
                ?? throw new ArgumentNullException(
                    nameof(mcbTcpClientService));

            _scbTcpClientService =
                scbTcpClientService
                ?? throw new ArgumentNullException(
                    nameof(scbTcpClientService));

            _mcbPacketBuilder =
                mcbPacketBuilder
                ?? throw new ArgumentNullException(
                    nameof(mcbPacketBuilder));

            _scbPacketBuilder =
                scbPacketBuilder
                ?? throw new ArgumentNullException(
                    nameof(scbPacketBuilder));

            PanTiltSpeedLevel =
                DEFAULT_PAN_TILT_SPEED;
        }

        #endregion

        #region [Pan / Tilt Continuous Methods]

        /// <summary>
        /// [Pan] 왼쪽 연속 이동
        /// </summary>
        public void PanLeft()
        {
            SendMcbPacket(
                _mcbPacketBuilder.BuildPanSpeedPacket(
                    PanTiltSpeedLevel),
                "Pan Left");
        }

        /// <summary>
        /// [Pan] 오른쪽 연속 이동
        /// </summary>
        public void PanRight()
        {
            SendMcbPacket(
                _mcbPacketBuilder.BuildPanSpeedPacket(
                    -PanTiltSpeedLevel),
                "Pan Right");
        }

        /// <summary>
        /// [Tilt] 위쪽 연속 이동
        /// </summary>
        public void TiltUp()
        {
            SendMcbPacket(
                _mcbPacketBuilder.BuildTiltSpeedPacket(
                    PanTiltSpeedLevel),
                "Tilt Up");
        }

        /// <summary>
        /// [Tilt] 아래쪽 연속 이동
        /// </summary>
        public void TiltDown()
        {
            SendMcbPacket(
                _mcbPacketBuilder.BuildTiltSpeedPacket(
                    -PanTiltSpeedLevel),
                "Tilt Down");
        }

        #endregion

        #region [Pan / Tilt Position Methods]

        /// <summary>
        /// [Pan] 절대 위치 이동
        /// 
        /// 현재 설정된 [PT Speed] 값을 함께 적용하여
        /// 지정한 [Pan] 각도로 이동한다.
        /// </summary>
        /// <param name="angle">
        /// 이동 대상 [Pan] 각도
        /// </param>
        public void MovePanAbsolute(
            double angle)
        {
            SendMcbPacket(
                _mcbPacketBuilder.BuildPanAbsolutePositionPacket(
                    angle,
                    PanTiltSpeedLevel),
                "Pan Absolute");
        }

        /// <summary>
        /// [Tilt] 절대 위치 이동
        /// 
        /// 현재 설정된 [PT Speed] 값을 함께 적용하여
        /// 지정한 [Tilt] 각도로 이동한다.
        /// </summary>
        /// <param name="angle">
        /// 이동 대상 [Tilt] 각도
        /// </param>
        public void MoveTiltAbsolute(
            double angle)
        {
            SendMcbPacket(
                _mcbPacketBuilder.BuildTiltAbsolutePositionPacket(
                    angle,
                    PanTiltSpeedLevel),
                "Tilt Absolute");
        }

        /// <summary>
        /// [Pan] 상대 위치 이동
        /// 
        /// 현재 설정된 [PT Speed] 값을 함께 적용하여
        /// 현재 위치 기준으로 [Pan] 상대 이동을 수행한다.
        /// </summary>
        /// <param name="angle">
        /// 상대 이동 [Pan] 각도
        /// </param>
        public void MovePanRelative(
            double angle)
        {
            SendMcbPacket(
                _mcbPacketBuilder.BuildPanRelativePositionPacket(
                    angle,
                    PanTiltSpeedLevel),
                "Pan Relative");
        }

        /// <summary>
        /// [Tilt] 상대 위치 이동
        /// 
        /// 현재 설정된 [PT Speed] 값을 함께 적용하여
        /// 현재 위치 기준으로 [Tilt] 상대 이동을 수행한다.
        /// </summary>
        /// <param name="angle">
        /// 상대 이동 [Tilt] 각도
        /// </param>
        public void MoveTiltRelative(
            double angle)
        {
            SendMcbPacket(
                _mcbPacketBuilder.BuildTiltRelativePositionPacket(
                    angle,
                    PanTiltSpeedLevel),
                "Tilt Relative");
        }

        /// <summary>
        /// [Pan] 이동 중 속도 갱신
        /// 
        /// 현재 설정된 [Pan / Tilt Speed] 값을 기준으로
        /// [Pan] 축 위치 이동 중 속도 갱신 명령을 송신한다.
        /// </summary>
        /// <param name="includeBeginCommand">
        /// [BG] 명령 포함 여부
        /// </param>
        public void UpdatePanMoveSpeed(
            bool includeBeginCommand)
        {
            byte[] packet =
                _mcbPacketBuilder
                    .BuildPanMoveSpeedPacket(
                        PanTiltSpeedLevel,
                        includeBeginCommand);

            _mcbTcpClientService
                .Send(
                    packet);
        }

        /// <summary>
        /// [Tilt] 이동 중 속도 갱신
        /// 
        /// 현재 설정된 [Pan / Tilt Speed] 값을 기준으로
        /// [Tilt] 축 위치 이동 중 속도 갱신 명령을 송신한다.
        /// </summary>
        /// <param name="includeBeginCommand">
        /// [BG] 명령 포함 여부
        /// </param>
        public void UpdateTiltMoveSpeed(
            bool includeBeginCommand)
        {
            byte[] packet =
                _mcbPacketBuilder
                    .BuildTiltMoveSpeedPacket(
                        PanTiltSpeedLevel,
                        includeBeginCommand);

            _mcbTcpClientService
                .Send(
                    packet);
        }

        /// <summary>
        /// [Pan] 현재 위치 [0] 설정
        /// 
        /// 이전 Set Zero 방식 호환용 메서드이다.
        /// 현재 UI에서는 각도값을 전달하는 Offset 저장 방식을 사용한다.
        /// </summary>
        public void SetPanZero()
        {
            SendMcbPacket(
                _mcbPacketBuilder.BuildPanSetZeroPacket(),
                "Pan Set Zero");
        }

        /// <summary>
        /// [Tilt] 현재 위치 [0] 설정
        /// 
        /// 이전 Set Zero 방식 호환용 메서드이다.
        /// 현재 UI에서는 각도값을 전달하는 Offset 저장 방식을 사용한다.
        /// </summary>
        public void SetTiltZero()
        {
            SendMcbPacket(
                _mcbPacketBuilder.BuildTiltSetZeroPacket(),
                "Tilt Set Zero");
        }

        /// <summary>
        /// [Pan] 현재 위치를 장비 Script 기준 [0] 위치로 저장
        /// 
        /// 현재 [Pan] 각도값을 장비 Script 내부 Offset 값으로 저장하여,
        /// 이후 장비 상태값이 해당 위치를 기준으로 보정되도록 한다.
        /// </summary>
        /// <param name="currentPan">
        /// 현재 Pan 각도
        /// </param>
        public void SetPanZero(
            double currentPan)
        {
            SendMcbPacket(
                _mcbPacketBuilder.BuildPanHomeOffsetPacket(
                    currentPan),
                "Pan Zero Offset");
        }

        /// <summary>
        /// [Tilt] 현재 위치를 장비 Script 기준 [0] 위치로 저장
        /// 
        /// 현재 [Tilt] 각도값을 장비 Script 내부 Offset 값으로 저장하여,
        /// 이후 장비 상태값이 해당 위치를 기준으로 보정되도록 한다.
        /// </summary>
        /// <param name="currentTilt">
        /// 현재 Tilt 각도
        /// </param>
        public void SetTiltZero(
            double currentTilt)
        {
            SendMcbPacket(
                _mcbPacketBuilder.BuildTiltHomeOffsetPacket(
                    currentTilt),
                "Tilt Zero Offset");
        }

        /// <summary>
        /// [Pan] Home Position 이동
        /// 
        /// 문서 기준 [Pan Home] 명령을 송신하여
        /// 장비 기준 Pan Home 위치로 이동한다.
        /// 
        /// [Home]은 현재 위치를 [0]으로 재설정하는 [Set0]가 아니라,
        /// 장비 기준 Home 위치로 이동하는 기능이다.
        /// </summary>
        public void MovePanHome()
        {
            SendMcbPacket(
                _mcbPacketBuilder.BuildPanHomePacket(),
                "Pan Home");
        }

        /// <summary>
        /// [Tilt] Home Position 이동
        /// 
        /// 문서 기준 [Tilt Home] 명령을 송신하여
        /// 장비 기준 Tilt Home 위치로 이동한다.
        /// 
        /// [Home]은 현재 위치를 [0]으로 재설정하는 [Set0]가 아니라,
        /// 장비 기준 Home 위치로 이동하는 기능이다.
        /// </summary>
        public void MoveTiltHome()
        {
            SendMcbPacket(
                _mcbPacketBuilder.BuildTiltHomePacket(),
                "Tilt Home");
        }

        /// <summary>
        /// [Home Position] 이동
        /// 
        /// 문서 기준 [Pan Home] / [Tilt Home] 명령을 순차 송신하여
        /// 장비 기준 Home Position으로 이동한다.
        /// 
        /// 기존 [Pan Absolute 0] / [Tilt Absolute 0] 이동 방식은
        /// 장비 누적 회전 상태에 따라 불필요하게 여러 바퀴 회전할 수 있으므로 사용하지 않는다.
        /// </summary>
        public void MoveHomePosition()
        {
            MovePanHome();

            MoveTiltHome();
        }

        #endregion

        #region [Zoom Continuous Methods]

        /// <summary>
        /// [Zoom] 확대
        /// </summary>
        public void ZoomIn()
        {
            SendScbPacket(
                _scbPacketBuilder.BuildZoomTelePacket(),
                "Zoom In");
        }

        /// <summary>
        /// [Zoom] 축소
        /// </summary>
        public void ZoomOut()
        {
            SendScbPacket(
                _scbPacketBuilder.BuildZoomWidePacket(),
                "Zoom Out");
        }

        #endregion

        #region [Zoom Position Methods]

        /// <summary>
        /// [Zoom] 위치 이동
        /// 
        /// [Zoom] 값을 [0 ~ 1000] 범위로 지정하여
        /// 해당 위치로 이동한다.
        /// </summary>
        public void MoveZoomPosition(
            ushort zoomValue)
        {
            SendScbPacket(
                _scbPacketBuilder.BuildZoomPositionPacket(
                    zoomValue),
                "Zoom Position");
        }

        #endregion

        #region [Focus Continuous Methods]

        /// <summary>
        /// [Focus] Near
        /// </summary>
        public void FocusNear()
        {
            SendScbPacket(
                _scbPacketBuilder.BuildFocusNearPacket(),
                "Focus Near");
        }

        /// <summary>
        /// [Focus] Far
        /// </summary>
        public void FocusFar()
        {
            SendScbPacket(
                _scbPacketBuilder.BuildFocusFarPacket(),
                "Focus Far");
        }

        #endregion

        #region [Focus Position Methods]

        /// <summary>
        /// [Focus] 위치 이동
        /// 
        /// [Focus] 값을 [0 ~ 1000] 범위로 지정하여
        /// 해당 위치로 이동한다.
        /// </summary>
        public void MoveFocusPosition(
            ushort focusValue)
        {
            SendScbPacket(
                _scbPacketBuilder.BuildFocusPositionPacket(
                    focusValue),
                "Focus Position");
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
        /// [Pan] / [Tilt] 이동 정지
        /// 
        /// [MCB]에만 정지 명령을 송신한다.
        /// [AUTO Tracking] 정지 시 사용하며,
        /// [SCB] [Zoom] / [Focus] 정지 명령은 송신하지 않는다.
        /// </summary>
        public void StopPanTiltMove()
        {
            SendMcbPacket(
                _mcbPacketBuilder.BuildPanStopPacket(),
                "Pan Stop");

            SendMcbPacket(
                _mcbPacketBuilder.BuildTiltStopPacket(),
                "Tilt Stop");
        }

        /// <summary>
        /// [Pan] 이동 정지
        /// 
        /// 대각선 이동 또는 키보드 방향키 제어 중
        /// Pan 축만 정지해야 하는 경우 사용한다.
        /// </summary>
        public void StopPanMove()
        {
            SendMcbPacket(
                _mcbPacketBuilder.BuildPanStopPacket(),
                "Pan Stop");
        }

        /// <summary>
        /// [Tilt] 이동 정지
        /// 
        /// 대각선 이동 또는 키보드 방향키 제어 중
        /// Tilt 축만 정지해야 하는 경우 사용한다.
        /// </summary>
        public void StopTiltMove()
        {
            SendMcbPacket(
                _mcbPacketBuilder.BuildTiltStopPacket(),
                "Tilt Stop");
        }

        /// <summary>
        /// [Pan] / [Tilt] / [Zoom] / [Focus] 모두 정지
        /// 
        /// 연결되지 않은 상태에서 [Stop Packet] 송신 시
        /// 불필요한 [Send Failed] 로그가 발생하므로
        /// [MCB] / [SCB] 연결 여부를 먼저 확인한다.
        /// </summary>
        public void StopMove()
        {
            if (!_mcbTcpClientService.IsConnected &&
                !_scbTcpClientService.IsConnected)
            {
                return;
            }

            if (_mcbTcpClientService.IsConnected)
            {
                SendMcbPacket(
                    _mcbPacketBuilder.BuildPanStopPacket(),
                    "Pan Stop");

                SendMcbPacket(
                    _mcbPacketBuilder.BuildTiltStopPacket(),
                    "Tilt Stop");
            }

            if (_scbTcpClientService.IsConnected)
            {
                SendScbPacket(
                    _scbPacketBuilder.BuildZoomStopPacket(),
                    "Zoom Stop");

                SendScbPacket(
                    _scbPacketBuilder.BuildFocusStopPacket(),
                    "Focus Stop");
            }
            PublishSendResult(
                string.Empty,
                null,
                "Stop Move",
                true);
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

        #region [Packet Send Methods]

        /// <summary>
        /// [MCB] [Packet] 송신
        /// </summary>
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
        #endregion
    }

}

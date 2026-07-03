using System;
using VertiportNexus.Common;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [Main] 화면 [ViewModel] - Continuous PTZ Control
    /// Pan / Tilt / Zoom / Focus 연속 이동 및 축별 정지 로직을 관리한다.
    /// </summary>
    public partial class MainViewModel
    {
        #region [Camera Continuous Control Methods]

        /// <summary>
        /// [Pan / Tilt] 연속 이동 속도 재적용
        /// 
        /// ADS1000 장비는 이동 중에도 [JV] 속도 명령을 다시 수신하면
        /// 현재 이동 속도를 갱신할 수 있으므로,
        /// 별도 정지 명령 없이 현재 이동 중인 Pan / Tilt 방향 명령을 재송신한다.
        /// 
        /// 대각선 이동 중에는 Pan / Tilt 두 축에 모두 변경된 속도를 반영한다.
        /// </summary>
        private void ApplyCurrentPanTiltContinuousMoveSpeed()
        {
            if (!_isUiContinuousMoveStarted)
            {
                return;
            }

            if (!_isPanContinuousMoving &&
                !_isTiltContinuousMoving)
            {
                return;
            }

            Console.WriteLine(
                "[UI][PTZ] Pan / Tilt Continuous Speed Changed : "
                + PanTiltSpeedLevel.ToString("F0"));

            switch (_currentPanContinuousMoveDirection)
            {
                case PanTiltContinuousMoveDirection.PanLeft:
                    _ads1000CameraControlService
                        .PanLeft();
                    break;

                case PanTiltContinuousMoveDirection.PanRight:
                    _ads1000CameraControlService
                        .PanRight();
                    break;

                default:
                    break;
            }

            switch (_currentTiltContinuousMoveDirection)
            {
                case PanTiltContinuousMoveDirection.TiltUp:
                    _ads1000CameraControlService
                        .TiltUp();
                    break;

                case PanTiltContinuousMoveDirection.TiltDown:
                    _ads1000CameraControlService
                        .TiltDown();
                    break;

                default:
                    break;
            }

        }

        /// <summary>
        /// [Pan / Tilt] 이동 속도 재적용
        /// 
        /// UI에서 Pan / Tilt 이동 중 [Pan / Tilt Speed] 값이 변경된 경우,
        /// 현재 이동 중인 축에 속도 갱신 명령을 송신하여
        /// 장비 실제 이동 속도에 변경값을 반영한다.
        /// 
        /// Absolute 이동은 [SP=속도;BG;] 형식으로 속도 변경을 반영하고,
        /// Relative 이동은 기존 [PR] 상대 이동량이 다시 실행되지 않도록
        /// [SP=속도;] 형식으로만 송신한다.
        /// </summary>
        private void ApplyCurrentPanTiltMoveSpeed()
        {
            if (_isHomePositionMoving)
            {
                Console.WriteLine(
                    "[UI][PTZ] Pan / Tilt Speed Apply Ignored : Home Position Moving");

                return;
            }

            if (_currentPanTiltMoveType == PanTiltMoveType.Continuous)
            {
                ApplyCurrentPanTiltContinuousMoveSpeed();

                return;
            }

            if (_currentPanTiltMoveAxis == PanTiltMoveAxis.None ||
                _currentPanTiltMoveType == PanTiltMoveType.None)
            {
                Console.WriteLine(
                    "[UI][PTZ] Pan / Tilt Speed Apply Ignored : Pan / Tilt Move State None");

                return;
            }

            bool includeBeginCommand =
                _currentPanTiltMoveType == PanTiltMoveType.Absolute;

            Console.WriteLine(
                "[UI][PTZ] Pan / Tilt Speed Apply : "
                + PanTiltSpeedLevel.ToString("F0")
                + " / "
                + _currentPanTiltMoveAxis
                + " / "
                + _currentPanTiltMoveType
                + " / BG="
                + includeBeginCommand);

            switch (_currentPanTiltMoveAxis)
            {
                case PanTiltMoveAxis.Pan:
                    _ads1000CameraControlService
                        .UpdatePanMoveSpeed(
                            includeBeginCommand);

                    break;

                case PanTiltMoveAxis.Tilt:
                    _ads1000CameraControlService
                        .UpdateTiltMoveSpeed(
                            includeBeginCommand);

                    break;

                default:
                    break;
            }

        }

        /// <summary>
        /// [Pan] 왼쪽 연속 이동 시작
        /// 
        /// 화면 버튼 [MouseDown] 또는 키보드 방향키 입력 시
        /// [Pan] 왼쪽 연속 이동 명령을 송신한다.
        /// 
        /// 이미 동일 방향으로 이동 중인 경우에는
        /// 키 반복 입력에 의한 중복 Packet 송신을 방지한다.
        /// </summary>
        public void StartPanLeftMove()
        {
            if (_isPanContinuousMoving &&
                _currentPanContinuousMoveDirection == PanTiltContinuousMoveDirection.PanLeft)
            {
                return;
            }

            _isUiContinuousMoveStarted =
                true;

            _isPanContinuousMoving =
                true;

            _currentPanContinuousMoveDirection =
                PanTiltContinuousMoveDirection.PanLeft;

            _currentPanTiltContinuousMoveDirection =
                PanTiltContinuousMoveDirection.PanLeft;

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.Pan;

            _currentPanTiltMoveType =
                PanTiltMoveType.Continuous;

            _ads1000CameraControlService
                .PanLeft();
        }

        /// <summary>
        /// [Pan] 오른쪽 연속 이동 시작
        /// 
        /// 화면 버튼 [MouseDown] 또는 키보드 방향키 입력 시
        /// [Pan] 오른쪽 연속 이동 명령을 송신한다.
        /// 
        /// 이미 동일 방향으로 이동 중인 경우에는
        /// 키 반복 입력에 의한 중복 Packet 송신을 방지한다.
        /// </summary>
        public void StartPanRightMove()
        {
            if (_isPanContinuousMoving &&
                _currentPanContinuousMoveDirection == PanTiltContinuousMoveDirection.PanRight)
            {
                return;
            }

            _isUiContinuousMoveStarted =
                true;

            _isPanContinuousMoving =
                true;

            _currentPanContinuousMoveDirection =
                PanTiltContinuousMoveDirection.PanRight;

            _currentPanTiltContinuousMoveDirection =
                PanTiltContinuousMoveDirection.PanRight;

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.Pan;

            _currentPanTiltMoveType =
                PanTiltMoveType.Continuous;

            _ads1000CameraControlService
                .PanRight();
        }

        /// <summary>
        /// [Tilt] 위쪽 연속 이동 시작
        /// 
        /// 화면 버튼 [MouseDown] 또는 키보드 방향키 입력 시
        /// [Tilt] 위쪽 연속 이동 명령을 송신한다.
        /// 
        /// 이미 동일 방향으로 이동 중인 경우에는
        /// 키 반복 입력에 의한 중복 Packet 송신을 방지한다.
        /// </summary>
        public void StartTiltUpMove()
        {
            if (_isTiltContinuousMoving &&
                _currentTiltContinuousMoveDirection == PanTiltContinuousMoveDirection.TiltUp)
            {
                return;
            }

            _isUiContinuousMoveStarted =
                true;

            _isTiltContinuousMoving =
                true;

            _currentTiltContinuousMoveDirection =
                PanTiltContinuousMoveDirection.TiltUp;

            _currentPanTiltContinuousMoveDirection =
                PanTiltContinuousMoveDirection.TiltUp;

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.Tilt;

            _currentPanTiltMoveType =
                PanTiltMoveType.Continuous;

            _ads1000CameraControlService
                .TiltUp();
        }

        /// <summary>
        /// [Tilt] 아래쪽 연속 이동 시작
        /// 
        /// 화면 버튼 [MouseDown] 또는 키보드 방향키 입력 시
        /// [Tilt] 아래쪽 연속 이동 명령을 송신한다.
        /// 
        /// 이미 동일 방향으로 이동 중인 경우에는
        /// 키 반복 입력에 의한 중복 Packet 송신을 방지한다.
        /// </summary>
        public void StartTiltDownMove()
        {
            if (_isTiltContinuousMoving &&
                _currentTiltContinuousMoveDirection == PanTiltContinuousMoveDirection.TiltDown)
            {
                return;
            }

            _isUiContinuousMoveStarted =
                true;

            _isTiltContinuousMoving =
                true;

            _currentTiltContinuousMoveDirection =
                PanTiltContinuousMoveDirection.TiltDown;

            _currentPanTiltContinuousMoveDirection =
                PanTiltContinuousMoveDirection.TiltDown;

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.Tilt;

            _currentPanTiltMoveType =
                PanTiltMoveType.Continuous;

            _ads1000CameraControlService
                .TiltDown();
        }

        /// <summary>
        /// [Pan Left] / [Tilt Up] 대각선 연속 이동 시작
        /// </summary>
        public void StartPanLeftTiltUpMove()
        {
            StartPanLeftMove();

            StartTiltUpMove();
        }

        /// <summary>
        /// [Pan Right] / [Tilt Up] 대각선 연속 이동 시작
        /// </summary>
        public void StartPanRightTiltUpMove()
        {
            StartPanRightMove();

            StartTiltUpMove();
        }

        /// <summary>
        /// [Pan Left] / [Tilt Down] 대각선 연속 이동 시작
        /// </summary>
        public void StartPanLeftTiltDownMove()
        {
            StartPanLeftMove();

            StartTiltDownMove();
        }

        /// <summary>
        /// [Pan Right] / [Tilt Down] 대각선 연속 이동 시작
        /// </summary>
        public void StartPanRightTiltDownMove()
        {
            StartPanRightMove();

            StartTiltDownMove();
        }

        /// <summary>
        /// [Zoom] 확대 연속 이동 시작
        /// 
        /// 화면 버튼 [MouseDown] 시
        /// [Zoom] 확대 연속 이동 명령을 송신한다.
        /// </summary>
        public void StartZoomInMove()
        {
            _isUiContinuousMoveStarted =
                true;

            _currentPanTiltContinuousMoveDirection =
                PanTiltContinuousMoveDirection.None;

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.None;

            _currentPanTiltMoveType =
                PanTiltMoveType.None;

            _ads1000CameraControlService
                .ZoomIn();
        }

        /// <summary>
        /// [Zoom] 축소 연속 이동 시작
        /// 
        /// 화면 버튼 [MouseDown] 시
        /// [Zoom] 축소 연속 이동 명령을 송신한다.
        /// </summary>
        public void StartZoomOutMove()
        {
            _isUiContinuousMoveStarted =
                true;

            _currentPanTiltContinuousMoveDirection =
                PanTiltContinuousMoveDirection.None;

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.None;

            _currentPanTiltMoveType =
                PanTiltMoveType.None;

            _ads1000CameraControlService
                .ZoomOut();
        }

        /// <summary>
        /// [Focus] Near 연속 이동 시작
        /// 
        /// 화면 버튼 [MouseDown] 시
        /// [Focus] Near 연속 이동 명령을 송신한다.
        /// </summary>
        public void StartFocusNearMove()
        {
            _isUiContinuousMoveStarted =
                true;

            _currentPanTiltContinuousMoveDirection =
                PanTiltContinuousMoveDirection.None;

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.None;

            _currentPanTiltMoveType =
                PanTiltMoveType.None;

            _ads1000CameraControlService
                .FocusNear();
        }

        /// <summary>
        /// [Focus] Far 연속 이동 시작
        /// 
        /// 화면 버튼 [MouseDown] 시
        /// [Focus] Far 연속 이동 명령을 송신한다.
        /// </summary>
        public void StartFocusFarMove()
        {
            _isUiContinuousMoveStarted =
                true;

            _currentPanTiltContinuousMoveDirection =
                PanTiltContinuousMoveDirection.None;

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.None;

            _currentPanTiltMoveType =
                PanTiltMoveType.None;

            _ads1000CameraControlService
                .FocusFar();
        }

        /// <summary>
        /// [Pan] 연속 이동 정지
        /// 
        /// 키보드 방향키 조합 제어 중
        /// Pan 축 입력이 해제된 경우 Pan 축만 정지한다.
        /// </summary>
        private void StopPanMove()
        {
            if (!_isPanContinuousMoving)
            {
                return;
            }

            _ads1000CameraControlService
                .StopPanMove();

            _isPanContinuousMoving =
                false;

            _currentPanContinuousMoveDirection =
                PanTiltContinuousMoveDirection.None;

            if (!_isTiltContinuousMoving)
            {
                _isUiContinuousMoveStarted =
                    false;

                _currentPanTiltContinuousMoveDirection =
                    PanTiltContinuousMoveDirection.None;

                _currentPanTiltMoveAxis =
                    PanTiltMoveAxis.None;

                _currentPanTiltMoveType =
                    PanTiltMoveType.None;
            }

        }

        /// <summary>
        /// [Tilt] 연속 이동 정지
        /// 
        /// 키보드 방향키 조합 제어 중
        /// Tilt 축 입력이 해제된 경우 Tilt 축만 정지한다.
        /// </summary>
        private void StopTiltMove()
        {
            if (!_isTiltContinuousMoving)
            {
                return;
            }

            _ads1000CameraControlService
                .StopTiltMove();

            _isTiltContinuousMoving =
                false;

            _currentTiltContinuousMoveDirection =
                PanTiltContinuousMoveDirection.None;

            if (!_isPanContinuousMoving)
            {
                _isUiContinuousMoveStarted =
                    false;

                _currentPanTiltContinuousMoveDirection =
                    PanTiltContinuousMoveDirection.None;

                _currentPanTiltMoveAxis =
                    PanTiltMoveAxis.None;

                _currentPanTiltMoveType =
                    PanTiltMoveType.None;
            }

        }

        /// <summary>
        /// [UI] 장비 이동 정지
        /// 
        /// 화면 버튼을 통해 시작된
        /// [Pan] / [Tilt] / [Zoom] / [Focus] 연속 이동뿐만 아니라,
        /// [Pan] / [Tilt] Absolute / Relative 위치 이동 중에도
        /// 정지 명령을 송신한다.
        /// 
        /// STOP 명령은 장비 이동 상태를 강제로 정지시키는 용도이므로,
        /// UI 내부 이동 상태값만 기준으로 차단하지 않고
        /// 장비가 연결된 상태라면 정지 명령을 송신한다.
        /// </summary>
        public void StopContinuousMove()
        {
            if (_mcbConnectionState != ConnectionState.Connected &&
                _scbConnectionState != ConnectionState.Connected)
            {
                Console.WriteLine(
                    "[UI][CMD] Stop Ignored : Device Not Connected");

                ConsoleLogHelper.PrintLine();

                return;
            }

            Console.WriteLine(
                "[UI][CMD] Stop Move Request");

            _isUiContinuousMoveStarted =
                false;

            _isPanContinuousMoving =
                false;

            _isTiltContinuousMoving =
                false;

            _currentPanContinuousMoveDirection =
                PanTiltContinuousMoveDirection.None;

            _currentTiltContinuousMoveDirection =
                PanTiltContinuousMoveDirection.None;

            _isKeyboardPanLeftPressed =
                false;

            _isKeyboardPanRightPressed =
                false;

            _isKeyboardTiltUpPressed =
                false;

            _isKeyboardTiltDownPressed =
                false;

            _currentPanTiltContinuousMoveDirection =
                PanTiltContinuousMoveDirection.None;

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.None;

            _currentPanTiltMoveType =
                PanTiltMoveType.None;

            _ads1000CameraControlService
                .StopMove();

            ConsoleLogHelper.PrintLine();
            OnPropertyChanged(nameof(IsPanTiltSpeedEnabled));
        }
        #endregion
    }

}

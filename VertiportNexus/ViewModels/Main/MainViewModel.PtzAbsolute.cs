using System;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [Main] 화면 [ViewModel] - Absolute Position Control
    /// Pan / Tilt 절대 위치 이동 제어 로직을 관리한다.
    /// </summary>
    public partial class MainViewModel
    {
        #region [Camera Absolute Control Methods]

        /// <summary>
        /// [Pan] 절대 위치 이동
        /// 
        /// 입력된 [Pan Absolute] 목표값을
        /// UI Zero 기준 [0 ~ 360] 범위로 보정한 후,
        /// 장비 실제 Target 값으로 변환하여 이동 명령을 송신한다.
        /// 
        /// 사용자가 [Pan Zero]를 설정한 경우,
        /// UI Target [0.00]은 Zero 설정 당시의 실제 Pan 위치로 변환된다.
        /// 
        /// 단, [360] 입력은 [0]과 표시 위치는 같지만,
        /// 사용자가 한 바퀴 이동을 의도한 값으로 보고 별도로 처리한다.
        /// </summary>
        private void MovePanAbsolute()
        {
            const double PAN_POSITION_EPSILON =
                0.001;

            if (!PanAbsoluteValue.HasValue)
            {
                Console.WriteLine(
                    "[UI][PTZ] Pan Absolute Failed : Value is empty");

                return;
            }

            double currentPanCommandAngle =
                GetCurrentPanCommandAngle();

            double currentPan =
                GetUiCurrentPan();

            double inputPan =
                RoundAngleToProtocolScale(
                    PanAbsoluteValue.Value);

            double targetPan =
                Clamp(
                    inputPan,
                    0,
                    360);

            bool isFullTurnTarget =
                Math.Abs(targetPan - 360.0) <= PAN_POSITION_EPSILON;

            double deviceCurrentPan =
                NormalizePanStatus(
                    currentPanCommandAngle);

            double deviceTargetPan =
                ConvertUiPanTargetToDeviceTarget(
                    targetPan);

            double panMoveAngle;

            if (isFullTurnTarget)
            {
                panMoveAngle =
                    360.0 - currentPan;
            }
            else
            {
                panMoveAngle =
                    CalculatePanMoveAngle(
                        currentPan,
                        targetPan,
                        _panTurnMode);
            }

            // [Pan Absolute] 동일 위치 명령 차단
            //
            // UI Zero 기준 현재 [Pan] 위치와 목표 [Pan] 위치가 이미 동일한 경우,
            // 장비에 불필요한 [PA] 명령을 송신하지 않는다.
            //
            // 단, [360] 입력은 표시 위치상 [0]과 같더라도
            // 사용자가 한 바퀴 이동을 의도한 값으로 보므로
            // 동일 위치 차단 대상에서 제외한다.
            if (!isFullTurnTarget &&
                Math.Abs(panMoveAngle) <= PAN_POSITION_EPSILON)
            {
                Console.WriteLine(
                    "[UI][PTZ] Pan Absolute Ignored : Already Target Position");

                Console.WriteLine(
                    "[UI][PTZ] Pan Absolute Current : "
                    + currentPan.ToString("F2"));

                Console.WriteLine(
                    "[UI][PTZ] Pan Absolute Target : "
                    + targetPan.ToString("F2"));

                Console.WriteLine(
                    "[UI][PTZ] Pan UI Zero Offset : "
                    + _panUiZeroOffset.ToString("F2"));

                return;
            }

            double panCommandTarget =
                currentPanCommandAngle + panMoveAngle;

            // [Pan] UI Zero 기준 장비 Target 보정
            //
            // 일반 입력값은 UI Zero Offset을 더한 장비 실제 Target으로 변환한다.
            // 단, 기존 누적 회전 처리와 [360] 한 바퀴 이동 처리를 유지하기 위해
            // 실제 송신값은 누적 계산 결과를 사용한다.
            //
            // 현재 실행 기준에서 UI Target 0.00이 Zero 설정 위치로 가야 하므로,
            // 누적 계산 결과가 장비 Target과 다를 수 있는 경우에는
            // 장비 Target 기준으로 송신한다.
            if (!isFullTurnTarget)
            {
                panCommandTarget =
                    deviceTargetPan;
            }

            Console.WriteLine(
                "[UI][PTZ] Pan Absolute Input : "
                + inputPan.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Pan Absolute Mode : "
                + _panTurnMode);

            Console.WriteLine(
                "[UI][PTZ] Pan Absolute Current : "
                + currentPan.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Pan Device Current : "
                + deviceCurrentPan.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Pan Absolute Accumulated Current : "
                + currentPanCommandAngle.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Pan UI Zero Offset : "
                + _panUiZeroOffset.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Pan Absolute Target : "
                + targetPan.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Pan Absolute Move Angle : "
                + panMoveAngle.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Pan Absolute Command Target : "
                + panCommandTarget.ToString("F2"));

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.Pan;

            _currentPanTiltMoveType =
                PanTiltMoveType.Absolute;

            _ads1000CameraControlService
                .MovePanAbsolute(
                    panCommandTarget);
        }

        /// <summary>
        /// [Tilt] 절대 위치 이동
        /// 
        /// 입력된 [Tilt Absolute] 값을
        /// UI Zero 기준 [-90 ~ 90] 범위로 보정한 후,
        /// 장비 실제 Target 값으로 변환하여 이동 명령을 송신한다.
        /// 
        /// 사용자가 [Tilt Zero]를 설정한 경우,
        /// UI Target [0.00]은 Zero 설정 당시의 실제 Tilt 위치로 변환된다.
        /// </summary>
        private void MoveTiltAbsolute()
        {
            if (!TiltAbsoluteValue.HasValue)
            {
                Console.WriteLine(
                    "[UI][PTZ] Tilt Absolute Failed : Value is empty");

                return;
            }

            double currentTilt =
                GetUiCurrentTilt();

            double inputTilt =
                RoundAngleToProtocolScale(
                    TiltAbsoluteValue.Value);

            double targetTilt =
                Clamp(
                    inputTilt,
                    -90,
                    90);

            double deviceTargetTilt =
                ConvertUiTiltTargetToDeviceTarget(
                    targetTilt);

            double tiltMoveAngle =
                targetTilt - currentTilt;

            // [Tilt Absolute] 동일 위치 명령 차단
            //
            // UI Zero 기준 현재 [Tilt] 위치와 목표 [Tilt] 위치가 이미 동일한 경우,
            // 장비에 불필요한 [PA] 명령을 송신하지 않는다.
            if (Math.Abs(tiltMoveAngle) <= 0.001)
            {
                Console.WriteLine(
                    "[UI][PTZ] Tilt Absolute Ignored : Already Target Position");

                Console.WriteLine(
                    "[UI][PTZ] Tilt Absolute Current : "
                    + currentTilt.ToString("F2"));

                Console.WriteLine(
                    "[UI][PTZ] Tilt Absolute Target : "
                    + targetTilt.ToString("F2"));

                Console.WriteLine(
                    "[UI][PTZ] Tilt UI Zero Offset : "
                    + _tiltUiZeroOffset.ToString("F2"));

                return;
            }

            Console.WriteLine(
                "[UI][PTZ] Tilt Absolute Input : "
                + inputTilt.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Tilt Absolute Current : "
                + currentTilt.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Tilt UI Zero Offset : "
                + _tiltUiZeroOffset.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Tilt Absolute Target : "
                + targetTilt.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Tilt Absolute Move Angle : "
                + tiltMoveAngle.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Tilt Absolute Command Target : "
                + deviceTargetTilt.ToString("F2"));

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.Tilt;

            _currentPanTiltMoveType =
                PanTiltMoveType.Absolute;

            _ads1000CameraControlService
                .MoveTiltAbsolute(
                    deviceTargetTilt);
        }
        #endregion
    }

}

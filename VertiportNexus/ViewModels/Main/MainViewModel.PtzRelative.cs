using System;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [Main] 화면 [ViewModel] - Relative Position Control
    /// Pan / Tilt 상대 이동 입력을 실제 장비 이동 명령으로 변환한다.
    /// </summary>
    public partial class MainViewModel
    {
        #region [Camera Relative Control Methods]

        /// <summary>
        /// [Pan] 상대 위치 이동
        /// 
        /// 입력된 [Pan Relative] 값을 기준으로
        /// UI Zero 기준 현재 Pan 위치에서 상대 이동량을 더한
        /// 최종 목표 위치를 계산한 후,
        /// [ADS1000] 장비에는 절대 위치 이동 명령으로 송신한다.
        /// 
        /// 장비의 [PR] 상대 이동 명령은 이동 중 속도 변경 시
        /// [SP] 단독 갱신이 즉시 반영되지 않고,
        /// [BG] 재송신 시 상대 이동량이 재실행될 수 있으므로,
        /// UI 상대 이동은 내부적으로 [PA] 절대 이동으로 변환하여 처리한다.
        /// </summary>
        private void MovePanRelative()
        {
            if (!PanRelativeValue.HasValue)
            {
                Console.WriteLine(
                    "[UI][PTZ] Pan Relative Failed : Value is empty");

                return;
            }

            double currentPan =
                GetUiCurrentPan();

            double movePan =
                RoundAngleToProtocolScale(
                    PanRelativeValue.Value);

            double targetPan =
                NormalizePanStatus(
                    currentPan + movePan);

            double deviceTargetPan =
                ConvertUiPanTargetToDeviceTarget(
                    targetPan);

            Console.WriteLine(
                "[UI][PTZ] Pan Relative Input : "
                + movePan.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Pan Relative Current : "
                + currentPan.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Pan UI Zero Offset : "
                + _panUiZeroOffset.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Pan Relative Move Angle : "
                + movePan.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Pan Relative Expected Display : "
                + targetPan.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Pan Relative Command Target : "
                + deviceTargetPan.ToString("F2"));

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.Pan;

            // [Pan Relative] 속도 변경 처리 기준
            //
            // UI 동작은 Relative이지만,
            // 장비에는 [PA] 절대 위치 이동 명령으로 송신하므로
            // 이동 중 속도 변경도 Absolute 방식인 [SP=속도;BG;]를 사용한다.
            _currentPanTiltMoveType =
                PanTiltMoveType.Absolute;

            _ads1000CameraControlService
                .MovePanAbsolute(
                    deviceTargetPan);
        }

        /// <summary>
        /// [Tilt] 상대 위치 이동
        /// 
        /// 입력된 [Tilt Relative] 값을 기준으로
        /// UI Zero 기준 현재 Tilt 위치에서 상대 이동량을 더한
        /// 최종 목표 위치를 계산한 후,
        /// [ADS1000] 장비에는 절대 위치 이동 명령으로 송신한다.
        /// 
        /// 장비의 [PR] 상대 이동 명령은 이동 중 속도 변경 시
        /// [SP] 단독 갱신이 즉시 반영되지 않고,
        /// [BG] 재송신 시 상대 이동량이 재실행될 수 있으므로,
        /// UI 상대 이동은 내부적으로 [PA] 절대 이동으로 변환하여 처리한다.
        /// </summary>
        private void MoveTiltRelative()
        {
            if (!TiltRelativeValue.HasValue)
            {
                Console.WriteLine(
                    "[UI][PTZ] Tilt Relative Failed : Value is empty");

                return;
            }

            double currentTilt =
                GetUiCurrentTilt();

            double moveTilt =
                RoundAngleToProtocolScale(
                    TiltRelativeValue.Value);

            double targetTilt =
                Clamp(
                    currentTilt + moveTilt,
                    -90,
                    90);

            double deviceTargetTilt =
                ConvertUiTiltTargetToDeviceTarget(
                    targetTilt);

            Console.WriteLine(
                "[UI][PTZ] Tilt Relative Input : "
                + moveTilt.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Tilt Relative Current : "
                + currentTilt.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Tilt UI Zero Offset : "
                + _tiltUiZeroOffset.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Tilt Relative Move Angle : "
                + moveTilt.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Tilt Relative Expected Display : "
                + targetTilt.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Tilt Relative Command Target : "
                + deviceTargetTilt.ToString("F2"));

            _currentPanTiltMoveAxis =
                PanTiltMoveAxis.Tilt;

            // [Tilt Relative] 속도 변경 처리 기준
            //
            // UI 동작은 Relative이지만,
            // 장비에는 [PA] 절대 위치 이동 명령으로 송신하므로
            // 이동 중 속도 변경도 Absolute 방식인 [SP=속도;BG;]를 사용한다.
            _currentPanTiltMoveType =
                PanTiltMoveType.Absolute;

            _ads1000CameraControlService
                .MoveTiltAbsolute(
                    deviceTargetTilt);
        }
        #endregion
    }

}

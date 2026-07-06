using System;
using System.Threading.Tasks;
using VertiportNexus.Common;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [Main] 화면 [ViewModel] - Home / Zero Position
    /// Home Position 이동과 Pan / Tilt Zero Offset 저장 로직을 관리한다.
    /// </summary>
    public partial class MainViewModel
    {
        #region [Camera Home / Zero Control Methods]

        /// <summary>
        /// [Home Position] 이동
        /// 
        /// 사용자가 [HOME POSITION] 버튼을 누른 경우,
        /// 장비 기준 Home Position으로 이동한다.
        /// 
        /// Home Position 이동 중에는
        /// 다른 운용 제어 / 이동 제어 명령이 중복 송신되지 않도록
        /// 장비 제어 영역을 비활성화한다.
        /// </summary>
        private async Task MoveHomePositionAsync()
        {
            await MoveHomePositionWithControlLockAsync(
                "[UI][PTZ] Home Position");
        }

        /// <summary>
        /// [Home Position] 이동 공통 처리
        /// 
        /// Home Position 이동 시작 시
        /// 장비 연결 / 해제 버튼 및 운용 제어 / 이동 제어 영역을 비활성화하고,
        /// 문서 기준 [Pan Home] / [Tilt Home] 명령을 송신한다.
        /// 
        /// Home Position 완료 응답을 별도로 판단하지 않고,
        /// 현재 Pan / Tilt 상태값이 [0] 부근으로 수렴했는지 확인하여
        /// Home Position 이동 완료 여부를 판단한다.
        /// </summary>
        /// <param name="logPrefix">
        /// 로그 출력 구분 문자열
        /// </param>
        private async Task MoveHomePositionWithControlLockAsync(
            string logPrefix)
        {
            if (_isHomePositionMoving)
            {
                ConsoleLogHelper.PrintBlock(
                    logPrefix
                    + " Ignored : Home Position Moving");

                return;
            }

            if (_mcbConnectionState != ConnectionState.Connected ||
                _scbConnectionState != ConnectionState.Connected)
            {
                ConsoleLogHelper.PrintBlock(
                    logPrefix
                    + " Skipped : Device Not Fully Connected");

                return;
            }

            try
            {
                Console.WriteLine(
                    logPrefix
                    + " Move Start");

                Console.WriteLine();

                // [Pan / Tilt] 이동 상태 초기화
                //
                // Home Position은 장비 내부 Script로 동작하므로,
                // Absolute / Relative / Continuous 이동 중 속도 갱신 대상으로 보지 않는다.
                _currentPanTiltContinuousMoveDirection =
                    PanTiltContinuousMoveDirection.None;

                _currentPanTiltMoveAxis =
                    PanTiltMoveAxis.None;

                _currentPanTiltMoveType =
                    PanTiltMoveType.None;

                SetHomePositionMovingState(
                    true);

                // [Home Position] 이동
                //
                // 문서 기준 [Pan Home] / [Tilt Home] 명령인
                // [XQ##START;]를 순차 송신한다.
                _ads1000CameraControlService
                    .MoveHomePosition();

                bool isHomePositionCompleted =
                    await WaitHomePositionCompletedAsync();

                if (isHomePositionCompleted)
                {
                    // [Pan] 누적 상태값 초기화
                    //
                    // Home Position 이동이 완료되면
                    // 장비 기준 Pan 위치가 [0]으로 복귀한 상태이므로,
                    // 소프트웨어에서 관리하는 Pan 누적 위치값도 함께 초기화한다.
                    ResetPanAccumulatedStatus();

                    // [UI Zero Offset] 초기화
                    //
                    // Home Position 이동 완료 후에는
                    // 장비 기준 Home 위치를 다시 UI 기준 [0]으로 사용한다.
                    _panUiZeroOffset =
                        0.0;

                    _tiltUiZeroOffset =
                        0.0;

                    PanAbsoluteValue =
                        0;

                    TiltAbsoluteValue =
                        0;

                    PanRelativeValue =
                        0;

                    TiltRelativeValue =
                        0;

                    OnPropertyChanged(nameof(CurrentPanDisplayText));
                    OnPropertyChanged(nameof(CurrentTiltDisplayText));

                    Console.WriteLine(
                        logPrefix
                        + " Move Complete");

                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine(
                        logPrefix
                        + " Move Timeout");

                    Console.WriteLine();
                }

            }
            finally
            {
                // [Pan / Tilt] 이동 상태 초기화
                //
                // Home Position 종료 후에는
                // 이전 이동 상태 기준으로 속도 갱신 명령이 송신되지 않도록 초기화한다.
                _currentPanTiltContinuousMoveDirection =
                    PanTiltContinuousMoveDirection.None;

                _currentPanTiltMoveAxis =
                    PanTiltMoveAxis.None;

                _currentPanTiltMoveType =
                    PanTiltMoveType.None;

                SetHomePositionMovingState(
                    false);
            }

        }

        /// <summary>
        /// [Pan] 현재 위치를 UI / 장비 Script 기준 [0] 위치로 저장
        /// 
        /// 현재 [Pan] 위치값을 장비 Offset 저장 프로토콜로 송신하고,
        /// 프로그램 화면에서도 현재 위치가 [0.00]으로 표시되도록
        /// UI Zero Offset을 저장한다.
        /// </summary>
        private void SetPanZero()
        {
            double currentPan =
                RoundAngleToProtocolScale(
                    NormalizePanStatus(
                        CurrentPan));

            int offsetValue =
                Convert.ToInt32(
                    Math.Round(
                        currentPan * 100.0,
                        MidpointRounding.AwayFromZero));

            ConsoleLogHelper.PrintLine();

            Console.WriteLine(
                "[UI][PTZ] Pan Zero Offset Request");

            Console.WriteLine(
                "[UI][PTZ] Pan Zero Current : "
                + currentPan.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Pan Zero Offset Value : "
                + offsetValue);

            // [Pan] UI Zero Offset 저장
            //
            // 현재 장비 Pan 위치를 UI 기준 [0] 위치로 저장한다.
            // 이후 Current Status 표시와 Target 0 이동 기준에 사용한다.
            _panUiZeroOffset =
                currentPan;

            // [Pan] 입력값 초기화
            //
            // Pan Zero 설정 후에는 현재 위치가 UI 기준 [0]이므로,
            // Absolute / Relative 입력값도 [0.00]으로 초기화한다.
            PanAbsoluteValue =
                0;

            PanRelativeValue =
                0;

            _ads1000CameraControlService
                .SetPanZero(
                    currentPan);

            ResetPanAccumulatedStatus();

            OnPropertyChanged(nameof(CurrentPanDisplayText));

            ConsoleLogHelper.PrintLine();
        }

        /// <summary>
        /// [Tilt] 현재 위치를 UI / 장비 Script 기준 [0] 위치로 저장
        /// 
        /// 현재 [Tilt] 위치값을 장비 Offset 저장 프로토콜로 송신하고,
        /// 프로그램 화면에서도 현재 위치가 [0.00]으로 표시되도록
        /// UI Zero Offset을 저장한다.
        /// </summary>
        private void SetTiltZero()
        {
            double currentTilt =
                RoundAngleToProtocolScale(
                    CurrentTilt);

            int offsetValue =
                Convert.ToInt32(
                    Math.Round(
                        currentTilt * 100.0,
                        MidpointRounding.AwayFromZero));

            ConsoleLogHelper.PrintLine();

            Console.WriteLine(
                "[UI][PTZ] Tilt Zero Offset Request");

            Console.WriteLine(
                "[UI][PTZ] Tilt Zero Current : "
                + currentTilt.ToString("F2"));

            Console.WriteLine(
                "[UI][PTZ] Tilt Zero Offset Value : "
                + offsetValue);

            // [Tilt] UI Zero Offset 저장
            //
            // 현재 장비 Tilt 위치를 UI 기준 [0] 위치로 저장한다.
            // 이후 Current Status 표시와 Target 0 이동 기준에 사용한다.
            _tiltUiZeroOffset =
                currentTilt;

            // [Tilt] 입력값 초기화
            //
            // Tilt Zero 설정 후에는 현재 위치가 UI 기준 [0]이므로,
            // Absolute / Relative 입력값도 [0.00]으로 초기화한다.
            TiltAbsoluteValue =
                0;

            TiltRelativeValue =
                0;

            _ads1000CameraControlService
                .SetTiltZero(
                    currentTilt);

            ConsoleLogHelper.PrintLine();
            OnPropertyChanged(nameof(CurrentTiltDisplayText));
        }

        /// <summary>
        /// [Home Position] 이동 완료 대기
        /// 
        /// Home Position 명령 송신 후
        /// 현재 Pan / Tilt 상태값이 [0] 부근으로 수렴했는지 확인한다.
        /// 
        /// 명확한 완료 응답 Packet을 사용하지 않으므로,
        /// 상태값 기준으로 일정 횟수 연속 만족 시 완료로 판단한다.
        /// </summary>
        /// <returns>
        /// Home Position 완료 여부
        /// </returns>
        private async Task<bool> WaitHomePositionCompletedAsync()
        {
            const int MIN_WAIT_MILLISECONDS =
                1500;

            const int CHECK_INTERVAL_MILLISECONDS =
                200;

            const int TIMEOUT_MILLISECONDS =
                20000;

            const int REQUIRED_STABLE_COUNT =
                3;

            const double PAN_TOLERANCE_DEGREES =
                0.2;

            const double TILT_TOLERANCE_DEGREES =
                0.2;

            // [Home Position] 최소 대기
            //
            // Home Position 명령 송신 직후에는
            // 장비 상태값이 아직 이동 전 위치로 들어올 수 있으므로
            // 안정 상태 판단 전 최소 대기 시간을 둔다.
            await Task.Delay(
                MIN_WAIT_MILLISECONDS);

            int stableCount =
                0;

            int elapsedMilliseconds =
                MIN_WAIT_MILLISECONDS;

            while (elapsedMilliseconds < TIMEOUT_MILLISECONDS)
            {
                if (_mcbConnectionState != ConnectionState.Connected ||
                    _scbConnectionState != ConnectionState.Connected)
                {
                    ConsoleLogHelper.PrintBlock(
                        "[DEVICE] Home Position Wait Canceled : Device Disconnected");

                    return false;
                }

                double currentPan =
                    NormalizePanStatus(
                        CurrentPan);

                double currentTilt =
                    NormalizeTiltStatus(
                        CurrentTilt);

                bool isPanHome =
                    Math.Abs(currentPan) <= PAN_TOLERANCE_DEGREES;

                bool isTiltHome =
                    Math.Abs(currentTilt) <= TILT_TOLERANCE_DEGREES;

                if (isPanHome &&
                    isTiltHome)
                {
                    stableCount++;

                    if (stableCount >= REQUIRED_STABLE_COUNT)
                    {
                        ConsoleLogHelper.PrintBlock(
                            "[DEVICE] Home Position Stable Count : "
                            + stableCount);

                        return true;
                    }

                }
                else
                {
                    stableCount =
                        0;
                }

                await Task.Delay(
                    CHECK_INTERVAL_MILLISECONDS);

                elapsedMilliseconds +=
                    CHECK_INTERVAL_MILLISECONDS;
            }
            return false;
        }
        #endregion
    }

}

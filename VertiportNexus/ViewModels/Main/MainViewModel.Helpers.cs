using System;
using VertiportNexus.Models.ADS1000;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [Main] 화면 [ViewModel] - Utility
    /// 각도 보정 / Zoom 변환 / 공통 계산 등 ViewModel 내부 보조 메서드를 관리한다.
    /// </summary>
    public partial class MainViewModel
    {
        #region [Utility Methods]

        /// <summary>
        /// [Pan] 누적 상태값 갱신
        /// 
        /// 장비 상태 Packet에서 수신한 [Pan] 원본 각도값을 기준으로
        /// 장비 제어용 누적 위치값을 갱신한다.
        /// 
        /// 화면 표시용 [Pan] 값은 [0 ~ 360] 범위로 정규화하지만,
        /// 장비 상태 Packet의 [Pan] 원본값은 한 바퀴 이상 회전한
        /// 누적 각도 정보를 포함할 수 있으므로 정규화하지 않고 보관한다.
        /// </summary>
        /// <param name="panStatus">
        /// 장비에서 수신한 Pan 원본 각도값
        /// </param>
        private void UpdatePanAccumulatedStatus(
            double panStatus)
        {
            _currentPanAccumulated =
                panStatus;

            _lastPanDisplayStatus =
                NormalizePanStatus(
                    panStatus);

            _hasPanAccumulatedStatus =
                true;
        }

        /// <summary>
        /// [Pan] 제어 기준 위치값 조회
        /// 
        /// Pan 누적 상태값이 초기화된 경우에는
        /// 장비 제어용 누적 위치값을 반환하고,
        /// 아직 상태값을 수신하지 못한 경우에는
        /// 화면 표시용 현재 Pan 값을 반환한다.
        /// </summary>
        /// <returns>
        /// Pan 제어 기준 위치값
        /// </returns>
        private double GetCurrentPanCommandAngle()
        {
            if (_hasPanAccumulatedStatus)
            {
                return _currentPanAccumulated;
            }
            return CurrentPan;
        }

        /// <summary>
        /// [Pan] 누적 상태값 초기화
        /// 
        /// Home Position 또는 Pan Zero 수행 후
        /// 장비 Pan 기준 위치가 [0]으로 재설정되는 경우,
        /// 소프트웨어에서 관리하는 누적 위치값도 함께 초기화한다.
        /// </summary>
        private void ResetPanAccumulatedStatus()
        {
            _currentPanAccumulated =
                0.0;

            _lastPanDisplayStatus =
                0.0;

            _hasPanAccumulatedStatus =
                true;
        }

        /// <summary>
        /// [Pan] 이동 각도 계산
        /// 
        /// 현재 [Pan] 위치와 목표 [Pan] 위치를 기준으로
        /// 선택된 선회 모드에 따라 장비로 송신할 이동 각도를 계산한다.
        /// 
        /// [Short] 모드는 가장 가까운 방향의 이동 각도를 계산하고,
        /// [Via 0] 모드는 단거리 보정 없이 목표 위치와 현재 위치의 차이를 사용한다.
        /// </summary>
        /// <param name="currentPan">
        /// 현재 Pan 위치 [0 ~ 360]
        /// </param>
        /// <param name="targetPan">
        /// 목표 Pan 위치 [0 ~ 360]
        /// </param>
        /// <param name="panTurnMode">
        /// Pan 선회 모드
        /// </param>
        /// <returns>
        /// 장비로 송신할 Pan 이동 각도
        /// </returns>
        private double CalculatePanMoveAngle(
            double currentPan,
            double targetPan,
            Ads1000PanTurnMode panTurnMode)
        {
            double normalizedCurrentPan =
                NormalizePanStatus(
                    currentPan);

            double normalizedTargetPan =
                NormalizePanStatus(
                    targetPan);

            if (panTurnMode == Ads1000PanTurnMode.Short)
            {
                return CalculateShortestPanDelta(
                    normalizedCurrentPan,
                    normalizedTargetPan);
            }

            return CalculateViaZeroPanDelta(
                normalizedCurrentPan,
                normalizedTargetPan);
        }

        /// <summary>
        /// [Pan] 최단 이동 각도 계산
        /// 
        /// 현재 [Pan] 위치에서 목표 [Pan] 위치까지
        /// 가장 가까운 방향의 이동 각도를 계산한다.
        /// 
        /// 결과값은 [-180 ~ 180] 범위로 반환하며,
        /// [0 → 350] 이동처럼 360도 경계를 넘어가는 경우에도
        /// 장비가 먼 방향으로 회전하지 않도록 처리한다.
        /// </summary>
        /// <param name="currentPan">
        /// 현재 Pan 위치 [0 ~ 360]
        /// </param>
        /// <param name="targetPan">
        /// 목표 Pan 위치 [0 ~ 360]
        /// </param>
        /// <returns>
        /// 최단 이동 각도
        /// </returns>
        private double CalculateShortestPanDelta(
            double currentPan,
            double targetPan)
        {
            const double FULL_ROTATION_DEGREES =
                360.0;

            const double HALF_ROTATION_DEGREES =
                180.0;

            double delta =
                (targetPan
                 - currentPan
                 + HALF_ROTATION_DEGREES
                 + FULL_ROTATION_DEGREES)
                % FULL_ROTATION_DEGREES
                - HALF_ROTATION_DEGREES;

            return NormalizeZeroAngle(
                delta);
        }

        /// <summary>
        /// [Pan] [Via 0] 이동 각도 계산
        /// 
        /// 현재 [Pan] 위치에서 목표 [Pan] 위치까지
        /// 단거리 보정 없이 이동 각도를 계산한다.
        /// 
        /// 예)
        /// 현재 [0] / 목표 [350]인 경우
        /// [Short] 모드는 [-10]으로 계산하지만,
        /// [Via 0] 모드는 [350]으로 계산한다.
        /// </summary>
        /// <param name="currentPan">
        /// 현재 Pan 위치 [0 ~ 360]
        /// </param>
        /// <param name="targetPan">
        /// 목표 Pan 위치 [0 ~ 360]
        /// </param>
        /// <returns>
        /// Via 0 기준 이동 각도
        /// </returns>
        private double CalculateViaZeroPanDelta(
            double currentPan,
            double targetPan)
        {
            double delta =
                targetPan - currentPan;

            return NormalizeZeroAngle(
                delta);
        }

        /// <summary>
        /// [각도] 미세 오차 보정
        /// 
        /// 장비 Encoder 오차 또는 계산 과정에서 발생한
        /// [0] 근처 미세값을 [0]으로 보정한다.
        /// </summary>
        /// <param name="angle">
        /// 원본 각도
        /// </param>
        /// <returns>
        /// 미세 오차가 보정된 각도
        /// </returns>
        private double NormalizeZeroAngle(
            double angle)
        {
            const double ZERO_EPSILON =
                0.001;

            if (Math.Abs(angle) <= ZERO_EPSILON)
            {
                return 0.0;
            }
            return angle;
        }

        /// <summary>
        /// [Pan] 상태값 범위 정규화
        /// 
        /// ADS1000 상태 Packet에서 수신한 Pan 값을
        /// 최종 ICD 기준 [0 ~ 360] 범위로 변환한다.
        /// 
        /// Pan 값이 360도를 초과하면 0도부터 다시 시작하고,
        /// 0도 미만이면 360도 기준으로 순환 처리한다.
        /// 
        /// 장비 Encoder 오차로 인해
        /// [0] 근처 또는 [360] 근처의 미세 오차가 발생하는 경우,
        /// 화면 표시 및 상태 응답 기준에서는 [0]으로 보정한다.
        /// </summary>
        /// <param name="pan">
        /// Pan 원본 상태값
        /// </param>
        /// <returns>
        /// [0 ~ 360] 범위로 정규화된 Pan 상태값
        /// </returns>
        private double NormalizePanStatus(
            double pan)
        {
            const double FULL_ROTATION_DEGREES =
                360.0;

            const double ZERO_EPSILON =
                0.001;

            double normalizedPan =
                pan % FULL_ROTATION_DEGREES;

            if (normalizedPan < 0)
            {
                normalizedPan +=
                    FULL_ROTATION_DEGREES;
            }

            if (Math.Abs(normalizedPan) <= ZERO_EPSILON ||
                Math.Abs(normalizedPan - FULL_ROTATION_DEGREES) <= ZERO_EPSILON)
            {
                return 0.0;
            }
            return NormalizePosition(
                normalizedPan);
        }

        /// <summary>
        /// [Tilt] 상태값 범위 정규화
        /// 
        /// ADS1000 상태 Packet에서 수신한 Tilt 값을
        /// 장비 물리 제한 기준 [-90 ~ 90] 범위로 보정한다.
        /// 
        /// 장비 Encoder 오차로 인해
        /// [0] 근처의 미세 오차가 발생하는 경우,
        /// 화면 표시 및 상태 응답 기준에서는 [0]으로 보정한다.
        /// </summary>
        /// <param name="tilt">
        /// Tilt 원본 상태값
        /// </param>
        /// <returns>
        /// [-90 ~ 90] 범위로 정규화된 Tilt 상태값
        /// </returns>
        private double NormalizeTiltStatus(
            double tilt)
        {
            const double MIN_TILT_DEGREES =
                -90.0;

            const double MAX_TILT_DEGREES =
                90.0;

            const double ZERO_EPSILON =
                0.001;

            double normalizedTilt =
                Clamp(
                    tilt,
                    MIN_TILT_DEGREES,
                    MAX_TILT_DEGREES);

            if (Math.Abs(normalizedTilt) <= ZERO_EPSILON)
            {
                return 0.0;
            }
            return NormalizePosition(
                normalizedTilt);
        }

        /// <summary>
        /// [범위 위치 상태값] 미세 오차 보정
        /// 
        /// 장비 상태 Packet에서 수신한 위치값을
        /// 지정한 최소 / 최대 범위로 보정한다.
        /// 
        /// 장비 Encoder 또는 위치 응답에서 발생하는
        /// [0] 근처 또는 정수 위치 근처의 미세 오차는
        /// 화면 표시 및 상태 응답 기준에서 보정한다.
        /// </summary>
        /// <param name="value">
        /// 원본 위치값
        /// </param>
        /// <param name="min">
        /// 최소 위치값
        /// </param>
        /// <param name="max">
        /// 최대 위치값
        /// </param>
        /// <returns>
        /// 범위 및 미세 오차가 보정된 위치값
        /// </returns>
        private double NormalizeRangePosition(
            double value,
            double min,
            double max)
        {
            double clampedValue =
                Clamp(
                    value,
                    min,
                    max);

            return NormalizePosition(
                clampedValue);
        }

        /// <summary>
        /// [위치 상태값] 미세 오차 보정
        /// 
        /// 장비 Encoder 또는 위치 응답에서 발생하는
        /// [0] 근처 또는 정수 위치 근처의 미세 오차를
        /// 화면 표시 및 상태 응답 기준에서 보정한다.
        /// </summary>
        /// <param name="value">
        /// 원본 위치값
        /// </param>
        /// <returns>
        /// 미세 오차가 보정된 위치값
        /// </returns>
        private double NormalizePosition(
            double value)
        {
            const double ZERO_EPSILON =
                0.001;

            const double INTEGER_EPSILON =
                0.001;

            if (Math.Abs(value) <= ZERO_EPSILON)
            {
                return 0.0;
            }

            double roundedValue =
                Math.Round(
                    value);

            if (Math.Abs(value - roundedValue) <= INTEGER_EPSILON)
            {
                return roundedValue;
            }
            return value;
        }

        /// <summary>
        /// [Pan] UI Zero 기준 현재 위치 계산
        /// 
        /// 장비에서 수신한 실제 Pan 위치값에서
        /// 사용자가 설정한 UI Zero Offset 값을 빼서,
        /// 화면 기준 Pan 현재 위치를 계산한다.
        /// </summary>
        /// <returns>
        /// UI Zero 기준 Pan 현재 위치
        /// </returns>
        private double GetUiCurrentPan()
        {
            return RoundAngleToProtocolScale(
                NormalizePanStatus(
                    CurrentPan
                    - _panUiZeroOffset));
        }

        /// <summary>
        /// [Tilt] UI Zero 기준 현재 위치 계산
        /// 
        /// 장비에서 수신한 실제 Tilt 위치값에서
        /// 사용자가 설정한 UI Zero Offset 값을 빼서,
        /// 화면 기준 Tilt 현재 위치를 계산한다.
        /// </summary>
        /// <returns>
        /// UI Zero 기준 Tilt 현재 위치
        /// </returns>
        private double GetUiCurrentTilt()
        {
            return RoundAngleToProtocolScale(
                CurrentTilt
                - _tiltUiZeroOffset);
        }

        /// <summary>
        /// [Pan] UI Target 값을 장비 실제 Target 값으로 변환
        /// 
        /// 사용자가 입력한 UI 기준 Pan Target 값에
        /// Pan UI Zero Offset 값을 더해
        /// 장비에 송신할 실제 Pan Target 값을 계산한다.
        /// </summary>
        /// <param name="uiTargetPan">
        /// UI 기준 Pan Target
        /// </param>
        /// <returns>
        /// 장비 실제 Pan Target
        /// </returns>
        private double ConvertUiPanTargetToDeviceTarget(
            double uiTargetPan)
        {
            return RoundAngleToProtocolScale(
                NormalizePanStatus(
                    uiTargetPan
                    + _panUiZeroOffset));
        }

        /// <summary>
        /// [Tilt] UI Target 값을 장비 실제 Target 값으로 변환
        /// 
        /// 사용자가 입력한 UI 기준 Tilt Target 값에
        /// Tilt UI Zero Offset 값을 더해
        /// 장비에 송신할 실제 Tilt Target 값을 계산한다.
        /// </summary>
        /// <param name="uiTargetTilt">
        /// UI 기준 Tilt Target
        /// </param>
        /// <returns>
        /// 장비 실제 Tilt Target
        /// </returns>
        private double ConvertUiTiltTargetToDeviceTarget(
            double uiTargetTilt)
        {
            return RoundAngleToProtocolScale(
                Clamp(
                    uiTargetTilt
                    + _tiltUiZeroOffset,
                    -90,
                    90));
        }

        /// <summary>
        /// [Pan / Tilt] 각도값 소수점 둘째 자리 보정
        /// 
        /// ADS3000 Offset 저장 프로토콜은
        /// 각도값을 [각도 * 100] 정수값으로 송신하므로,
        /// UI 입력 및 표시 기준도 소수점 둘째 자리로 통일한다.
        /// </summary>
        /// <param name="angle">
        /// 각도값
        /// </param>
        /// <returns>
        /// 소수점 둘째 자리로 반올림된 각도값
        /// </returns>
        private double RoundAngleToProtocolScale(
            double angle)
        {
            return Math.Round(
                angle,
                2,
                MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// 입력값 범위 제한
        /// 
        /// 입력값이 지정된 최소 / 최대 범위를 벗어난 경우
        /// 최소 / 최대값으로 보정한다.
        /// </summary>
        /// <param name="value">
        /// 원본 값
        /// </param>
        /// <param name="min">
        /// 최소 허용값
        /// </param>
        /// <param name="max">
        /// 최대 허용값
        /// </param>
        /// <returns>
        /// 범위 제한이 적용된 값
        /// </returns>
        private double Clamp(
            double value,
            double min,
            double max)
        {
            if (value < min)
                return min;

            if (value > max)
                return max;

            return value;
        }
        #endregion
    }

}

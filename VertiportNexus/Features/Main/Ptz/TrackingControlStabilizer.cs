using System;

namespace VertiportNexus.Services.Camera
{
    /// <summary>
    /// [Tracking] 자동 추적 보정 안정화 서비스
    /// 
    /// 자동 추적 과정에서 계산된 Pan / Tilt 보정 각도에 대해
    /// Dead Zone / Low Pass Filter / Step Limit을 적용하여
    /// 고배율 Zoom 상태에서 발생하는 떨림 / 헌팅 현상을 줄인다.
    /// </summary>
    internal sealed class TrackingControlStabilizer
    {
        #region [Constants]

        /// <summary>
        /// 기본 보정 각도 무시 범위 [Degree]
        /// 
        /// 계산된 Pan / Tilt 보정 각도가 해당 값보다 작으면
        /// 미세 흔들림으로 판단하여 명령을 전송하지 않는다.
        /// </summary>
        private const double BASE_ANGLE_DEAD_ZONE_DEGREE =
            0.008;

        /// <summary>
        /// Zoom 배율 기준 Dead Zone 증가 가중치
        /// 
        /// Zoom 배율이 높을수록 작은 각도 변화도 화면상 크게 보이므로,
        /// 배율에 따라 Dead Zone을 증가시킨다.
        /// </summary>
        private const double ZOOM_DEAD_ZONE_WEIGHT =
            0.003;

        /// <summary>
        /// 기본 1회 최대 보정 각도 [Degree]
        /// 
        /// 한 번의 Tracking 명령에서 너무 큰 Relative 이동이 발생하지 않도록 제한한다.
        /// </summary>
        private const double BASE_MAX_STEP_DEGREE =
            0.20;

        /// <summary>
        /// 최소 1회 최대 보정 각도 [Degree]
        /// 
        /// 고배율 상태에서는 작은 Relative 이동만 허용하여
        /// Pan / Tilt 헌팅을 줄인다.
        /// </summary>
        private const double MIN_MAX_STEP_DEGREE =
            0.025;

        /// <summary>
        /// 기본 필터 반영 비율
        /// 
        /// 값이 작을수록 움직임은 부드러워지고,
        /// 값이 클수록 Tracking 반응은 빨라진다.
        /// </summary>
        private const double BASE_FILTER_ALPHA =
            0.25;

        /// <summary>
        /// 최소 필터 반영 비율
        /// 
        /// 고배율 상태에서 이전 보정값을 더 많이 반영하여
        /// 급격한 방향 전환을 줄인다.
        /// </summary>
        private const double MIN_FILTER_ALPHA =
            0.08;

        /// <summary>
        /// 마지막 Pan 보정 방향
        /// 
        /// 고배율 상태에서 좌 / 우 방향이 짧은 주기로 반전되는 경우
        /// 헌팅으로 판단하기 위해 사용한다.
        /// </summary>
        private int _lastPanDirection;

        /// <summary>
        /// 마지막 Tilt 보정 방향
        /// 
        /// 고배율 상태에서 상 / 하 방향이 짧은 주기로 반전되는 경우
        /// 헌팅으로 판단하기 위해 사용한다.
        /// </summary>
        private int _lastTiltDirection;

        #endregion

        #region [Fields]

        /// <summary>
        /// 필터링된 마지막 Pan 보정 각도
        /// </summary>
        private double _filteredPanAngle;

        /// <summary>
        /// 필터링된 마지막 Tilt 보정 각도
        /// </summary>
        private double _filteredTiltAngle;

        /// <summary>
        /// 필터 초기화 여부
        /// </summary>
        private bool _isFilterInitialized;

        #endregion

        #region [Public Methods]

        /// <summary>
        /// 안정화된 Tracking 보정 각도 생성
        /// 
        /// 원본 Pan / Tilt 보정 각도에 대해
        /// Zoom 배율 기반 Dead Zone, Low Pass Filter, 최대 이동 각도 제한을 적용한다.
        /// 
        /// 반환값이 [false]이면 이번 Tracking Frame에서는
        /// Pan / Tilt Relative 명령을 전송하지 않는다.
        /// </summary>
        /// <param name="rawPanAngle">
        /// 원본 Pan 보정 각도
        /// </param>
        /// <param name="rawTiltAngle">
        /// 원본 Tilt 보정 각도
        /// </param>
        /// <param name="currentZoomRatio">
        /// 현재 Zoom 배율
        /// </param>
        /// <param name="stablePanAngle">
        /// 안정화된 Pan 보정 각도
        /// </param>
        /// <param name="stableTiltAngle">
        /// 안정화된 Tilt 보정 각도
        /// </param>
        /// <returns>
        /// Tracking 명령 전송 가능 여부
        /// </returns>
        internal bool TryCreateStableAngle(
            double rawPanAngle,
            double rawTiltAngle,
            double currentZoomRatio,
            out double stablePanAngle,
            out double stableTiltAngle)
        {
            stablePanAngle =
                0;

            stableTiltAngle =
                0;

            double zoomRatio =
                Math.Max(
                    1.0,
                    currentZoomRatio);

            double deadZoneDegree =
                CalculateAngleDeadZone(
                    zoomRatio);

            if (Math.Abs(rawPanAngle) < deadZoneDegree &&
                Math.Abs(rawTiltAngle) < deadZoneDegree)
            {
                ResetFilter();

                return false;
            }

            double filterAlpha =
                CalculateFilterAlpha(
                    zoomRatio);

            ApplyLowPassFilter(
                rawPanAngle,
                rawTiltAngle,
                filterAlpha);

            double maxStepDegree =
                CalculateMaxStepDegree(
                    zoomRatio);

            stablePanAngle =
                Clamp(
                    _filteredPanAngle,
                    -maxStepDegree,
                    maxStepDegree);

            stableTiltAngle =
                Clamp(
                    _filteredTiltAngle,
                    -maxStepDegree,
                    maxStepDegree);

            if (IsHighZoomRatio(
                zoomRatio) &&
                IsDirectionReversed(
                    stablePanAngle,
                    stableTiltAngle))
            {
                Console.WriteLine(
                    "[TRACKING][STABILIZER] Skip : Direction Reversed");

                UpdateLastDirection(
                    stablePanAngle,
                    stableTiltAngle);

                return false;
            }

            UpdateLastDirection(
                stablePanAngle,
                stableTiltAngle);

            if (Math.Abs(stablePanAngle) < deadZoneDegree &&
                Math.Abs(stableTiltAngle) < deadZoneDegree)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Tracking 안정화 상태 초기화
        /// 
        /// Tracking 정지 / 대상 중심 진입 / 모드 전환 시
        /// 이전 필터 값이 다음 제어에 영향을 주지 않도록 초기화한다.
        /// </summary>
        internal void Reset()
        {
            ResetFilter();
        }

        #endregion

        #region [Validation Methods]

        /// <summary>
        /// 고배율 Zoom 상태 여부 조회
        /// 
        /// 고배율 상태에서는 작은 방향 반전도 화면상 큰 떨림으로 보이므로,
        /// 방향 반전 억제 처리를 적용한다.
        /// </summary>
        /// <param name="zoomRatio">
        /// 현재 Zoom 배율
        /// </param>
        /// <returns>
        /// 고배율 Zoom 상태 여부
        /// </returns>
        private bool IsHighZoomRatio(
            double zoomRatio)
        {
            return zoomRatio >= 20.0;
        }

        /// <summary>
        /// Tracking 보정 방향 반전 여부 조회
        /// 
        /// 이전 보정 방향과 현재 보정 방향이 반대로 바뀐 경우
        /// 고배율 헌팅 가능성이 높은 상태로 판단한다.
        /// </summary>
        /// <param name="panAngle">
        /// 현재 Pan 보정 각도
        /// </param>
        /// <param name="tiltAngle">
        /// 현재 Tilt 보정 각도
        /// </param>
        /// <returns>
        /// 방향 반전 여부
        /// </returns>
        private bool IsDirectionReversed(
            double panAngle,
            double tiltAngle)
        {
            int currentPanDirection =
                GetDirection(
                    panAngle);

            int currentTiltDirection =
                GetDirection(
                    tiltAngle);

            bool isPanReversed =
                _lastPanDirection != 0 &&
                currentPanDirection != 0 &&
                _lastPanDirection != currentPanDirection;

            bool isTiltReversed =
                _lastTiltDirection != 0 &&
                currentTiltDirection != 0 &&
                _lastTiltDirection != currentTiltDirection;

            return isPanReversed ||
                   isTiltReversed;
        }

        /// <summary>
        /// 마지막 Tracking 보정 방향 갱신
        /// </summary>
        private void UpdateLastDirection(
            double panAngle,
            double tiltAngle)
        {
            int panDirection =
                GetDirection(
                    panAngle);

            int tiltDirection =
                GetDirection(
                    tiltAngle);

            if (panDirection != 0)
            {
                _lastPanDirection =
                    panDirection;
            }

            if (tiltDirection != 0)
            {
                _lastTiltDirection =
                    tiltDirection;
            }

        }

        /// <summary>
        /// 보정 각도 방향값 조회
        /// </summary>
        private int GetDirection(
            double angle)
        {
            if (angle > 0)
            {
                return 1;
            }

            if (angle < 0)
            {
                return -1;
            }

            return 0;
        }

        #endregion

        #region [Calculation Methods]

        /// <summary>
        /// Zoom 배율 기준 보정 각도 Dead Zone 계산
        /// </summary>
        private double CalculateAngleDeadZone(
            double zoomRatio)
        {
            return BASE_ANGLE_DEAD_ZONE_DEGREE +
                   Math.Sqrt(
                       zoomRatio) *
                   ZOOM_DEAD_ZONE_WEIGHT;
        }

        /// <summary>
        /// Zoom 배율 기준 필터 반영 비율 계산
        /// </summary>
        private double CalculateFilterAlpha(
            double zoomRatio)
        {
            double alpha =
                BASE_FILTER_ALPHA /
                Math.Sqrt(
                    zoomRatio);

            return Clamp(
                alpha,
                MIN_FILTER_ALPHA,
                BASE_FILTER_ALPHA);
        }

        /// <summary>
        /// Zoom 배율 기준 1회 최대 보정 각도 계산
        /// </summary>
        private double CalculateMaxStepDegree(
            double zoomRatio)
        {
            double maxStepDegree =
                BASE_MAX_STEP_DEGREE /
                Math.Sqrt(
                    zoomRatio);

            return Clamp(
                maxStepDegree,
                MIN_MAX_STEP_DEGREE,
                BASE_MAX_STEP_DEGREE);
        }

        #endregion

        #region [Filter Methods]

        /// <summary>
        /// Low Pass Filter 적용
        /// 
        /// 이전 보정값과 현재 보정값을 보간하여
        /// 급격한 방향 전환과 미세 떨림을 줄인다.
        /// </summary>
        private void ApplyLowPassFilter(
            double rawPanAngle,
            double rawTiltAngle,
            double alpha)
        {
            if (!_isFilterInitialized)
            {
                _filteredPanAngle =
                    rawPanAngle;

                _filteredTiltAngle =
                    rawTiltAngle;

                _isFilterInitialized =
                    true;

                return;
            }

            _filteredPanAngle =
                (_filteredPanAngle * (1.0 - alpha)) +
                (rawPanAngle * alpha);

            _filteredTiltAngle =
                (_filteredTiltAngle * (1.0 - alpha)) +
                (rawTiltAngle * alpha);
        }

        /// <summary>
        /// 필터 상태 초기화
        /// </summary>
        private void ResetFilter()
        {
            _filteredPanAngle =
                0;

            _filteredTiltAngle =
                0;

            _lastPanDirection =
                0;

            _lastTiltDirection =
                0;

            _isFilterInitialized =
                false;
        }

        #endregion

        #region [Utility Methods]

        /// <summary>
        /// 숫자 범위 보정
        /// </summary>
        private double Clamp(
            double value,
            double min,
            double max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }
        #endregion
    }

}

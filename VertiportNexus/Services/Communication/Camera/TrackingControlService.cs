using System;
using VertiportNexus.Models.Camera;
using VertiportNexus.Services.ADS1000;
using VertiportNexus.Common;

namespace VertiportNexus.Services.Camera
{
    /// <summary>
    /// [Tracking] 자동 추적 제어 서비스
    /// 
    /// 탐지 객체 [Bounding Box] 중심점과
    /// EO 영상 중심점을 비교한 뒤,
    /// 현재 [Zoom] 배율 기준 화각을 이용하여
    /// [Pixel] 오차를 [Pan] / [Tilt] 보정 각도로 변환한다.
    /// 
    /// 변환된 각도는 [Relative 이동] 명령으로 송신한다.
    /// </summary>
    internal class TrackingControlService
    {
        #region [Constants]

        /// <summary>
        /// EO 영상 가로 해상도
        /// </summary>
        private const double FRAME_WIDTH =
            1920.0;

        /// <summary>
        /// EO 영상 세로 해상도
        /// </summary>
        private const double FRAME_HEIGHT =
            1080.0;

        /// <summary>
        /// X축 중심 오차 허용 범위 [Pixel]
        /// 
        /// 탐지 객체 중심점과 영상 중심점의 X축 차이가
        /// 해당 값 이하이면 중앙 근처로 판단하여 Pan 보정을 수행하지 않는다.
        /// 
        /// 작은 오차까지 계속 보정하면서 발생할 수 있는
        /// 카메라 떨림 / 헌팅 현상을 방지하기 위해 사용한다.
        /// </summary>
        private const double DEAD_ZONE_X_PIXEL =
            100.0;

        /// <summary>
        /// Y축 중심 오차 허용 범위 [Pixel]
        /// 
        /// 탐지 객체 중심점과 영상 중심점의 Y축 차이가
        /// 해당 값 이하이면 중앙 근처로 판단하여 Tilt 보정을 수행하지 않는다.
        /// 
        /// 작은 오차까지 계속 보정하면서 발생할 수 있는
        /// 카메라 떨림 / 헌팅 현상을 방지하기 위해 사용한다.
        /// </summary>
        private const double DEAD_ZONE_Y_PIXEL =
            60.0;

        /// <summary>
        /// 자동 추적 명령 처리 제한 시간 [ms]
        /// </summary>
        private const double TRACKING_COMMAND_INTERVAL_MS =
            500.0;

        /// <summary>
        /// [Tracking] 중배율 Zoom 기준
        /// 
        /// 저배율보다 보수적인 Tracking 명령 송신 조건을
        /// 적용하기 위한 기준값이다.
        /// </summary>
        private const double MIDDLE_ZOOM_RATIO =
            10.0;

        /// <summary>
        /// [Tracking] 고배율 Zoom 기준
        /// 
        /// 작은 Relative 명령도 화면상 크게 보이는 구간으로 판단하여
        /// 명령 송신 간격 / 최소 송신 각도 / Dead Zone을 보정한다.
        /// </summary>
        private const double HIGH_ZOOM_RATIO =
            20.0;

        /// <summary>
        /// [Tracking] 중배율 Tracking 명령 처리 제한 시간 [ms]
        /// </summary>
        private const double MIDDLE_ZOOM_TRACKING_COMMAND_INTERVAL_MS =
            700.0;

        /// <summary>
        /// [Tracking] 고배율 Tracking 명령 처리 제한 시간 [ms]
        /// 
        /// 고배율 Zoom 상태에서는 작은 Relative 명령이
        /// 짧은 간격으로 반복 송신되면 화면상 떨림처럼 보일 수 있으므로,
        /// 명령 처리 간격을 늘린다.
        /// </summary>
        private const double HIGH_ZOOM_TRACKING_COMMAND_INTERVAL_MS =
            900.0;

        /// <summary>
        /// [Zoom] 최소 배율
        /// </summary>
        private const double MIN_ZOOM_RATIO =
            1.0;

        /// <summary>
        /// [Zoom] 최대 배율
        /// 
        /// 렌즈 사양 기준 최대 [66배]를 사용한다.
        /// </summary>
        private const double MAX_ZOOM_RATIO =
            66.0;

        /// <summary>
        /// [Zoom] 최소 Position
        /// </summary>
        private const double MIN_ZOOM_POSITION =
            0.0;

        /// <summary>
        /// [Zoom] 최대 Position
        /// </summary>
        private const double MAX_ZOOM_POSITION =
            1000.0;

        /// <summary>
        /// [Wide] 기준 수평 화각 [Degree]
        /// 
        /// 렌즈 사양 기준:
        /// Horizontal = 26.1°
        /// </summary>
        private const double WIDE_HORIZONTAL_FOV_DEGREE =
            26.1;

        /// <summary>
        /// [Wide] 기준 수직 화각 [Degree]
        /// 
        /// 렌즈 사양 기준:
        /// Vertical = 15.0°
        /// </summary>
        private const double WIDE_VERTICAL_FOV_DEGREE =
            15.0;

        /// <summary>
        /// [Tele] 기준 수평 화각 [Degree]
        /// 
        /// 렌즈 사양 기준:
        /// Horizontal = 0.44°
        /// </summary>
        private const double TELE_HORIZONTAL_FOV_DEGREE =
            0.44;

        /// <summary>
        /// [Tele] 기준 수직 화각 [Degree]
        /// 
        /// 렌즈 사양 기준:
        /// Vertical = 0.25°
        /// </summary>
        private const double TELE_VERTICAL_FOV_DEGREE =
            0.25;

        /// <summary>
        /// 자동 추적 Relative 이동 최소 각도 [Degree]
        /// 
        /// 너무 작은 각도 명령이 반복 송신되지 않도록 제한한다.
        /// </summary>
        private const double MIN_TRACKING_ANGLE_DEGREE =
            0.02;

        /// <summary>
        /// [Tracking] 중배율 최소 송신 각도 [Degree]
        /// 
        /// 중배율 Zoom 상태에서 미세 Relative 명령이
        /// 반복 송신되는 현상을 줄이기 위해 사용한다.
        /// </summary>
        private const double MIDDLE_ZOOM_MIN_TRACKING_ANGLE_DEGREE =
            0.04;

        /// <summary>
        /// [Tracking] 고배율 최소 송신 각도 [Degree]
        /// 
        /// 고배율 Zoom 상태에서 너무 작은 Relative 명령이 반복 송신되면
        /// 화면상 떨림처럼 보일 수 있으므로,
        /// 해당 값 이상 누적되었을 때만 송신한다.
        /// </summary>
        private const double HIGH_ZOOM_MIN_TRACKING_ANGLE_DEGREE =
            0.06;

        /// <summary>
        /// [Tracking] 고배율 Dead Zone 확대 비율
        /// 
        /// 고배율 Zoom 상태에서 중심 근처 미세 추적 명령이 반복되지 않도록
        /// Pixel Dead Zone을 소폭 확대한다.
        /// </summary>
        private const double HIGH_ZOOM_DEAD_ZONE_SCALE =
            1.4;

        /// <summary>
        /// [Tracking] 중배율 누적 송신 최대 각도 [Degree]
        /// 
        /// 누적된 Tracking 보정 각도가 과도하게 커지는 것을 방지한다.
        /// </summary>
        private const double MIDDLE_ZOOM_MAX_COMMAND_ANGLE_DEGREE =
            0.14;

        /// <summary>
        /// [Tracking] 고배율 누적 송신 최대 각도 [Degree]
        /// 
        /// 고배율 Zoom 상태에서 한 번에 과도한 Relative 명령이 송신되지 않도록 제한한다.
        /// </summary>
        private const double HIGH_ZOOM_MAX_COMMAND_ANGLE_DEGREE =
            0.10;

        /// <summary>
        /// [Tracking] 기본 누적 송신 최대 각도 [Degree]
        /// </summary>
        private const double DEFAULT_MAX_COMMAND_ANGLE_DEGREE =
            0.20;

        #endregion

        #region [Fields]

        /// <summary>
        /// [ADS1000] 카메라 제어 서비스
        /// 
        /// 자동 추적 계산 결과를 실제 [Pan] / [Tilt] Relative 이동 명령으로 송신한다.
        /// </summary>
        private readonly Ads1000CameraControlService _ads1000CameraControlService;

        /// <summary>
        /// 마지막 자동 추적 처리 시간
        /// </summary>
        private DateTime _lastTrackingCommandTime =
            DateTime.MinValue;

        /// <summary>
        /// [Tracking] 보정값 안정화 서비스
        /// 
        /// 고배율 Zoom 상태에서 발생하는
        /// Pan / Tilt 미세 떨림과 헌팅 현상을 줄이기 위해 사용한다.
        /// </summary>
        private readonly TrackingControlStabilizer _trackingControlStabilizer =
            new TrackingControlStabilizer();

        /// <summary>
        /// [Tracking] 누적 Pan 보정 각도
        /// 
        /// 고배율 Zoom 상태에서 너무 작은 Pan Relative 명령이
        /// 짧게 반복 송신되는 것을 방지하기 위해 사용한다.
        /// </summary>
        private double _pendingPanAngleOffset;

        /// <summary>
        /// [Tracking] 누적 Tilt 보정 각도
        /// 
        /// 고배율 Zoom 상태에서 너무 작은 Tilt Relative 명령이
        /// 짧게 반복 송신되는 것을 방지하기 위해 사용한다.
        /// </summary>
        private double _pendingTiltAngleOffset;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [TrackingControlService] 생성자
        /// </summary>
        /// <param name="ads1000CameraControlService">
        /// [ADS1000] 카메라 제어 서비스
        /// </param>
        public TrackingControlService(
            Ads1000CameraControlService ads1000CameraControlService)
        {
            _ads1000CameraControlService =
                ads1000CameraControlService
                ?? throw new ArgumentNullException(
                    nameof(ads1000CameraControlService));
        }

        #endregion

        #region [Public Methods]

        /// <summary>
        /// [AUTO] 추적 처리
        /// 
        /// 탐지 객체 [Bounding Box] 중심점과 영상 중심점을 비교한 뒤,
        /// 현재 [Zoom] 배율 기준 화각을 이용하여
        /// [Pixel] 오차를 [Pan] / [Tilt] 보정 각도로 변환한다.
        /// 
        /// 변환된 각도는 [Relative 이동] 명령으로 송신한다.
        /// </summary>
        /// <param name="boundingBox">
        /// 탐지 객체 영역 정보
        /// </param>
        /// <param name="currentZoomPosition">
        /// 현재 [Zoom] Position 값
        /// </param>
        public void ProcessTracking(
            DetectionBoundingBox boundingBox,
            double currentZoomPosition)
        {
            double currentZoomRatio =
                CalculateZoomRatioByPosition(
                    currentZoomPosition);

            if (!CanProcessTracking(
                    currentZoomRatio))
            {
                return;
            }

            ConsoleLogHelper.WriteLine(
                "[TRACKING][AUTO] Tracking Check");

            if (boundingBox == null)
            {
                ConsoleLogHelper.WriteLine(
                    "[TRACKING][AUTO] Failed : Bounding Box is null");

                return;
            }

            if (!boundingBox.CenterX.HasValue ||
                !boundingBox.CenterY.HasValue)
            {
                ConsoleLogHelper.WriteLine(
                    "[TRACKING][AUTO] Failed : Bounding Box Center is invalid");

                return;
            }

            double frameCenterX =
                FRAME_WIDTH / 2.0;

            double frameCenterY =
                FRAME_HEIGHT / 2.0;

            double errorX =
                boundingBox.CenterX.Value - frameCenterX;

            double errorY =
                boundingBox.CenterY.Value - frameCenterY;

            if (IsInDeadZone(
                    errorX,
                    errorY,
                    currentZoomRatio))
            {
                HandleTrackingDeadZone();

                return;
            }

            double horizontalFov =
                CalculateHorizontalFov(
                    currentZoomRatio);

            double verticalFov =
                CalculateVerticalFov(
                    currentZoomRatio);

            double panAngleOffset =
                CalculatePanAngleOffset(
                    errorX,
                    horizontalFov);

            double tiltAngleOffset =
                CalculateTiltAngleOffset(
                    errorY,
                    verticalFov);

            PrintAngleTrackingLog(
                boundingBox,
                frameCenterX,
                frameCenterY,
                errorX,
                errorY,
                currentZoomPosition,
                currentZoomRatio,
                horizontalFov,
                verticalFov,
                panAngleOffset,
                tiltAngleOffset);

            ExecuteTrackingAngleCommand(
                panAngleOffset,
                tiltAngleOffset,
                currentZoomRatio);
        }

        /// <summary>
        /// [AUTO] 추적 정지
        /// 
        /// 추적 종료 / [MANUAL] 전환 시 진행 중인 [Pan] / [Tilt] 이동을 정지한다.
        /// </summary>
        public void StopTracking()
        {
            ConsoleLogHelper.WriteLine(
                "[TRACKING][AUTO] Tracking Stop");

            _trackingControlStabilizer
                .Reset();

            ResetPendingTrackingAngle();

            StopPtz();
        }

        #endregion

        #region [Tracking Check Methods]

        /// <summary>
        /// 자동 추적 처리 가능 여부 확인
        /// 
        /// [Detect Continue]가 짧은 주기로 반복 수신될 수 있으므로,
        /// 현재 Zoom 배율 기준 제한 시간 이내의 중복 추적 명령은 무시한다.
        /// </summary>
        /// <param name="currentZoomRatio">
        /// 현재 Zoom 배율
        /// </param>
        /// <returns>
        /// 자동 추적 처리 가능 여부
        /// </returns>
        private bool CanProcessTracking(
            double currentZoomRatio)
        {
            DateTime currentTime =
                DateTime.Now;

            double elapsedMilliseconds =
                (currentTime - _lastTrackingCommandTime)
                    .TotalMilliseconds;

            double trackingCommandIntervalMs =
                GetTrackingCommandIntervalMs(
                    currentZoomRatio);

            if (elapsedMilliseconds <
                trackingCommandIntervalMs)
            {
                ConsoleLogHelper.WriteLine(
                    "[TRACKING][AUTO] Skip : Tracking Interval");

                return false;
            }

            _lastTrackingCommandTime =
                currentTime;

            return true;
        }

        /// <summary>
        /// [Tracking] Tracking 명령 처리 제한 시간 조회
        /// 
        /// 고배율 Zoom 상태에서는 작은 Relative 이동도 화면상 크게 보이므로,
        /// 명령 처리 간격을 늘려 짧은 이동 명령이 반복 송신되는 현상을 줄인다.
        /// </summary>
        /// <param name="currentZoomRatio">
        /// 현재 Zoom 배율
        /// </param>
        /// <returns>
        /// Tracking 명령 처리 제한 시간 [ms]
        /// </returns>
        private double GetTrackingCommandIntervalMs(
            double currentZoomRatio)
        {
            if (currentZoomRatio >= HIGH_ZOOM_RATIO)
            {
                return HIGH_ZOOM_TRACKING_COMMAND_INTERVAL_MS;
            }

            if (currentZoomRatio >= MIDDLE_ZOOM_RATIO)
            {
                return MIDDLE_ZOOM_TRACKING_COMMAND_INTERVAL_MS;
            }

            return TRACKING_COMMAND_INTERVAL_MS;
        }

        /// <summary>
        /// [Tracking] Dead Zone 여부 확인
        /// 
        /// 탐지 객체 중심점이 영상 중심점 기준 Dead Zone 내부에 있는지 확인한다.
        /// 
        /// 고배율 Zoom 상태에서는 중심 근처 미세 이동 명령이 반복될 경우
        /// 화면상 떨림으로 보일 수 있으므로,
        /// Pixel Dead Zone을 소폭 확대한다.
        /// </summary>
        /// <param name="errorX">
        /// 영상 중심 기준 X축 Pixel 오차
        /// </param>
        /// <param name="errorY">
        /// 영상 중심 기준 Y축 Pixel 오차
        /// </param>
        /// <param name="currentZoomRatio">
        /// 현재 Zoom 배율
        /// </param>
        /// <returns>
        /// Dead Zone 내부 여부
        /// </returns>
        private bool IsInDeadZone(
            double errorX,
            double errorY,
            double currentZoomRatio)
        {
            double deadZoneScale =
                currentZoomRatio >= HIGH_ZOOM_RATIO
                    ? HIGH_ZOOM_DEAD_ZONE_SCALE
                    : 1.0;

            double deadZoneX =
                DEAD_ZONE_X_PIXEL * deadZoneScale;

            double deadZoneY =
                DEAD_ZONE_Y_PIXEL * deadZoneScale;

            return Math.Abs(errorX) <= deadZoneX &&
                   Math.Abs(errorY) <= deadZoneY;
        }

        /// <summary>
        /// [AUTO] Dead Zone 진입 처리
        /// 
        /// 추적 대상이 중심 허용 범위에 들어온 경우
        /// 이전 Tracking 필터 값과 누적 보정 각도를 초기화한다.
        /// 
        /// Relative 이동 명령은 지정 각도 이동 후 장비가 정지하므로,
        /// Dead Zone 상태에서 매번 Stop 명령을 반복 송신하지 않는다.
        /// </summary>
        private void HandleTrackingDeadZone()
        {
            ConsoleLogHelper.WriteLine(
                "[TRACKING][AUTO] Stop : Target is in Dead Zone");

            _trackingControlStabilizer
                .Reset();

            ResetPendingTrackingAngle();
        }

        #endregion

        #region [Tracking Calculation Methods]

        /// <summary>
        /// [Zoom] Position 기준 현재 배율 계산
        /// 
        /// [0] → [1배]
        /// [1000] → [66배]
        /// 기준으로 선형 변환한다.
        /// </summary>
        /// <param name="zoomPosition">
        /// 현재 [Zoom] Position 값
        /// </param>
        /// <returns>
        /// 현재 [Zoom] 배율
        /// </returns>
        private double CalculateZoomRatioByPosition(
            double zoomPosition)
        {
            double clampedZoomPosition =
                Clamp(
                    zoomPosition,
                    MIN_ZOOM_POSITION,
                    MAX_ZOOM_POSITION);

            double ratio =
                clampedZoomPosition
                / MAX_ZOOM_POSITION;

            return MIN_ZOOM_RATIO +
                ((MAX_ZOOM_RATIO - MIN_ZOOM_RATIO) * ratio);
        }

        /// <summary>
        /// 현재 [Zoom] 배율 기준 수평 화각 계산
        /// 
        /// 렌즈는 배율이 증가할수록 화각이 좁아지므로,
        /// [Wide] 화각을 현재 배율로 나누는 역비례 근사식을 사용한다.
        /// 
        /// 최저 화각은 [Tele] 화각보다 작아지지 않도록 제한한다.
        /// </summary>
        /// <param name="zoomRatio">
        /// 현재 [Zoom] 배율
        /// </param>
        /// <returns>
        /// 현재 수평 화각 [Degree]
        /// </returns>
        private double CalculateHorizontalFov(
            double zoomRatio)
        {
            double clampedZoomRatio =
                Clamp(
                    zoomRatio,
                    MIN_ZOOM_RATIO,
                    MAX_ZOOM_RATIO);

            double horizontalFov =
                WIDE_HORIZONTAL_FOV_DEGREE
                / clampedZoomRatio;

            if (horizontalFov <
                TELE_HORIZONTAL_FOV_DEGREE)
            {
                return TELE_HORIZONTAL_FOV_DEGREE;
            }

            return horizontalFov;
        }

        /// <summary>
        /// 현재 [Zoom] 배율 기준 수직 화각 계산
        /// 
        /// 렌즈는 배율이 증가할수록 화각이 좁아지므로,
        /// [Wide] 화각을 현재 배율로 나누는 역비례 근사식을 사용한다.
        /// 
        /// 최저 화각은 [Tele] 화각보다 작아지지 않도록 제한한다.
        /// </summary>
        /// <param name="zoomRatio">
        /// 현재 [Zoom] 배율
        /// </param>
        /// <returns>
        /// 현재 수직 화각 [Degree]
        /// </returns>
        private double CalculateVerticalFov(
            double zoomRatio)
        {
            double clampedZoomRatio =
                Clamp(
                    zoomRatio,
                    MIN_ZOOM_RATIO,
                    MAX_ZOOM_RATIO);

            double verticalFov =
                WIDE_VERTICAL_FOV_DEGREE
                / clampedZoomRatio;

            if (verticalFov <
                TELE_VERTICAL_FOV_DEGREE)
            {
                return TELE_VERTICAL_FOV_DEGREE;
            }

            return verticalFov;
        }

        /// <summary>
        /// X축 [Pixel] 오차를 [Pan] 보정 각도로 변환
        /// </summary>
        /// <param name="errorX">
        /// X축 중심 오차 [Pixel]
        /// </param>
        /// <param name="horizontalFov">
        /// 현재 수평 화각 [Degree]
        /// </param>
        /// <returns>
        /// [Pan] 보정 각도 [Degree]
        /// </returns>
        private double CalculatePanAngleOffset(
            double errorX,
            double horizontalFov)
        {
            return errorX
                / FRAME_WIDTH
                * horizontalFov;
        }

        /// <summary>
        /// Y축 [Pixel] 오차를 [Tilt] 보정 각도로 변환
        /// 
        /// 화면 좌표계는 아래 방향이 [+]이고,
        /// 장비 [Tilt]는 위 방향을 [+]로 사용하므로 부호를 반전한다.
        /// </summary>
        /// <param name="errorY">
        /// Y축 중심 오차 [Pixel]
        /// </param>
        /// <param name="verticalFov">
        /// 현재 수직 화각 [Degree]
        /// </param>
        /// <returns>
        /// [Tilt] 보정 각도 [Degree]
        /// </returns>
        private double CalculateTiltAngleOffset(
            double errorY,
            double verticalFov)
        {
            return -errorY
                / FRAME_HEIGHT
                * verticalFov;
        }

        /// <summary>
        /// 최소 Tracking 명령 각도 조회
        /// 
        /// 고배율 Zoom 상태에서는 작은 Relative 명령이 반복될 경우
        /// 화면상 떨림으로 보일 수 있으므로,
        /// 최소 송신 각도를 높여 불필요한 미세 이동을 줄인다.
        /// </summary>
        /// <param name="currentZoomRatio">
        /// 현재 Zoom 배율
        /// </param>
        /// <returns>
        /// 최소 Tracking 명령 각도 [Degree]
        /// </returns>
        private double GetMinimumTrackingAngleDegree(
            double currentZoomRatio)
        {
            if (currentZoomRatio >= HIGH_ZOOM_RATIO)
            {
                return HIGH_ZOOM_MIN_TRACKING_ANGLE_DEGREE;
            }

            if (currentZoomRatio >= MIDDLE_ZOOM_RATIO)
            {
                return MIDDLE_ZOOM_MIN_TRACKING_ANGLE_DEGREE;
            }

            return MIN_TRACKING_ANGLE_DEGREE;
        }

        /// <summary>
        /// 최대 Tracking 명령 각도 조회
        /// 
        /// 누적된 보정 각도가 과도하게 커져
        /// 한 번에 큰 Relative 이동 명령이 송신되지 않도록 제한한다.
        /// </summary>
        /// <param name="currentZoomRatio">
        /// 현재 Zoom 배율
        /// </param>
        /// <returns>
        /// 최대 Tracking 명령 각도 [Degree]
        /// </returns>
        private double GetMaximumTrackingCommandAngleDegree(
            double currentZoomRatio)
        {
            if (currentZoomRatio >= HIGH_ZOOM_RATIO)
            {
                return HIGH_ZOOM_MAX_COMMAND_ANGLE_DEGREE;
            }

            if (currentZoomRatio >= MIDDLE_ZOOM_RATIO)
            {
                return MIDDLE_ZOOM_MAX_COMMAND_ANGLE_DEGREE;
            }

            return DEFAULT_MAX_COMMAND_ANGLE_DEGREE;
        }

        /// <summary>
        /// Tracking 송신 각도 생성
        /// 
        /// 안정화된 보정 각도를 바로 송신하지 않고,
        /// 최소 송신 각도에 도달할 때까지 누적한다.
        /// 
        /// 이를 통해 고배율 Zoom 상태에서
        /// 너무 짧은 Relative 명령이 반복 송신되는 현상을 줄인다.
        /// </summary>
        /// <param name="stableAngleOffset">
        /// 안정화된 보정 각도
        /// </param>
        /// <param name="pendingAngleOffset">
        /// 누적 보정 각도
        /// </param>
        /// <param name="minimumTrackingAngleDegree">
        /// 최소 송신 각도
        /// </param>
        /// <param name="maximumTrackingCommandAngleDegree">
        /// 최대 송신 각도
        /// </param>
        /// <param name="axisName">
        /// 축 이름
        /// </param>
        /// <returns>
        /// 실제 송신할 보정 각도
        /// </returns>
        private double CreateTrackingCommandAngle(
            double stableAngleOffset,
            ref double pendingAngleOffset,
            double minimumTrackingAngleDegree,
            double maximumTrackingCommandAngleDegree,
            string axisName)
        {
            if (Math.Abs(stableAngleOffset) <= 0.0)
            {
                return 0.0;
            }

            if (IsDirectionChanged(
                    pendingAngleOffset,
                    stableAngleOffset))
            {
                pendingAngleOffset =
                    0.0;

                ConsoleLogHelper.WriteLine(
                    "[TRACKING][AUTO] Pending "
                    + axisName
                    + " Reset : Direction Changed");
            }

            pendingAngleOffset +=
                stableAngleOffset;

            ConsoleLogHelper.WriteLine(
                "[TRACKING][AUTO] Pending "
                + axisName
                + " : "
                + pendingAngleOffset.ToString("F4"));

            if (Math.Abs(pendingAngleOffset) <
                minimumTrackingAngleDegree)
            {
                return 0.0;
            }

            double commandAngleOffset =
                ClampByAbsoluteValue(
                    pendingAngleOffset,
                    maximumTrackingCommandAngleDegree);

            pendingAngleOffset =
                0.0;

            return commandAngleOffset;
        }

        /// <summary>
        /// 방향 변경 여부 조회
        /// </summary>
        /// <param name="previousValue">
        /// 이전 누적값
        /// </param>
        /// <param name="currentValue">
        /// 현재 보정값
        /// </param>
        /// <returns>
        /// 방향 변경 여부
        /// </returns>
        private bool IsDirectionChanged(
            double previousValue,
            double currentValue)
        {
            if (Math.Abs(previousValue) <= 0.0 ||
                Math.Abs(currentValue) <= 0.0)
            {
                return false;
            }

            return Math.Sign(previousValue) !=
                   Math.Sign(currentValue);
        }

        /// <summary>
        /// 절대값 기준 범위 보정
        /// </summary>
        /// <param name="value">
        /// 입력값
        /// </param>
        /// <param name="maxAbsoluteValue">
        /// 최대 절대값
        /// </param>
        /// <returns>
        /// 보정된 값
        /// </returns>
        private double ClampByAbsoluteValue(
            double value,
            double maxAbsoluteValue)
        {
            if (value > maxAbsoluteValue)
            {
                return maxAbsoluteValue;
            }

            if (value < -maxAbsoluteValue)
            {
                return -maxAbsoluteValue;
            }

            return value;
        }

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

        #region [Tracking Command Methods]

        /// <summary>
        /// 자동 추적 각도 보정 명령 실행
        /// 
        /// [Pixel] 오차를 화각 기준 [Degree]로 변환한 값을
        /// 바로 송신하지 않고, Tracking 안정화 처리를 적용한 뒤
        /// 최소 송신 각도까지 누적하여
        /// [Pan] / [Tilt] Relative 이동 명령으로 송신한다.
        /// 
        /// Relative 이동은 지정 각도만큼 이동 후 장비가 정지하므로,
        /// Continuous 이동에서 사용하던 자동 정지 예약은 사용하지 않는다.
        /// </summary>
        /// <param name="panAngleOffset">
        /// [Pan] 원본 보정 각도 [Degree]
        /// </param>
        /// <param name="tiltAngleOffset">
        /// [Tilt] 원본 보정 각도 [Degree]
        /// </param>
        /// <param name="currentZoomRatio">
        /// 현재 Zoom 배율
        /// </param>
        private void ExecuteTrackingAngleCommand(
            double panAngleOffset,
            double tiltAngleOffset,
            double currentZoomRatio)
        {
            if (!_trackingControlStabilizer.TryCreateStableAngle(
                    panAngleOffset,
                    tiltAngleOffset,
                    currentZoomRatio,
                    out double stablePanAngleOffset,
                    out double stableTiltAngleOffset))
            {
                ConsoleLogHelper.WriteLine(
                    "[TRACKING][AUTO] Skip : Stabilized Angle Offset is too small");

                return;
            }

            PrintStabilizedTrackingLog(
                panAngleOffset,
                tiltAngleOffset,
                stablePanAngleOffset,
                stableTiltAngleOffset,
                currentZoomRatio);

            double minimumTrackingAngleDegree =
                GetMinimumTrackingAngleDegree(
                    currentZoomRatio);

            double maximumTrackingCommandAngleDegree =
                GetMaximumTrackingCommandAngleDegree(
                    currentZoomRatio);

            ConsoleLogHelper.WriteLine(
                "[TRACKING][AUTO] Minimum Angle : "
                + minimumTrackingAngleDegree.ToString("F4"));

            ConsoleLogHelper.WriteLine(
                "[TRACKING][AUTO] Maximum Angle : "
                + maximumTrackingCommandAngleDegree.ToString("F4"));

            double panCommandAngleOffset =
                CreateTrackingCommandAngle(
                    stablePanAngleOffset,
                    ref _pendingPanAngleOffset,
                    minimumTrackingAngleDegree,
                    maximumTrackingCommandAngleDegree,
                    "Pan");

            double tiltCommandAngleOffset =
                CreateTrackingCommandAngle(
                    stableTiltAngleOffset,
                    ref _pendingTiltAngleOffset,
                    minimumTrackingAngleDegree,
                    maximumTrackingCommandAngleDegree,
                    "Tilt");

            bool hasPanMove =
                Math.Abs(
                    panCommandAngleOffset)
                >= minimumTrackingAngleDegree;

            bool hasTiltMove =
                Math.Abs(
                    tiltCommandAngleOffset)
                >= minimumTrackingAngleDegree;

            if (!hasPanMove &&
                !hasTiltMove)
            {
                ConsoleLogHelper.WriteLine(
                    "[TRACKING][AUTO] Skip : Accumulated Angle Offset is too small");

                return;
            }

            if (hasPanMove)
            {
                ConsoleLogHelper.WriteLine(
                    "[TRACKING][AUTO] Send Pan : "
                    + panCommandAngleOffset.ToString("F4"));

                _ads1000CameraControlService
                    .MovePanRelative(
                        panCommandAngleOffset);
            }

            if (hasTiltMove)
            {
                ConsoleLogHelper.WriteLine(
                    "[TRACKING][AUTO] Send Tilt : "
                    + tiltCommandAngleOffset.ToString("F4"));

                _ads1000CameraControlService
                    .MoveTiltRelative(
                        tiltCommandAngleOffset);
            }

        }

        /// <summary>
        /// [Tracking] 누적 보정 각도 초기화
        /// </summary>
        private void ResetPendingTrackingAngle()
        {
            _pendingPanAngleOffset =
                0.0;

            _pendingTiltAngleOffset =
                0.0;
        }

        /// <summary>
        /// [Pan] / [Tilt] 이동 정지
        /// 
        /// 자동 추적은 [Pan] / [Tilt] 제어만 수행하므로,
        /// 추적 정지 시 [Zoom] / [Focus] 정지 명령은 송신하지 않는다.
        /// </summary>
        private void StopPtz()
        {
            _ads1000CameraControlService
                .StopPanTiltMove();
        }

        #endregion

        #region [Log Methods]

        /// <summary>
        /// [AUTO] 화각 기반 추적 계산 로그 출력
        /// </summary>
        private void PrintAngleTrackingLog(
            DetectionBoundingBox boundingBox,
            double frameCenterX,
            double frameCenterY,
            double errorX,
            double errorY,
            double currentZoomPosition,
            double currentZoomRatio,
            double horizontalFov,
            double verticalFov,
            double panAngleOffset,
            double tiltAngleOffset)
        {
            ConsoleLogHelper.WriteLine(
                "[TRACKING][AUTO] Angle Tracking Calculate");

            ConsoleLogHelper.WriteLine(
                "[TRACKING][BBOX] Center X : "
                + boundingBox.CenterX);

            ConsoleLogHelper.WriteLine(
                "[TRACKING][BBOX] Center Y : "
                + boundingBox.CenterY);

            ConsoleLogHelper.WriteLine(
                "[TRACKING][FRAME] Center X : "
                + frameCenterX);

            ConsoleLogHelper.WriteLine(
                "[TRACKING][FRAME] Center Y : "
                + frameCenterY);

            ConsoleLogHelper.WriteLine(
                "[TRACKING][ERROR] X : "
                + errorX);

            ConsoleLogHelper.WriteLine(
                "[TRACKING][ERROR] Y : "
                + errorY);

            ConsoleLogHelper.WriteLine(
                "[TRACKING][ZOOM] Position : "
                + currentZoomPosition);

            ConsoleLogHelper.WriteLine(
                "[TRACKING][ZOOM] Ratio : x"
                + currentZoomRatio.ToString("F1"));

            ConsoleLogHelper.WriteLine(
                "[TRACKING][FOV] Horizontal : "
                + horizontalFov.ToString("F2"));

            ConsoleLogHelper.WriteLine(
                "[TRACKING][FOV] Vertical : "
                + verticalFov.ToString("F2"));

            ConsoleLogHelper.WriteLine(
                "[TRACKING][ANGLE] Pan Offset : "
                + panAngleOffset.ToString("F2"));

            ConsoleLogHelper.WriteLine(
                "[TRACKING][ANGLE] Tilt Offset : "
                + tiltAngleOffset.ToString("F2"));
        }

        /// <summary>
        /// [AUTO] 안정화된 추적 보정 각도 로그 출력
        /// </summary>
        private void PrintStabilizedTrackingLog(
            double rawPanAngleOffset,
            double rawTiltAngleOffset,
            double stablePanAngleOffset,
            double stableTiltAngleOffset,
            double currentZoomRatio)
        {
            ConsoleLogHelper.WriteLine(
                "[TRACKING][STABILIZER] Angle Stabilized");

            ConsoleLogHelper.WriteLine(
                "[TRACKING][STABILIZER] Zoom Ratio : x"
                + currentZoomRatio.ToString("F1"));

            ConsoleLogHelper.WriteLine(
                "[TRACKING][STABILIZER] Raw Pan : "
                + rawPanAngleOffset.ToString("F4"));

            ConsoleLogHelper.WriteLine(
                "[TRACKING][STABILIZER] Raw Tilt : "
                + rawTiltAngleOffset.ToString("F4"));

            ConsoleLogHelper.WriteLine(
                "[TRACKING][STABILIZER] Stable Pan : "
                + stablePanAngleOffset.ToString("F4"));

            ConsoleLogHelper.WriteLine(
                "[TRACKING][STABILIZER] Stable Tilt : "
                + stableTiltAngleOffset.ToString("F4"));
        }
        #endregion
    }

}

using System;
using VertiportNexus.Models.Camera;
using VertiportNexus.Services.ADS1000;

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
        /// </summary>
        private const double DEAD_ZONE_X_PIXEL =
            320.0;

        /// <summary>
        /// Y축 중심 오차 허용 범위 [Pixel]
        /// </summary>
        private const double DEAD_ZONE_Y_PIXEL =
            180.0;

        /// <summary>
        /// 자동 추적 명령 처리 제한 시간 [ms]
        /// </summary>
        private const double TRACKING_COMMAND_INTERVAL_MS =
            500.0;

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
            if (!CanProcessTracking())
            {
                return;
            }

            Console.WriteLine(
                "[TRACKING][AUTO] Tracking Check");

            if (boundingBox == null)
            {
                Console.WriteLine(
                    "[TRACKING][AUTO] Failed : Bounding Box is null");

                return;
            }

            if (!boundingBox.CenterX.HasValue ||
                !boundingBox.CenterY.HasValue)
            {
                Console.WriteLine(
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
                errorY))
            {
                Console.WriteLine(
                    "[TRACKING][AUTO] Stop : Target is in Dead Zone");

                StopTracking();

                return;
            }

            double currentZoomRatio =
                CalculateZoomRatioByPosition(
                    currentZoomPosition);

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
                tiltAngleOffset);
        }

        /// <summary>
        /// [AUTO] 추적 정지
        /// 
        /// 추적 대상이 중심 허용 범위에 들어오거나,
        /// 추적 종료 / [MANUAL] 전환 시 진행 중인 [Pan] / [Tilt] 이동을 정지한다.
        /// </summary>
        public void StopTracking()
        {
            Console.WriteLine(
                "[TRACKING][AUTO] Tracking Stop");

            StopPtz();
        }

        #endregion

        #region [Tracking Check Methods]

        /// <summary>
        /// 자동 추적 처리 가능 여부 확인
        /// 
        /// [Detect Continue]가 짧은 주기로 반복 수신될 수 있으므로,
        /// 지정 시간 이내의 중복 추적 명령은 무시한다.
        /// </summary>
        /// <returns>
        /// 자동 추적 처리 가능 여부
        /// </returns>
        private bool CanProcessTracking()
        {
            DateTime currentTime =
                DateTime.Now;

            double elapsedMilliseconds =
                (currentTime - _lastTrackingCommandTime)
                    .TotalMilliseconds;

            if (elapsedMilliseconds <
                TRACKING_COMMAND_INTERVAL_MS)
            {
                Console.WriteLine(
                    "[TRACKING][AUTO] Skip : Tracking Interval");

                return false;
            }

            _lastTrackingCommandTime =
                currentTime;

            return true;
        }

        /// <summary>
        /// 중심 오차가 허용 범위 안에 있는지 확인
        /// </summary>
        /// <param name="errorX">
        /// X축 중심 오차 [Pixel]
        /// </param>
        /// <param name="errorY">
        /// Y축 중심 오차 [Pixel]
        /// </param>
        /// <returns>
        /// 중심 허용 범위 포함 여부
        /// </returns>
        private bool IsInDeadZone(
            double errorX,
            double errorY)
        {
            return
                Math.Abs(errorX) <= DEAD_ZONE_X_PIXEL &&
                Math.Abs(errorY) <= DEAD_ZONE_Y_PIXEL;
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
        /// [Pan] / [Tilt] Relative 이동 명령으로 송신한다.
        /// 
        /// Relative 이동은 지정 각도만큼 이동 후 장비가 정지하므로,
        /// Continuous 이동에서 사용하던 자동 정지 예약은 사용하지 않는다.
        /// </summary>
        /// <param name="panAngleOffset">
        /// [Pan] 보정 각도 [Degree]
        /// </param>
        /// <param name="tiltAngleOffset">
        /// [Tilt] 보정 각도 [Degree]
        /// </param>
        private void ExecuteTrackingAngleCommand(
            double panAngleOffset,
            double tiltAngleOffset)
        {
            bool hasPanMove =
                Math.Abs(
                    panAngleOffset)
                >= MIN_TRACKING_ANGLE_DEGREE;

            bool hasTiltMove =
                Math.Abs(
                    tiltAngleOffset)
                >= MIN_TRACKING_ANGLE_DEGREE;

            if (!hasPanMove &&
                !hasTiltMove)
            {
                Console.WriteLine(
                    "[TRACKING][AUTO] Skip : Angle Offset is too small");

                return;
            }

            if (hasPanMove)
            {
                _ads1000CameraControlService
                    .MovePanRelative(
                        panAngleOffset);
            }

            if (hasTiltMove)
            {
                _ads1000CameraControlService
                    .MoveTiltRelative(
                        tiltAngleOffset);
            }

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
            Console.WriteLine(
                "[TRACKING][AUTO] Angle Tracking Calculate");

            Console.WriteLine(
                "[TRACKING][BBOX] Center X : "
                + boundingBox.CenterX);

            Console.WriteLine(
                "[TRACKING][BBOX] Center Y : "
                + boundingBox.CenterY);

            Console.WriteLine(
                "[TRACKING][FRAME] Center X : "
                + frameCenterX);

            Console.WriteLine(
                "[TRACKING][FRAME] Center Y : "
                + frameCenterY);

            Console.WriteLine(
                "[TRACKING][ERROR] X : "
                + errorX);

            Console.WriteLine(
                "[TRACKING][ERROR] Y : "
                + errorY);

            Console.WriteLine(
                "[TRACKING][ZOOM] Position : "
                + currentZoomPosition);

            Console.WriteLine(
                "[TRACKING][ZOOM] Ratio : x"
                + currentZoomRatio.ToString("F1"));

            Console.WriteLine(
                "[TRACKING][FOV] Horizontal : "
                + horizontalFov.ToString("F4"));

            Console.WriteLine(
                "[TRACKING][FOV] Vertical : "
                + verticalFov.ToString("F4"));

            Console.WriteLine(
                "[TRACKING][ANGLE] Pan Offset : "
                + panAngleOffset.ToString("F4"));

            Console.WriteLine(
                "[TRACKING][ANGLE] Tilt Offset : "
                + tiltAngleOffset.ToString("F4"));
        }
        #endregion
    }

}

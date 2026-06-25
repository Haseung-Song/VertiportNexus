using System;
using System.Threading;
using System.Threading.Tasks;
using VertiportNexus.Models.Camera;
using VertiportNexus.Services.ADS1000;

namespace VertiportNexus.Services.Camera
{
    /// <summary>
    /// [Tracking] 자동 추적 제어 서비스
    /// 
    /// 탐지 객체 [Bounding Box] 중심점과
    /// 영상 중심점을 비교하여 [Pan] / [Tilt] 추적 방향과 속도를 계산한다.
    /// 
    /// 선임 코드의 이미지 추적 방식처럼
    /// [Relative 이동]이 아니라 [Continuous 이동 + Speed 제어] 방식으로 동작한다.
    /// </summary>
    internal class TrackingControlService
    {
        #region [Constants]

        /// <summary>
        /// EO 영상 가로 해상도
        /// </summary>
        private const double FRAME_WIDTH =
            1920;

        /// <summary>
        /// EO 영상 세로 해상도
        /// </summary>
        private const double FRAME_HEIGHT =
            1080;

        /// <summary>
        /// X축 중심 오차 허용 범위 [Pixel]
        /// </summary>
        private const double DEAD_ZONE_X_PIXEL =
            320;

        /// <summary>
        /// Y축 중심 오차 허용 범위 [Pixel]
        /// </summary>
        private const double DEAD_ZONE_Y_PIXEL =
            180;

        /// <summary>
        /// 자동 추적 명령 처리 제한 시간 [ms]
        /// </summary>
        private const double TRACKING_COMMAND_INTERVAL_MS =
            500;

        /// <summary>
        /// 자동 추적 명령 후 자동 정지 대기 시간 [ms]
        /// 
        /// [RabbitMQ] 수동 테스트처럼
        /// [Detect Continue]가 1회만 들어오는 경우에도
        /// 연속 이동 명령이 계속 유지되지 않도록 자동 정지를 수행한다.
        /// </summary>
        private const int TRACKING_AUTO_STOP_DELAY_MS =
            300;

        /// <summary>
        /// 자동 추적 최소 속도
        /// </summary>
        private const double MIN_TRACKING_SPEED =
            10;

        /// <summary>
        /// 자동 추적 최대 속도
        /// </summary>
        private const double MAX_TRACKING_SPEED =
            40;

        /// <summary>
        /// 속도 변경 무시 기준
        /// 
        /// 동일 방향에서 속도 차이가 해당 값보다 작으면
        /// 중복 명령으로 판단하여 재송신하지 않는다.
        /// </summary>
        private const double SPEED_CHANGE_THRESHOLD =
            3;

        #endregion

        #region [Fields]

        /// <summary>
        /// [ADS1000] 카메라 제어 서비스
        /// 
        /// 자동 추적 계산 결과를 실제 [Pan] / [Tilt] 연속 이동 명령으로 전송한다.
        /// </summary>
        private readonly Ads1000CameraControlService _ads1000CameraControlService;

        /// <summary>
        /// 마지막 자동 추적 처리 시간
        /// </summary>
        private DateTime _lastTrackingCommandTime =
            DateTime.MinValue;

        /// <summary>
        /// 마지막 [Pan] 이동 방향
        /// </summary>
        private string _lastPanDirection =
            "STOP";

        /// <summary>
        /// 마지막 [Tilt] 이동 방향
        /// </summary>
        private string _lastTiltDirection =
            "STOP";

        /// <summary>
        /// 마지막 추적 속도
        /// </summary>
        private double _lastTrackingSpeed =
            -1;

        /// <summary>
        /// 자동 추적 정지 예약 번호
        /// 
        /// 새 추적 명령이 발생할 때마다 증가시켜,
        /// 이전 정지 예약이 늦게 실행되더라도 무시할 수 있게 한다.
        /// </summary>
        private int _autoStopSequence;

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
        /// 탐지 객체 [Bounding Box] 중심점과 영상 중심점을 비교하여
        /// [Pan] / [Tilt] 이동 방향과 속도를 계산하고,
        /// 
        /// 실제 [ADS1000] 연속 이동 명령을 수행한다.
        /// </summary>
        /// <param name="boundingBox">
        /// 탐지 객체 영역 정보
        /// </param>
        public void ProcessTracking(
            DetectionBoundingBox boundingBox)
        {
            if (!CanProcessTracking())
            {
                return;
            }

            if (boundingBox == null)
            {
                Console.WriteLine("[TRACKING][AUTO] Failed : Bounding Box is null");
                return;
            }

            if (!boundingBox.CenterX.HasValue ||
                !boundingBox.CenterY.HasValue)
            {
                Console.WriteLine("[TRACKING][AUTO] Failed : Bounding Box Center is invalid");
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

            double trackingSpeed =
                CalculateTrackingSpeed(
                    errorX,
                    errorY);

            string panDirection =
                GetPanDirection(
                    errorX);

            string tiltDirection =
                GetTiltDirection(
                    errorY);

            PrintTrackingLog(
                boundingBox,
                frameCenterX,
                frameCenterY,
                errorX,
                errorY,
                panDirection,
                tiltDirection,
                trackingSpeed);

            ExecuteTrackingCommand(
                panDirection,
                tiltDirection,
                trackingSpeed);
        }

        /// <summary>
        /// [AUTO] 추적 정지
        /// 
        /// 추적 대상 유실 또는 [MANUAL] 전환 시
        /// 진행 중인 [Pan] / [Tilt] 연속 이동을 정지한다.
        /// </summary>
        public void StopTracking()
        {
            Interlocked.Increment(
                ref _autoStopSequence);

            Console.WriteLine(
                "[TRACKING][AUTO] Tracking Stop");

            StopPtz();

            _lastPanDirection =
                "STOP";

            _lastTiltDirection =
                "STOP";

            _lastTrackingSpeed =
                0;
        }

        #endregion

        #region [Tracking Check Methods]

        /// <summary>
        /// 자동 추적 처리 가능 여부 확인
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
                return false;
            }

            _lastTrackingCommandTime =
                currentTime;

            return true;
        }

        /// <summary>
        /// 중복 추적 명령 여부 확인
        /// 
        /// 이전 명령과 방향이 같고,
        /// 속도 차이가 작으면 중복 명령으로 판단한다.
        /// </summary>
        /// <param name="panDirection">
        /// [Pan] 이동 방향
        /// </param>
        /// <param name="tiltDirection">
        /// [Tilt] 이동 방향
        /// </param>
        /// <param name="trackingSpeed">
        /// 추적 속도
        /// </param>
        /// <returns>
        /// 중복 명령 여부
        /// </returns>
        private bool IsDuplicateTrackingCommand(
            string panDirection,
            string tiltDirection,
            double trackingSpeed)
        {
            bool isSameDirection =
                panDirection == _lastPanDirection &&
                tiltDirection == _lastTiltDirection;

            bool isSameSpeed =
                Math.Abs(
                    trackingSpeed - _lastTrackingSpeed)
                < SPEED_CHANGE_THRESHOLD;

            return
                isSameDirection &&
                isSameSpeed;
        }

        #endregion

        #region [Tracking Calculation Methods]

        /// <summary>
        /// [Pan] 이동 방향 계산
        /// </summary>
        /// <param name="errorX">
        /// X축 중심 오차 [Pixel]
        /// </param>
        /// <returns>
        /// [LEFT] / [RIGHT] / [STOP]
        /// </returns>
        private string GetPanDirection(
            double errorX)
        {
            if (Math.Abs(errorX) <=
                DEAD_ZONE_X_PIXEL)
            {
                return "STOP";
            }

            if (errorX > 0)
            {
                return "RIGHT";
            }
            return "LEFT";
        }

        /// <summary>
        /// [Tilt] 이동 방향 계산
        /// </summary>
        /// <param name="errorY">
        /// Y축 중심 오차 [Pixel]
        /// </param>
        /// <returns>
        /// [UP] / [DOWN] / [STOP]
        /// </returns>
        private string GetTiltDirection(
            double errorY)
        {
            if (Math.Abs(errorY) <=
                DEAD_ZONE_Y_PIXEL)
            {
                return "STOP";
            }

            if (errorY > 0)
            {
                return "DOWN";
            }
            return "UP";
        }

        /// <summary>
        /// 자동 추적 속도 계산
        /// 
        /// X축 / Y축 중심 오차 중 큰 값을 기준으로
        /// [MIN_TRACKING_SPEED] ~ [MAX_TRACKING_SPEED] 범위의 속도를 계산한다.
        /// </summary>
        /// <param name="errorX">
        /// X축 중심 오차 [Pixel]
        /// </param>
        /// <param name="errorY">
        /// Y축 중심 오차 [Pixel]
        /// </param>
        /// <returns>
        /// 자동 추적 속도
        /// </returns>
        private double CalculateTrackingSpeed(
            double errorX,
            double errorY)
        {
            double maxError =
                Math.Max(
                    Math.Abs(errorX),
                    Math.Abs(errorY));

            double maxBase =
                Math.Max(
                    FRAME_WIDTH,
                    FRAME_HEIGHT)
                / 2.0;

            if (maxBase <= 0)
            {
                return MIN_TRACKING_SPEED;
            }

            double ratio =
                maxError / maxBase;

            double speed =
                MIN_TRACKING_SPEED +
                ((MAX_TRACKING_SPEED - MIN_TRACKING_SPEED) * ratio);

            if (speed < MIN_TRACKING_SPEED)
            {
                return MIN_TRACKING_SPEED;
            }

            if (speed > MAX_TRACKING_SPEED)
            {
                return MAX_TRACKING_SPEED;
            }

            return Math.Round(
                speed,
                0);
        }

        #endregion

        #region [Tracking Command Methods]

        /// <summary>
        /// 자동 추적 명령 실행
        /// 
        /// 계산된 [Pan] / [Tilt] 방향과 속도 기준으로
        /// [ADS1000] 연속 이동 명령을 송신한다.
        /// </summary>
        /// <param name="panDirection">
        /// [Pan] 이동 방향
        /// </param>
        /// <param name="tiltDirection">
        /// [Tilt] 이동 방향
        /// </param>
        /// <param name="trackingSpeed">
        /// 추적 속도
        /// </param>
        private void ExecuteTrackingCommand(
            string panDirection,
            string tiltDirection,
            double trackingSpeed)
        {
            if (panDirection == "STOP" &&
                tiltDirection == "STOP")
            {
                StopTracking();
                return;
            }

            if (IsDuplicateTrackingCommand(
                panDirection,
                tiltDirection,
                trackingSpeed))
            {
                Console.WriteLine("[TRACKING][COMMAND] Skip : Duplicate Command");
                return;
            }

            _ads1000CameraControlService
                .PanTiltSpeedLevel =
                    trackingSpeed;

            ExecutePanCommand(
                panDirection);

            ExecuteTiltCommand(
                tiltDirection);

            ScheduleAutoStop();

            _lastPanDirection =
                panDirection;

            _lastTiltDirection =
                tiltDirection;

            _lastTrackingSpeed =
                trackingSpeed;
        }

        /// <summary>
        /// [Pan] 추적 명령 실행
        /// </summary>
        /// <param name="panDirection">
        /// [Pan] 이동 방향
        /// </param>
        private void ExecutePanCommand(
            string panDirection)
        {
            switch (panDirection)
            {
                case "LEFT":
                    _ads1000CameraControlService
                        .PanLeft();
                    break;

                case "RIGHT":
                    _ads1000CameraControlService
                        .PanRight();
                    break;
            }

        }

        /// <summary>
        /// [Tilt] 추적 명령 실행
        /// </summary>
        /// <param name="tiltDirection">
        /// [Tilt] 이동 방향
        /// </param>
        private void ExecuteTiltCommand(
            string tiltDirection)
        {
            switch (tiltDirection)
            {
                case "UP":
                    _ads1000CameraControlService
                        .TiltUp();
                    break;

                case "DOWN":
                    _ads1000CameraControlService
                        .TiltDown();
                    break;
            }

        }

        /// <summary>
        /// 자동 추적 정지 예약
        /// 
        /// 연속 이동 명령 송신 후 지정 시간이 지나면
        /// [PTZ] 정지 명령을 자동으로 송신한다.
        /// 
        /// 새 추적 명령이 들어오면 예약 번호가 증가하므로,
        /// 이전 예약 정지는 무시된다.
        /// </summary>
        private void ScheduleAutoStop()
        {
            int sequence =
                Interlocked.Increment(
                    ref _autoStopSequence);

            Task.Run(
                async () =>
                {
                    await Task.Delay(
                        TRACKING_AUTO_STOP_DELAY_MS);

                    if (sequence != _autoStopSequence)
                    {
                        return;
                    }

                    Console.WriteLine(
                        "[TRACKING][AUTO] Auto Stop");

                    StopPtz();

                    _lastPanDirection =
                        "STOP";

                    _lastTiltDirection =
                        "STOP";

                    _lastTrackingSpeed =
                        0;
                });


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
        /// [AUTO] 추적 계산 로그 출력
        /// </summary>
        private void PrintTrackingLog(
            DetectionBoundingBox boundingBox,
            double frameCenterX,
            double frameCenterY,
            double errorX,
            double errorY,
            string panDirection,
            string tiltDirection,
            double trackingSpeed)
        {
            Console.WriteLine("[TRACKING][AUTO] Tracking Calculate");
            Console.WriteLine("[TRACKING][BBOX] Center X : " + boundingBox.CenterX);
            Console.WriteLine("[TRACKING][BBOX] Center Y : " + boundingBox.CenterY);
            Console.WriteLine("[TRACKING][FRAME] Center X : " + frameCenterX);
            Console.WriteLine("[TRACKING][FRAME] Center Y : " + frameCenterY);
            Console.WriteLine("[TRACKING][ERROR] X : " + errorX);
            Console.WriteLine("[TRACKING][ERROR] Y : " + errorY);
            Console.WriteLine("[TRACKING][PTZ] Pan Direction : " + panDirection);
            Console.WriteLine("[TRACKING][PTZ] Tilt Direction : " + tiltDirection);
            Console.WriteLine("[TRACKING][PTZ] Speed : " + trackingSpeed);
        }
        #endregion
    }

}

using System;
using VertiportNexus.Models.Radar;
using VertiportNexus.Services.ADS1000;

namespace VertiportNexus.Services.Radar
{
    /// <summary>
    /// [Radar] 추적 제어 서비스
    /// 
    /// Radar Tracking Request에서 수신한
    /// 방위각 / 고각 / 거리 정보를 기반으로
    /// ADS1000 [Pan] / [Tilt] 제어 명령을 수행한다.
    /// </summary>
    internal class RadarTrackingControlService
    {
        #region [Constants]

        /// <summary>
        /// [rad] → [degree] 변환 계수
        /// </summary>
        private const double RADIAN_TO_DEGREE =
            180.0 / Math.PI;

        /// <summary>
        /// [PT Move] 해제
        /// </summary>
        private const byte PT_MOVE_RELEASE =
            0;

        /// <summary>
        /// [PT Move] 추적 이동
        /// </summary>
        private const byte PT_MOVE_ON =
            1;

        #endregion

        #region [Fields]

        /// <summary>
        /// [ADS1000] 카메라 제어 서비스
        /// 
        /// Radar 각도 정보를 실제 [Pan] / [Tilt] 제어 명령으로 변환하여 송신한다.
        /// </summary>
        private readonly Ads1000CameraControlService _ads1000CameraControlService;

        #endregion

        #region [Constructor]

        /// <summary>
        /// [RadarTrackingControlService] 생성자
        /// </summary>
        public RadarTrackingControlService(
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
        /// Radar 추적 요청 처리
        /// 
        /// [PT Move] 값이 [On]인 경우,
        /// Radar에서 전달된 [Azimuth] / [Elevation] 값을
        /// [Degree]로 변환하여 [Pan] / [Tilt] 절대 위치 이동을 수행한다.
        /// </summary>
        /// <param name="payload">
        /// Radar 추적 요청 Payload
        /// </param>
        public void HandleTrackingRequest(
            RadarTrackingRequestPayload payload)
        {
            if (payload == null)
            {
                Console.WriteLine(
                    "[RADAR][CONTROL] Failed : Payload is null");

                return;
            }

            if (payload.PtMove == PT_MOVE_RELEASE)
            {
                Console.WriteLine(
                    "[RADAR][CONTROL] PT Move Release");

                // [Radar] PT 이동 해제 처리
                //
                // Radar에서 추적 이동 해제 값이 들어온 경우,
                // 현재 진행 중인 Pan / Tilt 이동을 정지한다.
                _ads1000CameraControlService
                    .StopPanTiltMove();

                return;
            }

            if (payload.PtMove != PT_MOVE_ON)
            {
                Console.WriteLine(
                    "[RADAR][CONTROL] Skip : Unsupported PT Move");

                Console.WriteLine(
                    "[RADAR][CONTROL] PtMove : "
                    + payload.PtMove);

                return;
            }

            double panDegree =
                ConvertRadianToDegree(
                    payload.Azimuth);

            double tiltDegree =
                ConvertRadianToDegree(
                    payload.Elevation);

            PrintTrackingControlLog(
                payload,
                panDegree,
                tiltDegree);

            // [Radar] Pan 절대 위치 이동
            //
            // Radar의 [Azimuth] 값을 [Degree]로 변환하여
            // ADS1000 [Pan Absolute] 명령으로 송신한다.
            _ads1000CameraControlService
                .MovePanAbsolute(
                    panDegree);

            // [Radar] Tilt 절대 위치 이동
            //
            // Radar의 [Elevation] 값을 [Degree]로 변환하여
            // ADS1000 [Tilt Absolute] 명령으로 송신한다.
            _ads1000CameraControlService
                .MoveTiltAbsolute(
                    tiltDegree);
        }

        #endregion

        #region [Convert Methods]

        /// <summary>
        /// [radian] 값을 [degree] 값으로 변환
        /// </summary>
        private double ConvertRadianToDegree(
            double radian)
        {
            return radian
                * RADIAN_TO_DEGREE;
        }

        #endregion

        #region [Log Methods]

        /// <summary>
        /// Radar 기반 PTZ 제어 로그 출력
        /// </summary>
        private void PrintTrackingControlLog(
            RadarTrackingRequestPayload payload,
            double panDegree,
            double tiltDegree)
        {
            Console.WriteLine("[RADAR][CONTROL] Tracking Control");

            Console.WriteLine("[RADAR][CONTROL] Target Id : " + payload.Id);
            Console.WriteLine("[RADAR][CONTROL] Azimuth(rad) : " + payload.Azimuth);
            Console.WriteLine("[RADAR][CONTROL] Elevation(rad) : " + payload.Elevation);
            Console.WriteLine("[RADAR][CONTROL] Distance(m) : " + payload.Distance);

            Console.WriteLine("[RADAR][CONTROL] Pan(deg) : " + panDegree.ToString("F4"));
            Console.WriteLine("[RADAR][CONTROL] Tilt(deg) : " + tiltDegree.ToString("F4"));
        }
        #endregion
    }

}

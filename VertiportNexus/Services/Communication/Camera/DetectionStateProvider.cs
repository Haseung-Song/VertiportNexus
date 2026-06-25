using System;
using VertiportNexus.Models.Camera;

namespace VertiportNexus.Services.Camera
{
    /// <summary>
    /// [Detection] 탐지 상태 저장 서비스
    /// 
    /// [IF-GUIS-CSE-001] ~ [IF-GUIS-CSE-005] 명령 처리 결과와
    /// 영상처리유닛에서 전달되는 탐지 객체 정보를 보관한다.
    /// 
    /// 향후 [AUTO] 추적 제어 시
    /// 마지막 탐지 객체 위치를 기준으로 [Pan] / [Tilt] 보정에 사용한다.
    /// </summary>
    internal class DetectionStateProvider
    {
        #region [Fields]

        /// <summary>
        /// 상태값 동시 접근 제어 객체
        /// 
        /// [RabbitMQ] 명령 처리 Thread와
        /// [UI] / [Tracking] 처리 Thread가 동시에 접근할 수 있으므로
        /// lock 기준으로 상태값을 보호한다.
        /// </summary>
        private readonly object _syncLock = new object();

        /// <summary>
        /// 탐지 기능 활성화 여부
        /// </summary>
        private bool _isDetectEnabled;

        /// <summary>
        /// 탐지 동작 상태
        /// </summary>
        private bool _isDetectActive;

        /// <summary>
        /// 탐지 계속 수행 여부
        /// </summary>
        private bool _isDetectContinue;

        /// <summary>
        /// 현재 추적 ID
        /// </summary>
        private string _currentTrackId;

        /// <summary>
        /// 마지막 탐지 객체 영역 정보
        /// </summary>
        private DetectionBoundingBox _lastBoundingBox;

        /// <summary>
        /// 마지막 상태 갱신 시간
        /// </summary>
        private DateTime? _lastUpdatedTime;

        #endregion

        #region [Update Methods]

        /// <summary>
        /// 탐지 기능 활성화 상태 갱신
        /// </summary>
        /// <param name="isEnabled">
        /// 탐지 기능 활성화 여부
        /// </param>
        public void UpdateDetectEnabled(
            bool isEnabled)
        {
            lock (_syncLock)
            {
                _isDetectEnabled =
                    isEnabled;

                Console.WriteLine(
                    "[DETECTION][STATE] Detect Enabled : "
                    + _isDetectEnabled);

                if (!isEnabled)
                {
                    _isDetectActive =
                        false;

                    _isDetectContinue =
                        false;

                    _currentTrackId =
                        null;

                    _lastBoundingBox =
                        null;
                }
                _lastUpdatedTime =
                    DateTime.Now;
            }

        }

        /// <summary>
        /// 탐지 동작 상태 갱신
        /// </summary>
        /// <param name="isActive">
        /// 탐지 동작 여부
        /// </param>
        public void UpdateDetectActive(
            bool isActive)
        {
            lock (_syncLock)
            {
                _isDetectActive =
                    isActive;

                Console.WriteLine(
                    "[DETECTION][STATE] Detect Active : "
                    + _isDetectActive);

                if (!isActive)
                {
                    _isDetectContinue =
                        false;
                }
                _lastUpdatedTime =
                    DateTime.Now;
            }

        }

        /// <summary>
        /// 탐지 계속 수행 상태 갱신
        /// </summary>
        /// <param name="isContinue">
        /// 탐지 계속 수행 여부
        /// </param>
        public void UpdateDetectContinue(
            bool isContinue)
        {
            lock (_syncLock)
            {
                _isDetectContinue =
                    isContinue;

                Console.WriteLine(
                    "[DETECTION][STATE] Detect Continue : "
                    + _isDetectContinue);

                _lastUpdatedTime =
                    DateTime.Now;
            }

        }

        /// <summary>
        /// 추적 ID 갱신
        /// </summary>
        /// <param name="trackId">
        /// 현재 추적 ID
        /// </param>
        public void UpdateTrackId(
            string trackId)
        {
            lock (_syncLock)
            {
                _currentTrackId =
                    trackId;

                Console.WriteLine(
                    "[DETECTION][STATE] Track Id : "
                    + _currentTrackId);

                _lastUpdatedTime =
                    DateTime.Now;
            }

        }

        /// <summary>
        /// 마지막 탐지 객체 정보 갱신
        /// </summary>
        /// <param name="boundingBox">
        /// 탐지 객체 영역 정보
        /// </param>
        public void UpdateBoundingBox(
            DetectionBoundingBox boundingBox)
        {
            lock (_syncLock)
            {
                // [Bounding Box] 유효성 확인
                //
                // [Detect On]처럼 Bounding Box 좌표가 없는 명령에서는
                // 상태값 및 로그를 갱신하지 않는다.
                if (boundingBox == null ||
                    !boundingBox.X1.HasValue ||
                    !boundingBox.Y1.HasValue ||
                    !boundingBox.X2.HasValue ||
                    !boundingBox.Y2.HasValue)
                {
                    Console.WriteLine(
                        "[DETECTION][STATE] Bounding Box Skip : Empty");

                    return;
                }

                _lastBoundingBox =
                    boundingBox;

                Console.WriteLine(
                    "[DETECTION][STATE] Bounding Box Updated");

                Console.WriteLine(
                    "[DETECTION][STATE] X1 : "
                    + boundingBox.X1);

                Console.WriteLine(
                    "[DETECTION][STATE] Y1 : "
                    + boundingBox.Y1);

                Console.WriteLine(
                    "[DETECTION][STATE] X2 : "
                    + boundingBox.X2);

                Console.WriteLine(
                    "[DETECTION][STATE] Y2 : "
                    + boundingBox.Y2);

                Console.WriteLine(
                    "[DETECTION][STATE] Center X : "
                    + boundingBox.CenterX);

                Console.WriteLine(
                    "[DETECTION][STATE] Center Y : "
                    + boundingBox.CenterY);

                Console.WriteLine(
                    "[DETECTION][STATE] Confidence : "
                    + boundingBox.Confidence);

                _lastUpdatedTime =
                    DateTime.Now;
            }

        }

        #endregion

        #region [Read Methods]

        /// <summary>
        /// 탐지 상태 Snapshot 조회
        /// 
        /// 현재 탐지 상태값을 복사하여 반환한다.
        /// 외부에서는 반환된 Snapshot만 참조하고,
        /// 내부 상태값은 직접 수정하지 않는다.
        /// </summary>
        /// <returns>
        /// 탐지 상태 Snapshot
        /// </returns>
        public DetectionStateSnapshot GetSnapshot()
        {
            lock (_syncLock)
            {
                return new DetectionStateSnapshot
                {
                    IsDetectEnabled =
                        _isDetectEnabled,

                    IsDetectActive =
                        _isDetectActive,

                    IsDetectContinue =
                        _isDetectContinue,

                    CurrentTrackId =
                        _currentTrackId,

                    LastBoundingBox =
                        _lastBoundingBox,

                    LastUpdatedTime =
                        _lastUpdatedTime
                };

            }

        }
        #endregion
    }

}

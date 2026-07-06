using System;
using VertiportNexus.Models.Camera;

namespace VertiportNexus.Services.Camera
{
    /// <summary>
    /// [Detection] 탐지 상태 저장 서비스
    /// 
    /// [IF-GUIS-CSE-001] / [IF-GUIS-CSE-002] / [IF-GUIS-CSE-003]
    /// 명령 처리 결과와 영상처리유닛에서 전달되는
    /// 최신 탐지 객체 정보를 보관한다.
    /// 
    /// [detect_cont]는 약 [30Hz]로 수신될 수 있으므로
    /// Queue에 누적하지 않고 마지막 탐지 객체 정보만 유지한다.
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
        private readonly object _syncLock =
            new object();

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
        /// 
        /// [detect_cont] 메시지가 [30Hz]로 수신되므로
        /// Queue에 누적하지 않고 최신 값만 유지한다.
        /// </summary>
        private DetectionBoundingBox _lastBoundingBox;

        /// <summary>
        /// 마지막 상태 갱신 시간
        /// </summary>
        private DateTime? _lastUpdatedTime;

        #endregion

        #region [Properties]

        /// <summary>
        /// 탐지 기능 활성화 여부
        /// </summary>
        public bool IsDetectEnabled
        {
            get
            {
                lock (_syncLock)
                {
                    return _isDetectEnabled;
                }

            }

        }

        /// <summary>
        /// 탐지 동작 상태
        /// </summary>
        public bool IsDetectActive
        {
            get
            {
                lock (_syncLock)
                {
                    return _isDetectActive;
                }

            }

        }

        #endregion

        #region [Update Methods]

        /// <summary>
        /// 탐지 시작 상태 갱신
        /// 
        /// [detect_on] 수신 시 호출되며,
        /// 탐지 상태를 [LOCK ON] 상태로 변경한다.
        /// </summary>
        public void StartDetection()
        {
            lock (_syncLock)
            {
                _isDetectEnabled =
                    true;

                _isDetectActive =
                    true;

                _isDetectContinue =
                    true;

                _lastUpdatedTime =
                    DateTime.Now;
            }

            Console.WriteLine("[DETECTION][STATE] LOCK ON");
        }

        /// <summary>
        /// 탐지 정지 상태 갱신
        /// 
        /// [detect_off] 수신 시 호출되며,
        /// 탐지 상태를 [LOCK OFF] 상태로 변경하고
        /// 마지막 탐지 객체 정보를 초기화한다.
        /// </summary>
        public void StopDetection()
        {
            lock (_syncLock)
            {
                _isDetectEnabled =
                    false;

                _isDetectActive =
                    false;

                _isDetectContinue =
                    false;

                _currentTrackId =
                    null;

                _lastBoundingBox =
                    null;

                _lastUpdatedTime =
                    DateTime.Now;
            }

            Console.WriteLine("[DETECTION][STATE] LOCK OFF");
        }

        /// <summary>
        /// 탐지 기능 활성화 상태 갱신
        /// </summary>
        public void UpdateDetectEnabled(
            bool isEnabled)
        {
            lock (_syncLock)
            {
                _isDetectEnabled =
                    isEnabled;

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

            Console.WriteLine(
                "[DETECTION][STATE] Detect Enabled : "
                + isEnabled);
        }

        /// <summary>
        /// 탐지 동작 상태 갱신
        /// </summary>
        public void UpdateDetectActive(
            bool isActive)
        {
            lock (_syncLock)
            {
                _isDetectActive =
                    isActive;

                if (!isActive)
                {
                    _isDetectContinue =
                        false;
                }

                _lastUpdatedTime =
                    DateTime.Now;
            }

            Console.WriteLine(
                "[DETECTION][STATE] Detect Active : "
                + isActive);
        }

        /// <summary>
        /// 탐지 계속 수행 상태 갱신
        /// </summary>
        public void UpdateDetectContinue(
            bool isContinue)
        {
            lock (_syncLock)
            {
                _isDetectContinue =
                    isContinue;

                _lastUpdatedTime =
                    DateTime.Now;
            }

            Console.WriteLine(
                "[DETECTION][STATE] Detect Continue : "
                + isContinue);
        }

        /// <summary>
        /// 추적 ID 갱신
        /// </summary>
        public void UpdateTrackId(
            string trackId)
        {
            lock (_syncLock)
            {
                _currentTrackId =
                    trackId;

                _lastUpdatedTime =
                    DateTime.Now;
            }

            Console.WriteLine(
                "[DETECTION][STATE] Track Id : "
                + trackId);
        }

        /// <summary>
        /// 마지막 탐지 객체 정보 갱신
        /// 
        /// [detect_on] / [detect_cont] 수신 시 호출되며,
        /// 수신된 객체 화면 좌표를 마지막 탐지 객체 정보로 저장한다.
        /// </summary>
        public void UpdateBoundingBox(
            DetectionBoundingBox boundingBox)
        {
            if (boundingBox == null ||
                !boundingBox.X1.HasValue ||
                !boundingBox.Y1.HasValue ||
                !boundingBox.X2.HasValue ||
                !boundingBox.Y2.HasValue)
            {
                Console.WriteLine("[DETECTION][STATE] Bounding Box Skip : Empty");
                return;
            }

            lock (_syncLock)
            {
                _lastBoundingBox =
                    boundingBox;

                _lastUpdatedTime =
                    DateTime.Now;
            }
            Console.WriteLine("[DETECTION][STATE] Bounding Box Updated");
            Console.WriteLine("[DETECTION][STATE] X1 : " + boundingBox.X1);
            Console.WriteLine("[DETECTION][STATE] Y1 : " + boundingBox.Y1);
            Console.WriteLine("[DETECTION][STATE] X2 : " + boundingBox.X2);
            Console.WriteLine("[DETECTION][STATE] Y2 : " + boundingBox.Y2);
            Console.WriteLine("[DETECTION][STATE] Center X : " + boundingBox.CenterX);
            Console.WriteLine("[DETECTION][STATE] Center Y : " + boundingBox.CenterY);
            Console.WriteLine("[DETECTION][STATE] Confidence : " + boundingBox.Confidence);
        }

        #endregion

        #region [Read Methods]

        /// <summary>
        /// 마지막 탐지 객체 정보 조회
        /// </summary>
        public DetectionBoundingBox GetLastBoundingBox()
        {
            lock (_syncLock)
            {
                return _lastBoundingBox;
            }

        }

        /// <summary>
        /// 탐지 상태 Snapshot 조회
        /// 
        /// 현재 탐지 상태값을 복사하여 반환한다.
        /// 외부에서는 반환된 Snapshot만 참조하고,
        /// 내부 상태값은 직접 수정하지 않는다.
        /// </summary>
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

using System;

namespace VertiportNexus.Models.Camera
{
    /// <summary>
    /// [Detection] 탐지 상태 Snapshot
    /// 
    /// 현재 탐지 활성 상태와
    /// 마지막 탐지 객체 정보를 외부에서 안전하게 참조하기 위한 모델이다.
    /// </summary>
    public class DetectionStateSnapshot
    {
        #region [Properties]

        /// <summary>
        /// 탐지 기능 활성화 여부
        /// </summary>
        public bool IsDetectEnabled { get; set; }

        /// <summary>
        /// 탐지 동작 상태
        /// </summary>
        public bool IsDetectActive { get; set; }

        /// <summary>
        /// 탐지 계속 수행 여부
        /// </summary>
        public bool IsDetectContinue { get; set; }

        /// <summary>
        /// 현재 추적 ID
        /// </summary>
        public string CurrentTrackId { get; set; }

        /// <summary>
        /// 마지막 탐지 객체 영역 정보
        /// </summary>
        public DetectionBoundingBox LastBoundingBox { get; set; }

        /// <summary>
        /// 마지막 탐지 상태 갱신 시간
        /// </summary>
        public DateTime? LastUpdatedTime { get; set; }

        #endregion
    }

}

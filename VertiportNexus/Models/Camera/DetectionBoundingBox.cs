namespace VertiportNexus.Models.Camera
{
    /// <summary>
    /// [Detection] 탐지 객체 영역 정보
    /// 
    /// 영상처리유닛에서 수신한
    /// 탐지 객체의 화면 좌표와 신뢰도를 보관한다.
    /// 
    /// 향후 [AUTO] 추적 제어 시
    /// 객체 중심점과 화면 중심점 비교에 사용한다.
    /// </summary>
    public class DetectionBoundingBox
    {
        #region [Properties]

        /// <summary>
        /// 탐지 Frame 식별자
        /// </summary>
        public int? FrameId { get; set; }

        /// <summary>
        /// 객체 좌측 상단 X 좌표
        /// </summary>
        public double? X1 { get; set; }

        /// <summary>
        /// 객체 좌측 상단 Y 좌표
        /// </summary>
        public double? Y1 { get; set; }

        /// <summary>
        /// 객체 우측 하단 X 좌표
        /// </summary>
        public double? X2 { get; set; }

        /// <summary>
        /// 객체 우측 하단 Y 좌표
        /// </summary>
        public double? Y2 { get; set; }

        /// <summary>
        /// 탐지 객체 분류 ID
        /// </summary>
        public int? ClassId { get; set; }

        /// <summary>
        /// 탐지 신뢰도
        /// </summary>
        public double? Confidence { get; set; }

        /// <summary>
        /// 객체 중심 X 좌표
        /// </summary>
        public double? CenterX
        {
            get
            {
                if (!X1.HasValue ||
                    !X2.HasValue)
                {
                    return null;
                }

                return
                    (X1.Value + X2.Value) / 2.0;
            }

        }

        /// <summary>
        /// 객체 중심 Y 좌표
        /// </summary>
        public double? CenterY
        {
            get
            {
                if (!Y1.HasValue ||
                    !Y2.HasValue)
                {
                    return null;
                }

                return
                    (Y1.Value + Y2.Value) / 2.0;
            }

        }
        #endregion
    }

}

namespace VertiportNexus.Models.Radar
{
    /// <summary>
    /// [IF-CSR-CSE-001] Radar Tracking Request Payload 모델
    /// 
    /// [CSR]에서 [CSE]로 전달하는
    /// 선택 표적의 추적 요청 정보를 보관한다.
    /// 
    /// CSE는 본 Payload의 방위각 / 고각 정보를 기준으로
    /// ADS1000 Pan / Tilt 추적 제어와 연동한다.
    /// </summary>
    public class RadarTrackingRequestPayload
    {
        #region [Time Properties]

        /// <summary>
        /// Radar Tracking 요청 시간
        /// </summary>
        public long TimeStamp { get; set; }

        #endregion

        #region [Tracking Control Properties]

        /// <summary>
        /// Pan / Tilt 이동 요청 값
        /// 
        /// Radar에서 전달하는 추적 제어 수행 여부 또는
        /// 이동 제어 상태 구분값으로 사용한다.
        /// </summary>
        public byte PtMove { get; set; }

        /// <summary>
        /// 추적 대상 ID
        /// </summary>
        public ushort Id { get; set; }

        #endregion

        #region [Angle Properties]

        /// <summary>
        /// 추적 대상 방위각
        /// 
        /// ADS1000 Pan 제어값 계산에 사용한다.
        /// </summary>
        public float Azimuth { get; set; }

        /// <summary>
        /// 추적 대상 고각
        /// 
        /// ADS1000 Tilt 제어값 계산에 사용한다.
        /// </summary>
        public float Elevation { get; set; }

        #endregion

        #region [Distance Properties]

        /// <summary>
        /// 추적 대상 거리
        /// </summary>
        public float Distance { get; set; }

        #endregion

        #region [Velocity Properties]

        /// <summary>
        /// 추적 대상 X축 속도
        /// </summary>
        public float Vx { get; set; }

        /// <summary>
        /// 추적 대상 Y축 속도
        /// </summary>
        public float Vy { get; set; }

        /// <summary>
        /// 추적 대상 Z축 속도
        /// </summary>
        public float Vz { get; set; }

        #endregion

        #region [Position Properties]

        /// <summary>
        /// 추적 대상 ECEF X 좌표
        /// </summary>
        public double EcefX { get; set; }

        /// <summary>
        /// 추적 대상 ECEF Y 좌표
        /// </summary>
        public double EcefY { get; set; }

        /// <summary>
        /// 추적 대상 ECEF Z 좌표
        /// </summary>
        public double EcefZ { get; set; }

        #endregion

        #region [Reserved Properties]

        /// <summary>
        /// Reserved
        /// 
        /// ICD 확장 또는 정렬용 예비 필드이다.
        /// 현재 CSE 제어 로직에서는 사용하지 않는다.
        /// </summary>
        public uint Reserved { get; set; }

        #endregion
    }

}

namespace VertiportNexus.Common
{
    /// <summary>
    /// [ADS1000] 공통 상수
    /// 
    /// [ADS1000] Pan / Tilt 제어 및 상태값 계산 시
    /// 공통으로 사용하는 기준값을 정의한다.
    /// </summary>
    public static class Ads1000Constants
    {
        #region [Motor Constants]

        /// <summary>
        /// [Pan] 모터 엔코더 해상도
        /// 
        /// [PanTilt.ini] 기준:
        /// PMOTOR RESOLUTION = 2423611.0
        /// 
        /// 선임 검토 기준:
        /// 실장비 Home Position 수행 후 장비에서 수신되는
        /// Pan Encoder 기준값과 실제 0도 / 360도 위치가 일치하도록
        /// Raw Encoder 값을 기준으로 역산하여 산출한 보정 해상도이다.
        /// 
        /// 기존 2400000.0 기준에서는 Home Position 후
        /// 0도 복귀 및 360도 회전 시 실제 영상 기준 위치가 미세하게 어긋났으므로,
        /// 실장비 기준 회전 위치와 UI / 제어 각도 기준을 일치시키기 위해
        /// Pan 축에 한해 2423611.0 값을 적용한다.
        /// 
        /// [MCB] Pan 위치 제어 및 상태값 계산 시
        /// Pan 각도와 모터 위치값을 상호 변환하기 위한 기준값이다.
        /// 
        /// [Builder]
        /// 각도 → 모터 위치값 변환
        /// 위치 = Resolution / 360 × 각도
        /// 
        /// [Parser]
        /// 모터 위치값 → 각도 변환
        /// 각도 = 위치 × 360 / Resolution
        /// </summary>
        public const double PAN_MOTOR_ENCODER_RESOLUTION =
            2423611.0;

        /// <summary>
        /// [Tilt] 모터 엔코더 해상도
        /// 
        /// [PanTilt.ini] 기준:
        /// TMOTOR RESOLUTION = 2400000.0
        /// 
        /// [MCB] Tilt 위치 제어 및 상태값 계산 시
        /// Tilt 각도와 모터 위치값을 상호 변환하기 위한 기준값이다.
        /// 
        /// [Builder]
        /// 각도 → 모터 위치값 변환
        /// 위치 = Resolution / 360 × 각도
        /// 
        /// [Parser]
        /// 모터 위치값 → 각도 변환
        /// 각도 = 위치 × 360 / Resolution
        /// </summary>
        public const double TILT_MOTOR_ENCODER_RESOLUTION =
            2400000.0;

        #endregion
    }

}

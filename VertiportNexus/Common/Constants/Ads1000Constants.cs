namespace VertiportNexus.Common
{
    public static class Ads1000Constants
    {
        /// <summary>
        /// [Pan/Tilt] 모터 엔코더 해상도
        /// 
        /// [PanTilt.ini] 기준:
        /// 1) PMOTOR RESOLUTION = 2500000
        /// 
        /// 2) TMOTOR RESOLUTION = 2500000
        /// 
        /// [MCB] 위치 제어 및 상태값 계산 시
        /// 공통으로 사용하는 엔코더 해상도이다.
        /// 
        /// [Builder]
        /// 각도 → 모터 위치값 변환
        /// 
        /// 위치 = Resolution / 360 × 각도
        /// 
        /// [Parser]
        /// 모터 위치값 → 각도 변환
        /// 
        /// 각도 = 위치 × 360 / Resolution
        /// </summary>
        public const double MOTOR_ENCODER_RESOLUTION = 2500000.0;
    }

}

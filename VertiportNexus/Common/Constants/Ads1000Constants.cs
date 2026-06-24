namespace VertiportNexus.Common
{
    public static class Ads1000Constants
    {
        /// <summary>
        /// [Pan/Tilt] 모터 엔코더 해상도
        /// 
        /// [PanTilt.ini] 기준:
        /// PMOTOR RESOLUTION = 2500000
        /// TMOTOR RESOLUTION = 2500000
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
        public const double MOTOR_ENCODER_RESOLUTION =
            2500000.0;

        /// <summary>
        /// [Pan] / [Tilt] 위치 이동 기본 각속도
        /// 
        /// ADS3000 프로토콜 위치 제어 시
        /// [SP] 값 계산에 사용한다.
        /// 
        /// 단위:
        /// Degree / Second
        /// </summary>
        public const double DEFAULT_POSITION_SPEED =
            30.0;
    }

}

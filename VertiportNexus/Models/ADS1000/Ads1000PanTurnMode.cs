namespace VertiportNexus.Models.ADS1000
{
    /// <summary>
    /// [ADS1000] [Pan] 선회 모드
    /// 
    /// [Pan Absolute] 이동 시
    /// 현재 위치에서 목표 위치까지 이동하는 방향 계산 방식을 정의한다.
    /// </summary>
    public enum Ads1000PanTurnMode
    {
        /// <summary>
        /// [Via 0] 모드
        /// 
        /// 현재 위치에서 목표 위치까지
        /// 단거리 보정 없이 명령 위치 기준으로 이동한다.
        /// </summary>
        ViaZero,


        /// <summary>
        /// [Short] 모드
        /// 
        /// 현재 위치에서 목표 위치까지
        /// 가장 가까운 회전 방향으로 이동한다.
        /// </summary>
        Short
    }

}

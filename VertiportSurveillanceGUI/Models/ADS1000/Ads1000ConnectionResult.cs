namespace VertiportSurveillanceGUI.Models.ADS1000
{
    /// <summary>
    /// [ADS1000] 장비 연결 결과
    /// 
    /// [MCB] / [SCB] [TCP] 연결 성공 여부를 보관한다.
    /// </summary>
    public class Ads1000ConnectionResult
    {
        #region [Properties]

        /// <summary>
        /// [MCB] 연결 성공 여부
        /// </summary>
        public bool IsMcbConnected { get; }

        /// <summary>
        /// [SCB] 연결 성공 여부
        /// </summary>
        public bool IsScbConnected { get; }

        #endregion

        #region [Constructor]

        /// <summary>
        /// [Ads1000ConnectionResult] 생성자
        /// </summary>
        /// <param name="isMcbConnected">
        /// [MCB] 연결 성공 여부
        /// </param>
        /// 
        /// <param name="isScbConnected">
        /// [SCB] 연결 성공 여부
        /// </param>
        public Ads1000ConnectionResult(
            bool isMcbConnected,
            bool isScbConnected)
        {
            IsMcbConnected =
                isMcbConnected;

            IsScbConnected =
                isScbConnected;
        }
        #endregion
    }

}

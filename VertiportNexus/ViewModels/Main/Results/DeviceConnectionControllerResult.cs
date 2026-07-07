using VertiportNexus.Models.ADS1000;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [Device Connection] Controller 처리 결과
    /// 
    /// [MCB] / [SCB] 연결 결과와 화면 표시 메시지를
    /// [MainViewModel]로 반환하기 위해 사용한다.
    /// </summary>
    internal sealed class DeviceConnectionControllerResult : ControllerResult
    {
        #region [Properties]

        /// <summary>
        /// [ADS1000] 장비 연결 결과
        /// </summary>
        internal Ads1000ConnectionResult ConnectionResult { get; private set; }

        #endregion

        #region [Constructor]

        /// <summary>
        /// [Device Connection] Controller 처리 결과 생성
        /// </summary>
        private DeviceConnectionControllerResult(
            bool isSuccess,
            string message,
            Ads1000ConnectionResult connectionResult)
            : base(
                  isSuccess,
                  message)
        {
            ConnectionResult =
                connectionResult;
        }

        #endregion

        #region [Factory Methods]

        /// <summary>
        /// 장비 연결 성공 결과 생성
        /// </summary>
        internal static DeviceConnectionControllerResult Success(
            string message,
            Ads1000ConnectionResult connectionResult)
        {
            return new DeviceConnectionControllerResult(
                true,
                message,
                connectionResult);
        }

        /// <summary>
        /// 장비 연결 실패 결과 생성
        /// </summary>
        internal new static DeviceConnectionControllerResult Failed(
            string message)
        {
            return new DeviceConnectionControllerResult(
                false,
                message,
                null);
        }

        #endregion
    }
}

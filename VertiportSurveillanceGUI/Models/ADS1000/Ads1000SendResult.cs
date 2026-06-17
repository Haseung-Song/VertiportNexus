
namespace VertiportSurveillanceGUI.Models.ADS1000
{
    /// <summary>
    /// [ADS1000] [Packet] 송신 결과
    /// 
    /// [MCB] / [SCB] [Packet] 송신 결과와
    /// 화면에 표시할 명령 정보를 보관한다.
    /// </summary>
    public class Ads1000SendResult
    {
        #region [Properties]

        /// <summary>
        /// 송신 장비 이름
        /// </summary>
        public string DeviceName { get; }

        /// <summary>
        /// 송신 [Packet]
        /// </summary>
        public byte[] Packet { get; }

        /// <summary>
        /// 제어 명령 이름
        /// </summary>
        public string CommandName { get; }

        /// <summary>
        /// 송신 성공 여부
        /// </summary>
        public bool IsSuccess { get; }

        #endregion

        #region [Constructor]

        /// <summary>
        /// [Ads1000SendResult] 생성자
        /// </summary>
        /// <param name="deviceName">
        /// 송신 장비 이름
        /// </param>
        /// <param name="packet">
        /// 송신 [Packet]
        /// </param>
        /// <param name="commandName">
        /// 제어 명령 이름
        /// </param>
        /// <param name="isSuccess">
        /// 송신 성공 여부
        /// </param>
        public Ads1000SendResult(
            string deviceName,
            byte[] packet,
            string commandName,
            bool isSuccess)
        {
            DeviceName =
                deviceName;

            Packet =
                packet;

            CommandName =
                commandName;

            IsSuccess =
                isSuccess;
        }
        #endregion
    }

}

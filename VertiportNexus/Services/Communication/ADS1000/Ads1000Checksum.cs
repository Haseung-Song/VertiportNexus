namespace VertiportNexus.Services.ADS1000
{
    /// <summary>
    /// [ADS1000] [Checksum] 계산 클래스
    /// 
    /// [ADS1000] / [ADS3000] 계열 장비 프로토콜에서 사용하는
    /// [XOR] [Checksum] 계산을 담당한다.
    /// </summary>
    public class Ads1000Checksum
    {
        #region [Calculate]

        /// <summary>
        /// [byte] 배열 기준 [XOR] [Checksum] 계산
        /// 
        /// [SCB] 명령 기준:
        /// [Cmd2]부터 [Data] 마지막 바이트까지 [XOR] 계산한다.
        /// 
        /// 예)
        /// Zoom Tele:
        /// Cmd2   = 0x31
        /// Option = 0x01
        /// Data   = 0x00 0x05 0x00
        /// 
        /// Checksum = 0x31 ^ 0x01 ^ 0x00 ^ 0x05 ^ 0x00
        /// </summary>
        /// 
        /// <param name="data">
        /// [Checksum] 계산 대상 [byte] 배열
        /// </param>
        /// <returns>
        /// 계산된 [Checksum]
        /// </returns>
        public byte Calculate(
            params byte[] data)
        {
            byte checksum = 0x00;

            if (data == null ||
                data.Length == 0)
            {
                return checksum;
            }

            for (int i = 0; i < data.Length; i++)
            {
                checksum ^= data[i];
            }
            return checksum;
        }
        #endregion
    }

}

namespace VertiportNexus.Models.Radar
{
    /// <summary>
    /// [IF-CSR-CSE-002] BIST 요청 Payload
    /// 
    /// Radar에서 CSE로 전달하는 BIST 요청 정보를 보관한다.
    /// </summary>
    public class RadarBistRequestPayload
    {
        #region [Properties]

        public long TimeStamp { get; set; }

        public byte BistType { get; set; }

        public uint ComportNumber { get; set; }

        public uint CbistInterval { get; set; }

        #endregion
    }

}

using System;
using System.Text.Json;
using VertiportNexus.Models.Vertiport;

namespace VertiportNexus.Services.Vertiport
{
    /// <summary>
    /// [CSE] JSON 메시지 파서
    ///
    /// [MQ] 수신 문자열을
    /// [CseCommandMessage] 객체로 변환한다.
    /// </summary>
    public class CseMessageParser
    {
        #region [Public Methods]

        /// <summary>
        /// [JSON] 문자열 파싱
        /// </summary>
        /// <param name="jsonText">
        /// 수신 JSON 문자열
        /// </param>
        /// <returns>
        /// 파싱 결과
        /// </returns>
        public CseCommandMessage Parse(
            string jsonText)
        {
            if (string.IsNullOrWhiteSpace(
                jsonText))
            {
                return null;
            }

            try
            {
                return JsonSerializer
                    .Deserialize<CseCommandMessage>(
                        jsonText,
                        new JsonSerializerOptions
                        {
                            /// <summary>
                            /// [JSON] 속성명 대소문자 구분 없이 매핑
                            /// </summary>
                            PropertyNameCaseInsensitive = true
                        });

            }
            catch (Exception exception)
            {
                Console.WriteLine("[CSE] JSON Parse Failed");
                Console.WriteLine(exception.Message);

                return null;
            }

        }
        #endregion
    }

}

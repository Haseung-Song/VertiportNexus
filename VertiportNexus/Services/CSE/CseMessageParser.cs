using System;
using System.Text.Json;
using VertiportNexus.Models.Vertiport;
using VertiportNexus.Common;

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
        #region [Fields]

        /// <summary>
        /// [JSON] 역직렬화 옵션
        /// 
        /// [JSON] 속성명 대소문자를 구분하지 않고
        /// C# 모델 속성에 매핑한다.
        /// </summary>
        private readonly JsonSerializerOptions _jsonSerializerOptions =
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

        #endregion

        #region [Public Methods]

        /// <summary>
        /// [JSON] 문자열 파싱
        /// </summary>
        /// <param name="jsonText">
        /// 수신 [JSON] 문자열
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
                ConsoleLogHelper.WriteLine("[CSE][PARSER] Parse Failed : JSON is empty");
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<CseCommandMessage>(
                    jsonText,
                    _jsonSerializerOptions);
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.WriteLine("[CSE][PARSER] JSON Parse Failed");
                ConsoleLogHelper.WriteLine("[CSE][PARSER] Error : " + ex.Message);

                return null;
            }

        }
        #endregion
    }

}

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FinanceTool
{
    internal class TrialManager
    {

        // 만료 일자를 리터럴로 지정 (예: 2025년 3월 31일)
        private static readonly DateTime ExpirationDate = new DateTime(2025, 9, 30);

        // 허용된 MAC Address 목록 (하드코딩)
        private static readonly HashSet<string> AllowedMacAddresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "EC-91-61-6B-BF-9F",  // 예시 MAC Address (실제 값으로 교체 필요)
            "A0-02-A5-E0-7C-CA"  // 예시 MAC Address 2 (실제 값으로 교체 필요)
            // 필요한 만큼 MAC Address 추가
        };


        // 시간 API URL
        // 한국 지역 기준 시간 API URL
        private const string TimeApiUrl = "http://worldtimeapi.org/api/timezone/Asia/Seoul";
        //private const string TimeApiUrl = "http://worldclockapi.com/api/json/utc/now";

        public async Task checkMacaddress()
        {
            // MAC Address 검증 (최우선)
            if (!IsAuthorizedMachine())
            {
                MessageBox.Show("이 프로그램은 허가되지 않은 컴퓨터에서는 실행할 수 없습니다.\n" +
                               "라이선스 문의는 제작자에게 연락해주세요.",
                               "실행 권한 없음", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                Environment.Exit(0);
                return;
            }
        }

            

        // 프로그램 시작 시 호출하는 메서드
        public async Task CheckTrial()
        {
            try
            {
                
                // 인터넷에서 현재 시간 가져오기
                DateTime? currentTime = await GetOnlineTime();

                if (currentTime  == null)
                {
                    // 만료된 경우
                    MessageBox.Show("프로그램 사용을 위해 인터넷 연결이 필요합니다. 인터넷 연결을 확인해주세요.",
                                   "인터넷 연결 확인", MessageBoxButtons.OK, MessageBoxIcon.Stop);

                    // 프로그램 종료
                    Environment.Exit(0);
                }
                Debug.WriteLine($"프로그램 일자 : {currentTime} , 만료일자 : {ExpirationDate}");
                // 인터넷 연결이 안 되면 로컬 시간 사용 (보안상 더 좋은 접근법은 인터넷 연결 필수로 하는 것)
                DateTime timeToCheck = currentTime ?? DateTime.Now;

                // 만료 일자와 비교
                if (timeToCheck > ExpirationDate)
                {
                    // 만료된 경우
                    MessageBox.Show("이 프로그램의 평가판 기간이 만료되었습니다. 구매를 위해 제작자에게 연락해주세요.",
                                   "평가판 만료", MessageBoxButtons.OK, MessageBoxIcon.Stop);

                    // 프로그램 종료
                    Environment.Exit(0);
                }
                else
                {
                    // 만료되지 않은 경우, 남은 일수 계산
                    int daysLeft = (ExpirationDate - timeToCheck).Days;

                    // 사용자에게 남은 일수 알림
                    MessageBox.Show($"평가판 기간이 {daysLeft}일 남았습니다.",
                                   "평가판 정보", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                // 오류 발생 시 처리
                MessageBox.Show($"평가판 확인 중 오류가 발생했습니다: {ex.Message}",
                               "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // MAC Address 권한 확인
        private bool IsAuthorizedMachine()
        {
            try
            {
                // 현재 PC의 모든 MAC Address 가져오기
                var currentMacAddresses = GetAllMacAddresses();

                Debug.WriteLine("=== MAC Address 검증 ===");
                Debug.WriteLine($"현재 PC의 MAC Address 목록:");
                foreach (var mac in currentMacAddresses)
                {
                    Debug.WriteLine($"  - {mac}");
                }

                Debug.WriteLine($"허용된 MAC Address 목록:");
                foreach (var allowedMac in AllowedMacAddresses)
                {
                    Debug.WriteLine($"  - {allowedMac}");
                }

                // 현재 PC의 MAC Address 중 하나라도 허용 목록에 있으면 허가
                bool isAuthorized = currentMacAddresses.Any(mac => AllowedMacAddresses.Contains(mac));

                Debug.WriteLine($"권한 검증 결과: {(isAuthorized ? "허가됨" : "거부됨")}");
                Debug.WriteLine("=====================");

                return isAuthorized;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MAC Address 검증 중 오류: {ex.Message}");
                // 오류 발생 시 보안상 실행 거부
                return false;
            }
        }

        // 모든 네트워크 어댑터의 MAC Address 가져오기
        private List<string> GetAllMacAddresses()
        {
            var macAddresses = new List<string>();

            try
            {
                // 모든 네트워크 인터페이스 조회
                NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

                foreach (NetworkInterface ni in networkInterfaces)
                {
                    // 유효한 네트워크 어댑터만 필터링
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                        ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                        ni.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet)
                    {
                        // MAC Address 가져오기
                        PhysicalAddress macAddr = ni.GetPhysicalAddress();
                        if (macAddr != null)
                        {
                            byte[] macBytes = macAddr.GetAddressBytes();
                            if (macBytes.Length > 0)
                            {
                                // MAC Address를 "XX-XX-XX-XX-XX-XX" 형식으로 변환
                                string macString = string.Join("-", macBytes.Select(b => b.ToString("X2")));
                                if (macString != "00-00-00-00-00-00") // 유효하지 않은 MAC Address 제외
                                {
                                    macAddresses.Add(macString);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MAC Address 조회 중 오류: {ex.Message}");
            }

            return macAddresses;
        }



        public async Task<DateTime?> GetGoogleDateTime()

        {
            //리턴 할 날짜 선언
            DateTime dateTime = DateTime.MinValue;
            try
            {
                //WebRequest 객체로 구글사이트 접속 해당 날짜와 시간을 로컬 형태의 포맷으로 리턴 일자에 담는다.
                using (var response = WebRequest.Create("http://www.google.com").GetResponse())
                    dateTime = DateTime.ParseExact(response.Headers["date"],
                        "ddd, dd MMM yyyy HH:mm:ss 'GMT'",
                        CultureInfo.InvariantCulture.DateTimeFormat,
                        DateTimeStyles.AssumeUniversal);
            }
            catch (Exception ex)
            {
                //오류 발생시 로컬 날짜그대로 리턴
                //dateTime = DateTime.Now;
                Debug.WriteLine("GetGoogleDateTime Exception");
                Debug.WriteLine(ex.Message);
                return null;
            }

            return dateTime;
        }

        // 온라인 시간 가져오기
        private async Task<DateTime?> GetOnlineTime()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // 타임아웃 설정 (5초)
                    client.Timeout = TimeSpan.FromSeconds(5);

                    // API 응답 가져오기
                    HttpResponseMessage response = await client.GetAsync(TimeApiUrl);
                    
                    Debug.WriteLine("await client.GetAsync(TimeApiUrl) complete");


                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        Debug.WriteLine($"API 응답: {jsonResponse}"); // 디버깅용

                        // 첫 번째 API 형식 시도 (worldtimeapi.org)
                        try
                        {
                            using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                            {
                                if (doc.RootElement.TryGetProperty("datetime", out JsonElement datetimeElement))
                                {
                                    string datetimeStr = datetimeElement.GetString();
                                    return DateTime.Parse(datetimeStr);
                                }
                            }
                        }
                        catch
                        {
                            // 두 번째 API 형식 시도 (worldclockapi.com)
                            try
                            {
                                using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                                {
                                    if (doc.RootElement.TryGetProperty("currentDateTime", out JsonElement datetimeElement))
                                    {
                                        string datetimeStr = datetimeElement.GetString();
                                        return DateTime.Parse(datetimeStr);
                                    }
                                }
                            }
                            catch
                            {
                                Debug.WriteLine("두 번째 API 형식 파싱 실패");
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"response error : {response.StatusCode}");
                        Debug.WriteLine($"response error : {response.IsSuccessStatusCode}");
                        Debug.WriteLine($"response error : {response.Content}");

                        return await GetGoogleDateTime();
                    }

                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"온라인 시간 조회 오류: {ex.Message}");
                //return null;
                return await GetGoogleDateTime();
            }
        }
    }
}

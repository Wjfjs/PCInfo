
using System.Management;
using System.Text;
using System.Diagnostics;

namespace PCInfo
{
    public partial class Form1 : Form
    {
        private TextBox infoBox;

        // 실시간 사용률 표시 컨트롤
        private Label cpuUsageLabel;
        private Label ramUsageLabel;

        private ProgressBar cpuProgressBar;
        private ProgressBar ramProgressBar;

        // 성능 측정 객체
        private PerformanceCounter? cpuCounter;
        private PerformanceCounter? availableMemoryCounter;

        // 자동 갱신 타이머
        private System.Windows.Forms.Timer updateTimer;

        // 전체 RAM 용량
        private ulong totalRamBytes;

        public Form1()
        {
            // 기본 폼
            Text = "PC 정보 확인 프로그램";
            Size = new Size(750, 900);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            Label title = new Label
            {
                Text = "내 PC 시스템 정보",
                Font = new Font("맑은 고딕", 18, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(25, 20)
            };

            // CPU 사용률 제목
            cpuUsageLabel = new Label
            {
                Text = "CPU 사용률: 측정 중...",
                Font = new Font("맑은 고딕", 11),
                AutoSize = true,
                Location = new Point(25, 75)
            };

            // CPU 사용률 진행 표시줄
            cpuProgressBar = new ProgressBar
            {
                Location = new Point(25, 105),
                Size = new Size(680, 20),
                Maximum = 100
            };

            // RAM 사용률 제목
            ramUsageLabel = new Label
            {
                Text = "RAM 사용률: 측정 중...",
                Font = new Font("맑은 고딕", 11),
                AutoSize = true,
                Location = new Point(25, 140)
            };

            // RAM 사용률 진행 표시줄
            ramProgressBar = new ProgressBar
            {
                Location = new Point(25, 170),
                Size = new Size(680, 20),
                Maximum = 100
            };

            // 정보 출력창
            infoBox = new TextBox
            {
                Location = new Point(25, 215),
                Size = new Size(680, 300),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("맑은 고딕", 11),
                BackColor = Color.White
            };

            // 새로고침 버튼
            Button refreshButton = new Button
            {
                Text = "정보 새로고침",
                Location = new Point(25, 535),
                Size = new Size(150, 40)
            };

            refreshButton.Click += (s, e) => LoadPCInfo();

            Controls.Add(title);
            Controls.Add(cpuUsageLabel);
            Controls.Add(cpuProgressBar);
            Controls.Add(ramUsageLabel);
            Controls.Add(ramProgressBar);
            Controls.Add(infoBox);
            Controls.Add(refreshButton);

            // 성능 카운터 초기화
            InitializePerformanceCounters();

            // 1초마다 사용률 갱신
            updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = 1000;
            updateTimer.Tick += (s, e) => UpdateUsage();

            // 프로그램 실행 시 정보 조회
            Load += (s, e) =>
            {
                LoadPCInfo();
                UpdateUsage();
                updateTimer.Start();
            };

            // 프로그램 종료 시 리소스 정리
            FormClosed += (s, e) =>
            {
                updateTimer.Stop();
                updateTimer.Dispose();

                cpuCounter?.Dispose();
                availableMemoryCounter?.Dispose();
            };
        }

        // 성능 카운터 초기화
        private void InitializePerformanceCounters()
        {
            try
            {
                // 전체 CPU 사용률
                cpuCounter = new PerformanceCounter(
                    "Processor",
                    "% Processor Time",
                    "_Total"
                );

                // 사용 가능한 메모리
                availableMemoryCounter = new PerformanceCounter(
                    "Memory",
                    "Available MBytes"
                );

                // 첫 번째 측정값은 기준값이므로 버림
                cpuCounter.NextValue();
            }
            catch (Exception ex)
            {
                cpuUsageLabel.Text = "CPU 측정 초기화 오류";
                ramUsageLabel.Text = "RAM 측정 초기화 오류";

                MessageBox.Show(
                    $"오류 종류: {ex.GetType().Name}\n" +
                    $"오류 내용: {ex.Message}\n\n" +
                    $"상세 정보:\n{ex}"
                );
            }
        }

        // CPU 및 RAM 사용률 갱신
        private void UpdateUsage()
        {
            try
            {
                // CPU 사용률
                if (cpuCounter != null)
                {
                    float cpuUsage = cpuCounter.NextValue();

                    cpuUsage = Math.Clamp(cpuUsage, 0, 100);

                    cpuUsageLabel.Text =
                        $"CPU 사용률: {cpuUsage:F1}%";

                    cpuProgressBar.Value =
                        (int)cpuUsage;
                }

                // RAM 사용률
                if (availableMemoryCounter != null &&
                    totalRamBytes > 0)
                {
                    // 사용 가능한 RAM (MB)
                    float availableMB =
                        availableMemoryCounter.NextValue();

                    // 전체 RAM (MB)
                    double totalMB =
                        totalRamBytes / 1024.0 / 1024.0;

                    // 사용 중인 RAM
                    double usedMB =
                        totalMB - availableMB;

                    // RAM 사용률
                    double ramUsage =
                        (usedMB / totalMB) * 100;

                    ramUsage = Math.Clamp(ramUsage, 0, 100);

                    ramUsageLabel.Text =
                        $"RAM 사용률: {ramUsage:F1}% " +
                        $"({usedMB:N0} / {totalMB:N0} MB)";

                    ramProgressBar.Value =
                        (int)ramUsage;
                }
            }
            catch (Exception ex)
            {
                cpuUsageLabel.Text =
                    "사용률 조회 오류: " + ex.Message;
            }
        }

        // PC 정보 조회
        private void LoadPCInfo()
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                sb.AppendLine("========== PC 정보 ==========\n");

                // CPU 정보
                string cpu = GetWmiValue(
                    "Win32_Processor", "Name");

                sb.AppendLine("[ CPU ]");
                sb.AppendLine(cpu + "\n");

                // RAM 정보
                sb.AppendLine("[ RAM ]");

                string ram = GetWmiValue(
                    "Win32_ComputerSystem",
                    "TotalPhysicalMemory");

                if (ulong.TryParse(ram, out ulong ramBytes))
                {
                    totalRamBytes = ramBytes;

                    double ramGB = ramBytes / 1024.0 / 1024.0 / 1024.0;

                    sb.AppendLine($"전체 메모리: {ramGB:N2} GB");
                }
                else
                {
                    sb.AppendLine("정보 없음");
                    totalRamBytes = 0;
                }

                sb.AppendLine();

                // GPU 정보
                sb.AppendLine("[ GPU ]");

                var gpus = GetWmiValues(
                    "Win32_VideoController", "Name");

                foreach (string gpu in gpus)
                    sb.AppendLine(gpu);

                sb.AppendLine();

                // Windows 정보
                sb.AppendLine("[ 운영체제 ]");

                string os = GetWmiValue(
                    "Win32_OperatingSystem", "Caption");

                string version = GetWmiValue(
                    "Win32_OperatingSystem", "Version");

                sb.AppendLine(os);
                sb.AppendLine("버전: " + version);

                sb.AppendLine();

                // 디스크 정보
                sb.AppendLine("[ 디스크 정보 ]");

                using var searcher =
                    new ManagementObjectSearcher(
                        "SELECT DeviceID, Size, FreeSpace " +
                        "FROM Win32_LogicalDisk WHERE DriveType=3");

                using var results = searcher.Get();

                foreach (ManagementObject disk in results)
                {
                    using (disk)
                    {
                        string drive =
                            Convert.ToString(disk["DeviceID"]) ?? "";

                        ulong size =
                            Convert.ToUInt64(disk["Size"]);

                        ulong free =
                            Convert.ToUInt64(disk["FreeSpace"]);

                        sb.AppendLine(drive);
                        sb.AppendLine(
                            "전체 용량: " + FormatBytes(size));

                        sb.AppendLine(
                            "남은 용량: " + FormatBytes(free));

                        sb.AppendLine();
                    }
                }

                sb.AppendLine("==============================");
            }
            catch (Exception ex)
            {
                sb.AppendLine("정보 조회 오류: " + ex.Message);
            }

            infoBox.Text = sb.ToString();
        }

        // WMI 단일 값 조회
        private string GetWmiValue(
            string className, string propertyName)
        {
            using var searcher =
                new ManagementObjectSearcher(
                    $"SELECT {propertyName} FROM {className}");

            using var results = searcher.Get();

            foreach (ManagementObject item in results)
            {
                using (item)
                {
                    return Convert.ToString(
                        item[propertyName]) ?? "정보 없음";
                }
            }

            return "정보 없음";
        }

        // WMI 여러 값 조회
        private string[] GetWmiValues(
            string className, string propertyName)
        {
            var values = new List<string>();

            using var searcher =
                new ManagementObjectSearcher(
                    $"SELECT {propertyName} FROM {className}");

            using var results = searcher.Get();

            foreach (ManagementObject item in results)
            {
                using (item)
                {
                    string value = Convert.ToString(
                        item[propertyName]) ?? "";

                    if (!string.IsNullOrWhiteSpace(value))
                        values.Add(value);
                }
            }

            return values.ToArray();
        }

        // 바이트를 읽기 쉬운 단위로 변환
        private string FormatBytes(ulong bytes)
        {
            string[] units = { "B", "KB", "GB", "TB" };

            double size = bytes;
            int index = 0;

            while (size >= 1024 && index < units.Length - 1)
            {
                size /= 1024;
                index++;
            }

            return $"{size:N2} {units[index]}";
        }
    }
}

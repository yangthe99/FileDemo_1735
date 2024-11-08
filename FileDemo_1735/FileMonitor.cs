using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FileDemo_1735
{
    /// <summary>
    /// 檔案監控
    /// </summary>
    public class FileMonitor
    {
        /// <summary>
        /// 被監控的資料夾路徑
        /// </summary>
        private string _Path;
        /// <summary>
        /// 被監控的檔案們
        /// </summary>
        private List<string> _Files;
        /// <summary>
        /// 儲存檔案的內容，用於比對檔案是否有變更
        /// </summary>
        private Dictionary<string, string> _fileContents = new Dictionary<string, string>();

        private List<FileSystemEventArgs> _changeBuffer = new List<FileSystemEventArgs>();
        private Timer _timer;
        private HashSet<string> _processedChanges = new HashSet<string>();


        /// <summary>
        /// FileMonitor建構子
        /// </summary>
        /// <param name="configFilePath"></param>
        public FileMonitor(string configFilePath)
        {
            var config = LoadConfig(configFilePath);
            _Path = config.Path;
            _Files = config.Files;

            // 設定 Timer，定時處理變更
            // new Timer(執行的回呼方法, 傳遞給回呼方法的資料(創建 Timer 時提供的物件), 首次執行的延遲時間, 間隔時間)
            _timer = new Timer(FlushChanges, null, TimeSpan.Zero, TimeSpan.FromSeconds(5)); // 每5秒處理一次
        }

        /// <summary>
        /// 讀取並反序列化設定檔
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private Config LoadConfig(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath); // 讀取filePath內 檔案的所有內容
                return JsonConvert.DeserializeObject<Config>(json); // 反序列化
            }
            catch (Exception ex)
            {
                Console.WriteLine($"無法讀取設定檔: {ex.Message}");
                Environment.Exit(1); //非零值表示異常終止，0表示正常中止
                return null;
            }
        }
        /// <summary>
        /// 監控作業
        /// </summary>
        public void StartMonitoring()
        {
            foreach (var file in _Files)
            {
                try
                {
                    string filePath = Path.Combine(_Path, file); // 確保路徑與檔案名稱組合正確
                    if (File.Exists(filePath)) //檔案存在
                    {
                        string initialContent = File.ReadAllText(filePath); // 讀取指定檔案的所有文字內容
                        _fileContents[file] = initialContent; // 將指定檔案內容存入_fileContents
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"無法讀取檔案 {file} 的初始內容: {ex.Message}");
                }
            }

            // 使用 FileSystemWatcher 監控檔案內容變動
            FileSystemWatcher watcher = new FileSystemWatcher(_Path)
            {
                NotifyFilter = NotifyFilters.LastWrite
            };

            // 設定過濾條件: 只監控指定的檔案
            watcher.Filter = "*.*"; // 可以設定為特定擴展名，例如 "*.txt" 或 "*.log"

            // lambda寫法的事件處理
            watcher.Changed += (sender, e) => OnFileChanged(e);
            watcher.Created += (sender, e) => OnFileChanged(e); // 監控檔案新增
            watcher.Deleted += (sender, e) => OnFileChanged(e); // 監控檔案刪除

            // 啟動監控
            watcher.EnableRaisingEvents = true;

            Console.WriteLine("開始監控檔案變動...");
            Console.ReadLine(); // 按 Enter 鍵退出監控
        }
        /// <summary>
        /// 檢查檔案的異動
        /// </summary>
        private async void OnFileChanged(FileSystemEventArgs e)
        {
            // 只處理設定檔中指定的檔案
            if (_Files.Contains(e.Name))
            {
                string changeKey = $"{e.Name}_{e.ChangeType}";

                // 如果尚未處理過此檔案的變更
                if (!_processedChanges.Contains(changeKey))
                {
                    _processedChanges.Add(changeKey); // 記錄該檔案和變更類型的組合
                    //Console.WriteLine($"{e.Name}檔案({e.ChangeType})");

                    try
                    {
                        string filePath = Path.Combine(_Path, e.Name);
                        string currentContent = await File.ReadAllTextAsync(filePath); // 非同步讀取檔案內容

                        // 檢查檔案是否已經監控過
                        if (_fileContents.ContainsKey(e.Name))
                        {
                            string previousContent = _fileContents[e.Name];
                            Console.WriteLine($"檔案 {e.Name} 此批次的內容異動如下：");
                            DisplayFileDifferences(previousContent, currentContent); // 顯示內容差異
                        }
                        // 更新檔案內容為最新
                        _fileContents[e.Name] = currentContent;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"無法處理檔案 {e.Name}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 顯示檔案差異處
        /// </summary>
        /// <param name="previousContent"></param>
        /// <param name="currentContent"></param>
        private void DisplayFileDifferences(string previousContent, string currentContent)
        {
            // 將previousContent的內容拆分[], StringSplitOptions.RemoveEmptyEntries：則會將[]內空的項目("")去除
            var previousLines = previousContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var currentLines = currentContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // 找出新增的內容（currentContent 中有，而 previousContent 中沒有的行）
            int previousIndex = 0;
            bool contentChanged = false;  // 標示是否發生變更

            // 比較檔案的每一行
            for (int i = 0; i < currentLines.Length; i++)
            {
                if (previousIndex < previousLines.Length && currentLines[i] == previousLines[previousIndex])
                {
                    previousIndex++; // 當前行和之前的行相同，跳過
                }
                else
                {
                    // 顯示新增的行（不同的行）
                    contentChanged = true;
                    Console.WriteLine(currentLines[i]);
                }
            }

            // 若無新增行或變更，可以不顯示過多資訊
            if (!contentChanged)
            {
                Console.WriteLine("檔案內容沒有變更");
            }
        }
        /// <summary>
        /// 清理已處理過的變更標記
        /// </summary>
        /// <param name="state"></param>
        private async void FlushChanges(object state)
        {
            //// 如果有變更紀錄
            //if (_changeBuffer.Any())
            //{
            //    foreach (var changeEvent in _changeBuffer)
            //    {
            //        try
            //        {
            //            string filePath = Path.Combine(_Path, changeEvent.Name);
            //            string currentContent = await File.ReadAllTextAsync(filePath);

            //            // 檢查檔案是否已經監控過
            //            if (_fileContents.ContainsKey(changeEvent.Name))
            //            {
            //                string previousContent = _fileContents[changeEvent.Name];
            //                Console.WriteLine($"檔案 {changeEvent.Name} 此批次的內容異動如下：");
            //                DisplayFileDifferences(previousContent, currentContent);
            //            }

            //            // 更新檔案內容為最新
            //            _fileContents[changeEvent.Name] = currentContent;
            //        }
            //        catch (Exception ex)
            //        {
            //            Console.WriteLine($"無法處理檔案 {changeEvent.Name}: {ex.Message}");
            //        }
            //    }
                // 清理已處理過的檔案變更標記
                _processedChanges.Clear();
            //}
        }
    }
}

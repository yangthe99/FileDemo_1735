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
        /// <summary>
        /// 儲存檔案變更事件的緩衝區，降低處理的頻率(批次處理)
        /// </summary>
        private List<FileSystemEventArgs> _changeBuffer = new List<FileSystemEventArgs>();
        /// <summary>
        /// 追蹤最後一次檔案變更的時間，DateTime.MinValue表示尚未處理過任何變更事件
        /// </summary>
        private DateTime _lastChangeTime = DateTime.MinValue;
        /// <summary>
        /// 用於定期觸發處理緩衝區中的事件
        /// </summary>
        private Timer _timer;
        /// <summary>
        /// 記錄已經處理過的變更事件(確保每個事件只被處理一次)
        /// </summary>
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
            _lastChangeTime = DateTime.Now;

            // 設定 Timer，定時處理一段時間內的異動內容
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
            if (_Files.Contains(e.Name))
            {
                string changeKey = $"{e.Name}_{e.ChangeType}";
                // 增加這個檢查來確保這次變更會被處理
                if (!_processedChanges.Contains(changeKey))
                {
                    _processedChanges.Add(changeKey);
                    _changeBuffer.Add(e);
                }
                if (!_processedChanges.Contains(changeKey))
                {
                    _processedChanges.Add(changeKey);
                    _changeBuffer.Add(e);
                    _lastChangeTime = DateTime.Now;

                    try
                    {
                        string filePath = Path.Combine(_Path, e.Name);
                        string currentContent = await File.ReadAllTextAsync(filePath);

                        if (_fileContents.ContainsKey(e.Name))
                        {
                            string previousContent = _fileContents[e.Name];
                            Console.WriteLine($"檔案 {e.Name} 此批次的內容異動如下：");
                            DisplayFileDifferences(previousContent, currentContent);
                        }

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
            // 每次定時器觸發時，處理所有積累的變更
            if (_changeBuffer.Count > 0) // 緩衝區有資料的話
            {
                // 確保每次定時器處理緩衝區的變更
                foreach (var change in _changeBuffer)
                {
                    try
                    {
                        string filePath = Path.Combine(_Path, change.Name);  // 確保路徑與檔案名稱組合正確
                        string currentContent = await File.ReadAllTextAsync(filePath);

                        if (_fileContents.ContainsKey(change.Name))
                        {
                            string previousContent = _fileContents[change.Name];
                            Console.WriteLine($"檔案 {change.Name} 此批次的內容異動如下：");
                            DisplayFileDifferences(previousContent, currentContent);
                        }

                        // 更新檔案內容
                        _fileContents[change.Name] = currentContent;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"無法處理檔案 {change.Name}: {ex.Message}");
                    }
                }

                // 清空已處理的變更
                _processedChanges.Clear();
                _changeBuffer.Clear();
            }
        }
    }
}

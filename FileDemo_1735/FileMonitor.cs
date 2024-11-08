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
        /// 先儲存檔案內容，用於比對當前檔案是否有變更
        /// </summary>
        private Dictionary<string, string> _FileContents = new Dictionary<string, string>();
        /// <summary>
        /// 儲存檔案變更事件的緩衝區
        /// </summary>
        private List<FileSystemEventArgs> _ChangeBuffer = new List<FileSystemEventArgs>();
        /// <summary>
        /// 用於追蹤最後一次檔案變更的時間
        /// </summary>
        private DateTime _LastChangeTime;
        /// <summary>
        /// 要寫定期觸發處理緩衝區中的事件
        /// </summary>
        private Timer _Timer;
        /// <summary>
        /// 記錄已經處理過的變更事件(確保每個事件只被處理一次)
        /// </summary>
        private HashSet<string> _ProcessedChanges = new HashSet<string>();


        /// <summary>
        /// FileMonitor建構子
        /// </summary>
        /// <param name="configFilePath"></param>
        public FileMonitor(string configFilePath)
        {
            var config = LoadConfig(configFilePath);
            _Path = config.Path;
            _Files = config.Files;
            _LastChangeTime = DateTime.Now;

            // 設定 Timer，定時處理一段時間內的異動內容
            // new Timer(執行的回呼方法, 傳遞給回呼方法的資料(創建 Timer 時提供的物件), 首次執行的延遲時間, 間隔時間)
            _Timer = new Timer(FlushChanges, null, TimeSpan.Zero, TimeSpan.FromSeconds(5)); // 每5秒處理一次
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
                        _FileContents[file] = initialContent; // 將指定檔案內容存入_fileContents
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
            // 確保是被監控的檔案
            if (_Files.Contains(e.Name))
            {
                // 建立一個唯一的識別鍵 changeKey，追蹤檔案變更。
                string changeKey = $"{e.Name}_{e.ChangeType}";

                // changeKey沒有在_processedChanges的話
                if (!_ProcessedChanges.Contains(changeKey))
                {
                    _ProcessedChanges.Add(changeKey);  // 標記為已處理
                    _ChangeBuffer.Add(e);              // 加入緩衝區
                    _LastChangeTime = DateTime.Now;    // 更新_lastChangeTime
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
            if (_ChangeBuffer.Count > 0)
            {
                foreach (var change in _ChangeBuffer)
                {
                    try
                    {
                        string filePath = Path.Combine(_Path, change.Name); // 路徑與檔案名稱組合
                        string currentContent = await File.ReadAllTextAsync(filePath);

                        if (_FileContents.ContainsKey(change.Name)) // 確認檔案是否存在於_fileContents
                                                                    // ContainsKey：字典(_fileContents)中存在鍵(change.Name)就進行內容比對
                        {
                            string previousContent = _FileContents[change.Name];
                            Console.WriteLine($"檔案【{change.Name}】此批次的異動內容如下：");
                            DisplayFileDifferences(previousContent, currentContent);
                        }

                        // 更新_fileContents
                        _FileContents[change.Name] = currentContent;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"無法處理檔案 {change.Name}: {ex.Message}");
                    }
                }

                // 清空已經被處理的事件們和緩衝區，避免重複處理。
                _ProcessedChanges.Clear();
                _ChangeBuffer.Clear();
            }
        }
    }
}

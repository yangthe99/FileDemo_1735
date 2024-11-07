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
        /// FileMonitor建構子
        /// </summary>
        /// <param name="configFilePath"></param>
        public FileMonitor(string configFilePath)
        {
            var config = LoadConfig(configFilePath);
            _Path = config.Path;
            _Files = config.Files;
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
            // 啟動監控前，先讀取並記錄所有指定檔案的初始內容
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
        /// <param name="e"></param>
        private void OnFileChanged(FileSystemEventArgs e)
        {
            // 只處理設定檔中指定的檔案
            if (_Files.Contains(e.Name))
            {
                Console.WriteLine($"檔案 {e.Name} ({e.ChangeType}):");

                try
                {
                    string currentContent = File.ReadAllText(e.FullPath);

                    // 檢查檔案是否已經監控過
                    if (_fileContents.ContainsKey(e.Name))
                    {
                        // 與之前的內容對比，顯示差異
                        string previousContent = _fileContents[e.Name];
                        DisplayFileDifferences(previousContent, currentContent);
                    }

                    // 更新檔案內容為最新的內容
                    _fileContents[e.Name] = currentContent;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"無法讀取檔案內容: {ex.Message}");
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
            var previousLines = previousContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var currentLines = currentContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // 找出新增的內容（currentContent 中有，而 previousContent 中沒有的行）
            int previousIndex = 0;
            for (int i = 0; i < currentLines.Length; i++)
            {
                if (previousIndex < previousLines.Length && currentLines[i] == previousLines[previousIndex])
                    previousIndex++; // 當前行和之前的行相同，跳過
                Console.WriteLine(currentLines[i]); // 當前行是新增的，顯示它
            }
        }
    }
}

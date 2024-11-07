using System.Text.Json;

namespace FileDemo_1735
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            CheckFolderFiles(); // 檢查資料夾和檔案是否存在

            // 設定檔相對路徑
            string configFilePath = "config.json";

            // 創建 FileMonitor 物件並開始監控
            FileMonitor fileMonitor = new FileMonitor(configFilePath);
            fileMonitor.StartMonitoring();

            Console.ReadKey();
        }

        /// <summary>
        /// 檢查資料夾和檔案是否存在
        /// </summary>
        private static void CheckFolderFiles()
        {
            CheckFolderFiles checkFolderFiles = new CheckFolderFiles();

            checkFolderFiles.CheckFolder("FileDemo_1735");
            checkFolderFiles.CheckFile("FileDemo_1735", "file1.txt");
            checkFolderFiles.CheckFile("FileDemo_1735", "file2.txt");
        }
    }
}

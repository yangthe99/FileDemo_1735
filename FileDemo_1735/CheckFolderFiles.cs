using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace FileDemo_1735
{
    /// <summary>
    /// 檢查資料夾和檔案是否存在
    /// </summary>
    public class CheckFolderFiles
    {
        private string _DrivePath;

        /// <summary>
        /// 說明資料夾位置
        /// </summary>
        public CheckFolderFiles()
        {
            _DrivePath = @"C:\";  
            Console.WriteLine($"路徑：{_DrivePath}");
        }

        /// <summary>
        /// 檢查資料夾
        /// </summary>
        /// <param name="folderName"></param>
        /// <param name="fileName"></param>
        public void CheckFolder(string folderName)
        {
            // 組合完整的檔案路徑
            string folderPath = Path.Combine(_DrivePath, folderName);

            // 檢查資料夾是否已存在，若不存在則創建
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
            Console.WriteLine($"資料夾 '{folderName}' 已生成。");
        }

        /// <summary>
        /// 檢查檔案
        /// </summary>
        /// <param name="folderName"></param>
        /// <param name="fileName"></param>
        public void CheckFile(string folderName, string fileName)
        {
            // 組合完整的檔案路徑
            string folderPath = Path.Combine(_DrivePath, folderName);
            string filePath = Path.Combine(folderPath, fileName);

            // 檢查檔案是否已存在，若不存在則創建
            if (!File.Exists(filePath))
                File.Create(filePath).Dispose();
            Console.WriteLine($"檔案 '{fileName}' 已生成。");
        }
    }
}

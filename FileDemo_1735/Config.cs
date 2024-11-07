using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileDemo_1735
{
    /// <summary>
    /// config.json結構
    /// </summary>
    public class Config
    {
        /// <summary>
        /// 監控路徑
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// 監控檔案
        /// </summary>
        public List<string> Files { get; set; }
    }
}

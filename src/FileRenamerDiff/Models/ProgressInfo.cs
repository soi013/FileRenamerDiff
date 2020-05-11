using System;
using System.Collections.Generic;
using System.Text;

namespace FileRenamerDiff.Models
{
    /// <summary>
    /// 処理状態情報
    /// </summary>
    public class ProgressInfo
    {
        public int Count { get; }
        public string Message { get; }

        public ProgressInfo(int count, string message)
        {
            Count = count;
            Message = message;
        }
    }
}

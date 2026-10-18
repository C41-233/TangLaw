using System;

namespace Generator;

// 文档本身的错误（如引用了不存在的词条）用本异常作标记：
// Program 捕获后只输出错误信息链，不输出调用堆栈。
internal class SourceException : Exception
{
    public SourceException(string message) : base(message)
    {
    }

    public SourceException(string message, Exception innerException) : base(message, innerException)
    {
    }

    // 异常链上出现 SourceException 即视为文档本身的错误
    public static bool IsSourceError(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (e is SourceException)
            {
                return true;
            }
        }
        return false;
    }
}

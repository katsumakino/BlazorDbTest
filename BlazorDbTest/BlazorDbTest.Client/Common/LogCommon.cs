using System.Net.Http;
using System.Net.Sockets;
using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorDbTest.Common {
  public class LogCommon : ComponentBase {
    // ファイル名とソースコードの実行位置を取得する関数(ログ記録用) 
    public static string GetCallerFIlePath([CallerFilePath] string path = "", [CallerLineNumber] int lineNumber = 0) {
      return $"{path}:{lineNumber}";
    }

    // スクリーンショットを取得する関数
    public static async Task<string> GetScreenshotUrl(IJSRuntime jSRuntime) {
      var screenshotDataUrl = await jSRuntime.InvokeAsync<string>("captureScreenshot");
      var base64Data = screenshotDataUrl.Split(',')[1];
      return base64Data;
    }
  }

  // ログファイル書込み情報クラス
  public class LogInfo {
    public string Type { get; set; } = default!;            // [種別]
    public string Message { get; set; } = default!;         // [メッセージ]
    public string ErrLevel { get; set; } = default!;        // [エラーレベル]
    public string SourcePosition { get; set; } = default!;  // [ソース位置]
    public string SubMessage { get; set; } = default!;      // [備考]
    public string IPAddress { get; set; } = default!;       // [IPアドレス]
    public string Screenshot { get; set; } = default!;      // [スクリーンショット]
  }
}

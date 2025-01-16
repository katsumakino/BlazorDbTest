using BlazorDbTest.Client.Pages;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using static BlazorDbTest.Client.Pages.Counter;

namespace BlazorDbTest.Controllers {

  [Route("api/[controller]")]
  public class LogController : ControllerBase {

    private string logFilePathBase = $"logs_{DateTime.Now:yyyyMMdd}.txt";
    private string logFilePath = "";
    private string logDirTopPath = @"C:/test/";
    private string logDirPathBase = $"_{DateTime.Now:yyyyMMdd}";
    private string errLogDirTopPath = @"C:/testErr/";
    private string errLogDirPathBase = $"_{DateTime.Now:yyyyMMdd_HHmmss}";
    private Common.FIleUtilities utilities = new Common.FIleUtilities();

    [HttpPost("WriteLog")]
    public void WriteLog([FromBody] LogInfo conditions) {
      if (conditions == null) return;

      try {
        Log(conditions);

        // エラーレベルが高い場合は、エラーログを作成する
        if (conditions.ErrLevel == "W") {
          ErrorLog(conditions.Screenshot);
        }
      } catch {
      }
    }

    private void Log(Counter.LogInfo info) {
      CreateLogFile();
      using (StreamWriter writer = new StreamWriter(logFilePath, append: true)) {
        writer.WriteLineAsync($"{DateTime.Now:HH:mm:ss}, {info.Type}, {info.Message}");
      }
    }

    // ログファイルを作成する
    private void CreateLogFile() {
      // フォルダ作成
      var dirPath = logDirTopPath;
      dirPath += logDirPathBase;
      utilities.CreateDir(dirPath);
      CheckLogFolder();

      // ログファイル名に日付を付与
      logFilePath = dirPath + "/" + logFilePathBase;

      // ログファイルが存在しない場合は作成
      utilities.CreateFile(logFilePath);
    }

    // ログフォルダの整理
    private void CheckLogFolder() {
      // ログフォルダが30日分を超えた場合は、古いフォルダを削除する
      var dirPath = logDirTopPath;
      var dirs = Directory.GetDirectories(dirPath);
      if (dirs.Length > 30) {
        // 一番古いフォルダ(名前を昇順ソートした際に一番上に来るもの)を削除
        Directory.Delete(dirs[0], true);
      }
    }

    private async void ErrorLog(string errImg) {
      try {
        // エラーログファイルを作成する
        var dirPath = errLogDirTopPath;
        dirPath += errLogDirPathBase;
        utilities.CreateDir(dirPath);

        // 当日のログファイルをコピーしてフォルダに保存
        var errLogFilePath = dirPath + "/" + logFilePathBase;
        utilities.CopyFile(logFilePath, errLogFilePath, false);

        // エラーダイアログ画像を作成し保存
        var filePathBase = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        var filePath = dirPath + "/" + filePathBase;
        var imageBytes = Convert.FromBase64String(errImg);
        await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);
      } catch {
      }
    }
  }
}

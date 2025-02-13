using BlazorDbTest.Client.Pages;
using Microsoft.AspNetCore.Mvc;
using System.Net.Sockets;
using System.Net;
using AxialManagerS.Shared.Common;

namespace BlazorDbTest.Controllers {

  [Route("api/[controller]")]
  public class LogController : ControllerBase {

    private string logFilePathBase = $"logs_{DateTime.Now:yyyyMMdd}";
    private string logFilePath = "";
    private string logDirTopPath = @"C:/TomeyApp/AxialManager2/Log/";
    private string logDirPathBase = $"_{DateTime.Now:yyyyMMdd}";
    private string errLogDirTopPath = @"C:/TomeyApp/AxialManager2/ErrLog/";
    private string errLogDirPathBase = $"_{DateTime.Now:yyyyMMdd_HHmmss}";
    private const int maxFileSize = 1024 * 1024 * 10; // 10MB
    private const int maxFileCount = 30;
    private FIleUtilities utilities = new FIleUtilities();

    [HttpPost("WriteLog")]
    public void WriteLog([FromBody] LogInfo conditions) {
      if (conditions == null) return;

      try {
        // todo:
        conditions.IPAddress = GetClientIpAddress();

        //if (System.OperatingSystem.IsBrowser()) {
        //  // Client側のIPアドレスを取得
        //  conditions.IPAddress = GetClientIpAddress();
        //} else {
        //  // Server側のIPアドレスを取得
        //  conditions.IPAddress = GetLocalIPAddress().Result;
        //}

        Log(conditions);

        // エラーレベルが高い場合は、エラーログを作成する
        if (conditions.ErrLevel == "W") {
          ErrorLog(conditions.Screenshot);
        }
      } catch {
      }
    }

    private void Log(LogInfo info) {
      try {
        CreateLogFile();
        using (StreamWriter writer = new StreamWriter(logFilePath, append: true)) {
          writer.WriteLineAsync($"{DateTime.Now:HH:mm:ss}, {info.Type}, {info.IPAddress}, {info.Message}, {info.SourcePosition}");
          if (!string.IsNullOrEmpty(info.SubMessage)) {
            writer.WriteLineAsync($"{info.SubMessage}");
          }
        }
      } catch {
      }
    }

    // ログファイルを作成する
    private void CreateLogFile(int num = 0) {
      // フォルダ作成
      var dirPath = logDirTopPath;
      dirPath += logDirPathBase;
      utilities.CreateDir(dirPath);
      CheckLogFolder();

      // ログファイル名に日付を付与
      logFilePath = dirPath + "/" + logFilePathBase + "_" + num.ToString() + ".txt";

      // ログファイルが10MBを超えた場合は、新しいファイルを作成
      if (utilities.FileExists(logFilePath)) {
        var fileInfo = new FileInfo(logFilePath);
        if (fileInfo.Length > maxFileSize) {
          CreateLogFile(num + 1);
        }
      } else {
        // ログファイルが存在しない場合は作成
        utilities.CreateFile(logFilePath);
      }
    }

    // ログフォルダの整理
    private void CheckLogFolder() {
      // ログフォルダが30日分を超えた場合は、古いフォルダを削除する
      var dirPath = logDirTopPath;
      var dirs = Directory.GetDirectories(dirPath);
      if (dirs.Length > maxFileCount) {
        // 一番古いフォルダ(名前を昇順ソートした際に一番上に来るもの)を削除
        Directory.Delete(dirs[0], true);
      }
    }

    // エラーログを作成する
    private async void ErrorLog(string errImg) {
      try {
        // エラーログファイルを作成する
        var dirPath = errLogDirTopPath;
        dirPath += errLogDirPathBase;
        utilities.CreateDir(dirPath);

        // 当日のログファイルをコピーしてフォルダに保存
        var errLogFilePath = dirPath + "/" + utilities.ExtractFileName(logFilePath);
        utilities.CopyFile(logFilePath, errLogFilePath, false);

        // エラーダイアログ画像を作成し保存
        var filePathBase = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        var filePath = dirPath + "/" + filePathBase;
        var imageBytes = Convert.FromBase64String(errImg);
        await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);
      } catch {
      }
    }

    // ローカルIPアドレス(Server側)を取得
    public static async Task<string> GetLocalIPAddress() {
      string localIP = "NULL";
      try {
        var host = await Dns.GetHostEntryAsync(Dns.GetHostName());
        foreach (var ip in host.AddressList) {
          if (ip.AddressFamily == AddressFamily.InterNetwork) {
            localIP = ip.ToString();
            break;
          }
        }
      } catch (Exception ex) {
        Console.WriteLine($"Error: {ex.Message}");
      }
      return localIP;
    }

    // クライアントのIPアドレスを取得
    private string GetClientIpAddress() {
      var remoteIpAddress = HttpContext.Connection.RemoteIpAddress;
      string nullMessage = "IP NOT FOUND";

      if (remoteIpAddress == null) {
        return nullMessage;
      }

      // IPv4 アドレスを取得
      if (remoteIpAddress.AddressFamily == AddressFamily.InterNetwork) {
        return remoteIpAddress.ToString();
      }

      // IPv6 アドレスを IPv4 に変換
      if (remoteIpAddress.AddressFamily == AddressFamily.InterNetworkV6) {
        var ipv4Address = Dns.GetHostEntry(remoteIpAddress).AddressList
          .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        if (ipv4Address != null) {
          return ipv4Address.ToString();
        }
      }

      return nullMessage;
    }


    [HttpGet("GetClientIp")]
    public string GetClientIp() {
      var ipAddress = HttpContext.Connection.RemoteIpAddress;

      string nullMessage = "IP NOT FOUND";

      if (ipAddress == null) {
        return nullMessage;
      }

      // IPv4 アドレスを取得
      if (ipAddress.AddressFamily == AddressFamily.InterNetwork) {
        return ipAddress.ToString();
      }

      // IPv6 アドレスを IPv4 に変換
      if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6) {
        var ipv4Address = Dns.GetHostEntry(ipAddress).AddressList
          .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        if (ipv4Address != null) {
          return ipv4Address.ToString();
        }
      }

      return nullMessage;
    }
  }
}
using Microsoft.VisualBasic.FileIO;

namespace AxialManagerS.Shared.Common {

  using System.IO;

  public class FIleUtilities {

    /// <summary>
    /// ファイルをコピーする 
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="newName"></param>
    /// <param name="overwrite"></param>
    /// <returns></returns>
    public bool CopyFile(string fileName, string newName, bool overwrite) {
      bool ret = true;
      string dir = ExtractFilePath(fileName);

      try {
        CreateDir(dir);
        File.Copy(fileName, newName, overwrite);
      } catch {
        ret = false;
      }

      return ret;
    }

    /// <summary>
    /// ファイルをゴミ箱に移動する
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public bool RemoveFile(string fileName) {
      bool ret = true;

      try {
        FileSystem.DeleteFile(
        fileName,
        UIOption.OnlyErrorDialogs,
        RecycleOption.SendToRecycleBin);
      } catch {
        ret = false;
      }

      return ret;
    }

    /// <summary>
    /// ファイル名変更
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="newName"></param>
    /// <returns></returns>
    public bool RenameFile(string fileName, string newName) {
      bool ret = true;
      try {
        File.Move(fileName, newName, true);
      } catch {
        ret = false;
      }

      return ret;
    }

    /// <summary>
    /// フォルダ名変更
    /// </summary>
    /// <param name="dirName"></param>
    /// <param name="newName"></param>
    /// <returns></returns>
    public bool RenameFolder(string dirName, string newName) {
      bool ret = true;
      try {
        Directory.Move(dirName, newName);
      } catch {
        ret = false;
      }
      return ret;
    }

    /// <summary>
    /// ファイル存在確認
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public bool FileExists(string fileName) {
      return File.Exists(fileName);
    }

    /// <summary>
    /// フォルダ存在確認
    /// </summary>
    /// <param name="dirName"></param>
    /// <returns></returns>
    public bool FolderExists(string dirName) {
      return Directory.Exists(dirName);
    }

    /// <summary>
    /// パスからファイル名を取得する
    /// フォルダ名取得でも使用可能
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public string ExtractFileName(string fileName) {
      return Path.GetFileName(fileName);
    }

    /// <summary>
    /// パスから拡張子を取得する
    /// test.txtの場合、.txtを返す
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public string ExtractFileExt(string fileName) {
      return Path.GetExtension(fileName);
    }

    /// <summary>
    /// パスから拡張子を除いたファイル名を取得する
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public string ExtractFileBaseName(string fileName) {
      return Path.GetFileNameWithoutExtension(fileName);
    }

    /// <summary>
    /// ファイル名からパスを取得する
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public string ExtractFilePath(string fileName) {
      return Path.GetDirectoryName(fileName) + "/";
    }

    /// <summary>
    /// 拡張子変更
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="ext"></param>
    /// <returns></returns>
    public string ChangeFileExt(string fileName, string ext) {
      return Path.ChangeExtension(fileName, ext);
    }

    /// <summary>
    /// ファイル探索
    /// </summary>
    /// <param name="dirName"></param>
    /// <param name="ext">検索対象拡張子</param>
    /// <param name="files">結果リスト</param>
    /// <param name="limit">検索ディレクトリの深さ(初期値がマイナスの場合は、ほぼ無限)</param>
    public void SearchFile(string dirName, string ext, ref List<string> files, int limit) {
      if (limit == 0) return;

      string filter = "*" + ext;
      // 現フォルダ内のファイルを探索
      string[] currentDirFiles = Directory.GetFiles(dirName, filter, SearchOption.TopDirectoryOnly);
      foreach (string file in currentDirFiles) {
        files.Add(file);
      }

      // 現フォルダ内のサブフォルダを探索
      string[] subDir = Directory.GetDirectories(dirName, "*", SearchOption.TopDirectoryOnly);
      foreach (string dir in subDir) {
        // フォルダ再帰探索
        SearchFile(dir, ext, ref files, limit - 1);
      }
    }

    /// <summary>
    /// フォルダ探索
    /// </summary>
    /// <param name="dirName"></param>
    /// <param name="dirs">結果リスト</param>
    public void SearchFolder(string dirName, ref List<string> dirs) {
      string[] dirList = Directory.GetDirectories(dirName, "*", SearchOption.AllDirectories);
      foreach (string dir in dirList) {
        dirs.Add(dir);
      }
    }

    /// <summary>
    /// フォルダ以下全て削除
    /// </summary>
    /// <param name="dirName"></param>
    /// <returns></returns>
    public bool ForceRemoveDir(string dirName) {
      bool ret = true;

      if (Directory.Exists(dirName)) {
        try {
          Directory.Delete(dirName, true);
        } catch {
          ret = false;
        }
      }

      return ret;
    }

    /// <summary>
    /// フォルダ作成
    /// </summary>
    /// <param name="dir"></param>
    public void CreateDir(string dir) {
      if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
    }

    /// <summary>
    /// ファイル作成
    /// </summary>
    /// <param name="file"></param>
    public void CreateFile(string file) {
      if (!File.Exists(file)) File.Create(file).Close();
    }

    /// <summary>
    /// 属性(読取専用)
    /// </summary>
    /// <param name="fileName"></param>
    public void SetFileAttributeReadOnly(string fileName) {
      File.SetAttributes(fileName, FileAttributes.ReadOnly);
    }

    /// <summary>
    /// 属性(読取・書き込み可)
    /// </summary>
    /// <param name="fileName"></param>
    public void SetFileAttributeWritable(string fileName) {
      File.SetAttributes(fileName, FileAttributes.Normal);
    }

    /// <summary>
    /// フォルダコピー
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="overwrite"></param>
    /// <returns></returns>
    public bool CopyFolder(in string from, in string to, bool overwrite) {
      // フォルダではないと終了する
      if (!Directory.Exists(from)) return false;
      // コピー先のパスが空なら終了
      if (to.Length == 0) return false;

      // コピー先フォルダ作成
      CreateDir(to);

      // コピー元のフォルダとファイル情報を取得
      List<string> dirs = new();
      List<string> files = new();
      SearchFolder(from, ref dirs);
      SearchFile(from, "", ref files, -1);

      // コピー元のファイルを全てコピー
      foreach (string file in files) {
        string fileName = ExtractFileName(file);
        string fromPath = Path.Combine(from, fileName);
        string toPath = Path.Combine(to, fileName);
        CopyFile(fromPath, toPath, overwrite);
      }

      // 再帰呼び出しによりコピー元のフォルダを全てコピー
      foreach (string dir in dirs) {
        string dirName = ExtractFileName(dir);
        string fromPath = Path.Combine(from, dirName);
        string toPath = Path.Combine(to, dirName);
        CopyFolder(fromPath, toPath, overwrite);
      }

      return true;
    }

    /// <summary>
    /// 現在の1つ上のパスを返す
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public string CdUp(string path) {
      DirectoryInfo? dirParent = Directory.GetParent(path);
#pragma warning disable CS8602 // null 参照の可能性があるものの逆参照です。
      return dirParent.FullName;
    }

    /// <summary>
    /// 引数で指定したものがフォルダであるかどうかの確認
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public bool IsDir(string fileName) {
      return File.GetAttributes(fileName).HasFlag(FileAttributes.Directory);
    }

    /// <summary>
    /// 引数で指定したものがファイルであるかどうかの確認
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public bool IsFile(string fileName) {
      return !File.GetAttributes(fileName).HasFlag(FileAttributes.Directory);
    }

    /// <summary>
    /// ファイル名からフォルダ名を取得する
    /// 例：fileName=c:/test/example.bmp
    /// 戻り値：test
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public string GetCurrentFolderName(string fileName) {
      string? folderPath = Path.GetDirectoryName(fileName);
      return Path.GetFileName(folderPath);
    }

    /// <summary>
    /// 区切り文字の置換
    /// </summary>
    /// <param name="str"></param>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <returns></returns>
    public string ChangeDelimiter(string str, string before, string after) {
      return str.Replace(before, after);
    }

    /// <summary>
    /// デスクトップのパスをget
    /// </summary>
    /// <returns></returns>
    public string GetDesktopPath() {
      return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "\\";
    }

    /// <summary>
    /// CSVファイル読出
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public List<string> LoadCsv(string fileName) {
      List<string> list = new List<string>();

      // CSVファイルパスを指定して開く
      StreamReader sr = new StreamReader(@fileName);

      // ファイルの末尾まで繰り返す
      while (!sr.EndOfStream) {
        // 1行読込
        string line = sr.ReadLine();
        // ','区切りで配列に格納
        string[] values = line.Split(',');

        // Listに格納
        list.AddRange(values);
      }

      // ファイルを閉じる
      sr.Close();

      return list;
    }

  }
}

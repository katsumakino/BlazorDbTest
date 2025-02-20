using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxialManagerS.Shared.Common {
  public class DBAccessCommon {

    // 空文字&日本語をBase64に変換(Client側で実行)
    public static string ConvertString(string? str) {
      return string.IsNullOrEmpty(str) ? string.Empty : Convert.ToBase64String(Encoding.UTF8.GetBytes(str));
    }

    // 空文字チェックして、string型に復元(Server側で実行)
    public static string CheckConvertString(string str) {
      return (str != string.Empty) ? Encoding.UTF8.GetString(Convert.FromBase64String(str)) : string.Empty;
    }

  }
}

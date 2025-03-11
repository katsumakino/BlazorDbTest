using AxialManagerS.Shared.Common;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;
using System.Text;
using static BlazorDbTest.Controllers.DBCommonController;
using static BlazorDbTest.Controllers.DBTreatmentController;

namespace BlazorDbTest.Controllers {

  [Route("api/[controller]")]

  public class DBAxmCommentController : ControllerBase {

    // コメント登録
    [HttpPost("SetAxmComment")]
    public void SetAxmComment([FromBody] AxmCommentRequest conditions) {
      try {
        if (conditions == null) return;
        if (conditions.PatientID == null || conditions.PatientID == string.Empty) return;
        if (conditions.AxmComment.Description == null) return;

        bool result = false;
        DBAccess dbAccess = DBAccess.GetInstance();

        try {
          // PostgreSQL Server 通信接続
          NpgsqlConnection sqlConnection = dbAccess.GetSqlConnection();

          // クエリコマンド実行

          // UUIDの有無を確認(true:update / false:insert)
          var uuid = Select_PTUUID_by_PTID(sqlConnection, conditions.PatientID);
          if (uuid == string.Empty) {
            // コメント登録時は、必ず患者データが存在する
            return;
          }

          // コメントデータID取得
          var commenttype_id = Select_AxmCommentTypeId(sqlConnection, AXM_COMMENT_TYPE[(int)conditions.AxmComment.CommentType]);
          var comment_id = Select_AxmCommentID_by_PK(sqlConnection, uuid, (DateTime)conditions.AxmComment.ExamDateTime
            , conditions.AxmComment.CommentType, commenttype_id);
          if (comment_id == -1) {
            comment_id = SelectMaxCommentId(sqlConnection);
          }

          // 更新日、作成日は揃える
          var dateNow = DateTime.Now;

          // DB登録
          result = InsertAxmComment(new AxmCommentRec {
            comment_id = comment_id,
            commenttype_id = commenttype_id,
            pt_uuid = uuid,
            description = conditions.AxmComment.Description ?? string.Empty,
            measured_at = (conditions.AxmComment.CommentType == AxmCommentType.ExamDate) ? conditions.AxmComment.ExamDateTime : null,
            created_at = dateNow,
            updated_at = dateNow
          }, sqlConnection);

        } catch {
        } finally {
          if (!result) {
            // todo: Error通知
          }

          // PostgreSQL Server 通信切断
          dbAccess.CloseSqlConnection();
        }
      } catch {
      }

      return;
    }
    // コメントデータ取得
    [HttpGet("GetDBAxmCommentList/{pt_id}")]
    public List<AxmComment> GetDBAxmCommentList(string pt_id) {
      List<AxmComment> DataSource = new();
      if (pt_id == null || pt_id == string.Empty) return DataSource;

      DBAccess dbAccess = DBAccess.GetInstance();

      try {
        // PostgreSQL Server 通信接続
        NpgsqlConnection sqlConnection = dbAccess.GetSqlConnection();

        // クエリコマンド実行
        // UUIDの有無を確認
        var uuid = Select_PTUUID_by_PTID(sqlConnection, pt_id);
        if (uuid == string.Empty) {
          // 患者データが無ければ、測定データも存在しない
          return DataSource;
        } else {
          // 実行するクエリコマンド定義
          string Query = "SELECT * FROM ";
          Query += _table(DB_TableNames[(int)eDbTable.AXM_COMMENT]);
          Query += " WHERE ";
          Query += _col(COLNAME_AxmCommentList[(int)eAxmTreatment.pt_uuid]);
          Query += " = ";
          Query += _val(uuid);
          Query += " ORDER BY ";
          Query += _col(COLNAME_AxmCommentList[(int)eAxmComment.comment_id]);

          NpgsqlCommand Command = new(Query, sqlConnection);
          NpgsqlDataAdapter DataAdapter = new(Command);
          DataTable DataTable = new();
          DataAdapter.Fill(DataTable);

          DataSource = (from DataRow data in DataTable.Rows
                        select new AxmComment() {
                          ID = _objectToInt(data[COLNAME_AxmCommentList[(int)eAxmComment.comment_id]]),
                          CommentType = (AxmCommentType)Enum.ToObject(typeof(AxmCommentType), data[COLNAME_AxmCommentList[(int)eAxmComment.commenttype_id]]),
                          Description = data[COLNAME_AxmCommentList[(int)eAxmComment.description]].ToString() ?? string.Empty,
                          ExamDateTime = _objectToDateTime(data[COLNAME_AxmCommentList[(int)eAxmComment.measured_at]])
                        }).ToList();
        }
      } catch {
      } finally {
        // PostgreSQL Server 通信切断
        dbAccess.CloseSqlConnection();
      }

      return DataSource;
    }

    // コメント削除
    [HttpPost("DeleteAxmCommentData")]
    public void DeleteAxmCommentData([FromBody] int commentId) {
      try {
        DBAccess dbAccess = DBAccess.GetInstance();

        bool result = false;

        try {
          // PostgreSQL Server 通信接続
          NpgsqlConnection sqlConnection = dbAccess.GetSqlConnection();

          // クエリコマンド実行
          result = (delete_by_commentId(commentId, sqlConnection) != 0);
        } catch {
        } finally {
          if (!result) {
            // todo: Error通知
          }

          // PostgreSQL Server 通信切断
          dbAccess.CloseSqlConnection();
        }
      } catch {
      }

      return;
    }

    // コメント移動
    [HttpPost("MoveAxmCommentData")]
    public void MoveAxmCommentData([FromBody] MoveCommentData conditions) {
      try {
        if (conditions == null) return;
        if (conditions.ChangePatientID == null || conditions.ChangePatientID == string.Empty) return;

        bool result = false;
        DBAccess dbAccess = DBAccess.GetInstance();

        try {
          // PostgreSQL Server 通信接続
          NpgsqlConnection sqlConnection = dbAccess.GetSqlConnection();

          // クエリコマンド実行
          // UUIDの有無を確認(true:update / false:insert)
          var uuid = Select_PTUUID_by_PTID(sqlConnection, conditions.ChangePatientID);
          if (uuid == string.Empty) {
            // uuidが無ければ、被検者を新規登録(DBPatientで実行)
            DBPatientInfoController.InsertPatientId(sqlConnection, conditions.ChangePatientID);
            uuid = Select_PTUUID_by_PTID(sqlConnection, conditions.ChangePatientID);
          }

          if (uuid != string.Empty) {
            // EXAM_LISTの被検者IDを変更
            if (conditions.CommentID != null && conditions.CommentID != string.Empty) {
              result &= MoveCommentData(sqlConnection, uuid, conditions.CommentID);
            }
          }
        } catch {
        } finally {
          if (!result) {
            // todo: Error通知
          }

          // PostgreSQL Server 通信切断
          dbAccess.CloseSqlConnection();
        }
      } catch {
      }

      return;
    }

    public static bool InsertAxmComment(AxmCommentRec rec, NpgsqlConnection sqlConnection) {
      // SQLコマンド
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("insert into ");
      stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.AXM_COMMENT]));
      string text = " (";
      string text2 = " (";
      for (int i = 0; i < COLNAME_AxmCommentList.Count(); i++) {
        if (i != 0) {
          text += ",";
          text2 += ",";
        }

        text += _col(COLNAME_AxmCommentList[i]);
        text2 += _bind(COLNAME_AxmCommentList[i]);
      }

      text += ")";
      text2 += ")";
      stringBuilder.Append(text);
      stringBuilder.Append(" values ");
      stringBuilder.Append(text2);
      stringBuilder.Append(_onconflict("pk_axm_comment"));
      stringBuilder.Append(_doupdateexam(COLNAME_AxmCommentList[(int)eAxmComment.updated_at], DateTime.Now));
      stringBuilder.Append(_doupdatevalue(COLNAME_AxmCommentList[(int)eAxmComment.description], rec.description));
      stringBuilder.Append(";");
      int num = 0;
      // SQLコマンド実行
      using (NpgsqlCommand sqlCommand = new(stringBuilder.ToString(), sqlConnection)) {
        sqlCommand.Parameters.AddWithValue(COLNAME_AxmCommentList[(int)eAxmComment.comment_id], rec.comment_id);
        sqlCommand.Parameters.AddWithValue(COLNAME_AxmCommentList[(int)eAxmComment.commenttype_id], rec.commenttype_id);
        sqlCommand.Parameters.AddWithValue(COLNAME_AxmCommentList[(int)eAxmComment.pt_uuid], Guid.Parse(rec.pt_uuid));
        sqlCommand.Parameters.AddWithValue(COLNAME_AxmCommentList[(int)eAxmComment.description], rec.description);
        sqlCommand.Parameters.AddWithValue(COLNAME_AxmCommentList[(int)eAxmComment.measured_at], _DateTimeToObject(rec.measured_at));
        sqlCommand.Parameters.AddWithValue(COLNAME_AxmCommentList[(int)eAxmComment.created_at], _DateTimeToObject(rec.created_at));
        sqlCommand.Parameters.AddWithValue(COLNAME_AxmCommentList[(int)eAxmComment.updated_at], _DateTimeToObject(rec.updated_at));
        num = sqlCommand.ExecuteNonQuery();
      }

      return num != 0;
    }

    // コメントデータの被検者IDを変更する
    private static bool MoveCommentData(NpgsqlConnection sqlConnection, string pt_uuid, string commentId) {
      int num = 0;
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("update ");
      stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.AXM_COMMENT]));
      stringBuilder.Append("set ");
      stringBuilder.Append(_col(COLNAME_AxmCommentList[(int)eAxmComment.pt_uuid]));
      stringBuilder.Append("= ");
      stringBuilder.Append(_val(pt_uuid));
      stringBuilder.Append("where ");
      stringBuilder.Append(_col(COLNAME_AxmCommentList[(int)eAxmComment.comment_id]));
      stringBuilder.Append("= ");
      stringBuilder.Append(_val(commentId));
      stringBuilder.Append(";");
      using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
      num = npgsqlCommand.ExecuteNonQuery();
      return num != 0;
    }

    public static int Select_AxmCommentID_by_PK(NpgsqlConnection sqlConnection, string pt_uuid, DateTime measured_at, AxmCommentType type, int commenttype_id) {
      int result = -1;
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("select ");
      stringBuilder.Append(_col(COLNAME_AxmCommentList[(int)eAxmComment.comment_id]));
      stringBuilder.Append("from ");
      stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.AXM_COMMENT]));
      stringBuilder.Append("where ");
      stringBuilder.Append(_col(COLNAME_AxmCommentList[(int)eAxmComment.pt_uuid]));
      stringBuilder.Append("= ");
      stringBuilder.Append(_val(pt_uuid));
      stringBuilder.Append(" and ");
      stringBuilder.Append(_col(COLNAME_AxmCommentList[(int)eAxmComment.commenttype_id]));
      stringBuilder.Append("= ");
      stringBuilder.Append(_val(commenttype_id.ToString()));
      if (type == AxmCommentType.ExamDate) {
        stringBuilder.Append(" and ");
        stringBuilder.Append(_col(COLNAME_AxmCommentList[(int)eAxmComment.measured_at]));
        stringBuilder.Append("= ");
        stringBuilder.Append(_val(measured_at.ToString("yyyy-MM-dd HH:mm:ss")));
      }
      stringBuilder.Append(";");
      using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
      using NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();
      while (npgsqlDataReader.Read()) {
        result = _objectToInt(npgsqlDataReader[0]);
      }

      return result;
    }

    // comment_idの最大値取得
    public static int SelectMaxCommentId(NpgsqlConnection sqlConnection) {
      int result = -1;
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("select ");
      stringBuilder.Append(_maxcol(COLNAME_AxmCommentList[(int)eAxmComment.comment_id]));
      stringBuilder.Append("from ");
      stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.AXM_COMMENT]));
      stringBuilder.Append(";");
      using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
      using NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();
      while (npgsqlDataReader.Read()) {
        result = _objectToInt(npgsqlDataReader[0]);
      }

      return result != 0 ? result + 1 : 1;
    }

    public static int Select_AxmCommentTypeId(NpgsqlConnection sqlConnection, string commenttype) {
      int result = -1;
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("select ");
      stringBuilder.Append(_col(COLNAME_MstAxmCommentTypesList[0]));
      stringBuilder.Append("from ");
      stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.MST_AXMCOMMENTTYPES]));
      stringBuilder.Append("where ");
      stringBuilder.Append(_col(COLNAME_MstAxmCommentTypesList[1]));
      stringBuilder.Append("= ");
      stringBuilder.Append(_val(commenttype));
      stringBuilder.Append(";");

      using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
      using NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();
      while (npgsqlDataReader.Read()) {
        result = _objectToInt(npgsqlDataReader[0]);
      }

      return result;
    }

    public int delete_by_commentId(int commentId, NpgsqlConnection sqlConnection) {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("delete ");
      stringBuilder.Append("from ");
      stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.AXM_COMMENT]));
      stringBuilder.Append("where ");
      stringBuilder.Append(_col(COLNAME_AxmCommentList[(int)eAxmComment.comment_id]));
      stringBuilder.Append("= ");
      stringBuilder.Append(_bind(COLNAME_AxmCommentList[(int)eAxmComment.comment_id]));
      stringBuilder.Append(";");
      using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
      npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmCommentList[(int)eAxmComment.comment_id], commentId);
      return npgsqlCommand.ExecuteNonQuery();
    }

    public static string[] COLNAME_AxmCommentList = new string[(int)eAxmComment.MAX] {
            "comment_id", "commenttype_id", "pt_uuid", "description", "measured_at", "updated_at", "created_at"
        };

    public static string[] AXM_COMMENT_TYPE = ["none", "Patient", "ExamDate"];

    public enum eAxmCommentType {
      none = 0,
      Patient,
      ExamDate
    }

    public enum eAxmComment {
      comment_id = 0,
      commenttype_id,
      pt_uuid,
      description,
      measured_at,
      updated_at,
      created_at,
      MAX
    }

  }

  public class AxmCommentRec {
    public int comment_id { get; set; }
    public int commenttype_id { get; set; }
    public string pt_uuid { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public DateTime? measured_at { get; set; }
    public DateTime? updated_at { get; set; }

    public DateTime? created_at { get; set; }
  }
}

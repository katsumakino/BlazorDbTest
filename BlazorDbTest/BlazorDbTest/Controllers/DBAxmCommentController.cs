using BlazorDbTest.Client.Pages;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;
using System.Text;
using System.Text.Json;
using static BlazorDbTest.Controllers.CommonController;
using static BlazorDbTest.Controllers.DBTreatmentController;

namespace BlazorDbTest.Controllers {

    [Route("api/[controller]")]

    public class DBAxmCommentController : ControllerBase {

        // コメント登録
        [HttpGet("SetAxmComment/{pt_id}/{conditions}")]
        public void SetAxmComment(string pt_id, string conditions) {
            try {
                if (pt_id == null || pt_id == string.Empty) return;
                if (conditions == null || conditions == string.Empty) return;

                DBTest.AxmComment comment = JsonSerializer.Deserialize<DBTest.AxmComment>(conditions);

                if(comment == null) return;

                bool result = false;
                DBAccess dbAccess = DBAccess.GetInstance();

                try {
                    // PostgreSQL Server 通信接続
                    NpgsqlConnection sqlConnection = dbAccess.GetSqlConnection();

                    // クエリコマンド実行

                    // UUIDの有無を確認(true:update / false:insert)
                    var uuid = Select_PTUUID_by_PTID(sqlConnection, pt_id);
                    if (uuid == string.Empty) {
                        // コメント登録時は、必ず患者データが存在する
                        return;
                    }

                    // コメントデータID取得
                    var comment_id = SelectMaxCommentId(sqlConnection);

                    // 更新日、作成日は揃える
                    var dateNow = DateTime.Now;

                    // DB登録
                    result = InsertAxmComment(new AxmCommentRec {
                        comment_id = comment_id,
                        commenttype_id = Select_AxmCommentTypeId(sqlConnection, AXM_COMMENT_TYPE[(int)comment.CommentType]),
                        pt_uuid = uuid,
                        description = comment.Description,
                        measured_at = (comment.CommentType == DBTest.AxmCommentType.ExamDate) ? comment.ExamDateTime : null,
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
        public List<DBTest.AxmComment> GetDBAxmCommentList(string pt_id) {
            List<DBTest.AxmComment> DataSource = new();
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
                                  select new DBTest.AxmComment() {
                                      ID = _objectToInt(data[COLNAME_AxmCommentList[(int)eAxmComment.comment_id]]),
                                      CommentType = (DBTest.AxmCommentType)Enum.ToObject(typeof(DBTest.AxmCommentType), data[COLNAME_AxmCommentList[(int)eAxmComment.commenttype_id]]),
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

        public static int Select_AxmCommentID_by_PK(NpgsqlConnection sqlConnection, string pt_uuid, DateTime measured_at) {
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
            stringBuilder.Append(" or ");
            stringBuilder.Append(_col(COLNAME_AxmCommentList[(int)eAxmComment.measured_at]));
            stringBuilder.Append("= ");
            stringBuilder.Append(_val(measured_at.ToString("yyyy-MM-dd HH:mm:ss")));
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

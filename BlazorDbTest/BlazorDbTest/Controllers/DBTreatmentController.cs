using BlazorDbTest.Client.Pages;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;
using System.Text;
using System.Xml.Linq;
using static BlazorDbTest.Controllers.CommonController;
using static BlazorDbTest.Controllers.DBAxialDataController;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BlazorDbTest.Controllers {

    [Route("api/[controller]")]

    public class DBTreatmentController : ControllerBase {

        // 治療方法設定登録
        [HttpGet("SetTreatmentMethod/{id}/{name}/{red}/{green}/{blue}/{alpha}/{rate}")]
        public void SetTreatmentMethod(int id, string name, int red, int green, int blue, int alpha, int rate) {
            if (name == null || name == string.Empty) return;

            bool result = false;
            // todo: 接続処理の共通化
            // appsettings.jsonと接続
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            // appsettings.jsonからConnectionString情報取得
            string? ConnectionString = configuration.GetConnectionString("db");

            // PostgreSQL Server 通信接続
            NpgsqlConnection sqlConnection = new(ConnectionString);

            try {
                // PostgreSQL Server 通信接続
                sqlConnection.Open();

                // クエリコマンド実行

                // IDが登録済みであるか確認
                var type_id = Select_TreatmentTypeId_By_TreatmentInfo(sqlConnection, id);
                if (type_id == -1) {
                    // 新規登録なら、ID割り当て
                    type_id = SelectMaxTreatmentTypeId(sqlConnection);
                }

                // 更新日、作成日は揃える
                var dateNow = DateTime.Now;

                // DB登録
                result = InsertTreatmentInfo(new TreatmentInfoRec {
                    treatmenttype_id = type_id,
                    treatment_name = Encoding.UTF8.GetString(Convert.FromBase64String(name)),
                    color_r = red,
                    color_g = green,
                    color_b = blue,
                    color_a = alpha,
                    suppression_rate = rate,
                    created_at = dateNow,
                    updated_at = dateNow
                }, sqlConnection);

            } catch {
            } finally {
                if (!result) {
                    // todo: Error通知
                }

                // PostgreSQL Server 通信切断
                if (sqlConnection.State != ConnectionState.Closed) {
                    sqlConnection.Close();
                }
            }

            return;
        }

        // 治療方法データ取得
        [HttpGet("GetTreatmentMethodList")]
        public List<DBTest.TreatmentMethodSetting> GetDBTreatmentMethodList() {
            // appsettings.jsonと接続
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            // appsettings.jsonからConnectionString情報取得
            string? ConnectionString = configuration.GetConnectionString("db");

            // 実行するクエリコマンド定義
            string Query = "SELECT * FROM ";
            Query += _table(DB_TableNames[(int)eDbTable.AXM_TREATMENT_INFO]);
            Query += " ORDER BY ";
            Query += _col(COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.treatmenttype_id]);

            NpgsqlConnection sqlConnection = new(ConnectionString);

            List<DBTest.TreatmentMethodSetting> DataSource = new();

            try {
                sqlConnection.Open();
                //Using NpgsqlCommand and Query create connection with database
                NpgsqlCommand Command = new(Query, sqlConnection);
                //Using NpgsqlDataAdapter execute the NpgsqlCommand 
                NpgsqlDataAdapter DataAdapter = new(Command);
                DataTable DataTable = new();
                DataAdapter.Fill(DataTable);

                DataSource = (from DataRow data in DataTable.Rows
                              select new DBTest.TreatmentMethodSetting() {
                                  ID = _objectToInt(data[COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.treatmenttype_id]]),
                                  TreatName = data[COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.treatment_name]].ToString() ?? string.Empty,
                                  RGBAColor = new() {
                                      R = _objectToInt(data[COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.color_r]]),
                                      G = _objectToInt(data[COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.color_g]]),
                                      B = _objectToInt(data[COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.color_b]]),
                                      A = _objectToInt(data[COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.color_a]]),
                                  },
                                  SuppresionRate = _objectToInt(data[COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.suppression_rate]])
                              }).ToList();
            } catch {
            } finally {
                // PostgreSQL Server 通信切断
                if (sqlConnection.State != ConnectionState.Closed) {
                    sqlConnection.Close();
                }
            }

            return DataSource;
        }

        // 治療状況登録
        [HttpGet("SetTreatment/{pt_id}/{treatmenttype_id}/{start}/{end}")]
        public void SetTreatment(string pt_id, int treatmenttype_id, string start, string end) {
            if (start == null || start == string.Empty) return;

            bool result = false;
            // appsettings.jsonと接続
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            // appsettings.jsonからConnectionString情報取得
            string? ConnectionString = configuration.GetConnectionString("db");

            // PostgreSQL Server 通信接続
            NpgsqlConnection sqlConnection = new(ConnectionString);

            try {
                // PostgreSQL Server 通信接続
                sqlConnection.Open();

                // クエリコマンド実行

                // 治療方法IDが登録済みであるか確認
                var type_id = Select_TreatmentTypeId_By_TreatmentInfo(sqlConnection, treatmenttype_id);
                // 治療方法IDが登録されていない場合は、エラーとして処理を終了する
                if (type_id == -1) {
                    return;    
                }

                // 治療状況ID取得
                var traet_id = SelectMaxTreatmentId(sqlConnection);

                // 患者UUID取得
                var uuid = Select_PTUUID_by_PTID(sqlConnection, Encoding.UTF8.GetString(Convert.FromBase64String(pt_id)));
                if (uuid == string.Empty) {
                    // 治療状況登録時は、必ず患者データが存在する
                    return;
                }

                // 更新日、作成日は揃える
                var dateNow = DateTime.Now;

                // DB登録
                result = InsertTreatment(new TreatmentRec {
                    treatment_id = traet_id,
                    treatmenttype_id = type_id,
                    pt_uuid = uuid,
                    start_at = DateTime.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(start))),
                    end_at = DateTime.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(end))),
                    created_at = dateNow,
                    updated_at = dateNow
                }, sqlConnection);

            } catch {
            } finally {

                if (!result) {
                    // todo: Error通知
                }

                // PostgreSQL Server 通信切断
                if (sqlConnection.State != ConnectionState.Closed) {
                    sqlConnection.Close();
                }
            }

            return;
        }

        // 治療方法データ取得
        [HttpGet("GetTreatmentList/{pt_id}")]
        public List<DBTest.TreatmentData> GetDBTreatmentDataList(string pt_id) {
            List<DBTest.TreatmentData> DataSource = new();
            if (pt_id == null || pt_id == string.Empty) return DataSource;

            // appsettings.jsonと接続
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            // appsettings.jsonからConnectionString情報取得
            string? ConnectionString = configuration.GetConnectionString("db");

            // PostgreSQL Server 通信接続
            NpgsqlConnection sqlConnection = new(ConnectionString);

            try {
                // PostgreSQL Server 通信接続
                sqlConnection.Open();

                // クエリコマンド実行
                // UUIDの有無を確認
                var uuid = Select_PTUUID_by_PTID(sqlConnection, Encoding.UTF8.GetString(Convert.FromBase64String(pt_id)));
                if (uuid == string.Empty) {
                    // 患者データが無ければ、測定データも存在しない
                    return DataSource;
                } else {
                    // 実行するクエリコマンド定義
                    string Query = "SELECT * FROM ";
                    Query += _table(DB_TableNames[(int)eDbTable.AXM_TREATMENT]);
                    Query += " WHERE ";
                    Query += _col(COLNAME_AxmTreatmentList[(int)eAxmTreatment.pt_uuid]);
                    Query += " = ";
                    Query += _val(uuid);
                    Query += " ORDER BY ";
                    Query += _col(COLNAME_AxmTreatmentList[(int)eAxmTreatment.treatment_id]);

                    NpgsqlCommand Command = new(Query, sqlConnection);
                    NpgsqlDataAdapter DataAdapter = new(Command);
                    DataTable DataTable = new();
                    DataAdapter.Fill(DataTable);

                    DataSource = (from DataRow data in DataTable.Rows
                                  select new DBTest.TreatmentData() {
                                      ID = _objectToInt(data[COLNAME_AxmTreatmentList[(int)eAxmTreatment.treatment_id]]),
                                      TreatID = _objectToInt(data[COLNAME_AxmTreatmentList[(int)eAxmTreatment.treatmenttype_id]]),
                                      StartDateTime = _objectToDateTime(data[COLNAME_AxmTreatmentList[(int)eAxmTreatment.start_at]]),
                                      EndDateTime = _objectToDateTime(data[COLNAME_AxmTreatmentList[(int)eAxmTreatment.end_at]])
                                  }).ToList();
                }
            } catch {
            } finally {
                // PostgreSQL Server 通信切断
                if (sqlConnection.State != ConnectionState.Closed) {
                    sqlConnection.Close();
                }
            }

            return DataSource;
        }

        public static bool InsertTreatmentInfo(TreatmentInfoRec rec, NpgsqlConnection sqlConnection) {
            // SQLコマンド
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("insert into ");
            stringBuilder.Append(CommonController._table(CommonController.DB_TableNames[(int)CommonController.eDbTable.AXM_TREATMENT_INFO]));
            string text = " (";
            string text2 = " (";
            for (int i = 0; i < COLNAME_AxmTreatmentInfoList.Count(); i++) {
                if (i != 0) {
                    text += ",";
                    text2 += ",";
                }

                text += CommonController._col(COLNAME_AxmTreatmentInfoList[i]);
                text2 += CommonController._bind(COLNAME_AxmTreatmentInfoList[i]);
            }

            text += ")";
            text2 += ")";
            stringBuilder.Append(text);
            stringBuilder.Append(" values ");
            stringBuilder.Append(text2);
            stringBuilder.Append(CommonController._onconflict("pk_axm_treatment_info"));
            stringBuilder.Append(CommonController._doupdateexam(COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.updated_at], DateTime.Now));
            stringBuilder.Append(";");
            int num = 0;
            // SQLコマンド実行
            using (NpgsqlCommand sqlCommand = new(stringBuilder.ToString(), sqlConnection)) {
                sqlCommand.Parameters.Add(new NpgsqlParameter("@treatmenttype_id", rec.treatmenttype_id));
                sqlCommand.Parameters.Add(new NpgsqlParameter("@treatment_name", rec.treatment_name));
                sqlCommand.Parameters.Add(new NpgsqlParameter("@color_r", rec.color_r));
                sqlCommand.Parameters.Add(new NpgsqlParameter("@color_g", rec.color_g));
                sqlCommand.Parameters.Add(new NpgsqlParameter("@color_b", rec.color_b));
                sqlCommand.Parameters.Add(new NpgsqlParameter("@color_a", rec.color_a));
                sqlCommand.Parameters.Add(new NpgsqlParameter("@suppression_rate", rec.suppression_rate));
                sqlCommand.Parameters.Add(new NpgsqlParameter("@created_at", CommonController._DateTimeToObject(rec.created_at)));
                sqlCommand.Parameters.Add(new NpgsqlParameter("@updated_at", CommonController._DateTimeToObject(rec.updated_at)));
                num = sqlCommand.ExecuteNonQuery();
            }

            return num != 0;
        }

        public static bool InsertTreatment(TreatmentRec rec, NpgsqlConnection sqlConnection) {
            // SQLコマンド
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("insert into ");
            stringBuilder.Append(CommonController._table(CommonController.DB_TableNames[(int)CommonController.eDbTable.AXM_TREATMENT]));
            string text = " (";
            string text2 = " (";
            for (int i = 0; i < COLNAME_AxmTreatmentList.Count(); i++) {
                if (i != 0) {
                    text += ",";
                    text2 += ",";
                }

                text += CommonController._col(COLNAME_AxmTreatmentList[i]);
                text2 += CommonController._bind(COLNAME_AxmTreatmentList[i]);
            }

            text += ")";
            text2 += ")";
            stringBuilder.Append(text);
            stringBuilder.Append(" values ");
            stringBuilder.Append(text2);
            stringBuilder.Append(CommonController._onconflict("pk_axm_treatment"));
            stringBuilder.Append(CommonController._doupdateexam(COLNAME_AxmTreatmentList[(int)eAxmTreatment.updated_at], DateTime.Now));
            stringBuilder.Append(";");
            int num = 0;
            // SQLコマンド実行
            using (NpgsqlCommand sqlCommand = new(stringBuilder.ToString(), sqlConnection)) {
                sqlCommand.Parameters.Add(new NpgsqlParameter("@treatment_id", rec.treatment_id));
                sqlCommand.Parameters.Add(new NpgsqlParameter("@treatmenttype_id", rec.treatmenttype_id));
                sqlCommand.Parameters.Add(new NpgsqlParameter("@pt_uuid", Guid.Parse(rec.pt_uuid)));
                sqlCommand.Parameters.Add(new NpgsqlParameter("@start_at", CommonController._DateTimeToObject(rec.start_at)));
                sqlCommand.Parameters.Add(new NpgsqlParameter("@end_at", CommonController._DateTimeToObject(rec.end_at)));
                sqlCommand.Parameters.Add(new NpgsqlParameter("@created_at", CommonController._DateTimeToObject(rec.created_at)));
                sqlCommand.Parameters.Add(new NpgsqlParameter("@updated_at", CommonController._DateTimeToObject(rec.updated_at)));
                num = sqlCommand.ExecuteNonQuery();
            }

            return num != 0;
        }

        // treatmenttype_idの最大値取得
        public static int SelectMaxTreatmentTypeId(NpgsqlConnection sqlConnection) {
            int result = -1;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("select ");
            stringBuilder.Append(_maxcol(COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.treatmenttype_id]));
            stringBuilder.Append("from ");
            stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.AXM_TREATMENT_INFO]));
            stringBuilder.Append(";");
            using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
            using NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();
            while (npgsqlDataReader.Read()) {
                result = _objectToInt(npgsqlDataReader[0]);
            }

            return result != 0 ? result + 1 : 1;
        }

        // treatmenttype_idの有無を取得
        public static int Select_TreatmentTypeId_By_TreatmentInfo(NpgsqlConnection sqlConnection, int treatmentType) {
            int result = -1;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("select ");
            stringBuilder.Append(_col(COLNAME_AxmTreatmentInfoList[0]));
            stringBuilder.Append("from ");
            stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.AXM_TREATMENT_INFO]));
            stringBuilder.Append("where ");
            stringBuilder.Append(_col(COLNAME_AxmTreatmentInfoList[0]));
            stringBuilder.Append(" = ");
            stringBuilder.Append(_bind(COLNAME_AxmTreatmentInfoList[0]));
            stringBuilder.Append(";");
            using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
            npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmTreatmentInfoList[0], treatmentType);
            using NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();
            while (npgsqlDataReader.Read()) {
                result = _objectToInt(npgsqlDataReader[0]);
            }

            return result;
        }

        // todo: １つにまとめられる
        // treatment_idの最大値取得
        public static int SelectMaxTreatmentId(NpgsqlConnection sqlConnection) {
            int result = -1;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("select ");
            stringBuilder.Append(_maxcol(COLNAME_AxmTreatmentList[(int)eAxmTreatment.treatment_id]));
            stringBuilder.Append("from ");
            stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.AXM_TREATMENT]));
            stringBuilder.Append(";");
            using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
            using NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();
            while (npgsqlDataReader.Read()) {
                result = _objectToInt(npgsqlDataReader[0]);
            }

            return result != 0 ? result + 1 : 1;
        }

        public static string[] COLNAME_AxmTreatmentInfoList = new string[(int)eAxmTreatmentInfo.MAX] {
            "treatmenttype_id", "treatment_name", "color_r", "color_g", "color_b", "color_a", "suppression_rate","updated_at", "created_at"
        };

        public enum eAxmTreatmentInfo {
            treatmenttype_id = 0,
            treatment_name,
            color_r,
            color_g,
            color_b,
            color_a,
            suppression_rate,
            updated_at,
            created_at,
            MAX
        }

        public static string[] COLNAME_AxmTreatmentList = new string[(int)eAxmTreatment.MAX] {
            "treatment_id", "treatmenttype_id", "pt_uuid", "start_at", "end_at","updated_at", "created_at"
        };

        public enum eAxmTreatment {
            treatment_id = 0,
            treatmenttype_id,
            pt_uuid,
            start_at,
            end_at,
            updated_at,
            created_at,
            MAX
        }

    }
}

public class TreatmentInfoRec {
    public int treatmenttype_id { get; set; }
    public string treatment_name { get; set; } = string.Empty;
    public int color_r { get; set; }
    public int color_g { get; set; }
    public int color_b { get; set; }
    public int color_a { get; set; }
    public int suppression_rate { get; set; }
    public DateTime? updated_at { get; set; }

    public DateTime? created_at { get; set; }
}

public class TreatmentRec {
    public int treatment_id { get; set; }
    public int treatmenttype_id { get; set; }
    public string pt_uuid { get; set; } = string.Empty;
    public DateTime? start_at { get; set; }
    public DateTime? end_at { get; set; }

    public DateTime? updated_at { get; set; }

    public DateTime? created_at { get; set; }
}

using BlazorDbTest.Common;
using BlazorDbTest.Client.Pages;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;
using System.Text;
using static BlazorDbTest.Controllers.CommonController;

// todo: 関数・定義クラスの分離

namespace BlazorDbTest.Controllers {

    [Route("api/[controller]")]

    public class DBPatientInfoController : ControllerBase {

        // 患者情報書込み
        [HttpGet("SetPatientInfo/{id}/{lastname}/{firstname}/{gender}/{dob}/{mark}/{sameId}")]
        public void SetPatientInfo(string id, string lastname, string firstname, DBTest.Gender gender, string dob, bool mark, string sameId) {
            var id_ = CheckConvertString(id);
            if (id_ == null || id_ == string.Empty) return;

            var lastname_ = CheckConvertString(lastname);
            var firstname_ = CheckConvertString(firstname);
            var dob_ = CheckConvertString(dob);
            var sameId_ = CheckConvertString(sameId);

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

                // UUIDの有無を確認(true:update / false:insert)
                var uuid = Select_PTUUID_by_PTID(sqlConnection, id_);
                if (uuid == string.Empty) {
                    // Insert
                    DateTime dateTime = DateTime.Now;
                    PatientRec patientRec = new() {
                        pt_id = id_,
                        pt_lastname = lastname_,
                        pt_firstname = firstname_,
                        gender_id = Select_GenderId(sqlConnection, GENDER_TYPE[(int)gender]),
                        pt_dob = DateTime.Parse(dob_),
                        pt_description = string.Empty,
                        pt_updated_at = dateTime,
                        pt_created_at = dateTime
                    };

                    result = Insert(sqlConnection, patientRec);

                    // todo: AXM用患者情報テーブルにも登録
                    uuid = Select_PTUUID_by_PTID(sqlConnection, id_);
                    if (uuid != string.Empty) {
                        AxmPatientRec axmPatientRec = new() {
                            pt_uuid = uuid,
                            axm_pt_id = SelectMaxAxmPatientId(sqlConnection),
                            axm_flag = mark,
                            is_axm_same_pt_id = (sameId_ != string.Empty),
                            axm_same_pt_id = sameId_,
                            updated_at = dateTime,
                            created_at = dateTime
                        };

                        var axm_pt_id_ = Select_AxmPatientID_by_PK(sqlConnection, uuid);
                        if (axm_pt_id_ != -1) axmPatientRec.axm_pt_id = axm_pt_id_;

                        result &= InsertAxmPatient(sqlConnection, axmPatientRec);
                    }
                } else {
                    // Update
                    // 装置出力データ取込時は、入力あり→なしにはしない(アプリ上での編集時は可能)
                    DateTime dateTime = DateTime.Now;
                    PatientRec patientRec = new() {
                        pt_uuid = uuid,
                        pt_id = id_,
                        pt_lastname = lastname_,
                        pt_firstname = firstname_,
                        gender_id = Select_GenderId(sqlConnection, GENDER_TYPE[(int)gender]),
                        pt_dob = DateTime.Parse(dob_),
                        pt_updated_at = dateTime
                    };

                    result = Update(sqlConnection, patientRec);

                    // AXM用患者情報テーブルにも更新
                    AxmPatientRec axmPatientRec = new() {
                        pt_uuid = uuid,
                        axm_pt_id = SelectMaxAxmPatientId(sqlConnection),
                        axm_flag = mark,
                        is_axm_same_pt_id = (sameId_ != string.Empty),
                        axm_same_pt_id = sameId_,
                        updated_at = dateTime,
                        created_at = dateTime
                    };

                    var axm_pt_id_ = Select_AxmPatientID_by_PK(sqlConnection, uuid);
                    if (axm_pt_id_ != -1) axmPatientRec.axm_pt_id = axm_pt_id_;

                    result &= InsertAxmPatient(sqlConnection, axmPatientRec);
                }
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

        // 患者情報取得
        [HttpGet("GetPatientInfo/{patientId}")]
        public DBTest.PatientInfo GetDBPatientInfo(string patientId) {
            // appsettings.jsonと接続
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            // appsettings.jsonからConnectionString情報取得
            string? ConnectionString = configuration.GetConnectionString("db");

            // PostgreSQL Server 通信接続
            NpgsqlConnection sqlConnection = new(ConnectionString);

            DBTest.PatientInfo DataSource = new();

            try {
                sqlConnection.Open();

                // 実行するクエリコマンド定義
                string Query = "SELECT * FROM ";
                Query += _table(DB_TableNames[(int)eDbTable.PATIENT_LIST]);
                Query += " WHERE ";
                Query += _col(COLNAME_PatientList[(int)ePatientList.pt_id]);
                Query += " = ";
                Query += _val(Encoding.UTF8.GetString(Convert.FromBase64String(patientId)));

                //Using NpgsqlCommand and Query create connection with database
                NpgsqlCommand Command = new(Query, sqlConnection);
                //Using NpgsqlDataAdapter execute the NpgsqlCommand 
                NpgsqlDataAdapter DataAdapter = new(Command);
                DataTable DataTable = new();
                // Using NpgsqlDataAdapter, process the query string and fill the data into the dataset
                var result = DataAdapter.Fill(DataTable);

                // 患者情報取得結果をreturn
                if (result == 1) {
                    DataRow data = DataTable.Rows[0];
                    DataSource = new DBTest.PatientInfo() {
                        Mark = false,
                        ID = data[COLNAME_PatientList[(int)ePatientList.pt_id]].ToString() ?? string.Empty,
                        FamilyName = data[COLNAME_PatientList[(int)ePatientList.pt_lastname]].ToString() ?? string.Empty,
                        FirstName = data[COLNAME_PatientList[(int)ePatientList.pt_firstname]].ToString() ?? string.Empty,
                        Gender = (DBTest.Gender)Enum.ToObject(typeof(DBTest.Gender), data[COLNAME_PatientList[(int)ePatientList.gender_id]]),
                        Age = GetAge(_objectToDateTime(data[COLNAME_PatientList[(int)ePatientList.pt_dob]]), DateTime.Today),
                        BirthDate = _objectToDateTime(data[COLNAME_PatientList[(int)ePatientList.pt_dob]]),
                        SameID = default!       // todo: 別Tableから取得
                    };
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

        [HttpGet("GetPatientInfoList")]
        public List<DBTest.PatientInfo> GetDBPatientInfoList() {
            // todo: 検索条件の付与

            // appsettings.jsonと接続
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            // appsettings.jsonからConnectionString情報取得
            string? ConnectionString = configuration.GetConnectionString("db");

            // 実行するクエリコマンド定義
            string Query = "SELECT * FROM ";
            Query += _table(DB_TableNames[(int)eDbTable.PATIENT_LIST]);
            Query += " ORDER BY ";
            Query += _col(COLNAME_PatientList[(int)ePatientList.updated_at]);

            NpgsqlConnection sqlConnection = new(ConnectionString);

            List<DBTest.PatientInfo> DataSource = new();

            try {
                sqlConnection.Open();
                //Using NpgsqlCommand and Query create connection with database
                NpgsqlCommand Command = new(Query, sqlConnection);
                //Using NpgsqlDataAdapter execute the NpgsqlCommand 
                NpgsqlDataAdapter DataAdapter = new(Command);
                DataTable DataTable = new();
                DataAdapter.Fill(DataTable);

                // Cast the data fetched from NpgsqlDataAdapter to List<T>
                DataSource = (from DataRow data in DataTable.Rows
                              select new DBTest.PatientInfo() {
                                  ID = data[COLNAME_PatientList[(int)ePatientList.pt_id]].ToString() ?? string.Empty,
                                  FamilyName = data[COLNAME_PatientList[(int)ePatientList.pt_lastname]].ToString() ?? string.Empty,
                                  FirstName = data[COLNAME_PatientList[(int)ePatientList.pt_firstname]].ToString() ?? string.Empty,
                                  Gender = (DBTest.Gender)Enum.ToObject(typeof(DBTest.Gender), data[COLNAME_PatientList[(int)ePatientList.gender_id]]),
                                  Age = GetAge(_objectToDateTime(data[COLNAME_PatientList[(int)ePatientList.pt_dob]]), DateTime.Today),
                                  BirthDate = _objectToDateTime(data[COLNAME_PatientList[(int)ePatientList.pt_dob]]),
                                  SameID = default!       // todo: 別Tableから取得
                              }).ToList();
            } catch {
            } finally {
                sqlConnection.Close();
            }

            return DataSource;
        }

        // 主キー重複時Update
        private bool Insert(NpgsqlConnection sqlConnection, PatientRec aPatientRec) {
            int num = 0;

            StringBuilder stringBuilder = new();
            stringBuilder.Append("insert into ");
            stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.PATIENT_LIST]));
            string text = " (";
            string text2 = " (";
            for (int i = 1; i < COLNAME_PatientList.Count(); i++) {
                if (i != 1) {
                    text += ",";
                    text2 += ",";
                }

                text += _col(COLNAME_PatientList[i]);
                text2 += _bind(COLNAME_PatientList[i]);
            }

            text += ")";
            text2 += ")";
            stringBuilder.Append(text);
            stringBuilder.Append(" values ");
            stringBuilder.Append(text2);
            stringBuilder.Append(";");

            using (NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection)) {
                npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)ePatientList.pt_id], aPatientRec.pt_id);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)ePatientList.pt_lastname], aPatientRec.pt_lastname);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)ePatientList.pt_firstname], aPatientRec.pt_firstname);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)ePatientList.gender_id], aPatientRec.gender_id);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)ePatientList.pt_dob], _DateTimeToObject(aPatientRec.pt_dob));
                npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)ePatientList.pt_description], aPatientRec.pt_description);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)ePatientList.updated_at], _DateTimeToObject(aPatientRec.pt_updated_at));
                npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)ePatientList.created_at], _DateTimeToObject(aPatientRec.pt_created_at));
                num = npgsqlCommand.ExecuteNonQuery();
            }

            return num != 0;
        }

        private bool Update(NpgsqlConnection sqlConnection, PatientRec aPatientRec) {
            int num = 0;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("update ");
            stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.PATIENT_LIST]));
            stringBuilder.Append("set ");
            string text = "";
            for (int i = 1; i < (int)ePatientList.MAX; i++) {
                // コメントおよび作成日時は、アプリ上から更新されない
                if (i != (int)ePatientList.pt_description && i != (int)ePatientList.created_at) {
                    text = text + _col(COLNAME_PatientList[i]) + "= " + _bind(COLNAME_PatientList[i]);
                    if (i != (int)ePatientList.updated_at) {
                        text += ",";
                    }
                }
            }

            stringBuilder.Append(text);
            stringBuilder.Append(" where ");
            stringBuilder.Append(_col(COLNAME_PatientList[(int)ePatientList.pt_uuid]));
            stringBuilder.Append("= ");
            stringBuilder.Append(_val(aPatientRec.pt_uuid));
            stringBuilder.Append(";");
            using (NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection)) {
                npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)ePatientList.pt_id], aPatientRec.pt_id);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)ePatientList.pt_lastname], aPatientRec.pt_lastname);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)ePatientList.pt_firstname], aPatientRec.pt_firstname);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)ePatientList.gender_id], aPatientRec.gender_id);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)ePatientList.pt_dob], _DateTimeToObject(aPatientRec.pt_dob));
                npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)ePatientList.updated_at], _DateTimeToObject(DateTime.Now));
                num = npgsqlCommand.ExecuteNonQuery();
            }

            return num != 0;
        }

        // 主キー重複時Update
        private bool InsertAxmPatient2(NpgsqlConnection sqlConnection, AxmPatientRec aPatientRec) {
            int num = 0;

            StringBuilder stringBuilder = new();
            stringBuilder.Append("insert into ");
            stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.AXM_PATIENT_LIST]));
            string text = " (";
            string text2 = " (";
            for (int i = 1; i < COLNAME_AxmPatientList.Count(); i++) {
                if (i != 1) {
                    text += ",";
                    text2 += ",";
                }

                text += _col(COLNAME_AxmPatientList[i]);
                text2 += _bind(COLNAME_AxmPatientList[i]);
            }

            text += ")";
            text2 += ")";
            stringBuilder.Append(text);
            stringBuilder.Append(" values ");
            stringBuilder.Append(text2);
            stringBuilder.Append(";");

            using (NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection)) {
                npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.pt_uuid], aPatientRec.pt_uuid);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.axm_pt_id], aPatientRec.axm_pt_id);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.axm_flag], aPatientRec.axm_flag);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.is_axm_same_pt_id], aPatientRec.is_axm_same_pt_id);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.axm_same_pt_id], aPatientRec.axm_same_pt_id);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.updated_at], _DateTimeToObject(aPatientRec.updated_at));
                npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.created_at], _DateTimeToObject(aPatientRec.created_at));
                num = npgsqlCommand.ExecuteNonQuery();
            }

            return num != 0;
        }

        private bool UpdateAxmPatient(NpgsqlConnection sqlConnection, AxmPatientRec aPatientRec) {
            int num = 0;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("update ");
            stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.AXM_PATIENT_LIST]));
            stringBuilder.Append("set ");
            string text = "";
            for (int i = 1; i < (int)eAxmPatientList.MAX; i++) {
                // 作成日時は、アプリ上から更新されない
                if (i != (int)eAxmPatientList.created_at) {
                    text = text + _col(COLNAME_AxmPatientList[i]) + "= " + _bind(COLNAME_AxmPatientList[i]);
                    if (i != (int)eAxmPatientList.updated_at) {
                        text += ",";
                    }
                }
            }

            stringBuilder.Append(text);
            stringBuilder.Append(" where ");
            stringBuilder.Append(_col(COLNAME_AxmPatientList[(int)ePatientList.pt_uuid]));
            stringBuilder.Append("= ");
            stringBuilder.Append(_val(aPatientRec.pt_uuid));
            stringBuilder.Append(";");
            using (NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection)) {
                npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.axm_pt_id], aPatientRec.axm_pt_id);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.axm_flag], aPatientRec.axm_flag);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.is_axm_same_pt_id], aPatientRec.is_axm_same_pt_id);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.axm_same_pt_id], aPatientRec.axm_same_pt_id);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)eAxmPatientList.updated_at], _DateTimeToObject(DateTime.Now));
                num = npgsqlCommand.ExecuteNonQuery();
            }

            return num != 0;
        }

        private static bool InsertAxmPatient(NpgsqlConnection sqlConnection, AxmPatientRec aPatientRec) {
            int num = 0;

            StringBuilder stringBuilder = new();
            stringBuilder.Append("insert into ");
            stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.AXM_PATIENT_LIST]));
            string text = " (";
            string text2 = " (";
            for (int i = 0; i < COLNAME_AxmPatientList.Count(); i++) {
                if (i != 0) {
                    text += ",";
                    text2 += ",";
                }

                text += _col(COLNAME_AxmPatientList[i]);
                text2 += _bind(COLNAME_AxmPatientList[i]);
            }

            text += ")";
            text2 += ")";
            stringBuilder.Append(text);
            stringBuilder.Append(" values ");
            stringBuilder.Append(text2);
            stringBuilder.Append(_onconflict("pk_axm_patient_list"));
            stringBuilder.Append(_doupdateexam(COLNAME_AxmPatientList[(int)eAxmPatientList.updated_at], DateTime.Now));
            stringBuilder.Append(";");

            using (NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection)) {
                npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.pt_uuid], Guid.Parse(aPatientRec.pt_uuid));
                npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.axm_pt_id], aPatientRec.axm_pt_id);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.axm_flag], aPatientRec.axm_flag);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.is_axm_same_pt_id], aPatientRec.is_axm_same_pt_id);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.axm_same_pt_id], aPatientRec.axm_same_pt_id);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.updated_at], _DateTimeToObject(aPatientRec.updated_at));
                npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.created_at], _DateTimeToObject(aPatientRec.created_at));
                num = npgsqlCommand.ExecuteNonQuery();
            }

            return num != 0;
        }

        // axm_pt_idの最大値取得
        public static int SelectMaxAxmPatientId(NpgsqlConnection sqlConnection) {
            int result = -1;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("select ");
            stringBuilder.Append(_maxcol(COLNAME_AxmPatientList[(int)eAxmPatientList.axm_pt_id]));
            stringBuilder.Append("from ");
            stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.AXM_PATIENT_LIST]));
            stringBuilder.Append(";");
            using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
            using NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();
            while (npgsqlDataReader.Read()) {
                result = _objectToInt(npgsqlDataReader[0]);
            }

            return result != 0 ? result + 1 : 1;
        }

        public static int Select_AxmPatientID_by_PK(NpgsqlConnection sqlConnection, string pt_uuid) {
            int result = -1;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("select ");
            stringBuilder.Append(_col(COLNAME_AxmPatientList[(int)eAxmPatientList.axm_pt_id]));
            stringBuilder.Append("from ");
            stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.AXM_PATIENT_LIST]));
            stringBuilder.Append("where ");
            stringBuilder.Append(_col(COLNAME_AxmPatientList[(int)eAxmPatientList.pt_uuid]));
            stringBuilder.Append("= ");
            stringBuilder.Append(_val(pt_uuid));
            stringBuilder.Append(";");
            using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
            using NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();
            while (npgsqlDataReader.Read()) {
                result = _objectToInt(npgsqlDataReader[0]);
            }

            return result;
        }

        public static string[] GENDER_TYPE = ["", "male", "female", "other"];
    }
}

public class PatientRec {
    public string pt_uuid { get; set; } = "";
    public string pt_id { get; set; } = "";
    public string pt_lastname { get; set; } = "";
    public string pt_firstname { get; set; } = "";
    public int gender_id { get; set; }
    public DateTime? pt_dob { get; set; }
    public string pt_description { get; set; } = "";
    public DateTime? pt_updated_at { get; set; }
    public DateTime? pt_created_at { get; set; }
}

public class AxmPatientRec {
    public string pt_uuid { get; set; } = "";
    public int axm_pt_id { get; set; }
    public bool axm_flag { get; set; }
    public bool is_axm_same_pt_id { get; set; }
    public string axm_same_pt_id { get; set; } = "";
    public DateTime? updated_at { get; set; }
    public DateTime? created_at { get; set; }
}

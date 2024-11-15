using BlazorDbTest.Client.Pages;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;
using System.Text;

// todo: 関数・定義クラスの分離

namespace BlazorDbTest.Controllers {

    [Route("api/[controller]")]

    public class DBPatientInfoController : ControllerBase {

        // 患者情報書込み
        [HttpGet("SetPatientInfo/{id}/{lastname}/{firstname}/{gender}/{dob}")]
        public void SetPatientInfo(string id, string lastname, string firstname, DBTest.Gender gender, string dob) {
            if (id == null || id == string.Empty) return;

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
                var uuid = CommonController.Select_PTUUID_by_PTID(sqlConnection, Encoding.UTF8.GetString(Convert.FromBase64String(id)));
                if (uuid == string.Empty) {
                    // Insert
                    DateTime dateTime = DateTime.Now;
                    PatientRec patientRec = new PatientRec() {
                        pt_id = Encoding.UTF8.GetString(Convert.FromBase64String(id)),
                        pt_lastname = Encoding.UTF8.GetString(Convert.FromBase64String(lastname)),
                        pt_firstname = Encoding.UTF8.GetString(Convert.FromBase64String(firstname)),
                        gender_id = CommonController.Select_GenderId(sqlConnection, GENDER_TYPE[(int)gender]),
                        pt_dob = DateTime.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(dob))),
                        pt_description = string.Empty,
                        pt_updated_at = dateTime,
                        pt_created_at = dateTime
                    };

                    result = Insert(sqlConnection, patientRec);
                } else {
                    // Update
                    // 装置出力データ取込時は、入力あり→なしにはしない(アプリ上での編集時は可能)
                    DateTime dateTime = DateTime.Now;
                    PatientRec patientRec = new PatientRec() {
                        pt_uuid = uuid,
                        pt_id = Encoding.UTF8.GetString(Convert.FromBase64String(id)),
                        pt_lastname = Encoding.UTF8.GetString(Convert.FromBase64String(lastname)),
                        pt_firstname = Encoding.UTF8.GetString(Convert.FromBase64String(firstname)),
                        gender_id = CommonController.Select_GenderId(sqlConnection, GENDER_TYPE[(int)gender]),
                        pt_dob = DateTime.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(dob))),
                        pt_updated_at = dateTime
                    };

                    result = Update(sqlConnection, patientRec);
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
                Query += CommonController._table(CommonController.DB_TableNames[(int)CommonController.eDbTable.PATIENT_LIST]);
                Query += " WHERE ";
                Query += CommonController._col(CommonController.COLNAME_PatientList[(int)CommonController.ePatientList.pt_id]);
                Query += " = ";
                Query += CommonController._val(Encoding.UTF8.GetString(Convert.FromBase64String(patientId)));

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
                        ID = data[CommonController.COLNAME_PatientList[(int)CommonController.ePatientList.pt_id]].ToString() ?? string.Empty,
                        FamilyName = data[CommonController.COLNAME_PatientList[(int)CommonController.ePatientList.pt_lastname]].ToString() ?? string.Empty,
                        FirstName = data[CommonController.COLNAME_PatientList[(int)CommonController.ePatientList.pt_firstname]].ToString() ?? string.Empty,
                        Gender = (DBTest.Gender)Enum.ToObject(typeof(DBTest.Gender), data[CommonController.COLNAME_PatientList[(int)CommonController.ePatientList.gender_id]]),
                        Age = CommonController.GetAge(CommonController._objectToDateTime(data[CommonController.COLNAME_PatientList[(int)CommonController.ePatientList.pt_dob]]), DateTime.Today),
                        BirthDate = CommonController._objectToDateTime(data[CommonController.COLNAME_PatientList[(int)CommonController.ePatientList.pt_dob]]),
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
            Query += CommonController._table(CommonController.DB_TableNames[(int)CommonController.eDbTable.PATIENT_LIST]);
            Query += " ORDER BY ";
            Query += CommonController._col(CommonController.COLNAME_PatientList[(int)CommonController.ePatientList.updated_at]);

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
                                  ID = data[CommonController.COLNAME_PatientList[(int)CommonController.ePatientList.pt_id]].ToString() ?? string.Empty,
                                  FamilyName = data[CommonController.COLNAME_PatientList[(int)CommonController.ePatientList.pt_lastname]].ToString() ?? string.Empty,
                                  FirstName = data[CommonController.COLNAME_PatientList[(int)CommonController.ePatientList.pt_firstname]].ToString() ?? string.Empty,
                                  Gender = (DBTest.Gender)Enum.ToObject(typeof(DBTest.Gender), data[CommonController.COLNAME_PatientList[(int)CommonController.ePatientList.gender_id]]),
                                  Age = CommonController.GetAge(CommonController._objectToDateTime(data[CommonController.COLNAME_PatientList[(int)CommonController.ePatientList.pt_dob]]), DateTime.Today),
                                  BirthDate = CommonController._objectToDateTime(data[CommonController.COLNAME_PatientList[(int)CommonController.ePatientList.pt_dob]]),
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
            stringBuilder.Append(CommonController._table(CommonController.DB_TableNames[(int)CommonController.eDbTable.PATIENT_LIST]));
            string text = " (";
            string text2 = " (";
            for (int i = 1; i < CommonController.COLNAME_PatientList.Count(); i++) {
                if (i != 1) {
                    text += ",";
                    text2 += ",";
                }

                text += CommonController._col(CommonController.COLNAME_PatientList[i]);
                text2 += CommonController._bind(CommonController.COLNAME_PatientList[i]);
            }

            text += ")";
            text2 += ")";
            stringBuilder.Append(text);
            stringBuilder.Append(" values ");
            stringBuilder.Append(text2);
            stringBuilder.Append(";");

            using (NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection)) {
                npgsqlCommand.Parameters.AddWithValue(CommonController.COLNAME_PatientList[(int)CommonController.ePatientList.pt_id], aPatientRec.pt_id);
                npgsqlCommand.Parameters.AddWithValue(CommonController.COLNAME_PatientList[(int)CommonController.ePatientList.pt_lastname], aPatientRec.pt_lastname);
                npgsqlCommand.Parameters.AddWithValue(CommonController.COLNAME_PatientList[(int)CommonController.ePatientList.pt_firstname], aPatientRec.pt_firstname);
                npgsqlCommand.Parameters.AddWithValue(CommonController.COLNAME_PatientList[(int)CommonController.ePatientList.gender_id], aPatientRec.gender_id);
                npgsqlCommand.Parameters.AddWithValue(CommonController.COLNAME_PatientList[(int)CommonController.ePatientList.pt_dob], CommonController._DateTimeToObject(aPatientRec.pt_dob));
                npgsqlCommand.Parameters.AddWithValue(CommonController.COLNAME_PatientList[(int)CommonController.ePatientList.pt_description], aPatientRec.pt_description);
                npgsqlCommand.Parameters.AddWithValue(CommonController.COLNAME_PatientList[(int)CommonController.ePatientList.updated_at], CommonController._DateTimeToObject(aPatientRec.pt_updated_at));
                npgsqlCommand.Parameters.AddWithValue(CommonController.COLNAME_PatientList[(int)CommonController.ePatientList.created_at], CommonController._DateTimeToObject(aPatientRec.pt_created_at));
                num = npgsqlCommand.ExecuteNonQuery();
            }

            return num != 0;
        }

        private bool Update(NpgsqlConnection sqlConnection, PatientRec aPatientRec) {
            int num = 0;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("update ");
            stringBuilder.Append(CommonController._table(CommonController.DB_TableNames[(int)CommonController.eDbTable.PATIENT_LIST]));
            stringBuilder.Append("set ");
            string text = "";
            for (int i = 1; i < 9; i++) {
                // コメントおよび作成日時は、アプリ上から更新されない
                if (i != 6 && i != 8) {
                    text = text + CommonController._col(CommonController.COLNAME_PatientList[i]) + "= " + CommonController._bind(CommonController.COLNAME_PatientList[i]);
                    if (i != 7) {
                        text += ",";
                    }
                }
            }

            stringBuilder.Append(text);
            stringBuilder.Append(" where ");
            stringBuilder.Append(CommonController._col(CommonController.COLNAME_PatientList[(int)CommonController.ePatientList.pt_uuid]));
            stringBuilder.Append("= ");
            stringBuilder.Append(CommonController._val(aPatientRec.pt_uuid));
            stringBuilder.Append(";");
            using (NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection)) {
                npgsqlCommand.Parameters.AddWithValue(CommonController.COLNAME_PatientList[(int)CommonController.ePatientList.pt_id], aPatientRec.pt_id);
                npgsqlCommand.Parameters.AddWithValue(CommonController.COLNAME_PatientList[(int)CommonController.ePatientList.pt_lastname], aPatientRec.pt_lastname);
                npgsqlCommand.Parameters.AddWithValue(CommonController.COLNAME_PatientList[(int)CommonController.ePatientList.pt_firstname], aPatientRec.pt_firstname);
                npgsqlCommand.Parameters.AddWithValue(CommonController.COLNAME_PatientList[(int)CommonController.ePatientList.gender_id], aPatientRec.gender_id);
                npgsqlCommand.Parameters.AddWithValue(CommonController.COLNAME_PatientList[(int)CommonController.ePatientList.pt_dob], CommonController._DateTimeToObject(aPatientRec.pt_dob));
                //npgsqlCommand.Parameters.AddWithValue(CommonContorller.COLNAME_PatientList[(int)CommonController.ePatientList.pt_description], aPatientRec.pt_description);
                npgsqlCommand.Parameters.AddWithValue(CommonController.COLNAME_PatientList[(int)CommonController.ePatientList.updated_at], CommonController._DateTimeToObject(DateTime.Now));
                //npgsqlCommand.Parameters.AddWithValue(CommonContorller.COLNAME_PatientList[(int)CommonController.ePatientList.created_at], CommonContorller._DateTimeToObject(aPatientRec.pt_created_at));
                num = npgsqlCommand.ExecuteNonQuery();
            }

            return num != 0;
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

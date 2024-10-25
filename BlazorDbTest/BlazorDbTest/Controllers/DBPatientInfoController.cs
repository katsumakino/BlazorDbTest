using BlazorDbTest.Client.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
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

                // クエリコマンド実行
                // UUIDの有無を確認(true:update / false:insert)
                var uuid = Select_PTUUID_by_PTID(sqlConnection, Encoding.UTF8.GetString(Convert.FromBase64String(id)));
                if (uuid == string.Empty) {
                    // Insert
                    DateTime dateTime = DateTime.Now;
                    PatientRec patientRec = new PatientRec() {
                        pt_id = Encoding.UTF8.GetString(Convert.FromBase64String(id)),
                        pt_lastname = Encoding.UTF8.GetString(Convert.FromBase64String(lastname)),
                        pt_firstname = Encoding.UTF8.GetString(Convert.FromBase64String(firstname)),
                        gender_id = (int)gender,
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
                        gender_id = (int)gender,
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
                if(sqlConnection.State != ConnectionState.Closed) {
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
                Query += "\"";
                Query += DB_TableNames[1];
                Query += "\"";
                Query += " WHERE ";
                Query += "\"";
                Query += COLNAME_PatientList[1];
                Query += "\"";
                Query += " = ";
                Query += "\'";
                Query += Encoding.UTF8.GetString(Convert.FromBase64String(patientId));
                Query += "\'";

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
                        ID = data[COLNAME_PatientList[1]].ToString() ?? string.Empty,
                        FamilyName = data[COLNAME_PatientList[2]].ToString() ?? string.Empty,
                        FirstName = data[COLNAME_PatientList[3]].ToString() ?? string.Empty,
                        Gender = (DBTest.Gender)Enum.ToObject(typeof(DBTest.Gender), data[COLNAME_PatientList[4]]),
                        Age = GetAge(_objectToDateTime(data[COLNAME_PatientList[5]]), DateTime.Today),
                        BirthDate = _objectToDateTime(data[COLNAME_PatientList[5]]),
                        SameID = default!       // todo: 別Tableから取得
                    };
                }
            } catch {
            } finally {
                // PostgreSQL Server 通信切断
                if(sqlConnection.State != ConnectionState.Closed) {
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
            string Query = "SELECT * FROM \"patient_list\" ORDER BY \"" + COLNAME_PatientList[7] + "\"";
            NpgsqlConnection sqlConnection = new(ConnectionString);

            List<DBTest.PatientInfo> DataSource = new();

            try {
                sqlConnection.Open();
                //Using NpgsqlCommand and Query create connection with database
                NpgsqlCommand Command = new(Query, sqlConnection);
                //Using NpgsqlDataAdapter execute the NpgsqlCommand 
                NpgsqlDataAdapter DataAdapter = new(Command);
                DataTable DataTable = new();
                // Using NpgsqlDataAdapter, process the query string and fill the data into the dataset
                // Fillの戻り値は、正常に追加・更新された行数
                DataAdapter.Fill(DataTable);
                
                // Cast the data fetched from NpgsqlDataAdapter to List<T>
                DataSource = (from DataRow data in DataTable.Rows
                                  select new DBTest.PatientInfo() {
                                      ID = data[COLNAME_PatientList[1]].ToString() ?? string.Empty,
                                      FamilyName = data[COLNAME_PatientList[2]].ToString() ?? string.Empty,
                                      FirstName = data[COLNAME_PatientList[3]].ToString() ?? string.Empty,
                                      Gender = (DBTest.Gender)Enum.ToObject(typeof(DBTest.Gender), data[COLNAME_PatientList[4]]),
                                      Age = GetAge(_objectToDateTime(data[COLNAME_PatientList[5]]), DateTime.Today),
                                      BirthDate = _objectToDateTime(data[COLNAME_PatientList[5]]),
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
            stringBuilder.Append("\"" + DB_TableNames[1] + "\"");
            string text = " (";
            string text2 = " (";
            for (int i = 1; i < COLNAME_PatientList.Count(); i++) {
                if (i != 1) {
                    text += ",";
                    text2 += ",";
                }

                text += "\"" + COLNAME_PatientList[i] + "\"";
                text2 += "@" + COLNAME_PatientList[i];
            }

            text += ")";
            text2 += ")";
            stringBuilder.Append(text);
            stringBuilder.Append(" values ");
            stringBuilder.Append(text2);
            stringBuilder.Append(";");

            using (NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection)) {
                npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[1], aPatientRec.pt_id);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[2], aPatientRec.pt_lastname);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[3], aPatientRec.pt_firstname);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[4], aPatientRec.gender_id);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[5], _DateTimeToObject(aPatientRec.pt_dob));
                npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[6], aPatientRec.pt_description);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[7], _DateTimeToObject(aPatientRec.pt_updated_at));
                npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[8], _DateTimeToObject(aPatientRec.pt_created_at));
                num = npgsqlCommand.ExecuteNonQuery();
            }

            return num != 0;
        }

        private bool Update(NpgsqlConnection sqlConnection, PatientRec aPatientRec) {
            int num = 0;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("update ");
            stringBuilder.Append("\"" + DB_TableNames[1] + "\"");
            stringBuilder.Append("set ");
            string text = "";
            for (int i = 1; i < 9; i++) {
                // コメントおよび作成日時は、アプリ上から更新されない
                if(i != 6 && i != 8) {
                    text = text + "\"" + COLNAME_PatientList[i] + "\"" + "= " + "@" + COLNAME_PatientList[i];
                    if (i != 7) {
                        text += ",";
                    }
                }
            }

            stringBuilder.Append(text);
            stringBuilder.Append(" where ");
            stringBuilder.Append("\"" + COLNAME_PatientList[0] + "\"");
            stringBuilder.Append("= ");
            stringBuilder.Append("\'" + aPatientRec.pt_uuid + "\'");
            stringBuilder.Append(";");
            using (NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection)) {
                npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[1], aPatientRec.pt_id);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[2], aPatientRec.pt_lastname);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[3], aPatientRec.pt_firstname);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[4], aPatientRec.gender_id);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[5], _DateTimeToObject(aPatientRec.pt_dob));
                //npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[6], aPatientRec.pt_description);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[7], _DateTimeToObject(DateTime.Now));
                //npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[8], _DateTimeToObject(aPatientRec.pt_created_at));
                num = npgsqlCommand.ExecuteNonQuery();
            }

            return num != 0;
        }

        public string Select_PTUUID_by_PTID(NpgsqlConnection sqlConnection, string sPtid) {
            string result = string.Empty;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("select ");
            stringBuilder.Append("\"" + COLNAME_PatientList[0] + "\"");
            stringBuilder.Append("from ");
            stringBuilder.Append("\"" + DB_TableNames[1] + "\"");
            stringBuilder.Append("where ");
            stringBuilder.Append("\"" + COLNAME_PatientList[1] + "\"");
            stringBuilder.Append("= ");
            stringBuilder.Append("'" + sPtid + "'");
            stringBuilder.Append(";");
            using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
            using NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();
            while (npgsqlDataReader.Read()) {
                result = npgsqlDataReader[COLNAME_PatientList[0]].ToString() ?? string.Empty;
            }

            return result;
        }

        protected object _DateTimeToObject(DateTime? input) {
            object obj = input;
            obj ??= DBNull.Value;
            return obj;
        }

        private static int GetAge(DateTime? birthDate, DateTime today) {
            int age = -1;

            if (birthDate.HasValue) {
                age = (int.Parse(today.ToString("yyyyMMdd")) - int.Parse(birthDate?.ToString("yyyyMMdd"))) / 10000;
            }

            return age;
        }

        protected DateTime? _objectToDateTime(object oColumnRes, bool bisUTC = true) {
            if (!DateTime.TryParse(oColumnRes.ToString(), out var result)) {
                return null;
            }

            return bisUTC ? result.ToUniversalTime() : result;
        }

        // todo:
        public static string[] DB_TableNames = new string[2]
    {
        "", "patient_list" 
    };

        public static string[] COLNAME_PatientList = new string[9] { "pt_uuid", "pt_id", "pt_lastname", "pt_firstname", "gender_id", "pt_dob", "pt_description", "pt_updated_at", "pt_created_at" };

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

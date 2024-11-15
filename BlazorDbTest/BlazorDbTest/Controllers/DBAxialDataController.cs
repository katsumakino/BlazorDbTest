using BlazorDbTest.Client.Pages;
using BlazorDbTest.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Npgsql;
using System.Data;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using static BlazorDbTest.Client.Pages.DBTest;

namespace BlazorDbTest.Controllers {

    [Route("api/[controller]")]

    public class DBAxialDataController : ControllerBase {

        // 眼軸長測定値書込み
        [HttpGet("SetOptAxial/{id}/{axial_r}/{axial_l}/{exam_datetime}")]
        public void SetOptAxial(string id, double axial_r, double axial_l, string exam_datetime) {
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
                var uuid = CommonController.Select_PTUUID_by_PTID(sqlConnection, Encoding.UTF8.GetString(Convert.FromBase64String(id)));
                if (uuid == string.Empty) {
                    // AxMからの測定データ登録時は、必ず患者データが存在する
                    return;
                } else {
                    // EXAM_LISTに保存(右眼測定値)
                    var exam_id_r = CommonController.RegisterExamList(uuid, 
                        Common.Const.strMstDataType[Common.Const.eMSTDATATYPE.OPTAXIAL], 
                        Common.Const.eEyeType.RIGHT, 
                        DateTime.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(exam_datetime))), 
                        sqlConnection);
                    // EXAM_OPTAXIALに保存(右眼測定値)
                    var rec_optaxial_r = MakeOptaxialRec(exam_id_r, 
                        Common.Const.strEyeType[Common.Const.eEyeType.RIGHT], 
                        sqlConnection);
                    rec_optaxial_r.axial_mm = axial_r;
                    // todo: 表示設定の計算方法をセットする
                    rec_optaxial_r.target_eye_id = CommonController.Select_TargetEyeId_By_TargetEyeType(sqlConnection, "immersion");
                    rec_optaxial_r.measured_at = DateTime.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(exam_datetime)));

                    // DB登録
                    result = Insert(rec_optaxial_r, sqlConnection);

                    // EXAM_LISTに保存(左眼測定値)
                    var exam_id_l = CommonController.RegisterExamList(uuid,
                        Common.Const.strMstDataType[Common.Const.eMSTDATATYPE.OPTAXIAL],
                        Common.Const.eEyeType.LEFT,
                        DateTime.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(exam_datetime))),
                        sqlConnection);
                    // EXAM_OPTAXIALに保存(左眼測定値)
                    var rec_optaxial_l = MakeOptaxialRec(exam_id_l,
                        Common.Const.strEyeType[Common.Const.eEyeType.LEFT],
                        sqlConnection);
                    rec_optaxial_l.axial_mm = axial_l;
                    // todo: 表示設定の計算方法をセットする
                    rec_optaxial_l.target_eye_id = CommonController.Select_TargetEyeId_By_TargetEyeType(sqlConnection, "immersion");
                    rec_optaxial_l.measured_at = DateTime.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(exam_datetime)));

                    // DB登録
                    result &= Insert(rec_optaxial_l, sqlConnection);
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

        // 眼軸長測定値書込み
        [HttpGet("GetOptAxialList/{patientId}")]
        public List<DBTest.Axial> GetOptAxialList(string patientId) {
            List<DBTest.Axial> DataSource = new();
            if (patientId == null || patientId == string.Empty) return DataSource;

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
                var uuid = CommonController.Select_PTUUID_by_PTID(sqlConnection, Encoding.UTF8.GetString(Convert.FromBase64String(patientId)));
                if (uuid == string.Empty) {
                    // 患者データが無ければ、測定データも存在しない
                    return DataSource;
                } else {
                    // 実行するクエリコマンド定義
                    // ExamListの取得
                    string Query = "SELECT * FROM ";
                    Query += CommonController._table(CommonController.DB_TableNames[(int)CommonController.eDbTable.EXAM_LIST]);
                    Query += " WHERE ";
                    Query += CommonController._col(CommonController.COLNAME_ExamList[(int)CommonController.eExamList.pt_uuid]);
                    Query += " = '";
                    Query += CommonController._val(uuid);

                    NpgsqlCommand Command = new(Query, sqlConnection);
                    NpgsqlDataAdapter DataAdapter = new(Command);
                    DataTable DataTable = new();
                    DataAdapter.Fill(DataTable);

                    for(int i=0; i<DataTable.Rows.Count; i++) {
                        string? exam_id = DataTable.Rows[i][CommonController.COLNAME_ExamList[(int)CommonController.eExamList.exam_id]].ToString();

                        if(exam_id != null) {
                            Query = "SELECT * FROM ";
                            Query += CommonController._table(CommonController.DB_TableNames[(int)CommonController.eDbTable.EXAM_OPTAXIAL]);
                            Query += " WHERE ";
                            Query += CommonController._col(COLNAME_ExamOptaxialList[(int)eExamOptAxial.exam_id]);
                            Query += " = ";
                            Query += CommonController._val(exam_id);

                            // todo: 右左を考慮
                            // todo: 複数Tableの検索を同時に実行するやつ使用

                            // Axial測定値取得コマンド実行
                            Command = new(Query, sqlConnection);
                            DataAdapter = new(Command);
                            DataTable DataTable2 = new();
                            DataAdapter.Fill(DataTable2);

                            DataSource.Add(new DBTest.Axial {
                                ID = patientId,
                                RAxial = DataTable2.Rows[0][COLNAME_ExamOptaxialList[(int)eExamOptAxial.axial_mm]].ToString() != null ?
                                         double.Parse(DataTable2.Rows[0][COLNAME_ExamOptaxialList[(int)eExamOptAxial.axial_mm]].ToString()) : 0.0,
                                LAxial = double.Parse(DataTable2.Rows[1][COLNAME_ExamOptaxialList[(int)eExamOptAxial.axial_mm]].ToString()),
                                ExamDateTime = CommonController._objectToDateTime(DataTable2.Rows[0][COLNAME_ExamOptaxialList[(int)eExamOptAxial.measured_at]])
                            }) ;

                        }
                        
                    }
                                                      
                }
            } catch {
            } finally {
                if (!result) { }
            }

            return DataSource;
        }

        public static ExamOptaxialRec MakeOptaxialRec(int examId, string posEye, NpgsqlConnection sqlConnection) {

            var recOpax = new ExamOptaxialRec();
            try {
                recOpax.exam_id = examId;
                recOpax.examtype_id = CommonController.Select_Examtype_ID(sqlConnection, Const.strMstDataType[Const.eMSTDATATYPE.OPTAXIAL]);
                recOpax.eye_id = CommonController.Select_Eye_ID(sqlConnection, posEye);
                recOpax.is_exam_data = true;
                recOpax.comment = string.Empty;
                recOpax.select_id = CommonController.Select_SelectTypeID(sqlConnection, "none");
                recOpax.target_eye_id = 0;
                recOpax.fitting_id = CommonController.Select_FittingId_By_FittingType(sqlConnection, "none");
                recOpax.iol_eye_id = CommonController.Select_IolEyeId_By_IolEyeType(sqlConnection, "none");

                recOpax.is_meas_auto = false;
                recOpax.axial_mm = 0;
                recOpax.sd = 0;
                recOpax.snr = 0;
                recOpax.is_average_ref_ind = false;
                recOpax.axial_ref_ind = 0;
                recOpax.pachy_ref_ind = 0;
                recOpax.acd_ref_ind = 0;
                recOpax.lens_ref_ind = 0;
                recOpax.iol_ref_ind = 0;
                recOpax.vitreous_ref_ind = 0;

                recOpax.is_caliper = false;
                recOpax.is_reliabillty = false;
                recOpax.reliabillty = "";
                recOpax.data_path = string.Empty;
                recOpax.measured_at = null;

                // 更新日、作成日は揃える
                var dateNow = DateTime.Now;
                recOpax.updated_at = dateNow;
                recOpax.created_at = dateNow;
            } catch {
            } finally {
            }
            return recOpax;
        }

        public static bool Insert(ExamOptaxialRec aExamOptaxialRec, NpgsqlConnection sqlConnection) {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("insert into ");
            stringBuilder.Append(CommonController._table(CommonController.DB_TableNames[(int)CommonController.eDbTable.EXAM_OPTAXIAL]));
            string text = " (";
            string text2 = " (";
            for (int i = 0; i < COLNAME_ExamOptaxialList.Count(); i++) {
                if (i != 0) {
                    text += ",";
                    text2 += ",";
                }

                text += CommonController._col(COLNAME_ExamOptaxialList[i]);
                text2 += CommonController._bind(COLNAME_ExamOptaxialList[i]);
            }

            text += ")";
            text2 += ")";
            stringBuilder.Append(text);
            stringBuilder.Append(" values ");
            stringBuilder.Append(text2);
            stringBuilder.Append(CommonController._onconflict("pk_exam_optaxial"));
            stringBuilder.Append(CommonController._doupdateexam(COLNAME_ExamOptaxialList[(int)eExamOptAxial.updated_at], DateTime.Now));
            stringBuilder.Append(";");
            int num = 0;
            using (NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection)) {
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamOptaxialList[(int)eExamOptAxial.exam_id], aExamOptaxialRec.exam_id);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamOptaxialList[(int)eExamOptAxial.examtype_id], aExamOptaxialRec.examtype_id);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamOptaxialList[(int)eExamOptAxial.eye_id], aExamOptaxialRec.eye_id);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamOptaxialList[(int)eExamOptAxial.is_exam_data], aExamOptaxialRec.is_exam_data);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamOptaxialList[(int)eExamOptAxial.comment], aExamOptaxialRec.comment);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamOptaxialList[(int)eExamOptAxial.select_id], aExamOptaxialRec.select_id);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamOptaxialList[(int)eExamOptAxial.target_eye_id], aExamOptaxialRec.target_eye_id);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamOptaxialList[(int)eExamOptAxial.fitting_id], aExamOptaxialRec.fitting_id);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamOptaxialList[(int)eExamOptAxial.iol_eye_id], aExamOptaxialRec.iol_eye_id);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamOptaxialList[(int)eExamOptAxial.is_meas_auto], aExamOptaxialRec.is_meas_auto);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamOptaxialList[(int)eExamOptAxial.axial_mm], aExamOptaxialRec.axial_mm);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamOptaxialList[(int)eExamOptAxial.sd], aExamOptaxialRec.sd);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamOptaxialList[(int)eExamOptAxial.snr], aExamOptaxialRec.snr);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamOptaxialList[(int)eExamOptAxial.is_average_ref_ind], aExamOptaxialRec.is_average_ref_ind);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamOptaxialList[(int)eExamOptAxial.axial_ref_ind], aExamOptaxialRec.axial_ref_ind);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamOptaxialList[(int)eExamOptAxial.pachy_ref_ind], aExamOptaxialRec.pachy_ref_ind);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamOptaxialList[(int)eExamOptAxial.acd_ref_ind], aExamOptaxialRec.acd_ref_ind);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamOptaxialList[(int)eExamOptAxial.lens_ref_ind], aExamOptaxialRec.lens_ref_ind);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamOptaxialList[(int)eExamOptAxial.iol_ref_ind], aExamOptaxialRec.iol_ref_ind);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamOptaxialList[(int)eExamOptAxial.vitreous_ref_ind], aExamOptaxialRec.vitreous_ref_ind);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamOptaxialList[(int)eExamOptAxial.is_caliper], aExamOptaxialRec.is_caliper);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamOptaxialList[(int)eExamOptAxial.is_reliabillty], aExamOptaxialRec.is_reliabillty);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamOptaxialList[(int)eExamOptAxial.reliabillty], aExamOptaxialRec.reliabillty);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamOptaxialList[(int)eExamOptAxial.data_path], aExamOptaxialRec.data_path);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamOptaxialList[(int)eExamOptAxial.measured_at], CommonController._DateTimeToObject(aExamOptaxialRec.measured_at));
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamOptaxialList[(int)eExamOptAxial.updated_at], CommonController._DateTimeToObject(aExamOptaxialRec.updated_at));
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamOptaxialList[(int)eExamOptAxial.created_at], CommonController._DateTimeToObject(aExamOptaxialRec.created_at));
                num = npgsqlCommand.ExecuteNonQuery();
            }

            return num != 0;
        }

        public static string[] COLNAME_ExamOptaxialList = new string[27]
        {
            "exam_id", "examtype_id", "eye_id", "is_exam_data", "comment", "select_id", "target_eye_id", "fitting_id", "iol_eye_id", "is_meas_auto",
            "axial_mm", "sd", "snr", "is_average_ref_ind", "axial_ref_ind", "pachy_ref_ind", "acd_ref_ind", "lens_ref_ind", "iol_ref_ind", "vitreous_ref_ind",
            "is_caliper", "is_reliabillty", "reliabillty", "data_path", "measured_at", "updated_at", "created_at"
        };

        public enum eExamOptAxial {
            exam_id = 0,
            examtype_id,
            eye_id,
            is_exam_data,
            comment,
            select_id,
            target_eye_id,
            fitting_id,
            iol_eye_id,
            is_meas_auto,
            axial_mm,
            sd,
            snr,
            is_average_ref_ind,
            axial_ref_ind,
            pachy_ref_ind,
            acd_ref_ind,
            lens_ref_ind,
            iol_ref_ind,
            vitreous_ref_ind,
            is_caliper,
            is_reliabillty,
            reliabillty,
            data_path,
            measured_at,
            updated_at,
            created_at,
            MAX
        }
    }
}

public class ExamOptaxialRec {
    public int exam_id { get; set; }

    public int examtype_id { get; set; }

    public int eye_id { get; set; }

    public bool is_exam_data { get; set; }

    public string comment { get; set; } = string.Empty;


    public int select_id { get; set; }

    public int target_eye_id { get; set; }

    public int fitting_id { get; set; }

    public int iol_eye_id { get; set; }

    public bool is_meas_auto { get; set; }

    public double axial_mm { get; set; }

    public double sd { get; set; }

    public int snr { get; set; }

    public bool is_average_ref_ind { get; set; }

    public double axial_ref_ind { get; set; }

    public double pachy_ref_ind { get; set; }

    public double acd_ref_ind { get; set; }

    public double lens_ref_ind { get; set; }

    public double iol_ref_ind { get; set; }

    public double vitreous_ref_ind { get; set; }

    public bool is_caliper { get; set; }

    public bool is_reliabillty { get; set; }

    public string reliabillty { get; set; } = string.Empty;


    public string data_path { get; set; } = string.Empty;


    public DateTime? measured_at { get; set; }

    public DateTime? updated_at { get; set; }

    public DateTime? created_at { get; set; }
}

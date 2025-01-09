using BlazorDbTest.Client.Pages;
using BlazorDbTest.Common;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;
using System.Text;
using System.Text.Json;
using static BlazorDbTest.Client.Pages.DBTest;

namespace BlazorDbTest.Controllers {

    [Route("api/[controller]")]

    public class DBAxialDataController : ControllerBase {

        // 眼軸長測定値書込み
        [HttpGet("SetOptAxial/{conditions}/")]
        public void SetOptAxial(string conditions) {
            try {
                if (conditions == null || conditions == string.Empty) return;

                DBTest.AxialList axialList = JsonSerializer.Deserialize<DBTest.AxialList>(conditions);

                if(axialList == null) return;
                if(axialList.PatientID == null || axialList.PatientID == string.Empty) return;

                bool result = false;
                DBAccess dbAccess = DBAccess.GetInstance();

                try {
                    // PostgreSQL Server 通信接続
                    NpgsqlConnection sqlConnection = dbAccess.GetSqlConnection();

                    // クエリコマンド実行
                    // UUIDの有無を確認(true:update / false:insert)
                    var uuid = CommonController.Select_PTUUID_by_PTID(sqlConnection, axialList.PatientID);
                    if (uuid == string.Empty) {
                        // AxMからの測定データ登録時は、必ず患者データが存在する
                        return;
                    } else {
                        // EXAM_LISTに保存(右眼測定値)
                        var exam_id_r = CommonController.RegisterExamList(uuid,
                            Common.Const.strMstDataType[Common.Const.eMSTDATATYPE.OPTAXIAL],
                            Common.Const.eEyeType.RIGHT,
                            axialList.ExamDateTime,
                            sqlConnection);
                        // EXAM_OPTAXIALに保存(右眼測定値)
                        var rec_optaxial_r = MakeOptaxialRec(exam_id_r,
                            Common.Const.strEyeType[Common.Const.eEyeType.RIGHT],
                            sqlConnection);
                        rec_optaxial_r.axial_mm = axialList.RAxial;
                        // todo: 表示設定の計算方法をセットする
                        rec_optaxial_r.target_eye_id = CommonController.Select_TargetEyeId_By_TargetEyeType(sqlConnection, "immersion");
                        rec_optaxial_r.measured_at = axialList.ExamDateTime;

                        // DB登録
                        result = Insert(rec_optaxial_r, sqlConnection);

                        // EXAM_LISTに保存(左眼測定値)
                        var exam_id_l = CommonController.RegisterExamList(uuid,
                            Common.Const.strMstDataType[Common.Const.eMSTDATATYPE.OPTAXIAL],
                            Common.Const.eEyeType.LEFT,
                            axialList.ExamDateTime,
                            sqlConnection);
                        // EXAM_OPTAXIALに保存(左眼測定値)
                        var rec_optaxial_l = MakeOptaxialRec(exam_id_l,
                            Common.Const.strEyeType[Common.Const.eEyeType.LEFT],
                            sqlConnection);
                        rec_optaxial_l.axial_mm = axialList.LAxial;
                        // todo: 表示設定の計算方法をセットする
                        rec_optaxial_l.target_eye_id = CommonController.Select_TargetEyeId_By_TargetEyeType(sqlConnection, "immersion");
                        rec_optaxial_l.measured_at = axialList.ExamDateTime;

                        // DB登録
                        result &= Insert(rec_optaxial_l, sqlConnection);
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

        // 眼軸長測定値書込み
        [HttpGet("GetOptAxialList/{patientId}")]
        public List<DBTest.AxialList> GetOptAxialList(string patientId) {
            List<DBTest.AxialList> DataSource = new();
            if (patientId == null || patientId == string.Empty) return DataSource;

            DBAccess dbAccess = DBAccess.GetInstance();

            try {
                // PostgreSQL Server 通信接続
                NpgsqlConnection sqlConnection = dbAccess.GetSqlConnection();

                // クエリコマンド実行
                // UUIDの有無を確認(true:update / false:insert)
                var uuid = CommonController.Select_PTUUID_by_PTID(sqlConnection, patientId);
                if (uuid == string.Empty) {
                    // 患者データが無ければ、測定データも存在しない
                    return DataSource;
                } else {
                    // 実行するクエリコマンド定義
                    string Query = "SELECT * FROM ";
                    Query += CommonController._table(CommonController.DB_TableNames[(int)CommonController.eDbTable.EXAM_OPTAXIAL]);
                    Query += " WHERE ";
                    Query += " EXISTS( ";
                    Query += "SELECT * FROM ";
                    Query += CommonController._table(CommonController.DB_TableNames[(int)CommonController.eDbTable.EXAM_LIST]);
                    Query += " WHERE ";
                    Query += CommonController._table(CommonController.DB_TableNames[(int)CommonController.eDbTable.EXAM_OPTAXIAL]);
                    Query += ".";
                    Query += CommonController._col(COLNAME_ExamOptaxialList[(int)eExamOptAxial.exam_id]);
                    Query += " = ";
                    Query += CommonController._table(CommonController.DB_TableNames[(int)CommonController.eDbTable.EXAM_LIST]);
                    Query += ".";
                    Query += CommonController._col(CommonController.COLNAME_ExamList[(int)CommonController.eExamList.exam_id]);
                    Query += " AND ";
                    Query += CommonController._table(CommonController.DB_TableNames[(int)CommonController.eDbTable.EXAM_LIST]);
                    Query += ".";
                    Query += CommonController._col(CommonController.COLNAME_ExamList[(int)CommonController.eExamList.pt_uuid]);
                    Query += " = ";
                    Query += CommonController._val(uuid);
                    Query += " )";
                    Query += " ORDER BY ";
                    Query += CommonController._col(COLNAME_ExamOptaxialList[(int)eExamOptAxial.measured_at]);
                    Query += " ASC; ";

                    NpgsqlCommand Command = new(Query, sqlConnection);
                    NpgsqlDataAdapter DataAdapter = new(Command);
                    DataTable DataTable = new();
                    DataAdapter.Fill(DataTable);
                    List<DBTest.AxialData> AxialDataSource = new();

                    AxialDataSource = (from DataRow data in DataTable.Rows
                                  select new DBTest.AxialData() {
                                      ID = data[COLNAME_ExamOptaxialList[(int)eExamOptAxial.exam_id]].ToString() ?? string.Empty,
                                      Axial = Convert.ToDouble(data[COLNAME_ExamOptaxialList[(int)eExamOptAxial.axial_mm]].ToString()),
                                      EyeId = (DBTest.EyeType)Enum.ToObject(typeof(DBTest.EyeType), data[COLNAME_ExamOptaxialList[(int)eExamOptAxial.eye_id]]),
                                      DeviceID = 2,     // todo: 
                                      ExamDateTime = (DateTime)data[COLNAME_ExamOptaxialList[(int)eExamOptAxial.measured_at]],
                                  }).ToList();

                    DataSource = SetAxialList(patientId, AxialDataSource.ToArray());
                }
            } catch {
            } finally {
                // PostgreSQL Server 通信切断
                dbAccess.CloseSqlConnection();
            }

            return DataSource;
        }

        /// <summary>
        /// DBから取得したデータを下記ルールに則りリストへセット
        /// ・1測定日1データ(右左)とする
        /// ・同じ測定日のデータがある場合、装置種別AxMのデータを優先する
        /// ・同じ測定日のデータは、測定時間が新しいものを採用する
        /// ・装置種別AxMのデータは、1測定日に1つしか登録できない
        /// </summary>
        /// <param name="axialDataList"></param>
        public List<AxialList> SetAxialList(string pt_id, AxialData[] axialDataList) {
            List<AxialList> list = new List<AxialList>();
            if (axialDataList != null) {
                try {
                    for (int i = 0; i < axialDataList.Length; i++) {
                        bool isExist = false;
                        for (int j = 0; j < list.Count; j++) {
                            if (CommonController._objectToDateOnly(list[j].ExamDateTime) 
                                == CommonController._objectToDateOnly(axialDataList[i].ExamDateTime)) {

                                if (axialDataList[i].EyeId == EyeType.right) {
                                    // 装置種別AxMのデータを優先する
                                    // 装置種別AxMのデータは、1測定日に1つしか登録できない
                                    if (!list[j].IsRManualInput) {
                                        if (list[j].RAxial == 0.0) {
                                            // 右眼かつ同じ測定日の右眼が0のとき
                                            list[j].RExamID = axialDataList[i].ID;
                                            list[j].RAxial = axialDataList[i].Axial;
                                            list[j].IsRManualInput = (axialDataList[i].DeviceID == 2);  // todo:
                                            isExist = true;
                                            break;
                                        } else if (list[j].ExamDateTime < axialDataList[i].ExamDateTime) {
                                            // 右眼かつ同じ測定時間が新しい
                                            list[j].RExamID = axialDataList[i].ID;
                                            list[j].RAxial = axialDataList[i].Axial;
                                            list[j].IsRManualInput = (axialDataList[i].DeviceID == 2);  // todo:
                                            list[j].ExamDateTime = axialDataList[i].ExamDateTime;
                                            isExist = true;
                                            break;
                                        }
                                    }
                                } else if (axialDataList[i].EyeId == EyeType.left) {
                                    if (!list[j].IsLManualInput) {
                                        if (list[j].LAxial == 0.0) {
                                            // 左眼かつ同じ測定日の左眼が0のとき
                                            list[j].LExamID = axialDataList[i].ID;
                                            list[j].LAxial = axialDataList[i].Axial;
                                            list[j].IsLManualInput = (axialDataList[i].DeviceID == 2);  // todo:
                                            isExist = true;
                                            break;
                                        } else if (list[j].ExamDateTime < axialDataList[i].ExamDateTime) {
                                            // 左眼かつ同じ測定時間が新しい
                                            list[j].LExamID = axialDataList[i].ID;
                                            list[j].LAxial = axialDataList[i].Axial;
                                            list[j].IsLManualInput = (axialDataList[i].DeviceID == 2);  // todo:
                                            list[j].ExamDateTime = axialDataList[i].ExamDateTime;
                                            isExist = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        // 同じ測定日のデータがないとき追加
                        if (!isExist) {
                            AxialList var = new AxialList() {
                                PatientID = pt_id,
                                RExamID = string.Empty,
                                LExamID = string.Empty,
                                RAxial = 0.0,
                                LAxial = 0.0,
                                ExamDateTime = axialDataList[i].ExamDateTime,
                                IsRManualInput = false,
                                IsLManualInput = false,
                            };
                            if (axialDataList[i].EyeId == EyeType.right) {
                                var.RExamID = axialDataList[i].ID;
                                var.RAxial = axialDataList[i].Axial;
                                var.IsRManualInput = (axialDataList[i].DeviceID == 2);  // todo:
                            } else if (axialDataList[i].EyeId == EyeType.left) {
                                var.LExamID = axialDataList[i].ID;
                                var.LAxial = axialDataList[i].Axial;
                                var.IsLManualInput = (axialDataList[i].DeviceID == 2);  // todo:
                            }
                            list.Add(var);
                        }
                    }
                } catch {
                }
            }
            return list;
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

        /// <summary>
        /// 指定された範囲内の最新の眼軸長データを取得する
        /// </summary>
        /// <param name="pt_uuid"></param>
        /// <param name="eye_id"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="sqlConnection"></param>
        /// <returns></returns>
        public static double GetLatestAxialData(string pt_uuid, int eye_id, DateOnly examdate, double min, double max, NpgsqlConnection sqlConnection ) {
            double axial_mm = -1;
            try {
                // 実行するクエリコマンド定義
                string Query = "SELECT * FROM ";
                Query += CommonController._table(CommonController.DB_TableNames[(int)CommonController.eDbTable.EXAM_OPTAXIAL]);
                Query += " WHERE ";
                Query += " EXISTS( ";
                Query += "SELECT * FROM ";
                Query += CommonController._table(CommonController.DB_TableNames[(int)CommonController.eDbTable.EXAM_LIST]);
                Query += " WHERE ";
                Query += CommonController._table(CommonController.DB_TableNames[(int)CommonController.eDbTable.EXAM_OPTAXIAL]);
                Query += ".";
                Query += CommonController._col(COLNAME_ExamOptaxialList[(int)eExamOptAxial.exam_id]);
                Query += " = ";
                Query += CommonController._table(CommonController.DB_TableNames[(int)CommonController.eDbTable.EXAM_LIST]);
                Query += ".";
                Query += CommonController._col(CommonController.COLNAME_ExamList[(int)CommonController.eExamList.exam_id]);
                Query += " AND ";
                Query += CommonController._table(CommonController.DB_TableNames[(int)CommonController.eDbTable.EXAM_LIST]);
                Query += ".";
                Query += CommonController._col(CommonController.COLNAME_ExamList[(int)CommonController.eExamList.pt_uuid]);
                Query += " = ";
                Query += CommonController._val(pt_uuid);
                Query += " )";
                Query += " AND ";
                Query += CommonController._col(COLNAME_ExamOptaxialList[(int)eExamOptAxial.eye_id]);
                Query += " = ";
                Query += eye_id;
                Query += " AND ";
                Query += CommonController._col(COLNAME_ExamOptaxialList[(int)eExamOptAxial.axial_mm]);
                Query += " >= ";
                Query += min;
                Query += " AND ";
                Query += CommonController._col(COLNAME_ExamOptaxialList[(int)eExamOptAxial.axial_mm]);
                Query += " <= ";
                Query += max;
                Query += " ORDER BY ";
                Query += CommonController._col(COLNAME_ExamOptaxialList[(int)eExamOptAxial.measured_at]);
                Query += " DESC LIMIT 1; ";

                NpgsqlCommand Command = new(Query, sqlConnection);
                NpgsqlDataAdapter DataAdapter = new(Command);
                DataTable DataTable = new();
                DataAdapter.Fill(DataTable);

                if (DataTable.Rows.Count > 0) {
                    // 測定日が最新測定日と一致するデータを取得
                    if (CommonController._objectToDateOnly(DataTable.Rows[0][COLNAME_ExamOptaxialList[(int)eExamOptAxial.measured_at]]) == examdate) {
                        axial_mm = Convert.ToDouble(DataTable.Rows[0][COLNAME_ExamOptaxialList[(int)eExamOptAxial.axial_mm]].ToString());
                    }
                }
            } catch {
            } finally {
            }
            return axial_mm;
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

using AxialManagerS.Shared.Common;
using BlazorDbTest.Common;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;
using System.Text;
using System.Text.Json;
using BlazorDbTest.Controllers;
using static BlazorDbTest.Controllers.DBAxialDataController;

namespace BlazorDbTest.Controllers {

  [Route("api/[controller]")]

  public class DBPachyDataController : ControllerBase {

    // 角膜厚測定値書込み
    [HttpGet("SetPachy/{conditions}/")]
    public void SetPachy(string conditions) {
      try {
        if (conditions == null || conditions == string.Empty) return;

        PachyList PachyList = JsonSerializer.Deserialize<PachyList>(conditions);

        if (PachyList == null) return;
        if (PachyList.PatientID == null || PachyList.PatientID == string.Empty) return;

        bool result = false;
        DBAccess dbAccess = DBAccess.GetInstance();

        try {
          // PostgreSQL Server 通信接続
          NpgsqlConnection sqlConnection = dbAccess.GetSqlConnection();

          // クエリコマンド実行
          // UUIDの有無を確認(true:update / false:insert)
          var uuid = DBCommonController.Select_PTUUID_by_PTID(sqlConnection, PachyList.PatientID);
          if (uuid == string.Empty) {
            // AXMからの測定データ登録時は、必ず患者データが存在する
            return;
          } else {
            // EXAM_LISTに保存(右眼測定値)
            var exam_id_r = DBCommonController.RegisterExamList(uuid,
                DBConst.strMstDataType[DBConst.eMSTDATATYPE.PACHY_CCT],
                DBConst.eEyeType.RIGHT,
                PachyList.ExamDateTime,
                sqlConnection);
            // EXAM_Pachyに保存(右眼測定値)
            var rec_Pachy_r = MakePachyRec(exam_id_r,
                DBConst.strEyeType[DBConst.eEyeType.RIGHT],
                sqlConnection);
            rec_Pachy_r.pachy_um[0] = PachyList.RPachy;
            rec_Pachy_r.measured_at = PachyList.ExamDateTime;

            // DB登録
            result = Insert(rec_Pachy_r, sqlConnection);

            // EXAM_LISTに保存(左眼測定値)
            var exam_id_l = DBCommonController.RegisterExamList(uuid,
                DBConst.strMstDataType[DBConst.eMSTDATATYPE.PACHY_CCT],
                DBConst.eEyeType.LEFT,
                PachyList.ExamDateTime,
                sqlConnection);
            // EXAM_Pachyに保存(左眼測定値)
            var rec_Pachy_l = MakePachyRec(exam_id_l,
                DBConst.strEyeType[DBConst.eEyeType.LEFT],
                sqlConnection);
            rec_Pachy_l.pachy_um[0] = PachyList.LPachy;
            rec_Pachy_l.measured_at = PachyList.ExamDateTime;

            // DB登録
            result &= Insert(rec_Pachy_l, sqlConnection);
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

    // 角膜厚測定値書込み
    [HttpGet("GetPachyList/{patientId}")]
    public List<PachyList> GetPachyList(string patientId) {
      List<PachyList> DataSource = new();
      if (patientId == null || patientId == string.Empty) return DataSource;

      DBAccess dbAccess = DBAccess.GetInstance();

      try {
        // PostgreSQL Server 通信接続
        NpgsqlConnection sqlConnection = dbAccess.GetSqlConnection();

        // クエリコマンド実行
        // UUIDの有無を確認(true:update / false:insert)
        var uuid = DBCommonController.Select_PTUUID_by_PTID(sqlConnection, patientId);
        if (uuid == string.Empty) {
          // 患者データが無ければ、測定データも存在しない
          return DataSource;
        } else {
          // 実行するクエリコマンド定義
          string Query = "SELECT * FROM ";
          Query += DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_PACHY_CCT]);
          Query += " WHERE ";
          Query += " EXISTS( ";
          Query += "SELECT * FROM ";
          Query += DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_LIST]);
          Query += " WHERE ";
          Query += DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_PACHY_CCT]);
          Query += ".";
          Query += DBCommonController._col(COLNAME_ExamPachyList[(int)eExamPachy.exam_id]);
          Query += " = ";
          Query += DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_LIST]);
          Query += ".";
          Query += DBCommonController._col(DBCommonController.COLNAME_ExamList[(int)DBCommonController.eExamList.exam_id]);
          Query += " AND ";
          Query += DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_LIST]);
          Query += ".";
          Query += DBCommonController._col(DBCommonController.COLNAME_ExamList[(int)DBCommonController.eExamList.pt_uuid]);
          Query += " = ";
          Query += DBCommonController._val(uuid);
          Query += " )";
          Query += " ORDER BY ";
          Query += DBCommonController._col(COLNAME_ExamPachyList[(int)eExamPachy.measured_at]);
          Query += " ASC; ";

          NpgsqlCommand Command = new(Query, sqlConnection);
          NpgsqlDataAdapter DataAdapter = new(Command);
          DataTable DataTable = new();
          DataAdapter.Fill(DataTable);
          List<PachyData> PachyDataSource = new();

          PachyDataSource = (from DataRow data in DataTable.Rows
                           select new PachyData() {
                             ID = data[COLNAME_ExamPachyList[(int)eExamPachy.exam_id]].ToString() ?? string.Empty,
                             Pachy = DBCommonController._objectToDoubleList(data[COLNAME_ExamPachyList[(int)eExamPachy.pachy_um]]),
                             EyeId = (EyeType)Enum.ToObject(typeof(EyeType), data[COLNAME_ExamPachyList[(int)eExamPachy.eye_id]]),
                             IsExamData = (bool)data[COLNAME_ExamPachyList[(int)eExamPachy.is_exam_data]],
                             DeviceID = 4,     // todo: 
                             ExamDateTime = (DateTime)data[COLNAME_ExamPachyList[(int)eExamPachy.measured_at]],
                           }).ToList();

          DataSource = SetPachyList(patientId, PachyDataSource.ToArray());
        }
      } catch {
      } finally {
        // PostgreSQL Server 通信切断
        dbAccess.CloseSqlConnection();
      }

      return DataSource;
    }

    // 角膜厚測定値削除
    [HttpGet("DeletePachyData/{examId}/")]
    public void DeletePachyData(int examId) {
      try {
        DBAccess dbAccess = DBAccess.GetInstance();

        bool result = false;

        try {
          // PostgreSQL Server 通信接続
          NpgsqlConnection sqlConnection = dbAccess.GetSqlConnection();

          // クエリコマンド実行
          // EXAM_PACHY_CCTテーブルからから削除
          if (delete_by_examId(examId, sqlConnection) != 0) {
            // EXAM_LISTテーブルから削除
            result = (DBCommonController.delete_by_ExamId(examId, sqlConnection) != 0);
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

    /// <summary>
    /// DBから取得したデータを下記ルールに則りリストへセット
    /// ・1測定日1データ(右左)とする
    /// ・同じ測定日のデータがある場合、装置種別AxMのデータを優先する
    /// ・同じ測定日のデータは、測定時間が新しいものを採用する
    /// ・装置種別AxMのデータは、1測定日に1つしか登録できない
    /// </summary>
    /// <param name="PachyDataList"></param>
    public List<PachyList> SetPachyList(string pt_id, PachyData[] PachyDataList) {
      List<PachyList> list = new List<PachyList>();
      if (PachyDataList != null) {
        try {
          for (int i = 0; i < PachyDataList.Length; i++) {
            bool isExist = false;
            for (int j = 0; j < list.Count; j++) {
              if (DBCommonController._objectToDateOnly(list[j].ExamDateTime)
                  == DBCommonController._objectToDateOnly(PachyDataList[i].ExamDateTime)) {

                if (PachyDataList[i].EyeId == EyeType.right) {
                  // 装置種別AxMのデータを優先する
                  // 装置種別AxMのデータは、1測定日に1つしか登録できない
                  if (!list[j].IsRManualInput) {
                    if (list[j].RPachy == 0.0) {
                      // 右眼かつ同じ測定日の右眼が0のとき
                      list[j].RExamID = PachyDataList[i].ID;
                      list[j].RPachy = PachyDataList[i].Pachy[0] ?? 0.0;  // todo:
                      list[j].IsRManualInput = (PachyDataList[i].DeviceID == 4);  // todo:
                      isExist = true;
                      break;
                    } else if (list[j].ExamDateTime < PachyDataList[i].ExamDateTime) {
                      // 右眼かつ同じ測定時間が新しい
                      list[j].RExamID = PachyDataList[i].ID;
                      list[j].RPachy = PachyDataList[i].Pachy[0] ?? 0.0;  // todo:
                      list[j].IsRManualInput = (PachyDataList[i].DeviceID == 4);  // todo:
                      list[j].ExamDateTime = PachyDataList[i].ExamDateTime;
                      isExist = true;
                      break;
                    }
                  }
                } else if (PachyDataList[i].EyeId == EyeType.left) {
                  if (!list[j].IsLManualInput) {
                    if (list[j].LPachy == 0.0) {
                      // 左眼かつ同じ測定日の左眼が0のとき
                      list[j].LExamID = PachyDataList[i].ID;
                      list[j].LPachy = PachyDataList[i].Pachy[0] ?? 0.0;  // todo:
                      list[j].IsLManualInput = (PachyDataList[i].DeviceID == 4);  // todo:
                      isExist = true;
                      break;
                    } else if (list[j].ExamDateTime < PachyDataList[i].ExamDateTime) {
                      // 左眼かつ同じ測定時間が新しい
                      list[j].LExamID = PachyDataList[i].ID;
                      list[j].LPachy = PachyDataList[i].Pachy[0] ?? 0.0;  // todo:
                      list[j].IsLManualInput = (PachyDataList[i].DeviceID == 4);  // todo:
                      list[j].ExamDateTime = PachyDataList[i].ExamDateTime;
                      isExist = true;
                      break;
                    }
                  }
                }
              }
            }

            // 同じ測定日のデータがないとき追加
            if (!isExist) {
              PachyList var = new PachyList() {
                PatientID = pt_id,
                RExamID = string.Empty,
                LExamID = string.Empty,
                RPachy = 0.0,
                LPachy = 0.0,
                ExamDateTime = PachyDataList[i].ExamDateTime,
                IsRManualInput = false,
                IsLManualInput = false,
              };
              if (PachyDataList[i].EyeId == EyeType.right) {
                var.RExamID = PachyDataList[i].ID;
                var.RPachy = PachyDataList[i].Pachy[0] ?? 0.0;  // todo:
                var.IsRManualInput = (PachyDataList[i].DeviceID == 4);  // todo:
              } else if (PachyDataList[i].EyeId == EyeType.left) {
                var.LExamID = PachyDataList[i].ID;
                var.LPachy = PachyDataList[i].Pachy[0] ?? 0.0;  // todo:
                var.IsLManualInput = (PachyDataList[i].DeviceID == 4);  // todo:
              }
              list.Add(var);
            }
          }
        } catch {
        }
      }
      return list;
    }

    public static ExamPachyRec MakePachyRec(int examId, string posEye, NpgsqlConnection sqlConnection) {

      var recPachy = new ExamPachyRec();
      try {
        recPachy.exam_id = examId;
        recPachy.examtype_id = DBCommonController.Select_Examtype_ID(sqlConnection, DBConst.strMstDataType[DBConst.eMSTDATATYPE.PACHY_CCT]);
        recPachy.eye_id = DBCommonController.Select_Eye_ID(sqlConnection, posEye);
        recPachy.device_id = DBCommonController.Select_Device_ID(sqlConnection, "AXM2");

        recPachy.is_exam_data = true;   // todo: 要確認
        recPachy.comment = ""; // タグが無いので空文字
        recPachy.select_id = 0; // 0固定でよい

        recPachy.target_eye_id = 0; // todo: 要確認
        recPachy.fitting_id = 0; // todo: 要確認

        recPachy.is_meas_auto = false; // false固定でよい
        recPachy.pachy_um = new List<double?>() { 0, 0, 0 };

        recPachy.oct_sd = 0;
        recPachy.oct_snr = new List<int?>() { 0, 0, 0 };

        recPachy.is_oct_average_ref_ind = false;
        recPachy.oct_pachy_ref_ind = 0;

        recPachy.is_us_velocity = false;
        recPachy.us_velocity_mpers = 0;

        recPachy.is_us_bias_offset_um = false;
        recPachy.us_bias_offset_um = 0;
        recPachy.us_bias_offset_per = 0;

        recPachy.is_em_us_correction = false;
        recPachy.em_us_correction_um = 0;

        recPachy.is_reliabiliy = false;
        recPachy.reliability = new List<string?>() { "", "", "" };
        recPachy.data_path = ""; // データパスが無いので空文字

        recPachy.measured_at = null;

        // 更新日、作成日は揃える
        var dateNow = DateTime.Now;
        recPachy.updated_at = dateNow;
        recPachy.created_at = dateNow;
      } catch {
      } finally {
      }
      return recPachy;
    }

    public static bool Insert(ExamPachyRec aExamPachyRec, NpgsqlConnection sqlConnection) {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("insert into ");
      stringBuilder.Append(DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_PACHY_CCT]));
      string text = " (";
      string text2 = " (";
      for (int i = 0; i < COLNAME_ExamPachyList.Count(); i++) {
        if (i != 0) {
          text += ",";
          text2 += ",";
        }

        text += DBCommonController._col(COLNAME_ExamPachyList[i]);
        text2 += DBCommonController._bind(COLNAME_ExamPachyList[i]);
      }

      text += ")";
      text2 += ")";
      stringBuilder.Append(text);
      stringBuilder.Append(" values ");
      stringBuilder.Append(text2);
      stringBuilder.Append(DBCommonController._onconflict("pk_exam_pachy_cct"));
      stringBuilder.Append(DBCommonController._doupdateexam(COLNAME_ExamPachyList[(int)eExamPachy.updated_at], DateTime.Now));
      stringBuilder.Append(DBCommonController._doupdatedoublelist(COLNAME_ExamPachyList[(int)eExamPachy.pachy_um], aExamPachyRec.pachy_um));
      stringBuilder.Append(";");
      int num = 0;
      using (NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection)) {
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamPachyList[(int)eExamPachy.exam_id], aExamPachyRec.exam_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamPachyList[(int)eExamPachy.examtype_id], aExamPachyRec.examtype_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamPachyList[(int)eExamPachy.eye_id], aExamPachyRec.eye_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamPachyList[(int)eExamPachy.device_id], aExamPachyRec.device_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamPachyList[(int)eExamPachy.is_exam_data], aExamPachyRec.is_exam_data);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamPachyList[(int)eExamPachy.comment], aExamPachyRec.comment);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamPachyList[(int)eExamPachy.select_id], aExamPachyRec.select_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamPachyList[(int)eExamPachy.target_eye_id], aExamPachyRec.target_eye_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamPachyList[(int)eExamPachy.fitting_id], aExamPachyRec.fitting_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamPachyList[(int)eExamPachy.is_meas_auto], aExamPachyRec.is_meas_auto);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamPachyList[(int)eExamPachy.pachy_um], aExamPachyRec.pachy_um);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamPachyList[(int)eExamPachy.oct_sd], aExamPachyRec.oct_sd);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamPachyList[(int)eExamPachy.oct_snr], aExamPachyRec.oct_snr);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamPachyList[(int)eExamPachy.is_oct_average_ref_ind], aExamPachyRec.is_oct_average_ref_ind);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamPachyList[(int)eExamPachy.oct_pachy_ref_ind], aExamPachyRec.oct_pachy_ref_ind);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamPachyList[(int)eExamPachy.is_us_velocity], aExamPachyRec.is_us_velocity);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamPachyList[(int)eExamPachy.us_velocity_mpers], aExamPachyRec.us_velocity_mpers);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamPachyList[(int)eExamPachy.is_us_bias_offset_um], aExamPachyRec.is_us_bias_offset_um);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamPachyList[(int)eExamPachy.us_bias_offset_um], aExamPachyRec.us_bias_offset_um);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamPachyList[(int)eExamPachy.us_bias_offset_per], aExamPachyRec.us_bias_offset_per);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamPachyList[(int)eExamPachy.is_em_us_correction], aExamPachyRec.is_em_us_correction);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamPachyList[(int)eExamPachy.em_us_correction_um], aExamPachyRec.em_us_correction_um);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamPachyList[(int)eExamPachy.is_reliability], aExamPachyRec.is_reliabiliy);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamPachyList[(int)eExamPachy.reliability], aExamPachyRec.reliability);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamPachyList[(int)eExamPachy.data_path], aExamPachyRec.data_path);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamPachyList[(int)eExamPachy.measured_at], DBCommonController._DateTimeToObject(aExamPachyRec.measured_at));
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamPachyList[(int)eExamPachy.updated_at], DBCommonController._DateTimeToObject(aExamPachyRec.updated_at));
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamPachyList[(int)eExamPachy.created_at], DBCommonController._DateTimeToObject(aExamPachyRec.created_at));
        num = npgsqlCommand.ExecuteNonQuery();
      }

      return num != 0;
    }

    public int delete_by_examId(int examId, NpgsqlConnection sqlConnection) {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("delete ");
      stringBuilder.Append("from ");
      stringBuilder.Append(DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_PACHY_CCT]));
      stringBuilder.Append("where ");
      stringBuilder.Append(DBCommonController._col(COLNAME_ExamPachyList[(int)eExamPachy.exam_id]));
      stringBuilder.Append("= ");
      stringBuilder.Append(DBCommonController._bind(COLNAME_ExamPachyList[(int)eExamPachy.exam_id]));
      stringBuilder.Append(";");
      using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
      npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamPachyList[(int)eExamPachy.exam_id], examId);
      return npgsqlCommand.ExecuteNonQuery();
    }

    // todo: 誤字修正
    public static string[] COLNAME_ExamPachyList = new string[(int)eExamPachy.MAX]
    {
      "exam_id", "examtype_id", "eye_id", "device_id", "is_exam_data", "comment", "select_id", "target_eye_id", "fitting_id", "is_meas_auto",
      "pachy_um", "oct_sd", "oct_snr", "is_oct_average_ref_ind", "oct_pachy_ref_ind", "is_us_velocity", "us_velocity_mpers", "is_us_bias_offset_um",
      "us_bias_offset_um", "us_bias_offset_per", "is_em_us_correction", "em_us_correction_um", "is_reliabillty", "reliabillty", "data_path",
      "measured_at", "updated_at", "created_at"
    };

    public enum eExamPachy {
      exam_id = 0,
      examtype_id,
      eye_id,
      device_id,
      is_exam_data,
      comment,
      select_id,
      target_eye_id,
      fitting_id,
      is_meas_auto,
      pachy_um,
      oct_sd,
      oct_snr,
      is_oct_average_ref_ind,
      oct_pachy_ref_ind,
      is_us_velocity,
      us_velocity_mpers,
      is_us_bias_offset_um,
      us_bias_offset_um,
      us_bias_offset_per,
      is_em_us_correction,
      em_us_correction_um,
      is_reliability,
      reliability,
      data_path,
      measured_at,
      updated_at,
      created_at,
      MAX
    }
  }
}

public class ExamPachyRec {
  public int exam_id { get; set; }
  public int examtype_id { get; set; }
  public int eye_id { get; set; }
  public int device_id { get; set; }
  public bool is_exam_data { get; set; }
  public string comment { get; set; }
  public int select_id { get; set; }
  public int target_eye_id { get; set; }
  public int fitting_id { get; set; }
  public bool is_meas_auto { get; set; }
  public List<double?> pachy_um { get; set; } = new List<double?>();
  public double oct_sd { get; set; }
  public List<int?> oct_snr { get; set; } = new List<int?>();
  public bool is_oct_average_ref_ind { get; set; }
  public double oct_pachy_ref_ind { get; set; }
  public bool is_us_velocity { get; set; }
  public int us_velocity_mpers { get; set; }
  public bool is_us_bias_offset_um { get; set; }
  public int us_bias_offset_um { get; set; }
  public int us_bias_offset_per { get; set; }
  public bool is_em_us_correction { get; set; }
  public int em_us_correction_um { get; set; }
  public bool is_reliabiliy { get; set; }
  public List<string?> reliability { get; set; } = new List<string?>();
  public string? data_path { get; set; }
  public DateTime? measured_at { get; set; }
  public DateTime? updated_at { get; set; }
  public DateTime? created_at { get; set; }
}

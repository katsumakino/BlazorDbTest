using AxialManagerS.Shared.Common;
using BlazorDbTest.Common;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;
using System.Text;
using System.Text.Json;
using static BlazorDbTest.Controllers.DBAxialDataController;

namespace BlazorDbTest.Controllers {

  [Route("api/[controller]")]

  public class DBDiaDataController : ControllerBase {

    // 瞳孔径測定値書込み
    [HttpPost("SetDia")]
    public void SetDia([FromBody] DiaList conditions) {
      try {
        if (conditions == null) return;

        if (conditions.PatientID == null || conditions.PatientID == string.Empty) return;

        bool result = false;
        DBAccess dbAccess = DBAccess.GetInstance();

        try {
          // PostgreSQL Server 通信接続
          NpgsqlConnection sqlConnection = dbAccess.GetSqlConnection();

          // クエリコマンド実行
          // UUIDの有無を確認(true:update / false:insert)
          var uuid = DBCommonController.Select_PTUUID_by_PTID(sqlConnection, conditions.PatientID);
          if (uuid == string.Empty) {
            // AXMからの測定データ登録時は、必ず患者データが存在する
            return;
          } else {
            // EXAM_LISTに保存(右眼測定値)
            var exam_id_r = DBCommonController.RegisterExamList(uuid,
                DBConst.strMstDataType[DBConst.eMSTDATATYPE.DIA],
                DBConst.eEyeType.RIGHT,
                conditions.ExamDateTime,
                sqlConnection);
            // EXAM_Diaに保存(右眼測定値)
            var rec_Dia_r = MakeDiaRec(exam_id_r,
                DBConst.strEyeType[DBConst.eEyeType.RIGHT],
                sqlConnection);
            rec_Dia_r.pupil_mm = conditions.RPupil ?? 0.0;
            rec_Dia_r.is_exam_pupil_data = (conditions.RPupil != null);
            rec_Dia_r.measured_at = conditions.ExamDateTime;

            // DB登録
            result = Insert(rec_Dia_r, sqlConnection);

            // EXAM_LISTに保存(左眼測定値)
            var exam_id_l = DBCommonController.RegisterExamList(uuid,
                DBConst.strMstDataType[DBConst.eMSTDATATYPE.DIA],
                DBConst.eEyeType.LEFT,
                conditions.ExamDateTime,
                sqlConnection);
            // EXAM_Diaに保存(左眼測定値)
            var rec_Dia_l = MakeDiaRec(exam_id_l,
                DBConst.strEyeType[DBConst.eEyeType.LEFT],
                sqlConnection);
            rec_Dia_l.pupil_mm = conditions.LPupil ?? 0.0;
            rec_Dia_l.is_exam_pupil_data = (conditions.LPupil != null);
            rec_Dia_l.measured_at = conditions.ExamDateTime;

            // DB登録
            result &= Insert(rec_Dia_l, sqlConnection);
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

    // 瞳孔径測定値書込み
    [HttpGet("GetDiaList/{patientId}")]
    public List<DiaList> GetDiaList(string patientId) {
      List<DiaList> DataSource = new();
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
          Query += DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_DIA]);
          Query += " WHERE ";
          Query += " EXISTS( ";
          Query += "SELECT * FROM ";
          Query += DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_LIST]);
          Query += " WHERE ";
          Query += DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_DIA]);
          Query += ".";
          Query += DBCommonController._col(COLNAME_ExamDiaList[(int)eExamDia.exam_id]);
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
          Query += " AND ";
          Query += DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_DIA]);
          Query += ".";
          Query += DBCommonController._col(COLNAME_ExamDiaList[(int)eExamDia.is_exam_pupil_data]);
          Query += " = ";
          Query += DBCommonController._val("TRUE");
          Query += " )";
          Query += " ORDER BY ";
          Query += DBCommonController._col(COLNAME_ExamDiaList[(int)eExamDia.measured_at]);
          Query += " ASC; ";

          NpgsqlCommand Command = new(Query, sqlConnection);
          NpgsqlDataAdapter DataAdapter = new(Command);
          DataTable DataTable = new();
          DataAdapter.Fill(DataTable);
          List<DiaData> DiaDataSource = new();

          DiaDataSource = (from DataRow data in DataTable.Rows
                           select new DiaData() {
                             ID = data[COLNAME_ExamDiaList[(int)eExamDia.exam_id]].ToString() ?? string.Empty,
                             Pupil = Convert.ToDouble(data[COLNAME_ExamDiaList[(int)eExamDia.pupil_mm]]),
                             EyeId = (EyeType)Enum.ToObject(typeof(EyeType), data[COLNAME_ExamDiaList[(int)eExamDia.eye_id]]),
                             IsExamData = (bool)data[COLNAME_ExamDiaList[(int)eExamDia.is_exam_pupil_data]],
                             DeviceID = 4,     // todo: 
                             ExamDateTime = (DateTime)data[COLNAME_ExamDiaList[(int)eExamDia.measured_at]],
                           }).ToList();

          DataSource = SetDiaList(patientId, DiaDataSource.ToArray());
        }
      } catch {
      } finally {
        // PostgreSQL Server 通信切断
        dbAccess.CloseSqlConnection();
      }

      return DataSource;
    }

    // 瞳孔径測定値削除
    [HttpGet("DeleteDiaData/{examId}/")]
    public void DeleteDiaData(int examId) {
      try {
        DBAccess dbAccess = DBAccess.GetInstance();

        bool result = false;

        try {
          // PostgreSQL Server 通信接続
          NpgsqlConnection sqlConnection = dbAccess.GetSqlConnection();

          // クエリコマンド実行
          // EXAM_DIAテーブルからから削除
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
    /// <param name="DiaDataList"></param>
    public List<DiaList> SetDiaList(string pt_id, DiaData[] DiaDataList) {
      List<DiaList> list = new List<DiaList>();
      if (DiaDataList != null) {
        try {
          for (int i = 0; i < DiaDataList.Length; i++) {
            bool isExist = false;
            for (int j = 0; j < list.Count; j++) {
              if (DBCommonController._objectToDateOnly(list[j].ExamDateTime)
                  == DBCommonController._objectToDateOnly(DiaDataList[i].ExamDateTime)) {

                if (DiaDataList[i].EyeId == EyeType.right) {
                  // 装置種別AxMのデータを優先する
                  // 装置種別AxMのデータは、1測定日に1つしか登録できない
                  if (!list[j].IsRManualInput) {
                    if (list[j].RPupil == null) {
                      // 右眼かつ同じ測定日の右眼がnullのとき
                      list[j].RExamID = DiaDataList[i].ID;
                      list[j].RPupil = DiaDataList[i].Pupil;
                      list[j].IsRManualInput = (DiaDataList[i].DeviceID == 4);  // todo:
                      isExist = true;
                      break;
                    } else if (list[j].ExamDateTime < DiaDataList[i].ExamDateTime) {
                      // 右眼かつ同じ測定時間が新しい
                      list[j].RExamID = DiaDataList[i].ID;
                      list[j].RPupil = DiaDataList[i].Pupil;
                      list[j].IsRManualInput = (DiaDataList[i].DeviceID == 4);  // todo:
                      list[j].ExamDateTime = DiaDataList[i].ExamDateTime;
                      isExist = true;
                      break;
                    }
                  }
                } else if (DiaDataList[i].EyeId == EyeType.left) {
                  if (!list[j].IsLManualInput) {
                    if (list[j].LPupil == null) {
                      // 左眼かつ同じ測定日の左眼がnullのとき
                      list[j].LExamID = DiaDataList[i].ID;
                      list[j].LPupil = DiaDataList[i].Pupil;
                      list[j].IsLManualInput = (DiaDataList[i].DeviceID == 4);  // todo:
                      isExist = true;
                      break;
                    } else if (list[j].ExamDateTime < DiaDataList[i].ExamDateTime) {
                      // 左眼かつ同じ測定時間が新しい
                      list[j].LExamID = DiaDataList[i].ID;
                      list[j].LPupil = DiaDataList[i].Pupil;
                      list[j].IsLManualInput = (DiaDataList[i].DeviceID == 4);  // todo:
                      list[j].ExamDateTime = DiaDataList[i].ExamDateTime;
                      isExist = true;
                      break;
                    }
                  }
                }
              }
            }

            // 同じ測定日のデータがないとき追加
            if (!isExist) {
              DiaList var = new DiaList() {
                PatientID = pt_id,
                RExamID = string.Empty,
                LExamID = string.Empty,
                RPupil = null,
                LPupil = null,
                ExamDateTime = DiaDataList[i].ExamDateTime,
                IsRManualInput = false,
                IsLManualInput = false,
              };
              if (DiaDataList[i].EyeId == EyeType.right) {
                var.RExamID = DiaDataList[i].ID;
                var.RPupil = DiaDataList[i].Pupil;
                var.IsRManualInput = (DiaDataList[i].DeviceID == 4);  // todo:
              } else if (DiaDataList[i].EyeId == EyeType.left) {
                var.LExamID = DiaDataList[i].ID;
                var.LPupil = DiaDataList[i].Pupil;
                var.IsLManualInput = (DiaDataList[i].DeviceID == 4);  // todo:
              }
              list.Add(var);
            }
          }
        } catch {
        }
      }
      return list;
    }

    public static ExamDiaRec MakeDiaRec(int examId, string posEye, NpgsqlConnection sqlConnection) {

      var recDia = new ExamDiaRec();
      try {
        recDia.exam_id = examId;
        recDia.examtype_id = DBCommonController.Select_Examtype_ID(sqlConnection, DBConst.strMstDataType[DBConst.eMSTDATATYPE.DIA]);
        recDia.eye_id = DBCommonController.Select_Eye_ID(sqlConnection, posEye);
        recDia.device_id = DBCommonController.Select_Device_ID(sqlConnection, "AXM2");

        recDia.is_exam_pupil_data = true;   // todo: 要確認
        recDia.is_exam_wtw_data = true;     // todo: 要確認
        recDia.comment = ""; // タグが無いので空文字

        recDia.pupil_mm = 0.0;
        recDia.wtw_mm = 0.0;
        recDia.vd_mm = 0.0;

        recDia.environment_id = 0; // 環境IDが無いので0
        recDia.data_path = ""; // データパスが無いので空文字

        recDia.measured_at = null;

        // 更新日、作成日は揃える
        var dateNow = DateTime.Now;
        recDia.updated_at = dateNow;
        recDia.created_at = dateNow;
      } catch {
      } finally {
      }
      return recDia;
    }

    public static bool Insert(ExamDiaRec aExamDiaRec, NpgsqlConnection sqlConnection) {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("insert into ");
      stringBuilder.Append(DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_DIA]));
      string text = " (";
      string text2 = " (";
      for (int i = 0; i < COLNAME_ExamDiaList.Count(); i++) {
        if (i != 0) {
          text += ",";
          text2 += ",";
        }

        text += DBCommonController._col(COLNAME_ExamDiaList[i]);
        text2 += DBCommonController._bind(COLNAME_ExamDiaList[i]);
      }

      text += ")";
      text2 += ")";
      stringBuilder.Append(text);
      stringBuilder.Append(" values ");
      stringBuilder.Append(text2);
      stringBuilder.Append(DBCommonController._onconflict("pk_exam_dia"));
      stringBuilder.Append(DBCommonController._doupdateexam(COLNAME_ExamDiaList[(int)eExamDia.updated_at], DateTime.Now));
      stringBuilder.Append(DBCommonController._doupdatevalue(COLNAME_ExamDiaList[(int)eExamDia.pupil_mm], aExamDiaRec.pupil_mm.ToString()));
      stringBuilder.Append(DBCommonController._doupdatevalue(COLNAME_ExamDiaList[(int)eExamDia.is_exam_pupil_data], aExamDiaRec.is_exam_pupil_data.ToString()));
      stringBuilder.Append(";");
      int num = 0;
      using (NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection)) {
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamDiaList[(int)eExamDia.exam_id], aExamDiaRec.exam_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamDiaList[(int)eExamDia.examtype_id], aExamDiaRec.examtype_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamDiaList[(int)eExamDia.eye_id], aExamDiaRec.eye_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamDiaList[(int)eExamDia.device_id], aExamDiaRec.device_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamDiaList[(int)eExamDia.is_exam_pupil_data], aExamDiaRec.is_exam_pupil_data);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamDiaList[(int)eExamDia.is_exam_wtw_data], aExamDiaRec.is_exam_wtw_data);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamDiaList[(int)eExamDia.comment], aExamDiaRec.comment);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamDiaList[(int)eExamDia.pupil_mm], aExamDiaRec.pupil_mm);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamDiaList[(int)eExamDia.wtw_mm], aExamDiaRec.wtw_mm);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamDiaList[(int)eExamDia.vd_mm], aExamDiaRec.vd_mm);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamDiaList[(int)eExamDia.environment_id], aExamDiaRec.environment_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamDiaList[(int)eExamDia.data_path], aExamDiaRec.data_path);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamDiaList[(int)eExamDia.measured_at], DBCommonController._DateTimeToObject(aExamDiaRec.measured_at));
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamDiaList[(int)eExamDia.updated_at], DBCommonController._DateTimeToObject(aExamDiaRec.updated_at));
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamDiaList[(int)eExamDia.created_at], DBCommonController._DateTimeToObject(aExamDiaRec.created_at));
        num = npgsqlCommand.ExecuteNonQuery();
      }

      return num != 0;
    }

    public int delete_by_examId(int examId, NpgsqlConnection sqlConnection) {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("delete ");
      stringBuilder.Append("from ");
      stringBuilder.Append(DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_DIA]));
      stringBuilder.Append("where ");
      stringBuilder.Append(DBCommonController._col(COLNAME_ExamDiaList[(int)eExamDia.exam_id]));
      stringBuilder.Append("= ");
      stringBuilder.Append(DBCommonController._bind(COLNAME_ExamDiaList[(int)eExamDia.exam_id]));
      stringBuilder.Append(";");
      using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
      npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamOptaxialList[(int)eExamDia.exam_id], examId);
      return npgsqlCommand.ExecuteNonQuery();
    }

    // todo: 誤字修正
    public static string[] COLNAME_ExamDiaList = new string[(int)eExamDia.MAX]
    {
      "exam_id", "examtype_id", "eye_id", "device_id", "is_exam_pupil_data", "is_exam_wtw_data", "comment", "pupil_mm", "wtw_mm", "vd_mm",
      "environment_id", "data_path", "measured_at", "updated_at", "created_at"
    };

    public enum eExamDia {
      exam_id = 0,
      examtype_id,
      eye_id,
      device_id,
      is_exam_pupil_data,
      is_exam_wtw_data,
      comment,
      pupil_mm,
      wtw_mm,
      vd_mm,
      environment_id,
      data_path,
      measured_at,
      updated_at,
      created_at,
      MAX
    }
  }
}

public class ExamDiaRec {
  public int? exam_id { get; set; }
  public int? examtype_id { get; set; }
  public int? eye_id { get; set; }
  public int? device_id { get; set; }
  public bool? is_exam_pupil_data { get; set; }
  public bool? is_exam_wtw_data { get; set; }
  public string? comment { get; set; }
  public double? pupil_mm { get; set; }
  public double? wtw_mm { get; set; }
  public double? vd_mm { get; set; }
  public int? environment_id { get; set; }
  public string? data_path { get; set; }
  public DateTime? measured_at { get; set; }
  public DateTime? updated_at { get; set; }
  public DateTime? created_at { get; set; }
}

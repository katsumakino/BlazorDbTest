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

  public class DBSightDataController : ControllerBase {

    // 視力測定値書込み
    [HttpGet("SetSight/{conditions}/")]
    public void SetSight(string conditions) {
      try {
        if (conditions == null || conditions == string.Empty) return;

        SightList SightList = JsonSerializer.Deserialize<SightList>(conditions);

        if (SightList == null) return;
        if (SightList.PatientID == null || SightList.PatientID == string.Empty) return;

        bool result = false;
        DBAccess dbAccess = DBAccess.GetInstance();

        try {
          // PostgreSQL Server 通信接続
          NpgsqlConnection sqlConnection = dbAccess.GetSqlConnection();

          // クエリコマンド実行
          // UUIDの有無を確認(true:update / false:insert)
          var uuid = DBCommonController.Select_PTUUID_by_PTID(sqlConnection, SightList.PatientID);
          if (uuid == string.Empty) {
            // AXMからの測定データ登録時は、必ず患者データが存在する
            return;
          } else {
            // EXAM_LISTに保存(右眼測定値)
            var exam_id_r = DBCommonController.RegisterExamList(uuid,
                DBConst.strMstDataType[DBConst.eMSTDATATYPE.SIGHT],
                DBConst.eEyeType.RIGHT,
                SightList.ExamDateTime,
                sqlConnection);
            // EXAM_Sightに保存(右眼測定値)
            var rec_Sight_r = MakeSightRec(exam_id_r,
                DBConst.strEyeType[DBConst.eEyeType.RIGHT],
                sqlConnection);
            rec_Sight_r.sight_d = SightList.RSight;
            rec_Sight_r.measured_at = SightList.ExamDateTime;

            // DB登録
            result = Insert(rec_Sight_r, sqlConnection);

            // EXAM_LISTに保存(左眼測定値)
            var exam_id_l = DBCommonController.RegisterExamList(uuid,
                DBConst.strMstDataType[DBConst.eMSTDATATYPE.SIGHT],
                DBConst.eEyeType.LEFT,
                SightList.ExamDateTime,
                sqlConnection);
            // EXAM_Sightに保存(左眼測定値)
            var rec_Sight_l = MakeSightRec(exam_id_l,
                DBConst.strEyeType[DBConst.eEyeType.LEFT],
                sqlConnection);
            rec_Sight_l.sight_d = SightList.LSight;
            rec_Sight_l.measured_at = SightList.ExamDateTime;

            // DB登録
            result &= Insert(rec_Sight_l, sqlConnection);
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

    // 視力測定値書込み
    [HttpGet("GetSightList/{patientId}")]
    public List<SightList> GetSightList(string patientId) {
      List<SightList> DataSource = new();
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
          Query += DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.AXM_EXAM_SIGHT]);
          Query += " WHERE ";
          Query += " EXISTS( ";
          Query += "SELECT * FROM ";
          Query += DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_LIST]);
          Query += " WHERE ";
          Query += DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.AXM_EXAM_SIGHT]);
          Query += ".";
          Query += DBCommonController._col(COLNAME_ExamSightList[(int)eExamSight.exam_id]);
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
          Query += DBCommonController._col(COLNAME_ExamSightList[(int)eExamSight.measured_at]);
          Query += " ASC; ";

          NpgsqlCommand Command = new(Query, sqlConnection);
          NpgsqlDataAdapter DataAdapter = new(Command);
          DataTable DataTable = new();
          DataAdapter.Fill(DataTable);
          List<SightData> SightDataSource = new();

          SightDataSource = (from DataRow data in DataTable.Rows
                           select new SightData() {
                             ID = data[COLNAME_ExamSightList[(int)eExamSight.exam_id]].ToString() ?? string.Empty,
                             Sight = Convert.ToDouble(data[COLNAME_ExamSightList[(int)eExamSight.sight_d]]),
                             EyeId = (EyeType)Enum.ToObject(typeof(EyeType), data[COLNAME_ExamSightList[(int)eExamSight.eye_id]]),
                             IsExamData = (bool)data[COLNAME_ExamSightList[(int)eExamSight.is_exam_data]],
                             DeviceID = 4,     // todo: 
                             ExamDateTime = (DateTime)data[COLNAME_ExamSightList[(int)eExamSight.measured_at]],
                           }).ToList();

          DataSource = SetSightList(patientId, SightDataSource.ToArray());
        }
      } catch {
      } finally {
        // PostgreSQL Server 通信切断
        dbAccess.CloseSqlConnection();
      }

      return DataSource;
    }

    // 視力測定値削除
    [HttpGet("DeleteSightData/{examId}/")]
    public void DeleteSightData(int examId) {
      try {
        DBAccess dbAccess = DBAccess.GetInstance();

        bool result = false;

        try {
          // PostgreSQL Server 通信接続
          NpgsqlConnection sqlConnection = dbAccess.GetSqlConnection();

          // クエリコマンド実行
          // AXM_EXAM_SIGHTテーブルからから削除
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
    /// <param name="SightDataList"></param>
    public List<SightList> SetSightList(string pt_id, SightData[] SightDataList) {
      List<SightList> list = new List<SightList>();
      if (SightDataList != null) {
        try {
          for (int i = 0; i < SightDataList.Length; i++) {
            bool isExist = false;
            for (int j = 0; j < list.Count; j++) {
              if (DBCommonController._objectToDateOnly(list[j].ExamDateTime)
                  == DBCommonController._objectToDateOnly(SightDataList[i].ExamDateTime)) {

                if (SightDataList[i].EyeId == EyeType.right) {
                  // 装置種別AxMのデータを優先する
                  // 装置種別AxMのデータは、1測定日に1つしか登録できない
                  
                  if (list[j].RSight == 0.0) {    // todo: 0があり得るので要修正
                    // 右眼かつ同じ測定日の右眼が0のとき
                    list[j].RExamID = SightDataList[i].ID;
                    list[j].RSight = SightDataList[i].Sight;
                    isExist = true;
                    break;
                  } else if (list[j].ExamDateTime < SightDataList[i].ExamDateTime) {
                    // 右眼かつ同じ測定時間が新しい
                    list[j].RExamID = SightDataList[i].ID;
                    list[j].RSight = SightDataList[i].Sight;
                    list[j].ExamDateTime = SightDataList[i].ExamDateTime;
                    isExist = true;
                    break;
                  }
                } else if (SightDataList[i].EyeId == EyeType.left) {
                    if (list[j].LSight == 0.0) {
                      // 左眼かつ同じ測定日の左眼が0のとき
                      list[j].LExamID = SightDataList[i].ID;
                      list[j].LSight = SightDataList[i].Sight;
                      isExist = true;
                      break;
                    } else if (list[j].ExamDateTime < SightDataList[i].ExamDateTime) {
                      // 左眼かつ同じ測定時間が新しい
                      list[j].LExamID = SightDataList[i].ID;
                      list[j].LSight = SightDataList[i].Sight;
                      list[j].ExamDateTime = SightDataList[i].ExamDateTime;
                      isExist = true;
                      break;
                    }
                }
              }
            }

            // 同じ測定日のデータがないとき追加
            if (!isExist) {
              SightList var = new SightList() {
                PatientID = pt_id,
                RExamID = string.Empty,
                LExamID = string.Empty,
                RSight = 0.0,
                LSight = 0.0,
                ExamDateTime = SightDataList[i].ExamDateTime,
              };
              if (SightDataList[i].EyeId == EyeType.right) {
                var.RExamID = SightDataList[i].ID;
                var.RSight = SightDataList[i].Sight;
              } else if (SightDataList[i].EyeId == EyeType.left) {
                var.LExamID = SightDataList[i].ID;
                var.LSight = SightDataList[i].Sight;
              }
              list.Add(var);
            }
          }
        } catch {
        }
      }
      return list;
    }

    public static ExamSightRec MakeSightRec(int examId, string posEye, NpgsqlConnection sqlConnection) {

      var recSight = new ExamSightRec();
      try {
        recSight.exam_id = examId;
        recSight.examtype_id = DBCommonController.Select_Examtype_ID(sqlConnection, DBConst.strMstDataType[DBConst.eMSTDATATYPE.SIGHT]);
        recSight.eye_id = DBCommonController.Select_Eye_ID(sqlConnection, posEye);
        recSight.device_id = DBCommonController.Select_Device_ID(sqlConnection, "AXM2");

        recSight.is_exam_data = true;
        recSight.sight_d = 0.0;

        recSight.measured_at = null;

        // 更新日、作成日は揃える
        var dateNow = DateTime.Now;
        recSight.updated_at = dateNow;
        recSight.created_at = dateNow;
      } catch {
      } finally {
      }
      return recSight;
    }

    public static bool Insert(ExamSightRec aExamSightRec, NpgsqlConnection sqlConnection) {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("insert into ");
      stringBuilder.Append(DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.AXM_EXAM_SIGHT]));
      string text = " (";
      string text2 = " (";
      for (int i = 0; i < COLNAME_ExamSightList.Count(); i++) {
        if (i != 0) {
          text += ",";
          text2 += ",";
        }

        text += DBCommonController._col(COLNAME_ExamSightList[i]);
        text2 += DBCommonController._bind(COLNAME_ExamSightList[i]);
      }

      text += ")";
      text2 += ")";
      stringBuilder.Append(text);
      stringBuilder.Append(" values ");
      stringBuilder.Append(text2);
      stringBuilder.Append(DBCommonController._onconflict("pk_exam_sight"));
      stringBuilder.Append(DBCommonController._doupdateexam(COLNAME_ExamSightList[(int)eExamSight.updated_at], DateTime.Now));
      stringBuilder.Append(DBCommonController._doupdatevalue(COLNAME_ExamSightList[(int)eExamSight.sight_d], aExamSightRec.sight_d.ToString()));
      stringBuilder.Append(";");
      int num = 0;
      using (NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection)) {
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamSightList[(int)eExamSight.exam_id], aExamSightRec.exam_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamSightList[(int)eExamSight.examtype_id], aExamSightRec.examtype_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamSightList[(int)eExamSight.eye_id], aExamSightRec.eye_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamSightList[(int)eExamSight.device_id], aExamSightRec.device_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamSightList[(int)eExamSight.is_exam_data], aExamSightRec.is_exam_data);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamSightList[(int)eExamSight.sight_d], aExamSightRec.sight_d);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamSightList[(int)eExamSight.measured_at], DBCommonController._DateTimeToObject(aExamSightRec.measured_at));
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamSightList[(int)eExamSight.updated_at], DBCommonController._DateTimeToObject(aExamSightRec.updated_at));
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamSightList[(int)eExamSight.created_at], DBCommonController._DateTimeToObject(aExamSightRec.created_at));
        num = npgsqlCommand.ExecuteNonQuery();
      }

      return num != 0;
    }

    public int delete_by_examId(int examId, NpgsqlConnection sqlConnection) {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("delete ");
      stringBuilder.Append("from ");
      stringBuilder.Append(DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.AXM_EXAM_SIGHT]));
      stringBuilder.Append("where ");
      stringBuilder.Append(DBCommonController._col(COLNAME_ExamSightList[(int)eExamSight.exam_id]));
      stringBuilder.Append("= ");
      stringBuilder.Append(DBCommonController._bind(COLNAME_ExamSightList[(int)eExamSight.exam_id]));
      stringBuilder.Append(";");
      using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
      npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamSightList[(int)eExamSight.exam_id], examId);
      return npgsqlCommand.ExecuteNonQuery();
    }

    public static string[] COLNAME_ExamSightList = new string[(int)eExamSight.MAX]
    {
      "exam_id", "examtype_id", "eye_id", "device_id", "is_exam_data", "sight_d", "measured_at", "updated_at", "created_at"
    };

    public enum eExamSight {
      exam_id = 0,
      examtype_id,
      eye_id,
      device_id,
      is_exam_data,
      sight_d,
      measured_at,
      updated_at,
      created_at,
      MAX
    }
  }
}

public class ExamSightRec {
  public int exam_id { get; set; }
  public int examtype_id { get; set; }
  public int eye_id { get; set; }
  public int device_id { get; set; }
  public bool is_exam_data { get; set; }
  public double sight_d { get; set; }
  public DateTime? measured_at { get; set; }
  public DateTime? updated_at { get; set; }
  public DateTime? created_at { get; set; }
}

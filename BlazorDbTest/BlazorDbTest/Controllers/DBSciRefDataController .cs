using AxialManagerS.Shared.Common;
using BlazorDbTest.Common;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;
using System.Text;
using static BlazorDbTest.Controllers.DBAxialDataController;

namespace BlazorDbTest.Controllers {

  [Route("api/[controller]")]

  public class DBSciRefDataController : ControllerBase {

    // レフ(自覚)測定値書込み
    [HttpPost("SetSciRef")]
    public void SetSciRef([FromBody] RefList conditions) {
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
                DBConst.strMstDataType[DBConst.eMSTDATATYPE.SCI_REF],
                DBConst.eEyeType.RIGHT,
                conditions.ExamDateTime,
                sqlConnection);
            // EXAM_Refに保存(右眼測定値)
            var rec_Ref_r = MakeRefRec(exam_id_r,
                DBConst.strEyeType[DBConst.eEyeType.RIGHT],
                sqlConnection);
            rec_Ref_r.s_d = conditions.RS_d;
            rec_Ref_r.c_d = conditions.RC_d;
            rec_Ref_r.a_deg = conditions.RA_deg;
            rec_Ref_r.se_d = (conditions.RS_d + (conditions.RC_d / 2));
            rec_Ref_r.is_exam_data = (conditions.RS_d != null && conditions.RC_d != null && conditions.RA_deg != null);
            rec_Ref_r.measured_at = conditions.ExamDateTime;

            // DB登録
            result = Insert(rec_Ref_r, sqlConnection);

            // EXAM_LISTに保存(左眼測定値)
            var exam_id_l = DBCommonController.RegisterExamList(uuid,
                DBConst.strMstDataType[DBConst.eMSTDATATYPE.SCI_REF],
                DBConst.eEyeType.LEFT,
                conditions.ExamDateTime,
                sqlConnection);
            // EXAM_Refに保存(左眼測定値)
            var rec_Ref_l = MakeRefRec(exam_id_l,
                DBConst.strEyeType[DBConst.eEyeType.LEFT],
                sqlConnection);
            rec_Ref_l.s_d = conditions.LS_d;
            rec_Ref_l.c_d = conditions.LC_d;
            rec_Ref_l.a_deg = conditions.LA_deg;
            rec_Ref_l.se_d = (conditions.LS_d + (conditions.LC_d / 2));
            rec_Ref_l.is_exam_data = (conditions.LS_d != null && conditions.LC_d != null && conditions.LA_deg != null);
            rec_Ref_l.measured_at = conditions.ExamDateTime;

            // DB登録
            result &= Insert(rec_Ref_l, sqlConnection);
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

    // レフ(他覚)測定値書込み
    [HttpGet("GetSciRefList/{patientId}")]
    public List<RefList> GetSciRefList(string patientId) {
      List<RefList> DataSource = new();
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
          int axmId = DBCommonController.Select_Device_ID(sqlConnection, DBConst.AxmDeviceType);

          // 実行するクエリコマンド定義
          string Query = "SELECT * FROM ";
          Query += DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_SCI_REF]);
          Query += " WHERE ";
          Query += " EXISTS( ";
          Query += "SELECT * FROM ";
          Query += DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_LIST]);
          Query += " WHERE ";
          Query += DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_SCI_REF]);
          Query += ".";
          Query += DBCommonController._col(COLNAME_ExamSciRefList[(int)eExamSciRef.exam_id]);
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
          Query += DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_SCI_REF]);
          Query += ".";
          Query += DBCommonController._col(COLNAME_ExamSciRefList[(int)eExamSciRef.is_exam_data]);
          Query += " = ";
          Query += DBCommonController._val("TRUE");
          Query += " )";
          Query += " ORDER BY ";
          Query += DBCommonController._col(COLNAME_ExamSciRefList[(int)eExamSciRef.measured_at]);
          Query += " ASC; ";

          NpgsqlCommand Command = new(Query, sqlConnection);
          NpgsqlDataAdapter DataAdapter = new(Command);
          DataTable DataTable = new();
          DataAdapter.Fill(DataTable);
          List<SciRefData> RefDataSource = new();

          RefDataSource = (from DataRow data in DataTable.Rows
                           select new SciRefData() {
                             ID = data[COLNAME_ExamSciRefList[(int)eExamSciRef.exam_id]].ToString() ?? string.Empty,
                             S_d = Convert.ToDouble(data[COLNAME_ExamSciRefList[(int)eExamSciRef.s_d]]),
                             C_d = Convert.ToDouble(data[COLNAME_ExamSciRefList[(int)eExamSciRef.c_d]]),
                             A_deg = Convert.ToInt16(data[COLNAME_ExamSciRefList[(int)eExamSciRef.a_deg]]),
                             SE_d = Convert.ToDouble(data[COLNAME_ExamSciRefList[(int)eExamSciRef.se_d]]),
                             EyeId = (EyeType)Enum.ToObject(typeof(EyeType), data[COLNAME_ExamSciRefList[(int)eExamSciRef.eye_id]]),
                             IsExamData = (bool)data[COLNAME_ExamSciRefList[(int)eExamSciRef.is_exam_data]],
                             DeviceId = axmId, 
                             ExamDateTime = (DateTime)data[COLNAME_ExamSciRefList[(int)eExamSciRef.measured_at]],
                           }).ToList();

          DataSource = SetSciRefList(patientId, RefDataSource.ToArray(), sqlConnection);
        }
      } catch {
      } finally {
        // PostgreSQL Server 通信切断
        dbAccess.CloseSqlConnection();
      }

      return DataSource;
    }

    // レフ(自覚)測定値削除
    [HttpPost("DeleteSciRefData")]
    public void DeleteSciRefData([FromBody]int examId) {
      try {
        DBAccess dbAccess = DBAccess.GetInstance();

        bool result = false;

        try {
          // PostgreSQL Server 通信接続
          NpgsqlConnection sqlConnection = dbAccess.GetSqlConnection();

          // クエリコマンド実行
          // EXAM_SCIREFテーブルからから削除
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

    // レフ(自覚)測定値移動
    [HttpPost("MoveSciRefData")]
    public void MoveSciRefData([FromBody] MoveExamData conditions) {
      try {
        if (conditions == null) return;
        if (conditions.ChangePatientID == null || conditions.ChangePatientID == string.Empty) return;

        bool result = false;
        DBAccess dbAccess = DBAccess.GetInstance();

        try {
          // PostgreSQL Server 通信接続
          NpgsqlConnection sqlConnection = dbAccess.GetSqlConnection();

          // クエリコマンド実行
          // UUIDの有無を確認(true:update / false:insert)
          var uuid = DBCommonController.Select_PTUUID_by_PTID(sqlConnection, conditions.ChangePatientID);
          if (uuid == string.Empty) {
            // uuidが無ければ、被検者を新規登録
            DBPatientInfoController.InsertPatientId(sqlConnection, conditions.ChangePatientID);
            uuid = DBCommonController.Select_PTUUID_by_PTID(sqlConnection, conditions.ChangePatientID);
          }

          if (uuid != string.Empty) {
            // EXAM_LISTの被検者IDを変更
            if (conditions.RExamID != null && conditions.RExamID != string.Empty) {
              result &= DBCommonController.MoveExamData(sqlConnection, uuid, conditions.RExamID);
            }

            if (conditions.LExamID != null && conditions.LExamID != string.Empty) {
              result &= DBCommonController.MoveExamData(sqlConnection, uuid, conditions.LExamID);
            }
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
    /// <param name="SciRefDataList"></param>
    public List<RefList> SetSciRefList(string pt_id, SciRefData[] SciRefDataList, NpgsqlConnection sqlConnection) {
      List<RefList> list = new List<RefList>();
      if (SciRefDataList != null) {

        int axmId = DBCommonController.Select_Device_ID(sqlConnection, DBConst.AxmDeviceType);

        try {
          for (int i = 0; i < SciRefDataList.Length; i++) {
            bool isExist = false;
            for (int j = 0; j < list.Count; j++) {
              if (DBCommonController._objectToDateOnly(list[j].ExamDateTime)
                  == DBCommonController._objectToDateOnly(SciRefDataList[i].ExamDateTime)) {

                if (SciRefDataList[i].EyeId == EyeType.right) {
                  // 装置種別AXMのデータを優先する
                  // 装置種別AXMのデータは、1測定日に1つしか登録できない
                  if (!list[j].IsRManualInput) {
                    if (list[j].RS_d == null) {
                      // 右眼かつ同じ測定日の右眼がnullのとき
                      list[j].RExamID = SciRefDataList[i].ID;
                      list[j].RS_d = SciRefDataList[i].S_d;
                      list[j].RC_d = SciRefDataList[i].C_d;
                      list[j].RA_deg = SciRefDataList[i].A_deg;
                      list[j].RSE_d = SciRefDataList[i].SE_d;
                      list[j].IsRManualInput = (SciRefDataList[i].DeviceId == axmId);
                      isExist = true;
                      break;
                    } else if (list[j].ExamDateTime < SciRefDataList[i].ExamDateTime) {
                      // 右眼かつ同じ測定時間が新しい
                      list[j].RExamID = SciRefDataList[i].ID;
                      list[j].RS_d = SciRefDataList[i].S_d;
                      list[j].RC_d = SciRefDataList[i].C_d;
                      list[j].RA_deg = SciRefDataList[i].A_deg;
                      list[j].RSE_d = SciRefDataList[i].SE_d;
                      list[j].IsRManualInput = (SciRefDataList[i].DeviceId == axmId);
                      list[j].ExamDateTime = SciRefDataList[i].ExamDateTime;
                      isExist = true;
                      break;
                    }
                  }
                } else if (SciRefDataList[i].EyeId == EyeType.left) {
                  if (!list[j].IsLManualInput) {
                    if (list[j].LS_d == null) {
                      // 左眼かつ同じ測定日の左眼が0のとき
                      list[j].LExamID = SciRefDataList[i].ID;
                      list[j].LS_d = SciRefDataList[i].S_d;
                      list[j].LC_d = SciRefDataList[i].C_d;
                      list[j].LA_deg = SciRefDataList[i].A_deg;
                      list[j].LSE_d = SciRefDataList[i].SE_d;
                      list[j].IsLManualInput = (SciRefDataList[i].DeviceId == axmId);
                      isExist = true;
                      break;
                    } else if (list[j].ExamDateTime < SciRefDataList[i].ExamDateTime) {
                      // 左眼かつ同じ測定時間が新しい
                      list[j].LExamID = SciRefDataList[i].ID;
                      list[j].LS_d = SciRefDataList[i].S_d;
                      list[j].LC_d = SciRefDataList[i].C_d;
                      list[j].LA_deg = SciRefDataList[i].A_deg;
                      list[j].LSE_d = SciRefDataList[i].SE_d;
                      list[j].IsLManualInput = (SciRefDataList[i].DeviceId == axmId);
                      list[j].ExamDateTime = SciRefDataList[i].ExamDateTime;
                      isExist = true;
                      break;
                    }
                  }
                }
              }
            }

            // 同じ測定日のデータがないとき追加
            if (!isExist) {
              RefList var = new RefList() {
                PatientID = pt_id,
                RExamID = string.Empty,
                LExamID = string.Empty,
                RS_d = null,
                RC_d = null,
                RA_deg = null,
                RSE_d = null,
                LS_d = null,
                LC_d = null,
                LA_deg = null,
                LSE_d = null,
                ExamDateTime = SciRefDataList[i].ExamDateTime,
                IsRManualInput = false,
                IsLManualInput = false,
              };
              if (SciRefDataList[i].EyeId == EyeType.right) {
                var.RExamID = SciRefDataList[i].ID;
                var.RS_d = SciRefDataList[i].S_d;
                var.RC_d = SciRefDataList[i].C_d;
                var.RA_deg = SciRefDataList[i].A_deg;
                var.RSE_d = SciRefDataList[i].SE_d;
                var.IsRManualInput = (SciRefDataList[i].DeviceId == axmId);
              } else if (SciRefDataList[i].EyeId == EyeType.left) {
                var.LExamID = SciRefDataList[i].ID;
                var.LS_d = SciRefDataList[i].S_d;
                var.LC_d = SciRefDataList[i].C_d;
                var.LA_deg = SciRefDataList[i].A_deg;
                var.LSE_d = SciRefDataList[i].SE_d;
                var.IsLManualInput = (SciRefDataList[i].DeviceId == axmId);
              }
              list.Add(var);
            }
          }
        } catch {
        }
      }
      return list;
    }

    public static ExamSciRefRec MakeRefRec(int examId, string posEye, NpgsqlConnection sqlConnection) {

      var recRef = new ExamSciRefRec();
      try {
        recRef.exam_id = examId;
        recRef.examtype_id = DBCommonController.Select_Examtype_ID(sqlConnection, DBConst.strMstDataType[DBConst.eMSTDATATYPE.SCI_REF]);
        recRef.eye_id = DBCommonController.Select_Eye_ID(sqlConnection, posEye);
        recRef.device_id = DBCommonController.Select_Device_ID(sqlConnection, DBConst.AxmDeviceType);

        recRef.is_exam_data = true;
        recRef.comment = "";

        recRef.s_d = 0;
        recRef.c_d = 0;
        recRef.a_deg = 0;
        recRef.se_d = 0;

        recRef.measured_at = null;

        // 更新日、作成日は揃える
        var dateNow = DateTime.Now;
        recRef.updated_at = dateNow;
        recRef.created_at = dateNow;
      } catch {
      } finally {
      }
      return recRef;
    }

    public static bool Insert(ExamSciRefRec aExamSciRefRec, NpgsqlConnection sqlConnection) {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("insert into ");
      stringBuilder.Append(DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_SCI_REF]));
      string text = " (";
      string text2 = " (";
      for (int i = 0; i < COLNAME_ExamSciRefList.Count(); i++) {
        if (i != 0) {
          text += ",";
          text2 += ",";
        }

        text += DBCommonController._col(COLNAME_ExamSciRefList[i]);
        text2 += DBCommonController._bind(COLNAME_ExamSciRefList[i]);
      }

      text += ")";
      text2 += ")";
      stringBuilder.Append(text);
      stringBuilder.Append(" values ");
      stringBuilder.Append(text2);
      stringBuilder.Append(DBCommonController._onconflict("pk_exam_sciref"));
      stringBuilder.Append(DBCommonController._doupdateexam(COLNAME_ExamSciRefList[(int)eExamSciRef.updated_at], DateTime.Now));
      stringBuilder.Append(DBCommonController._doupdatevalue(COLNAME_ExamSciRefList[(int)eExamSciRef.s_d], aExamSciRefRec.s_d.ToString()));
      stringBuilder.Append(DBCommonController._doupdatevalue(COLNAME_ExamSciRefList[(int)eExamSciRef.c_d], aExamSciRefRec.c_d.ToString()));
      stringBuilder.Append(DBCommonController._doupdatevalue(COLNAME_ExamSciRefList[(int)eExamSciRef.a_deg], aExamSciRefRec.a_deg.ToString()));
      stringBuilder.Append(DBCommonController._doupdatevalue(COLNAME_ExamSciRefList[(int)eExamSciRef.se_d], aExamSciRefRec.se_d.ToString()));
      stringBuilder.Append(DBCommonController._doupdatevalue(COLNAME_ExamSciRefList[(int)eExamSciRef.is_exam_data], aExamSciRefRec.is_exam_data.ToString()));
      stringBuilder.Append(";");
      int num = 0;
      using (NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection)) {
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamSciRefList[(int)eExamSciRef.exam_id], aExamSciRefRec.exam_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamSciRefList[(int)eExamSciRef.examtype_id], aExamSciRefRec.examtype_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamSciRefList[(int)eExamSciRef.eye_id], aExamSciRefRec.eye_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamSciRefList[(int)eExamSciRef.device_id], aExamSciRefRec.device_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamSciRefList[(int)eExamSciRef.is_exam_data], aExamSciRefRec.is_exam_data);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamSciRefList[(int)eExamSciRef.comment], aExamSciRefRec.comment);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamSciRefList[(int)eExamSciRef.s_d], aExamSciRefRec.s_d);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamSciRefList[(int)eExamSciRef.c_d], aExamSciRefRec.c_d);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamSciRefList[(int)eExamSciRef.a_deg], aExamSciRefRec.a_deg);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamSciRefList[(int)eExamSciRef.se_d], aExamSciRefRec.se_d);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamSciRefList[(int)eExamSciRef.measured_at], DBCommonController._DateTimeToObject(aExamSciRefRec.measured_at));
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamSciRefList[(int)eExamSciRef.updated_at], DBCommonController._DateTimeToObject(aExamSciRefRec.updated_at));
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamSciRefList[(int)eExamSciRef.created_at], DBCommonController._DateTimeToObject(aExamSciRefRec.created_at));
        num = npgsqlCommand.ExecuteNonQuery();
      }

      return num != 0;
    }

    public int delete_by_examId(int examId, NpgsqlConnection sqlConnection) {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("delete ");
      stringBuilder.Append("from ");
      stringBuilder.Append(DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_SCI_REF]));
      stringBuilder.Append("where ");
      stringBuilder.Append(DBCommonController._col(COLNAME_ExamSciRefList[(int)eExamSciRef.exam_id]));
      stringBuilder.Append("= ");
      stringBuilder.Append(DBCommonController._bind(COLNAME_ExamSciRefList[(int)eExamSciRef.exam_id]));
      stringBuilder.Append(";");
      using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
      npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamSciRefList[(int)eExamSciRef.exam_id], examId);
      return npgsqlCommand.ExecuteNonQuery();
    }

    // todo: 誤字修正
    public static string[] COLNAME_ExamSciRefList = new string[(int)eExamSciRef.MAX]
    {
      "exam_id", "examtype_id", "eye_id", "device_id", "is_exam_data", "comment", "s_d", "c_d", "a_deg", "se_d", "measured_at", "updated_at", "created_at"
    };

    public enum eExamSciRef {
      exam_id = 0,
      examtype_id,
      eye_id,
      device_id,
      is_exam_data,
      comment,
      s_d,
      c_d,
      a_deg,
      se_d,
      measured_at,
      updated_at,
      created_at,
      MAX
    }
  }
}

public class ExamSciRefRec {
  public int? exam_id { get; set; }
  public int? examtype_id { get; set; }
  public int? eye_id { get; set; }
  public int? device_id { get; set; }
  public bool? is_exam_data { get; set; }
  public string? comment { get; set; }
  public double? s_d { get; set; }
  public double? c_d { get; set; }
  public int? a_deg { get; set; }
  public double? se_d { get; set; }
  public DateTime? measured_at { get; set; }
  public DateTime? updated_at { get; set; }
  public DateTime? created_at { get; set; }
}

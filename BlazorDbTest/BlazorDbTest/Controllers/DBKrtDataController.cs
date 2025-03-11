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

  public class DBKrtDataController : ControllerBase {

    // ケラト測定値書込み
      [HttpPost("SetKrt")]
      public void SetKrt([FromBody] KrtList conditions) {
        try {
        if (conditions == null) return;
        if (conditions.PatientID == null || conditions.PatientID == string.Empty) return;

        bool result = false;
        DBAccess dbAccess = DBAccess.GetInstance();

        try {
          // PostgreSQL Server 通信接続
          NpgsqlConnection sqlConnection = dbAccess.GetSqlConnection();

          // todo: 設定取得
          int selectId = DBCommonController.Select_SelectTypeID(sqlConnection, DBConst.SELECT_TYPE[(int)DBConst.SelectType.average]) - 1;

          // クエリコマンド実行
          // UUIDの有無を確認(true:update / false:insert)
          var uuid = DBCommonController.Select_PTUUID_by_PTID(sqlConnection, conditions.PatientID);
          if (uuid == string.Empty) {
            // AXMからの測定データ登録時は、必ず患者データが存在する
            return;
          } else {
            // EXAM_LISTに保存(右眼測定値)
            var exam_id_r = DBCommonController.RegisterExamList(uuid,
                DBConst.strMstDataType[DBConst.eMSTDATATYPE.KRT],
                DBConst.eEyeType.RIGHT,
                conditions.ExamDateTime,
                sqlConnection);
            // EXAM_KRTに保存(右眼測定値)
            var rec_krt_r = MakeKrtRec(exam_id_r,
                DBConst.strEyeType[DBConst.eEyeType.RIGHT],
                sqlConnection);
            rec_krt_r.k1_mm[selectId] = conditions.RK1_mm;
            rec_krt_r.k1_d[selectId] = conditions.RK1_d;
            rec_krt_r.k2_mm[selectId] = conditions.RK2_mm;
            rec_krt_r.k2_d[selectId] = conditions.RK2_d;
            rec_krt_r.avek_mm[selectId] = ((conditions.RK1_mm + conditions.RK2_mm) / 2);
            rec_krt_r.avek_d[selectId] = ((conditions.RK1_d + conditions.RK2_d) / 2);
            rec_krt_r.cyl_d[selectId] = conditions.RCyl_d;
            rec_krt_r.is_exam_data = ((conditions.RK1_mm != null && conditions.RK2_mm != null && conditions.RCyl_d != null)
              || (conditions.RK1_d != null && conditions.RK2_d != null && conditions.RCyl_d != null));
            rec_krt_r.measured_at = conditions.ExamDateTime;

            // DB登録
            result = Insert(rec_krt_r, sqlConnection);

            // EXAM_LISTに保存(左眼測定値)
            var exam_id_l = DBCommonController.RegisterExamList(uuid,
                DBConst.strMstDataType[DBConst.eMSTDATATYPE.KRT],
                DBConst.eEyeType.LEFT,
                conditions.ExamDateTime,
                sqlConnection);
            // EXAM_KRTに保存(左眼測定値)
            var rec_krt_l = MakeKrtRec(exam_id_l,
                DBConst.strEyeType[DBConst.eEyeType.LEFT],
                sqlConnection);
            rec_krt_l.k1_mm[selectId] = conditions.LK1_mm;
            rec_krt_l.k1_d[selectId] = conditions.LK1_d;
            rec_krt_l.k2_mm[selectId] = conditions.LK2_mm;
            rec_krt_l.k2_d[selectId] = conditions.LK2_d;
            rec_krt_l.avek_mm[selectId] = ((conditions.LK1_mm + conditions.LK2_mm) / 2);
            rec_krt_l.avek_d[selectId] = ((conditions.LK1_d + conditions.LK2_d) / 2);
            rec_krt_l.cyl_d[selectId] = conditions.LCyl_d;
            rec_krt_l.is_exam_data = ((conditions.LK1_mm != null && conditions.LK2_mm != null && conditions.LCyl_d != null)
              || (conditions.LK1_d != null && conditions.LK2_d != null && conditions.LCyl_d != null));
            rec_krt_l.measured_at = conditions.ExamDateTime;

            // DB登録
            result &= Insert(rec_krt_l, sqlConnection);
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

    // ケラト測定値書込み
    [HttpGet("GetKrtList/{patientId}")]
    public List<KrtList> GetKrtList(string patientId) {
      List<KrtList> DataSource = new();
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
          int deviceId = DBCommonController.Select_Device_ID(sqlConnection, DBConst.AxmDeviceType);

          // 実行するクエリコマンド定義
          string Query = "SELECT * FROM ";
          Query += DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_KRT]);
          Query += " WHERE ";
          Query += " EXISTS( ";
          Query += "SELECT * FROM ";
          Query += DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_LIST]);
          Query += " WHERE ";
          Query += DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_KRT]);
          Query += ".";
          Query += DBCommonController._col(COLNAME_ExamKrtList[(int)eExamKrt.exam_id]);
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
          Query += DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_KRT]);
          Query += ".";
          Query += DBCommonController._col(COLNAME_ExamKrtList[(int)eExamKrt.is_exam_data]);
          Query += " = ";
          Query += DBCommonController._val("TRUE");
          Query += " )";
          Query += " ORDER BY ";
          Query += DBCommonController._col(COLNAME_ExamKrtList[(int)eExamKrt.measured_at]);
          Query += " ASC; ";

          NpgsqlCommand Command = new(Query, sqlConnection);
          NpgsqlDataAdapter DataAdapter = new(Command);
          DataTable DataTable = new();
          DataAdapter.Fill(DataTable);
          List<KrtData> KrtDataSource = new();

          KrtDataSource = (from DataRow data in DataTable.Rows
                           select new KrtData() {
                             ID = data[COLNAME_ExamKrtList[(int)eExamKrt.exam_id]].ToString() ?? string.Empty,
                             K1_mm = DBCommonController._objectToDoubleList(data[COLNAME_ExamKrtList[(int)eExamKrt.k1_mm]]),
                             K1_d = DBCommonController._objectToDoubleList(data[COLNAME_ExamKrtList[(int)eExamKrt.k1_d]]),
                             K2_mm = DBCommonController._objectToDoubleList(data[COLNAME_ExamKrtList[(int)eExamKrt.k2_mm]]),
                             K2_d = DBCommonController._objectToDoubleList(data[COLNAME_ExamKrtList[(int)eExamKrt.k2_d]]),
                             AveK_mm = DBCommonController._objectToDoubleList(data[COLNAME_ExamKrtList[(int)eExamKrt.avek_mm]]),
                             AveK_d = DBCommonController._objectToDoubleList(data[COLNAME_ExamKrtList[(int)eExamKrt.avek_d]]),
                             Cyl_d = DBCommonController._objectToDoubleList(data[COLNAME_ExamKrtList[(int)eExamKrt.cyl_d]]),
                             EyeId = (EyeType)Enum.ToObject(typeof(EyeType), data[COLNAME_ExamKrtList[(int)eExamKrt.eye_id]]),
                             IsExamData = (bool)data[COLNAME_ExamKrtList[(int)eExamKrt.is_exam_data]],
                             DeviceID = deviceId, 
                             ExamDateTime = (DateTime)data[COLNAME_ExamKrtList[(int)eExamKrt.measured_at]],
                           }).ToList();

          DataSource = SetKrtList(patientId, KrtDataSource.ToArray(), sqlConnection);
        }
      } catch {
      } finally {
        // PostgreSQL Server 通信切断
        dbAccess.CloseSqlConnection();
      }

      return DataSource;
    }

    // ケラト測定値削除
    [HttpPost("DeleteKrtData")]
    public void DeleteKrtData([FromBody] int examId) {
      try {
        DBAccess dbAccess = DBAccess.GetInstance();

        bool result = false;

        try {
          // PostgreSQL Server 通信接続
          NpgsqlConnection sqlConnection = dbAccess.GetSqlConnection();

          // クエリコマンド実行
          // EXAM_KRTテーブルからから削除
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

    // ケラト測定値移動
    [HttpPost("MoveKrtData")]
    public void MoveKrtData([FromBody] MoveExamData conditions) {
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
    /// <param name="krtDataList"></param>
    public List<KrtList> SetKrtList(string pt_id, KrtData[] krtDataList, NpgsqlConnection sqlConnection) {
      List<KrtList> list = new List<KrtList>();
      if (krtDataList != null) {

        int deviceId = DBCommonController.Select_Device_ID(sqlConnection, DBConst.AxmDeviceType);
        // todo: 設定ファイルから取得
        int selectId = DBCommonController.Select_SelectTypeID(sqlConnection, DBConst.SELECT_TYPE[(int)DBConst.SelectType.average]) - 1;

        try {
          for (int i = 0; i < krtDataList.Length; i++) {
            bool isExist = false;
            for (int j = 0; j < list.Count; j++) {
              if (DBCommonController._objectToDateOnly(list[j].ExamDateTime)
                  == DBCommonController._objectToDateOnly(krtDataList[i].ExamDateTime)) {

                if (krtDataList[i].EyeId == EyeType.right) {
                  // 装置種別AXMのデータを優先する
                  // 装置種別AXMのデータは、1測定日に1つしか登録できない
                  if (!list[j].IsRManualInput) {
                    if (list[j].RK1_mm == null) {
                      // 右眼かつ同じ測定日の右眼が0のとき
                      list[j].RExamID = krtDataList[i].ID;
                      list[j].RK1_mm = krtDataList[i].K1_mm[selectId];
                      list[j].RK1_d = krtDataList[i].K1_d[selectId];
                      list[j].RK2_mm = krtDataList[i].K2_mm[selectId];
                      list[j].RK2_d = krtDataList[i].K2_d[selectId];
                      list[j].RAveK_mm = krtDataList[i].AveK_mm[selectId];
                      list[j].RAveK_d = krtDataList[i].AveK_d[selectId];
                      list[j].RCyl_d = krtDataList[i].Cyl_d[selectId];
                      list[j].IsRManualInput = (krtDataList[i].DeviceID == deviceId);
                      isExist = true;
                      break;
                    } else if (list[j].ExamDateTime < krtDataList[i].ExamDateTime) {
                      // 右眼かつ同じ測定時間が新しい
                      list[j].RExamID = krtDataList[i].ID;
                      list[j].RK1_mm = krtDataList[i].K1_mm[selectId];
                      list[j].RK1_d = krtDataList[i].K1_d[selectId];
                      list[j].RK2_mm = krtDataList[i].K2_mm[selectId];
                      list[j].RK2_d = krtDataList[i].K2_d[selectId];
                      list[j].RAveK_mm = krtDataList[i].AveK_mm[selectId];
                      list[j].RAveK_d = krtDataList[i].AveK_d[selectId];
                      list[j].RCyl_d = krtDataList[i].Cyl_d[selectId];
                      list[j].IsRManualInput = (krtDataList[i].DeviceID == deviceId);
                      list[j].ExamDateTime = krtDataList[i].ExamDateTime;
                      isExist = true;
                      break;
                    }
                  }
                } else if (krtDataList[i].EyeId == EyeType.left) {
                  if (!list[j].IsLManualInput) {
                    if (list[j].LK1_mm == null) {
                      // 左眼かつ同じ測定日の左眼が0のとき
                      list[j].LExamID = krtDataList[i].ID;
                      list[j].LK1_mm = krtDataList[i].K1_mm[selectId];
                      list[j].LK1_d = krtDataList[i].K1_d[selectId];
                      list[j].LK2_mm = krtDataList[i].K2_mm[selectId];
                      list[j].LK2_d = krtDataList[i].K2_d[selectId];
                      list[j].LAveK_mm = krtDataList[i].AveK_mm[selectId];
                      list[j].LAveK_d = krtDataList[i].AveK_d[selectId];
                      list[j].LCyl_d = krtDataList[i].Cyl_d[selectId];
                      list[j].IsLManualInput = (krtDataList[i].DeviceID == deviceId);
                      isExist = true;
                      break;
                    } else if (list[j].ExamDateTime < krtDataList[i].ExamDateTime) {
                      // 左眼かつ同じ測定時間が新しい
                      list[j].LExamID = krtDataList[i].ID;
                      list[j].LK1_mm = krtDataList[i].K1_mm[selectId];
                      list[j].LK1_d = krtDataList[i].K1_d[selectId];
                      list[j].LK2_mm = krtDataList[i].K2_mm[selectId];
                      list[j].LK2_d = krtDataList[i].K2_d[selectId];
                      list[j].LAveK_mm = krtDataList[i].AveK_mm[selectId];
                      list[j].LAveK_d = krtDataList[i].AveK_d[selectId];
                      list[j].LCyl_d = krtDataList[i].Cyl_d[selectId];
                      list[j].IsLManualInput = (krtDataList[i].DeviceID == deviceId);
                      list[j].ExamDateTime = krtDataList[i].ExamDateTime;
                      isExist = true;
                      break;
                    }
                  }
                }
              }
            }

            // 同じ測定日のデータがないとき追加
            if (!isExist) {
              KrtList var = new KrtList() {
                PatientID = pt_id,
                RExamID = string.Empty,
                LExamID = string.Empty,
                RK1_mm = null,
                RK1_d = null,
                RK2_mm = null,
                RK2_d = null,
                RAveK_mm = null,
                RAveK_d = null,
                RCyl_d = null,
                LK1_mm = null,
                LK1_d = null,
                LK2_mm = null,
                LK2_d = null,
                LAveK_mm = null,
                LAveK_d = null,
                LCyl_d = null,
                ExamDateTime = krtDataList[i].ExamDateTime,
                IsRManualInput = false,
                IsLManualInput = false,
              };
              if (krtDataList[i].EyeId == EyeType.right) {
                var.RExamID = krtDataList[i].ID;
                var.RK1_mm = krtDataList[i].K1_mm[selectId];
                var.RK1_d = krtDataList[i].K1_d[selectId];
                var.RK2_mm = krtDataList[i].K2_mm[selectId];
                var.RK2_d = krtDataList[i].K2_d[selectId];
                var.RAveK_mm = krtDataList[i].AveK_mm[selectId];
                var.RAveK_d = krtDataList[i].AveK_d[selectId];
                var.RCyl_d = krtDataList[i].Cyl_d[selectId];
                var.IsRManualInput = (krtDataList[i].DeviceID == deviceId);
              } else if (krtDataList[i].EyeId == EyeType.left) {
                var.LExamID = krtDataList[i].ID;
                var.LK1_mm = krtDataList[i].K1_mm[selectId];
                var.LK1_d = krtDataList[i].K1_d[selectId];
                var.LK2_mm = krtDataList[i].K2_mm[selectId];
                var.LK2_d = krtDataList[i].K2_d[selectId];
                var.LAveK_mm = krtDataList[i].AveK_mm[selectId];
                var.LAveK_d = krtDataList[i].AveK_d[selectId];
                var.LCyl_d = krtDataList[i].Cyl_d[selectId];
                var.IsLManualInput = (krtDataList[i].DeviceID == deviceId);
              }
              list.Add(var);
            }
          }
        } catch {
        }
      }
      return list;
    }

    public static ExamKrtRec MakeKrtRec(int examId, string posEye, NpgsqlConnection sqlConnection) {

      var recKrt = new ExamKrtRec();
      try {
        recKrt.exam_id = examId;
        recKrt.examtype_id = DBCommonController.Select_Examtype_ID(sqlConnection, DBConst.strMstDataType[DBConst.eMSTDATATYPE.KRT]);
        recKrt.eye_id = DBCommonController.Select_Eye_ID(sqlConnection, posEye);
        recKrt.device_id = DBCommonController.Select_Device_ID(sqlConnection, DBConst.AxmDeviceType);

        recKrt.is_exam_data = false; 
        recKrt.comment = ""; // タグが無いので空文字
        recKrt.select_id = DBCommonController.Select_SelectTypeID(sqlConnection, DBConst.SELECT_TYPE[(int)DBConst.SelectType.average]) - 1;

        // todo: 設定反映
        recKrt.phi_id = DBCommonController.Select_PhiId_By_PhiType(sqlConnection, DBConst.PHI_TYPE[(int)DBConst.PhiType.e3_0]);
        recKrt.is_meas_auto = false; // false固定でよい

        recKrt.k1_mm = new List<double?>() { 0, 0, 0 };
        recKrt.k1_d = new List<double?>() { 0, 0, 0 };
        recKrt.k1_axis_deg = new List<int?>() { 0, 0, 0 };
        recKrt.k2_mm = new List<double?>() { 0, 0, 0 };
        recKrt.k2_d = new List<double?>() { 0, 0, 0 };
        recKrt.k2_axis_deg = new List<int?>() { 0, 0, 0 };
        recKrt.avek_mm = new List<double?>() { 0, 0, 0 };
        recKrt.avek_d = new List<double?>() { 0, 0, 0 };
        recKrt.k_index = 0; // 0固定でよい
        recKrt.cyl_d = new List<double?>() { 0, 0, 0 };
        recKrt.axis_deg = new List<int?>() { 0, 0, 0 };
        recKrt.is_reliabiliy = false;
        recKrt.reliability = new List<string?>() { "", "", "" };
        recKrt.data_path = ""; // データパスが無いので空文字

        recKrt.measured_at = null;

        // 更新日、作成日は揃える
        var dateNow = DateTime.Now;
        recKrt.updated_at = dateNow;
        recKrt.created_at = dateNow;
      } catch {
      } finally {
      }
      return recKrt;
    }

    public static bool Insert(ExamKrtRec aExamKeratoRec, NpgsqlConnection sqlConnection) {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("insert into ");
      stringBuilder.Append(DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_KRT]));
      string text = " (";
      string text2 = " (";
      for (int i = 0; i < COLNAME_ExamKrtList.Count(); i++) {
        if (i != 0) {
          text += ",";
          text2 += ",";
        }

        text += DBCommonController._col(COLNAME_ExamKrtList[i]);
        text2 += DBCommonController._bind(COLNAME_ExamKrtList[i]);
      }

      text += ")";
      text2 += ")";
      stringBuilder.Append(text);
      stringBuilder.Append(" values ");
      stringBuilder.Append(text2);
      stringBuilder.Append(DBCommonController._onconflict("pk_exam_krt"));
      stringBuilder.Append(DBCommonController._doupdateexam(COLNAME_ExamKrtList[(int)eExamKrt.updated_at], DateTime.Now));
      stringBuilder.Append(DBCommonController._doupdatevalue(COLNAME_ExamKrtList[(int)eExamKrt.select_id], aExamKeratoRec.select_id.ToString()));
      stringBuilder.Append(DBCommonController._doupdatevalue(COLNAME_ExamKrtList[(int)eExamKrt.phi_id], aExamKeratoRec.phi_id.ToString()));
      stringBuilder.Append(DBCommonController._doupdatedoublelist(COLNAME_ExamKrtList[(int)eExamKrt.k1_mm], aExamKeratoRec.k1_mm));
      stringBuilder.Append(DBCommonController._doupdatedoublelist(COLNAME_ExamKrtList[(int)eExamKrt.k1_d], aExamKeratoRec.k1_d));
      stringBuilder.Append(DBCommonController._doupdatedoublelist(COLNAME_ExamKrtList[(int)eExamKrt.k2_mm], aExamKeratoRec.k2_mm));
      stringBuilder.Append(DBCommonController._doupdatedoublelist(COLNAME_ExamKrtList[(int)eExamKrt.k2_d], aExamKeratoRec.k2_d));
      stringBuilder.Append(DBCommonController._doupdatedoublelist(COLNAME_ExamKrtList[(int)eExamKrt.avek_mm], aExamKeratoRec.avek_mm));
      stringBuilder.Append(DBCommonController._doupdatedoublelist(COLNAME_ExamKrtList[(int)eExamKrt.avek_d], aExamKeratoRec.avek_d));
      stringBuilder.Append(DBCommonController._doupdatedoublelist(COLNAME_ExamKrtList[(int)eExamKrt.cyl_d], aExamKeratoRec.cyl_d));
      stringBuilder.Append(DBCommonController._doupdatevalue(COLNAME_ExamKrtList[(int)eExamKrt.is_exam_data], aExamKeratoRec.is_exam_data.ToString()));
      stringBuilder.Append(";");
      int num = 0;
      using (NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection)) {
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamKrtList[(int)eExamKrt.exam_id], aExamKeratoRec.exam_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamKrtList[(int)eExamKrt.examtype_id], aExamKeratoRec.examtype_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamKrtList[(int)eExamKrt.eye_id], aExamKeratoRec.eye_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamKrtList[(int)eExamKrt.device_id], aExamKeratoRec.device_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamKrtList[(int)eExamKrt.is_exam_data], aExamKeratoRec.is_exam_data);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamKrtList[(int)eExamKrt.comment], aExamKeratoRec.comment);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamKrtList[(int)eExamKrt.select_id], aExamKeratoRec.select_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamKrtList[(int)eExamKrt.phi_id], aExamKeratoRec.phi_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamKrtList[(int)eExamKrt.is_meas_auto], aExamKeratoRec.is_meas_auto);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamKrtList[(int)eExamKrt.k1_mm], aExamKeratoRec.k1_mm);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamKrtList[(int)eExamKrt.k1_d], aExamKeratoRec.k1_d);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamKrtList[(int)eExamKrt.k1_axis_deg], aExamKeratoRec.k1_axis_deg);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamKrtList[(int)eExamKrt.k2_mm], aExamKeratoRec.k2_mm);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamKrtList[(int)eExamKrt.k2_d], aExamKeratoRec.k2_d);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamKrtList[(int)eExamKrt.k2_axis_deg], aExamKeratoRec.k2_axis_deg);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamKrtList[(int)eExamKrt.avek_mm], aExamKeratoRec.avek_mm);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamKrtList[(int)eExamKrt.avek_d], aExamKeratoRec.avek_d);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamKrtList[(int)eExamKrt.k_index], aExamKeratoRec.k_index);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamKrtList[(int)eExamKrt.cyl_d], aExamKeratoRec.cyl_d);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamKrtList[(int)eExamKrt.axis_deg], aExamKeratoRec.axis_deg);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamKrtList[(int)eExamKrt.is_reliability], aExamKeratoRec.is_reliabiliy);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamKrtList[(int)eExamKrt.reliability], aExamKeratoRec.reliability);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamKrtList[(int)eExamKrt.data_path], aExamKeratoRec.data_path);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamKrtList[(int)eExamKrt.measured_at], DBCommonController._DateTimeToObject(aExamKeratoRec.measured_at));
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamKrtList[(int)eExamKrt.updated_at], DBCommonController._DateTimeToObject(aExamKeratoRec.updated_at));
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamKrtList[(int)eExamKrt.created_at], DBCommonController._DateTimeToObject(aExamKeratoRec.created_at));
        num = npgsqlCommand.ExecuteNonQuery();
      }

      return num != 0;
    }

    public int delete_by_examId(int examId, NpgsqlConnection sqlConnection) {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("delete ");
      stringBuilder.Append("from ");
      stringBuilder.Append(DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_KRT]));
      stringBuilder.Append("where ");
      stringBuilder.Append(DBCommonController._col(COLNAME_ExamKrtList[(int)eExamKrt.exam_id]));
      stringBuilder.Append("= ");
      stringBuilder.Append(DBCommonController._bind(COLNAME_ExamKrtList[(int)eExamKrt.exam_id]));
      stringBuilder.Append(";");
      using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
      npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamKrtList[(int)eExamKrt.exam_id], examId);
      return npgsqlCommand.ExecuteNonQuery();
    }

    // todo: 誤字修正
    public static string[] COLNAME_ExamKrtList = new string[(int)eExamKrt.MAX]
    {
      "exam_id", "examtype_id", "eye_id", "device_id", "is_exam_data", "comment", "select_id", "phi_id", "is_meas_auto", "k1_mm"
      , "k1_d", "k1_axis_deg", "k2_mm", "k2_d", "k2_axis_deg", "avek_mm", "avek_d", "k_index", "cyl_d", "axis_deg", "is_reliabillty"
      , "reliabillty", "data_path","measured_at", "updated_at", "created_at"
    };

    public enum eExamKrt {
      exam_id = 0,
      examtype_id,
      eye_id,
      device_id,
      is_exam_data,
      comment,
      select_id,
      phi_id,
      is_meas_auto,
      k1_mm,
      k1_d,
      k1_axis_deg,
      k2_mm,
      k2_d,
      k2_axis_deg,
      avek_mm,
      avek_d,
      k_index,
      cyl_d,
      axis_deg,
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

public class ExamKrtRec {
  public int? exam_id { get; set; }
  public int? examtype_id { get; set; }
  public int? eye_id { get; set; }
  public int? device_id { get; set; }
  public bool? is_exam_data { get; set; }
  public string? comment { get; set; }
  public int? select_id { get; set; }
  public int? phi_id { get; set; }
  public bool? is_meas_auto { get; set; }
  public List<double?> k1_mm { get; set; } = new List<double?>();
  public List<double?> k1_d { get; set; } = new List<double?>();
  public List<int?> k1_axis_deg { get; set; } = new List<int?>();
  public List<double?> k2_mm { get; set; } = new List<double?>();
  public List<double?> k2_d { get; set; } = new List<double?>();
  public List<int?> k2_axis_deg { get; set; } = new List<int?>();
  public List<double?> avek_mm { get; set; } = new List<double?>();
  public List<double?> avek_d { get; set; } = new List<double?>();
  public double? k_index { get; set; }
  public List<double?> cyl_d { get; set; } = new List<double?>();
  public List<int?> axis_deg { get; set; } = new List<int?>();
  public bool? is_reliabiliy { get; set; }
  public List<string?> reliability { get; set; } = new List<string?>();
  public string? data_path { get; set; }
  public DateTime? measured_at { get; set; }
  public DateTime? updated_at { get; set; }
  public DateTime? created_at { get; set; }
}

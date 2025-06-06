﻿using AxialManagerS.Shared.Common;
using BlazorDbTest.Common;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;
using System.Text;
using System.Text.Json;
using static BlazorDbTest.Controllers.DBAxialDataController;

namespace BlazorDbTest.Controllers {

  [Route("api/[controller]")]

  public class DBRefDataController : ControllerBase {

    // レフ(他覚)測定値書込み
    [HttpPost("SetRef")]
    public void SetRef([FromBody] RefList conditions) {
      try {
        if (conditions == null) return;
        if (conditions.PatientID == null || conditions.PatientID == string.Empty) return;

        bool result = true;
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
                DBConst.strMstDataType[DBConst.eMSTDATATYPE.REF],
                DBConst.eEyeType.RIGHT,
                conditions.ExamDateTime,
                sqlConnection);
            // EXAM_Refに保存(右眼測定値)
            var rec_Ref_r = MakeRefRec(exam_id_r,
                DBConst.strEyeType[DBConst.eEyeType.RIGHT],
                sqlConnection);
            rec_Ref_r.s_d[selectId] = conditions.RS_d;
            rec_Ref_r.c_d[selectId] = conditions.RC_d;
            rec_Ref_r.a_deg[selectId] = conditions.RA_deg;
            rec_Ref_r.se_d[selectId] = (conditions.RS_d + (conditions.RC_d / 2));
            rec_Ref_r.is_exam_data = (conditions.RS_d != null && conditions.RC_d != null && conditions.RA_deg != null);
            rec_Ref_r.measured_at = conditions.ExamDateTime;

            // DB登録
            if (rec_Ref_r.is_exam_data == true) {
              result = Insert(rec_Ref_r, sqlConnection);
            }

            // EXAM_LISTに保存(左眼測定値)
            var exam_id_l = DBCommonController.RegisterExamList(uuid,
                DBConst.strMstDataType[DBConst.eMSTDATATYPE.REF],
                DBConst.eEyeType.LEFT,
                conditions.ExamDateTime,
                sqlConnection);
            // EXAM_Refに保存(左眼測定値)
            var rec_Ref_l = MakeRefRec(exam_id_l,
                DBConst.strEyeType[DBConst.eEyeType.LEFT],
                sqlConnection);
            rec_Ref_l.s_d[selectId] = conditions.LS_d;
            rec_Ref_l.c_d[selectId] = conditions.LC_d;
            rec_Ref_l.a_deg[selectId] = conditions.LA_deg;
            rec_Ref_l.se_d[selectId] = (conditions.LS_d + (conditions.LC_d / 2));
            rec_Ref_l.is_exam_data = (conditions.LS_d != null && conditions.LC_d != null && conditions.LA_deg != null);
            rec_Ref_l.measured_at = conditions.ExamDateTime;

            // DB登録
            if (rec_Ref_l.is_exam_data == true) {
              result &= Insert(rec_Ref_l, sqlConnection);
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

    // レフ(他覚)測定値書込み
    [HttpGet("GetRefList/{patientId}")]
    public List<RefList> GetRefList(string patientId) {
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
          int axmOldId = DBCommonController.Select_Device_ID(sqlConnection, DBConst.AxmOldDeviceType);
          // todo: 設定取得
          int deviceId = DBCommonController.Select_Device_ID(sqlConnection, DBConst.REF_DEVICE_TYPE[0]);

          // 実行するクエリコマンド定義
          string Query = "SELECT * FROM ";
          Query += DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_REF]);
          Query += " WHERE ";
          Query += " EXISTS( ";
          Query += "SELECT * FROM ";
          Query += DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_LIST]);
          Query += " WHERE ";
          Query += DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_REF]);
          Query += ".";
          Query += DBCommonController._col(COLNAME_ExamRefList[(int)eExamRef.exam_id]);
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
          Query += DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_REF]);
          Query += ".";
          Query += DBCommonController._col(COLNAME_ExamRefList[(int)eExamRef.is_exam_data]);
          Query += " = ";
          Query += DBCommonController._val("TRUE");
          Query += " AND ";
          Query += "( ";
          Query += DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_REF]);
          Query += ".";
          Query += DBCommonController._col(COLNAME_ExamRefList[(int)eExamRef.device_id]);
          Query += " = ";
          Query += DBCommonController._val(axmId.ToString());
          Query += " OR ";
          Query += DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_REF]);
          Query += ".";
          Query += DBCommonController._col(COLNAME_ExamRefList[(int)eExamRef.device_id]);
          Query += " = ";
          Query += DBCommonController._val(axmOldId.ToString());
          Query += " OR ";
          Query += DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_REF]);
          Query += ".";
          Query += DBCommonController._col(COLNAME_ExamRefList[(int)eExamRef.device_id]);
          Query += " = ";
          Query += DBCommonController._val(deviceId.ToString());
          Query += " )";
          Query += " )";
          Query += " ORDER BY ";
          Query += DBCommonController._col(COLNAME_ExamRefList[(int)eExamRef.measured_at]);
          Query += " ASC; ";

          NpgsqlCommand Command = new(Query, sqlConnection);
          NpgsqlDataAdapter DataAdapter = new(Command);
          DataTable DataTable = new();
          DataAdapter.Fill(DataTable);
          List<RefData> RefDataSource = new();

          RefDataSource = (from DataRow data in DataTable.Rows
                           select new RefData() {
                             ID = data[COLNAME_ExamRefList[(int)eExamRef.exam_id]].ToString() ?? string.Empty,
                             S_d = DBCommonController._objectToDoubleList(data[COLNAME_ExamRefList[(int)eExamRef.s_d]]),
                             C_d = DBCommonController._objectToDoubleList(data[COLNAME_ExamRefList[(int)eExamRef.c_d]]),
                             A_deg = DBCommonController._objectToIntList(data[COLNAME_ExamRefList[(int)eExamRef.a_deg]]),
                             SE_d = DBCommonController._objectToDoubleList(data[COLNAME_ExamRefList[(int)eExamRef.se_d]]),
                             EyeId = (EyeType)Enum.ToObject(typeof(EyeType), data[COLNAME_ExamRefList[(int)eExamRef.eye_id]]),
                             IsExamData = (bool)data[COLNAME_ExamRefList[(int)eExamRef.is_exam_data]],
                             DeviceId = axmId, 
                             ExamDateTime = (DateTime)data[COLNAME_ExamRefList[(int)eExamRef.measured_at]],
                           }).ToList();

          DataSource = SetRefList(patientId, RefDataSource.ToArray(), sqlConnection);
        }
      } catch {
      } finally {
        // PostgreSQL Server 通信切断
        dbAccess.CloseSqlConnection();
      }

      return DataSource;
    }

    // レフ(他覚)測定値削除
    [HttpPost("DeleteRefData")]
    public void DeleteRefData([FromBody] int examId) {
      try {
        DBAccess dbAccess = DBAccess.GetInstance();

        bool result = false;

        try {
          // PostgreSQL Server 通信接続
          NpgsqlConnection sqlConnection = dbAccess.GetSqlConnection();

          // クエリコマンド実行
          // EXAM_REFテーブルからから削除
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

    // レフ(他覚)測定値移動
    [HttpPost("MoveRefData")]
    public void MoveRefData([FromBody] MoveExamData conditions) {
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
    /// <param name="RefDataList"></param>
    public List<RefList> SetRefList(string pt_id, RefData[] RefDataList, NpgsqlConnection sqlConnection) {
      List<RefList> list = new List<RefList>();
      if (RefDataList != null) {

        int axmId = DBCommonController.Select_Device_ID(sqlConnection, DBConst.AxmDeviceType);
        // todo: 設定ファイルから取得
        int selectId = DBCommonController.Select_SelectTypeID(sqlConnection, DBConst.SELECT_TYPE[(int)DBConst.SelectType.average]) - 1;

        try {
          for (int i = 0; i < RefDataList.Length; i++) {
            bool isExist = false;
            for (int j = 0; j < list.Count; j++) {
              if (DBCommonController._objectToDateOnly(list[j].ExamDateTime)
                  == DBCommonController._objectToDateOnly(RefDataList[i].ExamDateTime)) {

                if (RefDataList[i].EyeId == EyeType.right) {
                  // 装置種別AXMのデータを優先する
                  // 装置種別AXMのデータは、1測定日に1つしか登録できない
                  if (!list[j].IsRManualInput) {
                    if (list[j].RS_d == null) {
                      // 右眼かつ同じ測定日の右眼が0のとき
                      list[j].RExamID = RefDataList[i].ID;
                      list[j].RS_d = RefDataList[i].S_d[selectId];
                      list[j].RC_d = RefDataList[i].C_d[selectId];
                      list[j].RA_deg = RefDataList[i].A_deg[selectId];
                      list[j].RSE_d = RefDataList[i].SE_d[selectId];
                      list[j].IsRManualInput = (RefDataList[i].DeviceId == axmId);
                      isExist = true;
                      break;
                    } else if (list[j].ExamDateTime < RefDataList[i].ExamDateTime) {
                      // 右眼かつ同じ測定時間が新しい
                      list[j].RExamID = RefDataList[i].ID;
                      list[j].RS_d = RefDataList[i].S_d[selectId];
                      list[j].RC_d = RefDataList[i].C_d[selectId];
                      list[j].RA_deg = RefDataList[i].A_deg[selectId];
                      list[j].RSE_d = RefDataList[i].SE_d[selectId];
                      list[j].IsRManualInput = (RefDataList[i].DeviceId == axmId);
                      list[j].ExamDateTime = RefDataList[i].ExamDateTime;
                      isExist = true;
                      break;
                    }
                  }
                } else if (RefDataList[i].EyeId == EyeType.left) {
                  if (!list[j].IsLManualInput) {
                    if (list[j].LS_d == null) {
                      // 左眼かつ同じ測定日の左眼が0のとき
                      list[j].LExamID = RefDataList[i].ID;
                      list[j].LS_d = RefDataList[i].S_d[selectId];
                      list[j].LC_d = RefDataList[i].C_d[selectId];
                      list[j].LA_deg = RefDataList[i].A_deg[selectId];
                      list[j].LSE_d = RefDataList[i].SE_d[selectId];
                      list[j].IsLManualInput = (RefDataList[i].DeviceId == axmId);
                      isExist = true;
                      break;
                    } else if (list[j].ExamDateTime < RefDataList[i].ExamDateTime) {
                      // 左眼かつ同じ測定時間が新しい
                      list[j].LExamID = RefDataList[i].ID;
                      list[j].LS_d = RefDataList[i].S_d[selectId];
                      list[j].LC_d = RefDataList[i].C_d[selectId];
                      list[j].LA_deg = RefDataList[i].A_deg[selectId];
                      list[j].LSE_d = RefDataList[i].SE_d[selectId];
                      list[j].IsLManualInput = (RefDataList[i].DeviceId == axmId);
                      list[j].ExamDateTime = RefDataList[i].ExamDateTime;
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
                ExamDateTime = RefDataList[i].ExamDateTime,
                IsRManualInput = false,
                IsLManualInput = false,
              };
              if (RefDataList[i].EyeId == EyeType.right) {
                var.RExamID = RefDataList[i].ID;
                var.RS_d = RefDataList[i].S_d[selectId];
                var.RC_d = RefDataList[i].C_d[selectId];
                var.RA_deg = RefDataList[i].A_deg[selectId];
                var.RSE_d = RefDataList[i].SE_d[selectId];
                var.IsRManualInput = (RefDataList[i].DeviceId == axmId);
              } else if (RefDataList[i].EyeId == EyeType.left) {
                var.LExamID = RefDataList[i].ID;
                var.LS_d = RefDataList[i].S_d[selectId];
                var.LC_d = RefDataList[i].C_d[selectId];
                var.LA_deg = RefDataList[i].A_deg[selectId];
                var.LSE_d = RefDataList[i].SE_d[selectId];
                var.IsLManualInput = (RefDataList[i].DeviceId == axmId);
              }
              list.Add(var);
            }
          }
        } catch {
        }
      }
      return list;
    }

    public static ExamRefRec MakeRefRec(int examId, string posEye, NpgsqlConnection sqlConnection) {

      var recRef = new ExamRefRec();
      try {
        recRef.exam_id = examId;
        recRef.examtype_id = DBCommonController.Select_Examtype_ID(sqlConnection, DBConst.strMstDataType[DBConst.eMSTDATATYPE.REF]);
        recRef.eye_id = DBCommonController.Select_Eye_ID(sqlConnection, posEye);
        recRef.device_id = DBCommonController.Select_Device_ID(sqlConnection, DBConst.AxmDeviceType);

        recRef.is_exam_data = false;
        recRef.comment = ""; // タグが無いので空文字
        recRef.select_id = DBCommonController.Select_SelectTypeID(sqlConnection, DBConst.SELECT_TYPE[(int)DBConst.SelectType.average]) - 1;

        recRef.is_meas_auto = false; // false固定でよい

        recRef.is_human_eye_correction = false;
        recRef.s_d = new List<double?>() { 0, 0, 0 };
        recRef.c_d = new List<double?>() { 0, 0, 0 };
        recRef.a_deg = new List<int?>() { 0, 0, 0 };
        recRef.se_d = new List<double?>() { 0, 0, 0 };
        recRef.vd_mm = 0;

        recRef.is_reliabiliy = false;
        recRef.reliability = new List<string?>() { "", "", "" };
        recRef.data_path = ""; // データパスが無いので空文字

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

    public static bool Insert(ExamRefRec aExamRefRec, NpgsqlConnection sqlConnection) {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("insert into ");
      stringBuilder.Append(DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_REF]));
      string text = " (";
      string text2 = " (";
      for (int i = 0; i < COLNAME_ExamRefList.Count(); i++) {
        if (i != 0) {
          text += ",";
          text2 += ",";
        }

        text += DBCommonController._col(COLNAME_ExamRefList[i]);
        text2 += DBCommonController._bind(COLNAME_ExamRefList[i]);
      }

      text += ")";
      text2 += ")";
      stringBuilder.Append(text);
      stringBuilder.Append(" values ");
      stringBuilder.Append(text2);
      stringBuilder.Append(DBCommonController._onconflict("pk_exam_ref"));
      stringBuilder.Append(DBCommonController._doupdateexam(COLNAME_ExamRefList[(int)eExamRef.updated_at], DateTime.Now));
      stringBuilder.Append(DBCommonController._doupdatevalue(COLNAME_ExamRefList[(int)eExamRef.select_id], aExamRefRec.select_id.ToString()));
      stringBuilder.Append(DBCommonController._doupdatedoublelist(COLNAME_ExamRefList[(int)eExamRef.s_d], aExamRefRec.s_d));
      stringBuilder.Append(DBCommonController._doupdatedoublelist(COLNAME_ExamRefList[(int)eExamRef.c_d], aExamRefRec.c_d));
      stringBuilder.Append(DBCommonController._doupdateintlist(COLNAME_ExamRefList[(int)eExamRef.a_deg], aExamRefRec.a_deg));
      stringBuilder.Append(DBCommonController._doupdatedoublelist(COLNAME_ExamRefList[(int)eExamRef.se_d], aExamRefRec.se_d));
      stringBuilder.Append(DBCommonController._doupdatevalue(COLNAME_ExamRefList[(int)eExamRef.is_exam_data], aExamRefRec.is_exam_data.ToString()));
      stringBuilder.Append(";");
      int num = 0;
      using (NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection)) {
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamRefList[(int)eExamRef.exam_id], aExamRefRec.exam_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamRefList[(int)eExamRef.examtype_id], aExamRefRec.examtype_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamRefList[(int)eExamRef.eye_id], aExamRefRec.eye_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamRefList[(int)eExamRef.device_id], aExamRefRec.device_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamRefList[(int)eExamRef.is_exam_data], aExamRefRec.is_exam_data);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamRefList[(int)eExamRef.comment], aExamRefRec.comment);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamRefList[(int)eExamRef.select_id], aExamRefRec.select_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamRefList[(int)eExamRef.is_meas_auto], aExamRefRec.is_meas_auto);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamRefList[(int)eExamRef.is_human_eye_correction], aExamRefRec.is_human_eye_correction);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamRefList[(int)eExamRef.s_d], aExamRefRec.s_d);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamRefList[(int)eExamRef.c_d], aExamRefRec.c_d);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamRefList[(int)eExamRef.a_deg], aExamRefRec.a_deg);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamRefList[(int)eExamRef.se_d], aExamRefRec.se_d);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamRefList[(int)eExamRef.vd_mm], aExamRefRec.vd_mm);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamRefList[(int)eExamRef.is_reliability], aExamRefRec.is_reliabiliy);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamRefList[(int)eExamRef.reliability], aExamRefRec.reliability);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamRefList[(int)eExamRef.data_path], aExamRefRec.data_path);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamRefList[(int)eExamRef.measured_at], DBCommonController._DateTimeToObject(aExamRefRec.measured_at));
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamRefList[(int)eExamRef.updated_at], DBCommonController._DateTimeToObject(aExamRefRec.updated_at));
        npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamRefList[(int)eExamRef.created_at], DBCommonController._DateTimeToObject(aExamRefRec.created_at));
        num = npgsqlCommand.ExecuteNonQuery();
      }

      return num != 0;
    }

    public int delete_by_examId(int examId, NpgsqlConnection sqlConnection) {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("delete ");
      stringBuilder.Append("from ");
      stringBuilder.Append(DBCommonController._table(DBCommonController.DB_TableNames[(int)DBCommonController.eDbTable.EXAM_REF]));
      stringBuilder.Append("where ");
      stringBuilder.Append(DBCommonController._col(COLNAME_ExamRefList[(int)eExamRef.exam_id]));
      stringBuilder.Append("= ");
      stringBuilder.Append(DBCommonController._bind(COLNAME_ExamRefList[(int)eExamRef.exam_id]));
      stringBuilder.Append(";");
      using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
      npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamRefList[(int)eExamRef.exam_id], examId);
      return npgsqlCommand.ExecuteNonQuery();
    }

    // todo: 誤字修正
    public static string[] COLNAME_ExamRefList = new string[(int)eExamRef.MAX]
    {
      "exam_id", "examtype_id", "eye_id", "device_id", "is_exam_data", "comment", "select_id", "is_meas_auto"
      ,"is_human_eye_correction", "s_d", "c_d", "a_deg", "se_d", "vd_mm", "is_reliabillty"
      , "reliabillty", "data_path","measured_at", "updated_at", "created_at"
    };

    public enum eExamRef {
      exam_id = 0,
      examtype_id,
      eye_id,
      device_id,
      is_exam_data,
      comment,
      select_id,
      is_meas_auto,
      is_human_eye_correction,
      s_d,
      c_d,
      a_deg,
      se_d,
      vd_mm,
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

public class ExamRefRec {
  public int? exam_id { get; set; }
  public int? examtype_id { get; set; }
  public int? eye_id { get; set; }
  public int? device_id { get; set; }
  public bool? is_exam_data { get; set; }
  public string? comment { get; set; }
  public int? select_id { get; set; }
  public bool? is_meas_auto { get; set; }
  public bool? is_human_eye_correction { get; set; }
  public List<double?> s_d { get; set; } = new List<double?>();
  public List<double?> c_d { get; set; } = new List<double?>();
  public List<int?> a_deg { get; set; } = new List<int?>();
  public List<double?> se_d { get; set; } = new List<double?>();
  public double? vd_mm { get; set; }
  public bool? is_reliabiliy { get; set; }
  public List<string?> reliability { get; set; } = new List<string?>();
  public string? data_path { get; set; }
  public DateTime? measured_at { get; set; }
  public DateTime? updated_at { get; set; }
  public DateTime? created_at { get; set; }
}

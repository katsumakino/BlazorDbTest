using AxialManagerS.Shared.Common;
using BlazorDbTest.Common;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;
using System.Text;
using System.Text.Json;
using static BlazorDbTest.Controllers.DBCommonController;
using static BlazorDbTest.Controllers.DBAxmCommentController;
using static BlazorDbTest.Controllers.DBAxialDataController;
using static BlazorDbTest.Controllers.DBTreatmentController;

namespace BlazorDbTest.Controllers {

  [Route("api/[controller]")]

  public class DBPatientInfoController : ControllerBase {

    // 患者情報書込み
    [HttpGet("SetPatientInfo/{conditions}")]
    public void SetPatientInfo(string conditions) {
      try {
        if (conditions == null || conditions == string.Empty) return;

        PatientInfo patientInfo = JsonSerializer.Deserialize<PatientInfo>(conditions);
        if (patientInfo == null) return;

        if (patientInfo.ID == null || patientInfo.ID == string.Empty) return;

        bool result = false;
        DBAccess dbAccess = DBAccess.GetInstance();

        try {
          // PostgreSQL Server 通信接続
          NpgsqlConnection sqlConnection = dbAccess.GetSqlConnection();

          // UUIDの有無を確認(true:update / false:insert)
          var uuid = Select_PTUUID_by_PTID(sqlConnection, patientInfo.ID);
          if (uuid == string.Empty) {
            // Insert
            DateTime dateTime = DateTime.Now;
            PatientRec patientRec = new() {
              pt_id = patientInfo.ID,
              pt_lastname = patientInfo.FamilyName,
              pt_firstname = patientInfo.FirstName,
              gender_id = Select_GenderId(sqlConnection, GENDER_TYPE[(int)patientInfo.Gender]),
              pt_dob = patientInfo.BirthDate,
              pt_description = string.Empty,
              pt_updated_at = dateTime,
              pt_created_at = dateTime
            };

            result = Insert(sqlConnection, patientRec);

            // AXM用患者情報テーブルにも登録
            uuid = Select_PTUUID_by_PTID(sqlConnection, patientInfo.ID);
            if (uuid != string.Empty) {
              AxmPatientRec axmPatientRec = new() {
                pt_uuid = uuid,
                axm_pt_id = SelectMaxAxmPatientId(sqlConnection),
                axm_flag = patientInfo.Mark,
                is_axm_same_pt_id = (patientInfo.SameID != string.Empty),
                axm_same_pt_id = patientInfo.SameID,
                updated_at = dateTime,
                created_at = dateTime
              };

              var axm_pt_id_ = Select_AxmPatientID_by_PK(sqlConnection, uuid);
              if (axm_pt_id_ != -1) axmPatientRec.axm_pt_id = axm_pt_id_;

              result &= InsertAxmPatient(sqlConnection, axmPatientRec);
            }
          } else {
            // Update
            // 装置出力データ取込時は、入力あり→なしにはしない(アプリ上での編集時は可能)
            DateTime dateTime = DateTime.Now;
            PatientRec patientRec = new() {
              pt_uuid = uuid,
              pt_id = patientInfo.ID,
              pt_lastname = patientInfo.FamilyName,
              pt_firstname = patientInfo.FirstName,
              gender_id = Select_GenderId(sqlConnection, GENDER_TYPE[(int)patientInfo.Gender]),
              pt_dob = patientInfo.BirthDate,
              pt_updated_at = dateTime
            };

            result = Update(sqlConnection, patientRec);

            // AXM用患者情報テーブルにも更新
            AxmPatientRec axmPatientRec = new() {
              pt_uuid = uuid,
              axm_pt_id = SelectMaxAxmPatientId(sqlConnection),
              axm_flag = patientInfo.Mark,
              is_axm_same_pt_id = (patientInfo.SameID != string.Empty),
              axm_same_pt_id = patientInfo.SameID,
              updated_at = dateTime,
              created_at = dateTime
            };

            var axm_pt_id_ = Select_AxmPatientID_by_PK(sqlConnection, uuid);
            if (axm_pt_id_ != -1) axmPatientRec.axm_pt_id = axm_pt_id_;

            result &= InsertAxmPatient(sqlConnection, axmPatientRec);
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

    // 患者情報取得
    [HttpGet("GetPatientInfo/{uuid}")]
    public PatientInfo GetDBPatientInfo(string uuid) {
      DBAccess dbAccess = DBAccess.GetInstance();

      PatientInfo DataSource = new();
      if (uuid == null || uuid == string.Empty) return DataSource;

      try {
        // PostgreSQL Server 通信接続
        NpgsqlConnection sqlConnection = dbAccess.GetSqlConnection();

        // 実行するクエリコマンド定義
        string Query = "SELECT * FROM ";
        Query += _table(DB_TableNames[(int)eDbTable.PATIENT_LIST]);
        Query += " WHERE ";
        Query += _col(COLNAME_PatientList[(int)ePatientList.pt_uuid]);
        Query += " = ";
        Query += _val(uuid);

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
          DataSource = new PatientInfo() {
            Mark = false,
            ID = data[COLNAME_PatientList[(int)ePatientList.pt_id]].ToString() ?? string.Empty,
            FamilyName = data[COLNAME_PatientList[(int)ePatientList.pt_lastname]].ToString() ?? string.Empty,
            FirstName = data[COLNAME_PatientList[(int)ePatientList.pt_firstname]].ToString() ?? string.Empty,
            Gender = (Gender)Enum.ToObject(typeof(Gender), data[COLNAME_PatientList[(int)ePatientList.gender_id]]),
            Age = GetAge(_objectToDateTime(data[COLNAME_PatientList[(int)ePatientList.pt_dob]]), DateTime.Today),
            BirthDate = _objectToDateTime(data[COLNAME_PatientList[(int)ePatientList.pt_dob]]),
            SameID = default!       // todo: 別Tableから取得
          };
        }
      } catch {
      } finally {
        // PostgreSQL Server 通信切断
        dbAccess.CloseSqlConnection();
      }

      return DataSource;
    }

    [HttpGet("GetPatientInfoList")]
    public List<PatientInfo> GetDBPatientInfoList() {
      DBAccess dbAccess = DBAccess.GetInstance();

      // 実行するクエリコマンド定義
      string Query = "SELECT * FROM ";
      Query += _table(DB_TableNames[(int)eDbTable.PATIENT_LIST]);
      Query += " ORDER BY ";
      Query += _col(COLNAME_PatientList[(int)ePatientList.updated_at]);

      List<PatientInfo> DataSource = new();

      try {
        // PostgreSQL Server 通信接続
        NpgsqlConnection sqlConnection = dbAccess.GetSqlConnection();

        //Using NpgsqlCommand and Query create connection with database
        NpgsqlCommand Command = new(Query, sqlConnection);
        //Using NpgsqlDataAdapter execute the NpgsqlCommand 
        NpgsqlDataAdapter DataAdapter = new(Command);
        DataTable DataTable = new();
        DataAdapter.Fill(DataTable);

        // Cast the data fetched from NpgsqlDataAdapter to List<T>
        DataSource = (from DataRow data in DataTable.Rows
                      select new PatientInfo() {
                        ID = data[COLNAME_PatientList[(int)ePatientList.pt_id]].ToString() ?? string.Empty,
                        FamilyName = data[COLNAME_PatientList[(int)ePatientList.pt_lastname]].ToString() ?? string.Empty,
                        FirstName = data[COLNAME_PatientList[(int)ePatientList.pt_firstname]].ToString() ?? string.Empty,
                        Gender = (Gender)Enum.ToObject(typeof(Gender), data[COLNAME_PatientList[(int)ePatientList.gender_id]]),
                        Age = GetAge(_objectToDateTime(data[COLNAME_PatientList[(int)ePatientList.pt_dob]]), DateTime.Today),
                        BirthDate = _objectToDateTime(data[COLNAME_PatientList[(int)ePatientList.pt_dob]]),
                        SameID = default!       // todo: 別Tableから取得
                      }).ToList();
      } catch {
      } finally {
        // PostgreSQL Server 通信切断
        dbAccess.CloseSqlConnection();
      }

      return DataSource;
    }

    [HttpGet("GetSearchPatientList/{conditions}")]
    public List<PatientList> GetSearchPatientList(string conditions) {
      List<PatientList> DataSource = new();

      try {
        if (conditions == null || conditions == string.Empty) return DataSource;
        PatientSearch patientSearch = JsonSerializer.Deserialize<PatientSearch>(conditions);

        if (patientSearch == null) return DataSource;

        patientSearch.IdOrName = DBAccessCommon.CheckConvertString(patientSearch.IdOrName);
        patientSearch.PatientComment = DBAccessCommon.CheckConvertString(patientSearch.PatientComment);

        DBAccess dbAccess = DBAccess.GetInstance();
        try {
          // PostgreSQL Server 通信接続
          NpgsqlConnection sqlConnection = dbAccess.GetSqlConnection();

          int eye_id_r = Select_Eye_ID(sqlConnection, DBConst.strEyeType[DBConst.eEyeType.RIGHT]);
          int eye_id_l = Select_Eye_ID(sqlConnection, DBConst.strEyeType[DBConst.eEyeType.LEFT]);
          int commenttype_patient = Select_AxmCommentTypeId(sqlConnection, AXM_COMMENT_TYPE[(int)eAxmCommentType.Patient]);
          int device_axm_id = Select_Device_ID(sqlConnection, "AxialManager2");
          int exam_optaxial_id = Select_Examtype_ID(sqlConnection, DBConst.strMstDataType[DBConst.eMSTDATATYPE.OPTAXIAL]);

          var tblPatientList = "tblPatientList";          // PatientList
          var tblAxmPatientList = "tblAxmPatientList";    // AxmPatientList
          var tblExamList = "tblExamList";                // ExamList
          var tblAxmCommentList = "tblAxmCommentList";    // AxmCommentList

          // クエリコマンド実行
          string Query1 = "WITH RankedExams AS (SELECT *,RANK() OVER (PARTITION BY {0} ORDER BY {1} DESC) AS rank FROM {2}) ";
          string Query = string.Format(Query1, _col(COLNAME_ExamList[(int)eExamList.pt_uuid])
              , _col(COLNAME_ExamList[(int)eExamList.measured_at])
              , _table(DB_TableNames[(int)eDbTable.EXAM_LIST]));
          string Query2 = "SELECT DISTINCT ON({0}{1}) ";
          Query += string.Format(Query2, tblPatientList, _dotcol(COLNAME_PatientList[(int)ePatientList.pt_uuid]));
          for (int i = (int)ePatientList.pt_uuid; i < (int)eSearchPatientList.MAX; i++) {
            switch (i) {
              case (int)eSearchPatientList.pt_uuid:
              case (int)eSearchPatientList.pt_id:
              case (int)eSearchPatientList.pt_lastname:
              case (int)eSearchPatientList.pt_firstname:
              case (int)eSearchPatientList.gender_id:
              case (int)eSearchPatientList.pt_dob:
                Query += tblPatientList;
                break;
              case (int)eSearchPatientList.axm_flag:
              case (int)eSearchPatientList.axm_same_pt_id:
                Query += tblAxmPatientList;
                break;
              case (int)eSearchPatientList.exam_datetime:
              case (int)eSearchPatientList.examtype_id:
                Query += tblExamList;
                break;
              case (int)eSearchPatientList.description:
                Query += tblAxmCommentList;
                break;
              default:
                continue;
            }
            Query += _dotcol(COLNAME_SearchPatientList[i]);
            if (i != (int)eSearchPatientList.MAX - 1) Query += ", ";
          }
          string Query3 = " LEFT JOIN {0} {1} ON {2}{3} = {1}{4}";
          Query += " FROM (";
          Query += "(";
          Query += _table(DB_TableNames[(int)eDbTable.PATIENT_LIST]);
          Query += " ";
          Query += tblPatientList;
          Query += string.Format(Query3, _table(DB_TableNames[(int)eDbTable.AXM_PATIENT_LIST]), tblAxmPatientList
                                     , tblPatientList, _dotcol(COLNAME_PatientList[(int)ePatientList.pt_uuid])
                                     , _dotcol(COLNAME_AxmPatientList[(int)eAxmPatientList.pt_uuid]));
          Query += ") ";
          Query += string.Format(Query3, "RankedExams", tblExamList
                                       , tblPatientList, _dotcol(COLNAME_PatientList[(int)ePatientList.pt_uuid])
                                       , _dotcol(COLNAME_ExamList[(int)eExamList.pt_uuid]));
          Query += " AND ";
          Query += tblExamList;
          Query += _dotcol("rank");
          Query += " = 1) ";
          Query += string.Format(Query3, _table(DB_TableNames[(int)eDbTable.AXM_COMMENT]), tblAxmCommentList
                                       , tblPatientList, _dotcol(COLNAME_PatientList[(int)ePatientList.pt_uuid])
                                       , _dotcol(COLNAME_AxmCommentList[(int)eAxmComment.pt_uuid]));
          // 検索条件設定
          Query += " WHERE (";
          // 患者コメントを表示
          string Query4 = "({0}{1} = {2} OR {0}{1} IS NULL) ";
          Query += string.Format(Query4, tblAxmCommentList, _dotcol(COLNAME_AxmCommentList[(int)eAxmComment.commenttype_id]), commenttype_patient);
          // 検査タイプは、OptAxialのみ
          string Query5 = "AND ({0}{1} = {2})";
          if (patientSearch.IsExamDate == true) {
            Query += string.Format(Query5, tblExamList, _dotcol(COLNAME_ExamList[(int)eExamList.examtype_id]), exam_optaxial_id);
          } else {
            // 測定日時を指定しないときは、NULLも含める
            // todo: 装置種別も確認
            Query += "AND ";
            Query += string.Format(Query4, tblExamList, _dotcol(COLNAME_ExamList[(int)eExamList.examtype_id]), exam_optaxial_id);
          }
          // ID/名前の曖昧一致検索
          string Query6 = "{0}{1} LIKE '%{2}%'";
          if (patientSearch.IdOrName != string.Empty && patientSearch.IdOrName != null) {
            Query += "AND (";
            for (int i = (int)ePatientList.pt_id; i <= (int)ePatientList.pt_firstname; i++) {
              Query += string.Format(Query6, tblPatientList, _dotcol(COLNAME_PatientList[i]), patientSearch.IdOrName);
              if (i != (int)ePatientList.pt_firstname) Query += " OR ";
            }
            Query += ")";
          }
          // 性別検索
          if ((patientSearch.Gender == Gender.male
              || patientSearch.Gender == Gender.female)) {
            Query += string.Format(Query5, tblPatientList, _dotcol(COLNAME_PatientList[(int)ePatientList.gender_id]), (int)patientSearch.Gender);
          }
          // 年齢範囲検索
          string Query7 = "AND ({0}{1} BETWEEN '{2}' AND '{3}')";
          if (patientSearch.IsAge == true) {
            Query += string.Format(Query7, tblPatientList, _dotcol(COLNAME_PatientList[(int)ePatientList.pt_dob])
                                         , CalculateBirthDateFromAge(patientSearch.AgeMax, true), CalculateBirthDateFromAge(patientSearch.AgeMin));
          }
          // 最新測定日範囲検索
          if (patientSearch.IsExamDate == true) {
            Query += string.Format(Query7, tblExamList, _dotcol(COLNAME_ExamList[(int)eExamList.measured_at])
                                         , (patientSearch.ExamDateMin != null) ? patientSearch.ExamDateMin : DateTime.Today
                                         , (patientSearch.ExamDateMax != null) ? patientSearch.ExamDateMax : DateTime.Today);
          }
          // 患者コメント曖昧一致検索
          if (patientSearch.PatientComment != string.Empty && patientSearch.PatientComment != null) {
            Query += "AND ";
            Query += string.Format(Query6, tblAxmCommentList, _dotcol(COLNAME_AxmCommentList[(int)eAxmComment.description]), patientSearch.PatientComment);
          }
          // フラグ検索
          if (patientSearch.IsMark == true) {
            Query += string.Format(Query5, tblAxmPatientList, _dotcol(COLNAME_AxmPatientList[(int)eAxmPatientList.axm_flag]), patientSearch.IsMark);
          }
          // 同一患者ID有無検索
          if (patientSearch.IsSameID == true) {
            Query += string.Format(Query5, tblAxmPatientList, _dotcol(COLNAME_AxmPatientList[(int)eAxmPatientList.is_axm_same_pt_id]), patientSearch.IsSameID);
          }
          // ExamListの検索条件を使用するときのみ、装置情報の検索条件に付与
          if (patientSearch.IsExamDate == true || patientSearch.IsAxial == true) {
            Query += string.Format(Query5, tblExamList, _dotcol(COLNAME_ExamList[(int)eExamList.device_id]), device_axm_id);
          }
          // todo: 表示設定の反映
          Query += ");";

          NpgsqlCommand Command = new(Query, sqlConnection);
          NpgsqlDataAdapter DataAdapter = new(Command);
          DataTable DataTable = new();
          DataAdapter.Fill(DataTable);

          for (int i = 0; i < DataTable.Rows.Count; i++) {
            DataRow data = DataTable.Rows[i];

            string pt_id = data[(int)eSearchPatientList.pt_id].ToString() ?? string.Empty;
            DateOnly examdate = _objectToDateOnly(data[(int)eSearchPatientList.exam_datetime]);

            if (pt_id != null) {
              string pt_uuid = data[(int)eSearchPatientList.pt_uuid].ToString() ?? string.Empty;
              double axial_r = -1;
              double axial_l = -1;
              double axialMin = (patientSearch.IsAxial) ? patientSearch.AxialMin : 0;
              double axialMax = (patientSearch.IsAxial) ? patientSearch.AxialMax : 40;
              string allTreatName = string.Empty;

              if (pt_uuid != null && pt_uuid != string.Empty) {
                // 最新測定日の測定値取得
                axial_r = GetLatestAxialData(pt_uuid, eye_id_r, examdate, axialMin, axialMax, sqlConnection);
                axial_l = GetLatestAxialData(pt_uuid, eye_id_l, examdate, axialMin, axialMax, sqlConnection);

                // 眼軸長検索条件ありのとき、両眼測定値無しなら、リストに追加しない
                if (axial_r < 0 && axial_l < 0 && patientSearch.IsAxial == true) {
                  continue;
                }

                // 使用した治療方法の文字列取得
                allTreatName = GetTreatmentListString(pt_uuid, patientSearch.TreatmentType, patientSearch.TreatmentTypeCount, sqlConnection);

                // 治療方法の検索条件ありのとき、治療方法がない場合、リストに追加しない
                if (allTreatName == string.Empty && patientSearch.TreatmentTypeCount > 0) {
                  continue;
                }

                PatientList list =
                    new PatientList() {
                      PatientInfo = new PatientInfo() {
                        ID = pt_id,
                        FamilyName = data[(int)eSearchPatientList.pt_lastname].ToString() ?? string.Empty,
                        FirstName = data[(int)eSearchPatientList.pt_firstname].ToString() ?? string.Empty,
                        Gender = (Gender)Enum.ToObject(typeof(Gender), data[(int)eSearchPatientList.gender_id]),
                        Age = GetAge(_objectToDateTime(data[(int)eSearchPatientList.pt_dob]), DateTime.Today),
                        BirthDate = _objectToDateTime(data[(int)eSearchPatientList.pt_dob]),
                        Mark = (data[(int)eSearchPatientList.axm_flag] != DBNull.Value) && (bool)data[(int)eSearchPatientList.axm_flag],
                        SameID = data[(int)eSearchPatientList.axm_same_pt_id].ToString() ?? string.Empty,
                        UUID = pt_uuid
                      },
                      LatestPicDate = _objectToDateTime(data[(int)eSearchPatientList.exam_datetime]),
                      LatestRAxial = axial_r,
                      LatestLAxial = axial_l,
                      PatientComment = data[(int)eSearchPatientList.description].ToString() ?? string.Empty,
                      AllTreatName = allTreatName
                    };

                DataSource.Add(list);
              }
            }
          }

        } catch {
        } finally {
          // PostgreSQL Server 通信切断
          dbAccess.CloseSqlConnection();
        }
      } catch {

      }

      return DataSource;
    }

    // 主キー重複時Update
    private bool Insert(NpgsqlConnection sqlConnection, PatientRec aPatientRec) {
      int num = 0;

      StringBuilder stringBuilder = new();
      stringBuilder.Append("insert into ");
      stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.PATIENT_LIST]));
      string text = " (";
      string text2 = " (";
      for (int i = 1; i < COLNAME_PatientList.Count(); i++) {
        if (i != 1) {
          text += ",";
          text2 += ",";
        }

        text += _col(COLNAME_PatientList[i]);
        text2 += _bind(COLNAME_PatientList[i]);
      }

      text += ")";
      text2 += ")";
      stringBuilder.Append(text);
      stringBuilder.Append(" values ");
      stringBuilder.Append(text2);
      stringBuilder.Append(";");

      using (NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection)) {
        npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)ePatientList.pt_id], aPatientRec.pt_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)ePatientList.pt_lastname], aPatientRec.pt_lastname);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)ePatientList.pt_firstname], aPatientRec.pt_firstname);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)ePatientList.gender_id], aPatientRec.gender_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)ePatientList.pt_dob], _DateTimeToObject(aPatientRec.pt_dob));
        npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)ePatientList.pt_description], aPatientRec.pt_description);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)ePatientList.updated_at], _DateTimeToObject(aPatientRec.pt_updated_at));
        npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)ePatientList.created_at], _DateTimeToObject(aPatientRec.pt_created_at));
        num = npgsqlCommand.ExecuteNonQuery();
      }

      return num != 0;
    }

    private bool Update(NpgsqlConnection sqlConnection, PatientRec aPatientRec) {
      int num = 0;
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("update ");
      stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.PATIENT_LIST]));
      stringBuilder.Append("set ");
      string text = "";
      for (int i = 1; i < (int)ePatientList.MAX; i++) {
        // コメントおよび作成日時は、アプリ上から更新されない
        if (i != (int)ePatientList.pt_description && i != (int)ePatientList.created_at) {
          text = text + _col(COLNAME_PatientList[i]) + "= " + _bind(COLNAME_PatientList[i]);
          if (i != (int)ePatientList.updated_at) {
            text += ",";
          }
        }
      }

      stringBuilder.Append(text);
      stringBuilder.Append(" where ");
      stringBuilder.Append(_col(COLNAME_PatientList[(int)ePatientList.pt_uuid]));
      stringBuilder.Append("= ");
      stringBuilder.Append(_val(aPatientRec.pt_uuid));
      stringBuilder.Append(";");
      using (NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection)) {
        npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)ePatientList.pt_id], aPatientRec.pt_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)ePatientList.pt_lastname], aPatientRec.pt_lastname);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)ePatientList.pt_firstname], aPatientRec.pt_firstname);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)ePatientList.gender_id], aPatientRec.gender_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)ePatientList.pt_dob], _DateTimeToObject(aPatientRec.pt_dob));
        npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)ePatientList.updated_at], _DateTimeToObject(DateTime.Now));
        num = npgsqlCommand.ExecuteNonQuery();
      }

      return num != 0;
    }

    // 主キー重複時Update
    private bool InsertAxmPatient2(NpgsqlConnection sqlConnection, AxmPatientRec aPatientRec) {
      int num = 0;

      StringBuilder stringBuilder = new();
      stringBuilder.Append("insert into ");
      stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.AXM_PATIENT_LIST]));
      string text = " (";
      string text2 = " (";
      for (int i = 1; i < COLNAME_AxmPatientList.Count(); i++) {
        if (i != 1) {
          text += ",";
          text2 += ",";
        }

        text += _col(COLNAME_AxmPatientList[i]);
        text2 += _bind(COLNAME_AxmPatientList[i]);
      }

      text += ")";
      text2 += ")";
      stringBuilder.Append(text);
      stringBuilder.Append(" values ");
      stringBuilder.Append(text2);
      stringBuilder.Append(";");

      using (NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection)) {
        npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.pt_uuid], aPatientRec.pt_uuid);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.axm_pt_id], aPatientRec.axm_pt_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.axm_flag], aPatientRec.axm_flag);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.is_axm_same_pt_id], aPatientRec.is_axm_same_pt_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.axm_same_pt_id], aPatientRec.axm_same_pt_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.updated_at], _DateTimeToObject(aPatientRec.updated_at));
        npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.created_at], _DateTimeToObject(aPatientRec.created_at));
        num = npgsqlCommand.ExecuteNonQuery();
      }

      return num != 0;
    }

    private bool UpdateAxmPatient(NpgsqlConnection sqlConnection, AxmPatientRec aPatientRec) {
      int num = 0;
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("update ");
      stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.AXM_PATIENT_LIST]));
      stringBuilder.Append("set ");
      string text = "";
      for (int i = 1; i < (int)eAxmPatientList.MAX; i++) {
        // 作成日時は、アプリ上から更新されない
        if (i != (int)eAxmPatientList.created_at) {
          text = text + _col(COLNAME_AxmPatientList[i]) + "= " + _bind(COLNAME_AxmPatientList[i]);
          if (i != (int)eAxmPatientList.updated_at) {
            text += ",";
          }
        }
      }

      stringBuilder.Append(text);
      stringBuilder.Append(" where ");
      stringBuilder.Append(_col(COLNAME_AxmPatientList[(int)ePatientList.pt_uuid]));
      stringBuilder.Append("= ");
      stringBuilder.Append(_val(aPatientRec.pt_uuid));
      stringBuilder.Append(";");
      using (NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection)) {
        npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.axm_pt_id], aPatientRec.axm_pt_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.axm_flag], aPatientRec.axm_flag);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.is_axm_same_pt_id], aPatientRec.is_axm_same_pt_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.axm_same_pt_id], aPatientRec.axm_same_pt_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_PatientList[(int)eAxmPatientList.updated_at], _DateTimeToObject(DateTime.Now));
        num = npgsqlCommand.ExecuteNonQuery();
      }

      return num != 0;
    }

    private static bool InsertAxmPatient(NpgsqlConnection sqlConnection, AxmPatientRec aPatientRec) {
      int num = 0;

      StringBuilder stringBuilder = new();
      stringBuilder.Append("insert into ");
      stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.AXM_PATIENT_LIST]));
      string text = " (";
      string text2 = " (";
      for (int i = 0; i < COLNAME_AxmPatientList.Count(); i++) {
        if (i != 0) {
          text += ",";
          text2 += ",";
        }

        text += _col(COLNAME_AxmPatientList[i]);
        text2 += _bind(COLNAME_AxmPatientList[i]);
      }

      text += ")";
      text2 += ")";
      stringBuilder.Append(text);
      stringBuilder.Append(" values ");
      stringBuilder.Append(text2);
      stringBuilder.Append(_onconflict("pk_axm_patient_list"));
      stringBuilder.Append(_doupdateexam(COLNAME_AxmPatientList[(int)eAxmPatientList.updated_at], DateTime.Now));
      stringBuilder.Append(";");

      using (NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection)) {
        npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.pt_uuid], Guid.Parse(aPatientRec.pt_uuid));
        npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.axm_pt_id], aPatientRec.axm_pt_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.axm_flag], aPatientRec.axm_flag);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.is_axm_same_pt_id], aPatientRec.is_axm_same_pt_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.axm_same_pt_id], aPatientRec.axm_same_pt_id);
        npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.updated_at], _DateTimeToObject(aPatientRec.updated_at));
        npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmPatientList[(int)eAxmPatientList.created_at], _DateTimeToObject(aPatientRec.created_at));
        num = npgsqlCommand.ExecuteNonQuery();
      }

      return num != 0;
    }

    // axm_pt_idの最大値取得
    public static int SelectMaxAxmPatientId(NpgsqlConnection sqlConnection) {
      int result = -1;
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("select ");
      stringBuilder.Append(_maxcol(COLNAME_AxmPatientList[(int)eAxmPatientList.axm_pt_id]));
      stringBuilder.Append("from ");
      stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.AXM_PATIENT_LIST]));
      stringBuilder.Append(";");
      using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
      using NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();
      while (npgsqlDataReader.Read()) {
        result = _objectToInt(npgsqlDataReader[0]);
      }

      return result != 0 ? result + 1 : 1;
    }

    public static int Select_AxmPatientID_by_PK(NpgsqlConnection sqlConnection, string pt_uuid) {
      int result = -1;
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("select ");
      stringBuilder.Append(_col(COLNAME_AxmPatientList[(int)eAxmPatientList.axm_pt_id]));
      stringBuilder.Append("from ");
      stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.AXM_PATIENT_LIST]));
      stringBuilder.Append("where ");
      stringBuilder.Append(_col(COLNAME_AxmPatientList[(int)eAxmPatientList.pt_uuid]));
      stringBuilder.Append("= ");
      stringBuilder.Append(_val(pt_uuid));
      stringBuilder.Append(";");
      using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
      using NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();
      while (npgsqlDataReader.Read()) {
        result = _objectToInt(npgsqlDataReader[0]);
      }

      return result;
    }

    public static string[] GENDER_TYPE = ["", "male", "female", "other"];

    public static string[] COLNAME_SearchPatientList = {
      COLNAME_PatientList[(int)ePatientList.pt_uuid],
      COLNAME_PatientList[(int)ePatientList.pt_id],
      COLNAME_PatientList[(int)ePatientList.pt_lastname],
      COLNAME_PatientList[(int)ePatientList.pt_firstname],
      COLNAME_PatientList[(int)ePatientList.gender_id],
      COLNAME_PatientList[(int)ePatientList.pt_dob],
      COLNAME_AxmPatientList[(int)eAxmPatientList.axm_flag],
      COLNAME_AxmPatientList[(int)eAxmPatientList.axm_same_pt_id],
      COLNAME_ExamList[(int)eExamList.measured_at],
      COLNAME_AxmCommentList[(int)eAxmComment.description],
      COLNAME_ExamList[(int)eExamList.examtype_id]
    };
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

public class AxmPatientRec {
  public string pt_uuid { get; set; } = "";
  public int axm_pt_id { get; set; }
  public bool axm_flag { get; set; }
  public bool is_axm_same_pt_id { get; set; }
  public string axm_same_pt_id { get; set; } = "";
  public DateTime? updated_at { get; set; }
  public DateTime? created_at { get; set; }
}

public enum eSearchPatientList {
  pt_uuid = 0,
  pt_id,
  pt_lastname,
  pt_firstname,
  gender_id,
  pt_dob,
  axm_flag,
  axm_same_pt_id,
  exam_datetime,
  description,
  examtype_id,
  MAX
}

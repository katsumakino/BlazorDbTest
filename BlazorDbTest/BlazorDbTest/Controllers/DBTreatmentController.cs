using AxialManagerS.Shared.Common;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;
using System.Text;
using System.Text.Json;
using static BlazorDbTest.Controllers.DBCommonController;

namespace BlazorDbTest.Controllers {

  [Route("api/[controller]")]

  public class DBTreatmentController : ControllerBase {

    // 治療方法設定登録
    [HttpPost("SetTreatmentMethod")]
    public void SetTreatmentMethod([FromBody] TreatmentMethodSetting conditions) {
      try {
        if (conditions == null) return;

        if (conditions.TreatName == null || conditions.TreatName == string.Empty) return;
        if (conditions.RGBAColor == null) return;

        bool result = false;
        DBAccess dbAccess = DBAccess.GetInstance();

        try {
          // PostgreSQL Server 通信接続
          NpgsqlConnection sqlConnection = dbAccess.GetSqlConnection();

          // クエリコマンド実行

          // IDが登録済みであるか確認
          var type_id = Select_TreatmentTypeId_By_TreatmentInfo(sqlConnection, conditions.ID);
          if (type_id == -1) {
            // 新規登録なら、ID割り当て
            type_id = SelectMaxTreatmentTypeId(sqlConnection);
          }

          // 更新日、作成日は揃える
          var dateNow = DateTime.Now;

          // DB登録
          result = InsertTreatmentInfo(new TreatmentInfoRec {
            treatmenttype_id = type_id,
            treatment_name = conditions.TreatName,
            color_r = conditions.RGBAColor.R,
            color_g = conditions.RGBAColor.G,
            color_b = conditions.RGBAColor.B,
            color_a = conditions.RGBAColor.A,
            suppression_rate = conditions.SuppresionRate,
            created_at = dateNow,
            updated_at = dateNow
          }, sqlConnection);

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

    // 治療方法データ取得
    [HttpGet("GetTreatmentMethodList")]
    public List<TreatmentMethodSetting> GetDBTreatmentMethodList() {
      DBAccess dbAccess = DBAccess.GetInstance();

      // 実行するクエリコマンド定義
      string Query = "SELECT * FROM ";
      Query += _table(DB_TableNames[(int)eDbTable.AXM_TREATMENT_INFO]);
      Query += " ORDER BY ";
      Query += _col(COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.treatmenttype_id]);

      List<TreatmentMethodSetting> DataSource = new();

      try {
        // PostgreSQL Server 通信接続
        NpgsqlConnection sqlConnection = dbAccess.GetSqlConnection();

        //Using NpgsqlCommand and Query create connection with database
        NpgsqlCommand Command = new(Query, sqlConnection);
        //Using NpgsqlDataAdapter execute the NpgsqlCommand 
        NpgsqlDataAdapter DataAdapter = new(Command);
        DataTable DataTable = new();
        DataAdapter.Fill(DataTable);

        DataSource = (from DataRow data in DataTable.Rows
                      select new TreatmentMethodSetting() {
                        ID = _objectToInt(data[COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.treatmenttype_id]]),
                        TreatName = data[COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.treatment_name]].ToString() ?? string.Empty,
                        RGBAColor = new() {
                          R = _objectToInt(data[COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.color_r]]),
                          G = _objectToInt(data[COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.color_g]]),
                          B = _objectToInt(data[COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.color_b]]),
                          A = _objectToInt(data[COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.color_a]]),
                        },
                        SuppresionRate = _objectToInt(data[COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.suppression_rate]])
                      }).ToList();
      } catch {
      } finally {
        // PostgreSQL Server 通信切断
        dbAccess.CloseSqlConnection();
      }

      return DataSource;
    }

    // 治療状況登録

    [HttpPost("SetTreatment")]
    public void SetTreatment([FromBody] TreatmentDataRequest conditions) {
      try {
        if (conditions == null) return;
        if (conditions.PatientID == null || conditions.PatientID == string.Empty) return;

        if (conditions.TreatmentData == null) return;

        bool result = false;
        DBAccess dbAccess = DBAccess.GetInstance();

        try {
          // PostgreSQL Server 通信接続
          NpgsqlConnection sqlConnection = dbAccess.GetSqlConnection();

          // クエリコマンド実行

          // 治療方法IDが登録済みであるか確認
          var type_id = Select_TreatmentTypeId_By_TreatmentInfo(sqlConnection, conditions.TreatmentData.TreatID);
          // 治療方法IDが登録されていない場合は、エラーとして処理を終了する
          if (type_id == -1) {
            return;
          }

          // 患者UUID取得
          var uuid = Select_PTUUID_by_PTID(sqlConnection, conditions.PatientID);
          if (uuid == string.Empty) {
            // 治療状況登録時は、必ず患者データが存在する
            return;
          }

          // IDが登録済みであるか確認
          var treat_id = Select_TreatmentId_By_Treatment(sqlConnection, conditions.TreatmentData.ID);
          if (treat_id == -1) {
            // 新規登録なら、ID割り当て
            treat_id = SelectMaxTreatmentId(sqlConnection);
          }

          // 更新日、作成日は揃える
          var dateNow = DateTime.Now;

          // DB登録
          result = InsertTreatment(new TreatmentRec {
            treatment_id = treat_id,
            treatmenttype_id = type_id,
            pt_uuid = uuid,
            start_at = conditions.TreatmentData.StartDateTime,
            end_at = conditions.TreatmentData.EndDateTime,
            created_at = dateNow,
            updated_at = dateNow
          }, sqlConnection);

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

    // 治療方法データ取得
    [HttpGet("GetTreatmentList/{pt_id}")]
    public List<TreatmentData> GetDBTreatmentDataList(string pt_id) {
      List<TreatmentData> DataSource = new();
      if (pt_id == null || pt_id == string.Empty) return DataSource;

      DBAccess dbAccess = DBAccess.GetInstance();

      try {
        // PostgreSQL Server 通信接続
        NpgsqlConnection sqlConnection = dbAccess.GetSqlConnection();

        // クエリコマンド実行
        // UUIDの有無を確認
        var uuid = Select_PTUUID_by_PTID(sqlConnection, pt_id);
        if (uuid == string.Empty) {
          // 患者データが無ければ、測定データも存在しない
          return DataSource;
        } else {
          // 実行するクエリコマンド定義
          string Query = "SELECT * FROM ";
          Query += _table(DB_TableNames[(int)eDbTable.AXM_TREATMENT]);
          Query += " WHERE ";
          Query += _col(COLNAME_AxmTreatmentList[(int)eAxmTreatment.pt_uuid]);
          Query += " = ";
          Query += _val(uuid);
          Query += " ORDER BY ";
          Query += _col(COLNAME_AxmTreatmentList[(int)eAxmTreatment.treatment_id]);

          NpgsqlCommand Command = new(Query, sqlConnection);
          NpgsqlDataAdapter DataAdapter = new(Command);
          DataTable DataTable = new();
          DataAdapter.Fill(DataTable);

          DataSource = (from DataRow data in DataTable.Rows
                        select new TreatmentData() {
                          ID = _objectToInt(data[COLNAME_AxmTreatmentList[(int)eAxmTreatment.treatment_id]]),
                          TreatID = _objectToInt(data[COLNAME_AxmTreatmentList[(int)eAxmTreatment.treatmenttype_id]]),
                          StartDateTime = _objectToDateTime(data[COLNAME_AxmTreatmentList[(int)eAxmTreatment.start_at]]),
                          EndDateTime = _objectToDateTime(data[COLNAME_AxmTreatmentList[(int)eAxmTreatment.end_at]])
                        }).ToList();
        }
      } catch {
      } finally {
        // PostgreSQL Server 通信切断
        dbAccess.CloseSqlConnection();
      }

      return DataSource;
    }

    // 治療状況削除
    [HttpPost("DeleteTreatmentData")]
    public void DeleteTreatmentData([FromBody]int treatmentId) {
      try {
        DBAccess dbAccess = DBAccess.GetInstance();

        bool result = false;

        try {
          // PostgreSQL Server 通信接続
          NpgsqlConnection sqlConnection = dbAccess.GetSqlConnection();

          // クエリコマンド実行
          // AXM_TREATMENTテーブルからから削除
          result = (delete_treatment_by_treatmentId(treatmentId, sqlConnection) != 0);
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

    // 治療方法削除
    [HttpPost("DeleteTreatmentInfoData")]
    public void DeleteTreatmentInfoData([FromBody]int treatmentTypeId) {
      try {
        DBAccess dbAccess = DBAccess.GetInstance();

        bool result = false;

        try {
          // PostgreSQL Server 通信接続
          NpgsqlConnection sqlConnection = dbAccess.GetSqlConnection();

          // クエリコマンド実行
          // AXM_TREATMENTテーブルからから全データ削除
          if (delete_treatment_by_treatmentTypeId(treatmentTypeId, sqlConnection) != 0) {
            // AXM_TREATMENT_INFOテーブルからから削除
            result = (delete_treatmentinfo_by_treatmentTypeId(treatmentTypeId, sqlConnection) != 0);
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

    public static bool InsertTreatmentInfo(TreatmentInfoRec rec, NpgsqlConnection sqlConnection) {
      // SQLコマンド
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("insert into ");
      stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.AXM_TREATMENT_INFO]));
      string text = " (";
      string text2 = " (";
      for (int i = 0; i < COLNAME_AxmTreatmentInfoList.Count(); i++) {
        if (i != 0) {
          text += ",";
          text2 += ",";
        }

        text += _col(COLNAME_AxmTreatmentInfoList[i]);
        text2 += _bind(COLNAME_AxmTreatmentInfoList[i]);
      }

      text += ")";
      text2 += ")";
      stringBuilder.Append(text);
      stringBuilder.Append(" values ");
      stringBuilder.Append(text2);
      stringBuilder.Append(_onconflict("pk_axm_treatment_info"));
      stringBuilder.Append(_doupdateexam(COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.updated_at], DateTime.Now));
      stringBuilder.Append(_doupdatevalue(COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.treatment_name], rec.treatment_name));
      stringBuilder.Append(_doupdatevalue(COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.color_r], rec.color_r.ToString()));
      stringBuilder.Append(_doupdatevalue(COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.color_g], rec.color_g.ToString()));
      stringBuilder.Append(_doupdatevalue(COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.color_b], rec.color_b.ToString()));
      stringBuilder.Append(_doupdatevalue(COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.color_a], rec.color_a.ToString()));
      stringBuilder.Append(_doupdatevalue(COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.suppression_rate], rec.suppression_rate.ToString()));
      stringBuilder.Append(";");
      int num = 0;
      // SQLコマンド実行
      using (NpgsqlCommand sqlCommand = new(stringBuilder.ToString(), sqlConnection)) {
        sqlCommand.Parameters.AddWithValue(COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.treatmenttype_id], rec.treatmenttype_id);
        sqlCommand.Parameters.AddWithValue(COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.treatment_name], rec.treatment_name);
        sqlCommand.Parameters.AddWithValue(COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.color_r], rec.color_r);
        sqlCommand.Parameters.AddWithValue(COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.color_g], rec.color_g);
        sqlCommand.Parameters.AddWithValue(COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.color_b], rec.color_b);
        sqlCommand.Parameters.AddWithValue(COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.color_a], rec.color_a);
        sqlCommand.Parameters.AddWithValue(COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.suppression_rate], rec.suppression_rate);
        sqlCommand.Parameters.AddWithValue(COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.created_at], _DateTimeToObject(rec.created_at));
        sqlCommand.Parameters.AddWithValue(COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.updated_at], _DateTimeToObject(rec.updated_at));
        num = sqlCommand.ExecuteNonQuery();
      }

      return num != 0;
    }

    public static bool InsertTreatment(TreatmentRec rec, NpgsqlConnection sqlConnection) {
      // SQLコマンド
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("insert into ");
      stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.AXM_TREATMENT]));
      string text = " (";
      string text2 = " (";
      for (int i = 0; i < COLNAME_AxmTreatmentList.Count(); i++) {
        if (i != 0) {
          text += ",";
          text2 += ",";
        }

        text += _col(COLNAME_AxmTreatmentList[i]);
        text2 += _bind(COLNAME_AxmTreatmentList[i]);
      }

      text += ")";
      text2 += ")";
      stringBuilder.Append(text);
      stringBuilder.Append(" values ");
      stringBuilder.Append(text2);
      stringBuilder.Append(_onconflict("pk_axm_treatment"));
      stringBuilder.Append(_doupdateexam(COLNAME_AxmTreatmentList[(int)eAxmTreatment.updated_at], DateTime.Now));
      stringBuilder.Append(_doupdatevalue(COLNAME_AxmTreatmentList[(int)eAxmTreatment.treatmenttype_id], rec.treatmenttype_id.ToString()));
      stringBuilder.Append(_doupdatevalue(COLNAME_AxmTreatmentList[(int)eAxmTreatment.start_at], _DateTimeToObject(rec.start_at).ToString()));
      stringBuilder.Append(_doupdatevalue(COLNAME_AxmTreatmentList[(int)eAxmTreatment.end_at], _DateTimeToObject(rec.end_at).ToString()));
      stringBuilder.Append(";");
      int num = 0;
      // SQLコマンド実行
      using (NpgsqlCommand sqlCommand = new(stringBuilder.ToString(), sqlConnection)) {
        sqlCommand.Parameters.AddWithValue(COLNAME_AxmTreatmentList[(int)eAxmTreatment.treatment_id], rec.treatment_id);
        sqlCommand.Parameters.AddWithValue(COLNAME_AxmTreatmentList[(int)eAxmTreatment.treatmenttype_id], rec.treatmenttype_id);
        sqlCommand.Parameters.AddWithValue(COLNAME_AxmTreatmentList[(int)eAxmTreatment.pt_uuid], Guid.Parse(rec.pt_uuid));
        sqlCommand.Parameters.AddWithValue(COLNAME_AxmTreatmentList[(int)eAxmTreatment.start_at], _DateTimeToObject(rec.start_at));
        sqlCommand.Parameters.AddWithValue(COLNAME_AxmTreatmentList[(int)eAxmTreatment.end_at], _DateTimeToObject(rec.end_at));
        sqlCommand.Parameters.AddWithValue(COLNAME_AxmTreatmentList[(int)eAxmTreatment.created_at], _DateTimeToObject(rec.created_at));
        sqlCommand.Parameters.AddWithValue(COLNAME_AxmTreatmentList[(int)eAxmTreatment.updated_at], _DateTimeToObject(rec.updated_at));
        num = sqlCommand.ExecuteNonQuery();
      }

      return num != 0;
    }

    // treatmenttype_idの最大値取得
    public static int SelectMaxTreatmentTypeId(NpgsqlConnection sqlConnection) {
      int result = -1;
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("select ");
      stringBuilder.Append(_maxcol(COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.treatmenttype_id]));
      stringBuilder.Append("from ");
      stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.AXM_TREATMENT_INFO]));
      stringBuilder.Append(";");
      using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
      using NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();
      while (npgsqlDataReader.Read()) {
        result = _objectToInt(npgsqlDataReader[0]);
      }

      return result != 0 ? result + 1 : 1;
    }

    // treatmenttype_idの有無を取得
    public static int Select_TreatmentTypeId_By_TreatmentInfo(NpgsqlConnection sqlConnection, int treatmentType) {
      int result = -1;
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("select ");
      stringBuilder.Append(_col(COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.treatmenttype_id]));
      stringBuilder.Append("from ");
      stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.AXM_TREATMENT_INFO]));
      stringBuilder.Append("where ");
      stringBuilder.Append(_col(COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.treatmenttype_id]));
      stringBuilder.Append(" = ");
      stringBuilder.Append(_bind(COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.treatmenttype_id]));
      stringBuilder.Append(";");
      using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
      npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmTreatmentInfoList[0], treatmentType);
      using NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();
      while (npgsqlDataReader.Read()) {
        result = _objectToInt(npgsqlDataReader[0]);
      }

      return result;
    }

    // todo: １つにまとめられる
    // treatment_idの最大値取得
    public static int SelectMaxTreatmentId(NpgsqlConnection sqlConnection) {
      int result = -1;
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("select ");
      stringBuilder.Append(_maxcol(COLNAME_AxmTreatmentList[(int)eAxmTreatment.treatment_id]));
      stringBuilder.Append("from ");
      stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.AXM_TREATMENT]));
      stringBuilder.Append(";");
      using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
      using NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();
      while (npgsqlDataReader.Read()) {
        result = _objectToInt(npgsqlDataReader[0]);
      }

      return result != 0 ? result + 1 : 1;
    }

    // treatment_idの有無を取得
    public static int Select_TreatmentId_By_Treatment(NpgsqlConnection sqlConnection, int treatment) {
      int result = -1;
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("select ");
      stringBuilder.Append(_col(COLNAME_AxmTreatmentList[(int)eAxmTreatment.treatment_id]));
      stringBuilder.Append("from ");
      stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.AXM_TREATMENT]));
      stringBuilder.Append("where ");
      stringBuilder.Append(_col(COLNAME_AxmTreatmentList[(int)eAxmTreatment.treatment_id]));
      stringBuilder.Append(" = ");
      stringBuilder.Append(_bind(COLNAME_AxmTreatmentList[(int)eAxmTreatment.treatment_id]));
      stringBuilder.Append(";");
      using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
      npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmTreatmentList[(int)eAxmTreatment.treatment_id], treatment);
      using NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();
      while (npgsqlDataReader.Read()) {
        result = _objectToInt(npgsqlDataReader[0]);
      }

      return result;
    }

    /// <summary>
    /// 治療方法の一覧を取得する関数
    /// </summary>
    /// <param name="pt_uuid"></param>
    /// <param name="conditions"></param>
    /// <param name="sqlConnection"></param>
    /// <returns></returns>
    public static string GetTreatmentListString(string pt_uuid, int[] conditions, int count, NpgsqlConnection sqlConnection) {
      string result = string.Empty;

      string TreatmentQuery = "SELECT * FROM ";
      TreatmentQuery += _table(DB_TableNames[(int)eDbTable.AXM_TREATMENT]);
      TreatmentQuery += " WHERE ";
      TreatmentQuery += _col(COLNAME_AxmTreatmentList[(int)eAxmTreatment.pt_uuid]);
      TreatmentQuery += " = ";
      TreatmentQuery += _val(pt_uuid);
      if (count > 0) {
        TreatmentQuery += " AND ";
        TreatmentQuery += " (";
        for (int i = 0; i < count; i++) {
          if (i != 0) {
            TreatmentQuery += " OR ";
          }

          TreatmentQuery += _col(COLNAME_AxmTreatmentList[(int)eAxmTreatment.treatmenttype_id]);
          TreatmentQuery += " = ";
          TreatmentQuery += conditions[i];
        }
        TreatmentQuery += ")";
      }
      TreatmentQuery += " ORDER BY ";
      TreatmentQuery += _col(COLNAME_AxmTreatmentList[(int)eAxmTreatment.treatment_id]);

      NpgsqlCommand TreatmentCommand = new(TreatmentQuery, sqlConnection);
      NpgsqlDataAdapter TreatmentDataAdapter = new(TreatmentCommand);
      DataTable TreatmentDataTable = new();
      TreatmentDataAdapter.Fill(TreatmentDataTable);

      for (int i = 0; i < TreatmentDataTable.Rows.Count; i++) {
        DataRow data = TreatmentDataTable.Rows[i];
        // 治療名称取得
        string treatmenttype_id = data[COLNAME_AxmTreatmentList[(int)eAxmTreatment.treatmenttype_id]].ToString() ?? string.Empty;
        result += GetTreatmentName(treatmenttype_id, sqlConnection) + " ";
      }

      return result;
    }

    /// <summary>
    /// 治療方法の名称を取得する関数
    /// </summary>
    /// <param name="treatmenttype_id"></param>
    /// <param name="sqlConnection"></param>
    /// <returns></returns>
    public static string GetTreatmentName(string treatmenttype_id, NpgsqlConnection sqlConnection) {
      string result = string.Empty;

      string TreatmentQuery = "SELECT * FROM ";
      TreatmentQuery += _table(DB_TableNames[(int)eDbTable.AXM_TREATMENT_INFO]);
      TreatmentQuery += " WHERE ";
      TreatmentQuery += _col(COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.treatmenttype_id]);
      TreatmentQuery += " = ";
      TreatmentQuery += _val(treatmenttype_id);

      NpgsqlCommand TreatmentCommand = new(TreatmentQuery, sqlConnection);
      NpgsqlDataAdapter TreatmentDataAdapter = new(TreatmentCommand);
      DataTable TreatmentDataTable = new();
      TreatmentDataAdapter.Fill(TreatmentDataTable);

      if (TreatmentDataTable.Rows.Count > 0) {
        result = TreatmentDataTable.Rows[0][COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.treatment_name]].ToString() ?? string.Empty;
      }

      return result;
    }

    public int delete_treatment_by_treatmentId(int treatmentId, NpgsqlConnection sqlConnection) {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("delete ");
      stringBuilder.Append("from ");
      stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.AXM_TREATMENT]));
      stringBuilder.Append("where ");
      stringBuilder.Append(_col(COLNAME_AxmTreatmentList[(int)eAxmTreatment.treatment_id]));
      stringBuilder.Append("= ");
      stringBuilder.Append(_bind(COLNAME_AxmTreatmentList[(int)eAxmTreatment.treatment_id]));
      stringBuilder.Append(";");
      using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
      npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmTreatmentList[(int)eAxmTreatment.treatment_id], treatmentId);
      return npgsqlCommand.ExecuteNonQuery();
    }

    public int delete_treatment_by_treatmentTypeId(int treatmentTypeId, NpgsqlConnection sqlConnection) {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("delete ");
      stringBuilder.Append("from ");
      stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.AXM_TREATMENT]));
      stringBuilder.Append("where ");
      stringBuilder.Append(_col(COLNAME_AxmTreatmentList[(int)eAxmTreatment.treatmenttype_id]));
      stringBuilder.Append("= ");
      stringBuilder.Append(_bind(COLNAME_AxmTreatmentList[(int)eAxmTreatment.treatmenttype_id]));
      stringBuilder.Append(";");
      using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
      npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmTreatmentList[(int)eAxmTreatment.treatmenttype_id], treatmentTypeId);
      return npgsqlCommand.ExecuteNonQuery();
    }

    public int delete_treatmentinfo_by_treatmentTypeId(int treatmentTypeId, NpgsqlConnection sqlConnection) {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("delete ");
      stringBuilder.Append("from ");
      stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.AXM_TREATMENT_INFO]));
      stringBuilder.Append("where ");
      stringBuilder.Append(_col(COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.treatmenttype_id]));
      stringBuilder.Append("= ");
      stringBuilder.Append(_bind(COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.treatmenttype_id]));
      stringBuilder.Append(";");
      using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
      npgsqlCommand.Parameters.AddWithValue(COLNAME_AxmTreatmentInfoList[(int)eAxmTreatmentInfo.treatmenttype_id], treatmentTypeId);
      return npgsqlCommand.ExecuteNonQuery();
    }

    public static string[] COLNAME_AxmTreatmentInfoList = new string[(int)eAxmTreatmentInfo.MAX] {
            "treatmenttype_id", "treatment_name", "color_r", "color_g", "color_b", "color_a", "suppression_rate","updated_at", "created_at"
        };

    public enum eAxmTreatmentInfo {
      treatmenttype_id = 0,
      treatment_name,
      color_r,
      color_g,
      color_b,
      color_a,
      suppression_rate,
      updated_at,
      created_at,
      MAX
    }

    public static string[] COLNAME_AxmTreatmentList = new string[(int)eAxmTreatment.MAX] {
            "treatment_id", "treatmenttype_id", "pt_uuid", "start_at", "end_at","updated_at", "created_at"
        };

    public enum eAxmTreatment {
      treatment_id = 0,
      treatmenttype_id,
      pt_uuid,
      start_at,
      end_at,
      updated_at,
      created_at,
      MAX
    }

  }
}

public class TreatmentInfoRec {
  public int treatmenttype_id { get; set; }
  public string treatment_name { get; set; } = string.Empty;
  public int color_r { get; set; }
  public int color_g { get; set; }
  public int color_b { get; set; }
  public int color_a { get; set; }
  public int suppression_rate { get; set; }
  public DateTime? updated_at { get; set; }

  public DateTime? created_at { get; set; }
}

public class TreatmentRec {
  public int treatment_id { get; set; }
  public int treatmenttype_id { get; set; }
  public string pt_uuid { get; set; } = string.Empty;
  public DateTime? start_at { get; set; }
  public DateTime? end_at { get; set; }

  public DateTime? updated_at { get; set; }

  public DateTime? created_at { get; set; }
}

using BlazorDbTest.Client.Pages;
using Npgsql;
using System.Data;
using System.Reflection;
using System.Text;

namespace BlazorDbTest.Controllers {
    public class CommonController {

        // 患者UUID取得
        public static string Select_PTUUID_by_PTID(NpgsqlConnection sqlConnection, string sPtid) {
            string result = string.Empty;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("select ");
            stringBuilder.Append(_col(COLNAME_PatientList[(int)ePatientList.pt_uuid]));
            stringBuilder.Append("from ");
            stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.PATIENT_LIST]));
            stringBuilder.Append("where ");
            stringBuilder.Append(_col(COLNAME_PatientList[(int)ePatientList.pt_id]));
            stringBuilder.Append("= ");
            stringBuilder.Append(_val(sPtid));
            stringBuilder.Append(";");
            using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
            using NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();
            while (npgsqlDataReader.Read()) {
                result = npgsqlDataReader[COLNAME_PatientList[(int)ePatientList.pt_uuid]].ToString() ?? string.Empty;
            }

            return result;
        }

        // exam_idの最大値取得
        public static int SelectMaxExamId(NpgsqlConnection sqlConnection) {
            int result = -1;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("select ");
            stringBuilder.Append(_maxcol(COLNAME_ExamList[(int)eExamList.exam_id]));
            stringBuilder.Append("from ");
            stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.EXAM_LIST]));
            stringBuilder.Append(";");
            using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
            using NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();
            while (npgsqlDataReader.Read()) {
                result = _objectToInt(npgsqlDataReader[0]);
            }

            return result != 0 ? result + 1 : 1;
        }

        public static int RegisterExamList(string pt_uuid, string dataType, Common.Const.eEyeType posEye, DateTime? exam_datetime, NpgsqlConnection sqlConnection) {
            int retExamId = -1;
            var examrec = new ExamListRec();
            try {
                // 主キー項目
                examrec.exam_id = SelectMaxExamId(sqlConnection);
                examrec.examtype_id = Select_Examtype_ID(sqlConnection, dataType);
                examrec.eye_id = Select_Eye_ID(sqlConnection, Common.Const.strEyeType[posEye]);

                examrec.pt_uuid = pt_uuid;

                // todo:
                // ログインユーザ名
                //examrec.exam_operator_lastname = string.IsNullOrWhiteSpace(retLastName) ? "" : retLastName;
                //examrec.exam_operator_firstname = string.IsNullOrWhiteSpace(retFirstName) ? "" : retFirstName;
                examrec.exam_operator_lastname = string.Empty;
                examrec.exam_operator_firstname = string.Empty;

                // デバイスID
                examrec.device_id = Select_Device_ID(sqlConnection, "AxialManager2");

                // 検査日時
                examrec.exam_datetime = exam_datetime;

                // レコード更新日、作成日
                var dateNow = DateTime.Now;
                examrec.updated_at = dateNow;
                examrec.created_at = dateNow;

                // todo: ↓の条件に装置種別=AxMも追加
                // 検査タイプID、検査眼ID、検査日時で検索し該当するものがあればExamIDを変更
                retExamId = Select_ExamID_by_PK_and_ExamDateTime(sqlConnection, examrec.examtype_id, examrec.eye_id, (DateTime)examrec.exam_datetime);
                if (retExamId != -1) { examrec.exam_id = retExamId; } else { retExamId = examrec.exam_id; }

                // Insert(Upsert)実施
                if (!InsertExamList(sqlConnection, examrec)) {
                    // todo: error
                }
            } catch {
            }

            return retExamId;
        }

        private static bool InsertExamList(NpgsqlConnection sqlConnection, ExamListRec aExamRec) {
            int num = 0;

            StringBuilder stringBuilder = new();
            stringBuilder.Append("insert into ");
            stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.EXAM_LIST]));
            string text = " (";
            string text2 = " (";
            for (int i = 0; i < COLNAME_ExamList.Count(); i++) {
                if (i != 0) {
                    text += ",";
                    text2 += ",";
                }

                text += _col(COLNAME_ExamList[i]);
                text2 += _bind(COLNAME_ExamList[i]);
            }

            text += ")";
            text2 += ")";
            stringBuilder.Append(text);
            stringBuilder.Append(" values ");
            stringBuilder.Append(text2);
            stringBuilder.Append(_onconflict("pk_exam_list"));
            stringBuilder.Append(_doupdateexam(COLNAME_ExamList[(int)eExamList.updated_at], DateTime.Now));
            stringBuilder.Append(";");

            using (NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection)) {
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamList[(int)eExamList.exam_id], aExamRec.exam_id);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamList[(int)eExamList.examtype_id], aExamRec.examtype_id);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamList[(int)eExamList.eye_id], aExamRec.eye_id);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamList[(int)eExamList.pt_uuid], Guid.Parse(aExamRec.pt_uuid));
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamList[(int)eExamList.exam_operator_lastname], aExamRec.exam_operator_lastname);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamList[(int)eExamList.exam_operator_firstname], aExamRec.exam_operator_firstname);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamList[(int)eExamList.device_id], aExamRec.device_id);
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamList[(int)eExamList.exam_datetime], _DateTimeToObject(aExamRec.exam_datetime));
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamList[(int)eExamList.updated_at], _DateTimeToObject(aExamRec.updated_at));
                npgsqlCommand.Parameters.AddWithValue(COLNAME_ExamList[(int)eExamList.created_at], _DateTimeToObject(aExamRec.created_at));
                num = npgsqlCommand.ExecuteNonQuery();
            }

            return num != 0;
        }

        public static int Select_GenderId(NpgsqlConnection sqlConnection, string gender) {
            int result = -1;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("select ");
            stringBuilder.Append(_col(COLNAME_MstGendersList[0]));
            stringBuilder.Append("from ");
            stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.MST_GENDERS]));
            stringBuilder.Append("where ");
            stringBuilder.Append(_col(COLNAME_MstGendersList[1]));
            stringBuilder.Append("= ");
            stringBuilder.Append(_val(gender));
            stringBuilder.Append(";");

            using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
            using NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();
            while (npgsqlDataReader.Read()) {
                result = _objectToInt(npgsqlDataReader[0]);
            }

            return result;
        }

        public static int Select_Examtype_ID(NpgsqlConnection sqlConnection, string examType) {
            int result = -1;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("select ");
            stringBuilder.Append(_col(COLNAME_MstExamTypesList[0]));
            stringBuilder.Append("from ");
            stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.MST_EXAMTYPES]));
            stringBuilder.Append("where");
            stringBuilder.Append(_col(COLNAME_MstExamTypesList[1]));
            stringBuilder.Append("like ");
            stringBuilder.Append(_val(examType));
            stringBuilder.Append(";");
            using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
            using NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();
            while (npgsqlDataReader.Read()) {
                result = _objectToInt(npgsqlDataReader[0]);
            }

            return result;
        }

        public static int Select_Eye_ID(NpgsqlConnection sqlConnection, string eyeType) {
            int result = -1;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("select ");
            stringBuilder.Append(_col(COLNAME_MstEyesList[0]));
            stringBuilder.Append("from ");
            stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.MST_EYES]));
            stringBuilder.Append("where");
            stringBuilder.Append(_col(COLNAME_MstEyesList[1]));
            stringBuilder.Append("= ");
            stringBuilder.Append(_val(eyeType));
            stringBuilder.Append(";");
            using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
            using NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();
            while (npgsqlDataReader.Read()) {
                result = _objectToInt(npgsqlDataReader[0]);
            }

            return result;
        }

        public static int Select_Device_ID(NpgsqlConnection sqlConnection, string deviceName) {
            int result = -1;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("select ");
            stringBuilder.Append(_col(COLNAME_MstDevicesList[0]));
            stringBuilder.Append("from ");
            stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.MST_DEVICES]));
            stringBuilder.Append("where");
            stringBuilder.Append(_col(COLNAME_MstDevicesList[1]));
            stringBuilder.Append("= ");
            stringBuilder.Append(_val(deviceName));
            stringBuilder.Append(";");
            using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
            using NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();
            while (npgsqlDataReader.Read()) {
                result = _objectToInt(npgsqlDataReader[0]);
            }

            return result;
        }

        public static int Select_ExamID_by_PK_and_ExamDateTime(NpgsqlConnection sqlConnection, int examtypeId, int eyeId, DateTime dtExam) {
            int result = -1;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("select ");
            stringBuilder.Append(_col(COLNAME_ExamList[(int)eExamList.exam_id]));
            stringBuilder.Append("from ");
            stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.EXAM_LIST]));
            stringBuilder.Append("where ");
            stringBuilder.Append(_col(COLNAME_ExamList[(int)eExamList.examtype_id]));
            stringBuilder.Append("= ");
            stringBuilder.Append(_val(examtypeId.ToString()));
            stringBuilder.Append(" and ");
            stringBuilder.Append(_col(COLNAME_ExamList[(int)eExamList.eye_id]));
            stringBuilder.Append("= ");
            stringBuilder.Append(_val(eyeId.ToString()));
            stringBuilder.Append(" and ");
            stringBuilder.Append(_col(COLNAME_ExamList[(int)eExamList.exam_datetime]));
            stringBuilder.Append("= ");
            stringBuilder.Append(_val(dtExam.ToString("yyyy-MM-dd HH:mm:ss")));
            stringBuilder.Append(";");
            using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
            using NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();
            while (npgsqlDataReader.Read()) {
                result = _objectToInt(npgsqlDataReader[0]);
            }

            return result;
        }

        public static int Select_SelectTypeID(NpgsqlConnection sqlConnection, string selectType) {
            int result = -1;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("select ");
            stringBuilder.Append(_col(COLNAME_MstSelecttypesList[0]));
            stringBuilder.Append("from ");
            stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.MST_SELECTTYPES]));
            stringBuilder.Append("where");
            stringBuilder.Append(_col(COLNAME_MstSelecttypesList[1]));
            stringBuilder.Append("= ");
            stringBuilder.Append(_val(selectType.ToLower()));
            stringBuilder.Append(";");
            using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
            using NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();
            while (npgsqlDataReader.Read()) {
                result = _objectToInt(npgsqlDataReader[0]);
            }

            return result;
        }

        public static int Select_TargetEyeId_By_TargetEyeType(NpgsqlConnection sqlConnection, string targetEyeType) {
            int result = -1;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("select ");
            stringBuilder.Append(_col(COLNAME_MstTargeteyeList[0]));
            stringBuilder.Append("from ");
            stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.MST_TARGET_EYES]));
            stringBuilder.Append("where ");
            stringBuilder.Append(_col(COLNAME_MstTargeteyeList[1]));
            stringBuilder.Append(" = ");
            stringBuilder.Append(_bind(COLNAME_MstTargeteyeList[1]));
            stringBuilder.Append(";");
            using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
            npgsqlCommand.Parameters.AddWithValue(COLNAME_MstTargeteyeList[1], targetEyeType);
            using NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();
            while (npgsqlDataReader.Read()) {
                result = _objectToInt(npgsqlDataReader[0]);
            }

            return result;
        }

        public static int Select_FittingId_By_FittingType(NpgsqlConnection sqlConnection, string fittingType) {
            int result = -1;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("select ");
            stringBuilder.Append(_col(COLNAME_MstFittingsList[0]));
            stringBuilder.Append("from ");
            stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.MST_FITTINGS]));
            stringBuilder.Append(" where ");
            stringBuilder.Append(_col(COLNAME_MstFittingsList[1]));
            stringBuilder.Append("= ");
            stringBuilder.Append(_bind(COLNAME_MstFittingsList[1]));
            stringBuilder.Append(";");
            using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
            npgsqlCommand.Parameters.AddWithValue(COLNAME_MstFittingsList[1], fittingType);
            using NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();
            while (npgsqlDataReader.Read()) {
                result = _objectToInt(npgsqlDataReader[0]);
            }

            return result;
        }

        public static int Select_IolEyeId_By_IolEyeType(NpgsqlConnection sqlConnection, string iolEyeType) {
            int result = -1;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("select ");
            stringBuilder.Append(_col(COLNAME_MstIolEyesList[0]));
            stringBuilder.Append("from ");
            stringBuilder.Append(_table(DB_TableNames[(int)eDbTable.MST_IOL_EYES]));
            stringBuilder.Append(" where ");
            stringBuilder.Append(_col(COLNAME_MstIolEyesList[1]));
            stringBuilder.Append("= ");
            stringBuilder.Append(_bind(COLNAME_MstIolEyesList[1]));
            stringBuilder.Append(";");
            using NpgsqlCommand npgsqlCommand = new NpgsqlCommand(stringBuilder.ToString(), sqlConnection);
            npgsqlCommand.Parameters.AddWithValue(COLNAME_MstIolEyesList[1], iolEyeType);
            using NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();
            while (npgsqlDataReader.Read()) {
                result = _objectToInt(npgsqlDataReader[0]);
            }

            return result;
        }

        public static object _DateTimeToObject(DateTime? input) {
            object obj = input;
            obj ??= DBNull.Value;
            return obj;
        }

        public static int GetAge(DateTime? birthDate, DateTime today) {
            int age = -1;

            if (birthDate.HasValue) {
                age = (int.Parse(today.ToString("yyyyMMdd")) - int.Parse(birthDate?.ToString("yyyyMMdd"))) / 10000;
            }

            return age;
        }

        public static DateTime? _objectToDateTime(object oColumnRes, bool bisUTC = true) {
            if (!DateTime.TryParse(oColumnRes.ToString(), out var result)) {
                return null;
            }

            return bisUTC ? result.ToUniversalTime() : result;
        }

        public static int _objectToInt(object oColumnRes) {
            int result = 0;
            int.TryParse(oColumnRes.ToString(), out result);
            return result;
        }

        // todo:
        public static string _col(string aColumn) {
            return " \"" + aColumn + "\" ";
        }

        public static string _maxcol(string aColumn) {
            return " max( \"" + aColumn + "\") ";
        }

        public static string _table(string aTableName) {
            return " \"" + aTableName + "\" ";
        }

        public static string _val(string aVal) {
            return " '" + aVal + "' ";
        }

        public static string _likeval(string aVal) {
            return " '%" + aVal + "%' ";
        }

        public static string _bind(string aBindStr) {
            return "@" + aBindStr;
        }

        public static string _onconflict(string constraint) {
            return "on conflict on constraint \"" + constraint + "\" ";
        }

        public static string _doupdateexam(string colUpdatedAt, DateTime dtUpdate) {
            return "do update set \"" + colUpdatedAt + "\" = '" + dtUpdate.ToString("yyyy-MM-dd HH:mm:ss.FFFFFF") + "' ";
        }

        // todo:
        public static string[] DB_TableNames =
        [
            "", "PATIENT_LIST", "OPERATOR_LIST", "EXAM_LIST", "EXAM_BMODE", "EXAM_DIA", "EXAM_KRT", "EXAM_OPTAXIAL", "EXAM_OPTACD", "EXAM_OPTLENS",
            "EXAM_PACHY_CCT", "EXAM_REF", "EXAM_SCI_REF", "EXAM_SPECULAR", "EXAM_TONO", "EXAM_TOPO", "EXAM_USAXIAL", "MST_DEVICES", "MST_ENVIRONMENTS", "MST_EXAMTYPES",
            "MST_EYES", "MST_FITTINGS", "MST_GENDERS", "MST_IOL_EYES", "MST_PHITYPES", "MST_SELECTTYPES", "MST_SPECULAR_ANALYSIS_METHODS", "MST_SPECULAR_FIXATION_LIGHT_POS", "MST_TARGET_EYES", "VERSION_INFO",
            "VERSION_MST_INFO"
        ];

        public static string[] COLNAME_PatientList = ["pt_uuid", "pt_id", "pt_lastname", "pt_firstname", "gender_id", "pt_dob", "pt_description", "updated_at", "created_at"];
        public static string[] COLNAME_ExamList = ["exam_id", "examtype_id", "eye_id", "pt_uuid", "exam_operator_lastname", "exam_operator_firstname", "device_id", "exam_datetime", "updated_at", "created_at"];
        public static string[] COLNAME_MstGendersList = ["gender_id", "gender_type", "updated_at", "created_at"];
        public static string[] COLNAME_MstExamTypesList = ["examtype_id", "examtype_type", "updated_at", "created_at"];
        public static string[] COLNAME_MstEyesList = ["eye_id", "eye_type", "updated_at", "created_at"];
        public static string[] COLNAME_MstFittingsList = ["fitting_id", "fitting_type", "updated_at", "created_at"];
        public static string[] COLNAME_MstDevicesList = ["device_id", "device_type", "updated_at", "created_at"];
        public static string[] COLNAME_MstSelecttypesList = ["select_id", "select_type", "updated_at", "created_at"];
        public static string[] COLNAME_MstTargeteyeList = ["target_eye_id", "target_eye_type", "updated_at", "created_at"];
        public static string[] COLNAME_MstIolEyesList = ["iol_eye_id", "iol_eye_type", "updated_at", "created_at"];

        public enum eDbTable {
            none = 0,
            PATIENT_LIST,
            OPERATOR_LIST,
            EXAM_LIST,
            EXAM_BMODE,
            EXAM_DIA,
            EXAM_KRT,
            EXAM_OPTAXIAL,
            EXAM_OPTACD,
            EXAM_OPTLENS,
            EXAM_PACHY_CCT,
            EXAM_REF,
            EXAM_SCI_REF,
            EXAM_SPECULAR,
            EXAM_TONO,
            EXAM_TOPO,
            EXAM_USAXIAL,
            MST_DEVICES,
            MST_ENVIRONMENTS,
            MST_EXAMTYPES,
            MST_EYES,
            MST_FITTINGS,
            MST_GENDERS,
            MST_IOL_EYES,
            MST_PHITYPES,
            MST_SELECTTYPES,
            MST_SPECULAR_ANALYSIS_METHODS,
            MST_SPECULAR_FIXATION_LIGHT_POS,
            MST_TARGET_EYES,
            VERSION_INFO,
            VERSION_MST_INFO,
            MAX
        }

        public enum ePatientList {
            pt_uuid = 0,
            pt_id,
            pt_lastname,
            pt_firstname,
            gender_id,
            pt_dob,
            pt_description,
            updated_at,
            created_at,
            MAX
        }

        public enum eExamList {
            exam_id = 0,
            examtype_id,
            eye_id,
            pt_uuid,
            exam_operator_lastname,
            exam_operator_firstname,
            device_id,
            exam_datetime,
            updated_at,
            created_at,
            MAX
        }

        public class ExamListRec {
            public int exam_id { get; set; }

            public int examtype_id { get; set; }

            public int eye_id { get; set; }

            public string pt_uuid { get; set; } = string.Empty;


            public string exam_operator_lastname { get; set; } = string.Empty;


            public string exam_operator_firstname { get; set; } = string.Empty;


            public int device_id { get; set; }

            public DateTime? exam_datetime { get; set; }

            public DateTime? updated_at { get; set; }

            public DateTime? created_at { get; set; }
        }
    }
}

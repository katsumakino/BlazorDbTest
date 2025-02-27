namespace AxialManagerS.Shared.Common {
  //患者情報
  public class PatientInfo {
    public bool Mark { get; set; }                      //お気に入り(仮)
    public string ID { get; set; } = default!;
    public string FamilyName { get; set; } = default!;  //患者姓
    public string FirstName { get; set; } = default!;   //患者名
    public Gender Gender { get; set; }                  //性別
    public int Age { get; set; }                        //年齢
    public DateTime? BirthDate { get; set; }            //生年月日
    public string SameID { get; set; } = default!;      //同一ID
    public int DeviceID { get; set; }                   //装置種別
    public string UUID { get; set; } = default!;        //UUID
  }

  //性別
  public enum Gender {
    none,
    male,
    female,
    other, //未指定
  }

  //検索結果
  public class PatientList {
      public PatientInfo PatientInfo { get; set; } = default!;    //患者情報
      public DateTime? LatestPicDate { get; set; }                 //最新撮影日
      public double LatestRAxial { get; set; }                    //最新右眼軸長
      public double LatestLAxial { get; set; }                    //最新左眼軸長
      public string PatientComment { get; set; } = default!;      //患者コメント
      public string AllTreatName { get; set; } = default!;        //治療名称一覧
  }

  //検索条件
  public class PatientSearch() {
      public string IdOrName { get; set; } = default!;            //IDまたは名前
      public Gender Gender { get; set; }                          //性別
      public bool IsAge { get; set; }                             //年齢検索の有無
      public int AgeMin { get; set; }                             //年齢下限
      public int AgeMax { get; set; }                             //年齢上限
      public bool IsAxial { get; set; }                           //眼軸長範囲検索の有無
      public double AxialMin { get; set; }                        //眼軸長下限
      public double AxialMax { get; set; }                        //眼軸長上限
      public bool IsExamDate { get; set; }                        //測定日検索の有無
      public DateTime? ExamDateMin { get; set; }                  //最新測定日下限
      public DateTime? ExamDateMax { get; set; }                  //最新測定日上限
      public int[] TreatmentType { get; set; } = new int[5];      //治療方法(最大5つ)
      public int TreatmentTypeCount { get; set; }                 //治療方法検索条件数
      public string PatientComment { get; set; } = default!;      //患者コメント
      public bool IsMark { get; set; }                            //お気に入り患者のみ
      public bool IsSameID { get; set; }                          //同一ID患者のみ
  }

    //眼軸長(データ表示/書込に使用するもの)
  public class AxialList {
    public string? PatientID { get; set; } = default!;   //患者ID
    public string? RExamID { get; set; } = default!;     //右測定データID
    public string? LExamID { get; set; } = default!;     //左測定データID
    public double? RAxial { get; set; }                  //右眼軸長
    public double? LAxial { get; set; }                  //左眼軸長
    public DateTime? ExamDateTime { get; set; }         //測定日
    public bool IsRManualInput { get; set; }            //右眼手入力フラグ
    public bool IsLManualInput { get; set; }            //左眼手入力フラグ
  }

  // DBから取得する眼軸長データ
  public class AxialData {
    public string? ID { get; set; } = default!;                      //測定データID
    public List<double?> Axial { get; set; } = new List<double?>(); //眼軸長
    public EyeType EyeId { get; set; }                              //左右眼情報
    public int? DeviceID { get; set; }                               //測定装置ID
    public DateTime? ExamDateTime { get; set; }                     //測定日時
  }

  // ↓ここから

  //ケラト(データ表示/書込に使用するもの)
  public class KrtList {
    public string? PatientID { get; set; } = default!;   //患者ID
    public string? RExamID { get; set; } = default!;     //右測定データID
    public string? LExamID { get; set; } = default!;     //左測定データID
    public double? RK1_mm { get; set; }                  //右角膜曲率半径(弱主経線)[mm]
    public double? RK1_d { get; set; }                   //右角膜曲率屈折力(弱主経線)[D]
    public double? RK2_mm { get; set; }                  //右角膜曲率平均(強主経線)[mm]
    public double? RK2_d { get; set; }                   //右角膜曲率屈折力(強主経線)[D]
    public double? RAveK_mm { get; set; }                //右平均角膜曲率半径[mm]
    public double? RAveK_d { get; set; }                 //右平均角膜曲率屈折力[D]
    public double? RCyl_d { get; set; }                  //右乱視度数[D]
    public double? LK1_mm { get; set; }                  //左角膜曲率半径(弱主経線)[mm]
    public double? LK1_d { get; set; }                   //左角膜曲率屈折力(弱主経線)[D]
    public double? LK2_mm { get; set; }                  //左角膜曲率平均(強主経線)[mm]
    public double? LK2_d { get; set; }                   //左角膜曲率屈折力(強主経線)[D]
    public double? LAveK_mm { get; set; }                //左平均角膜曲率半径[mm]
    public double? LAveK_d { get; set; }                 //左平均角膜曲率屈折力[D]
    public double? LCyl_d { get; set; }                  //左乱視度数[D]
    public DateTime? ExamDateTime { get; set; }         //測定日時
    public bool IsRManualInput { get; set; }            //右眼手入力フラグ
    public bool IsLManualInput { get; set; }            //左眼手入力フラグ
  }

  //DBから取得するケラトデータ
  public class KrtData {
    public string? ID { get; set; } = default!;  //測定データID
    public List<double?> K1_mm { get; set; } = new List<double?>();  //角膜曲率半径(弱主経線)[mm]
    public List<double?> K1_d { get; set; } = new List<double?>();   //角膜曲率屈折力(弱主経線)[D]
    public List<double?> K2_mm { get; set; } = new List<double?>();  //角膜曲率平均(強主経線)[mm]
    public List<double?> K2_d { get; set; } = new List<double?>();   //角膜曲率屈折力(強主経線)[D]
    public List<double?> AveK_mm { get; set; } = new List<double?>();//平均角膜曲率半径[mm]
    public List<double?> AveK_d { get; set; } = new List<double?>(); //平均角膜曲率屈折力[D]
    public List<double?> Cyl_d { get; set; } = new List<double?>();  //乱視度数[D]
    public EyeType EyeId { get; set; }          //左右眼情報
    public int? DeviceID { get; set; }           //測定装置ID
    public bool? IsExamData { get; set; }        //測定データ有無
    public DateTime? ExamDateTime { get; set; } //測定日時
  }

  //屈折力(データ表示/書込に使用するもの)
  public class RefList {
    public string? PatientID { get; set; } = default!;   //患者ID
    public string? RExamID { get; set; } = default!;     //右測定データID
    public string? LExamID { get; set; } = default!;     //左測定データID
    public double? RS_d { get; set; }                    //右球面度数[D]
    public double? RC_d { get; set; }                    //右乱視度数[D]
    public int? RA_deg { get; set; }                     //右乱視軸[°]
    public double? RSE_d { get; set; }                   //右等価球面度数[D]
    public double? LS_d { get; set; }                    //左球面度数[D]
    public double? LC_d { get; set; }                    //左乱視度数[D]
    public int? LA_deg { get; set; }                     //左乱視軸[°]
    public double? LSE_d { get; set; }                   //左等価球面度数[D]
    public DateTime? ExamDateTime { get; set; }         //測定日
    public bool IsRManualInput { get; set; }            //右眼手入力フラグ
    public bool IsLManualInput { get; set; }            //左眼手入力フラグ
  }

  // DBから取得する他覚屈折力データ
  public class RefData {
    public string? ID { get; set; } = default!;                      //測定データID
    public List<double?> S_d { get; set; } = new List<double?>();   //球面度数[D]
    public List<double?> C_d { get; set; } = new List<double?>();   //乱視度数[D]
    public List<int?> A_deg { get; set; } = new List<int?>();       //乱視軸[°]
    public List<double?> SE_d { get; set; } = new List<double?>();  //等価球面度数[D]
    public EyeType EyeId { get; set; }                              //左右眼情報
    public int? DeviceID { get; set; }                               //測定装置ID
    public bool? IsExamData { get; set; }                            //測定データ有無
    public DateTime? ExamDateTime { get; set; }                     //測定日時
  }

  // DBから取得する自覚屈折力データ
  public class SciRefData {
    public string? ID { get; set; } = default!;          //測定データID
    public double? S_d { get; set; }                     //球面度数[D]
    public double? C_d { get; set; }                     //乱視度数[D]
    public int? A_deg { get; set; }                      //乱視軸[°]
    public double? SE_d { get; set; }                    //等価球面度数[D]
    public EyeType EyeId { get; set; }                  //左右眼情報
    public int? DeviceID { get; set; }                   //測定装置ID
    public bool? IsExamData { get; set; }                //測定データ有無
    public DateTime? ExamDateTime { get; set; }         //測定日時
  }

  //中央角膜厚(データ表示/書込に使用するもの)
  public class PachyList {
    public string? PatientID { get; set; } = default!;   //患者ID
    public string? RExamID { get; set; } = default!;     //右測定データID
    public string? LExamID { get; set; } = default!;     //左測定データID
    public double? RPachy { get; set; }                  //右眼中央角膜厚
    public double? LPachy { get; set; }                  //左眼中央角膜厚
    public DateTime? ExamDateTime { get; set; }         //測定日
    public bool IsRManualInput { get; set; }            //右眼手入力フラグ
    public bool IsLManualInput { get; set; }            //左眼手入力フラグ
  }

  // DBから取得する中央角膜厚データ
  public class PachyData {
    public string? ID { get; set; } = default!;                      //測定データID
    public List<double?> Pachy { get; set; } = new List<double?>(); //中央角膜厚
    public EyeType EyeId { get; set; }                              //左右眼情報
    public int? DeviceID { get; set; }                               //測定装置ID
    public bool? IsExamData { get; set; }                            //測定データ有無
    public DateTime? ExamDateTime { get; set; }                     //測定日時
  }

  //瞳孔径(データ表示/書込に使用するもの)
  public class DiaList {
    public string? PatientID { get; set; } = default!;   //患者ID
    public string? RExamID { get; set; } = default!;     //右測定データID
    public string? LExamID { get; set; } = default!;     //左測定データID
    public double? RPupil { get; set; }                  //右瞳孔径
    public double? LPupil { get; set; }                  //左瞳孔径
    public DateTime? ExamDateTime { get; set; }         //測定日
    public bool IsRManualInput { get; set; }            //右眼手入力フラグ
    public bool IsLManualInput { get; set; }            //左眼手入力フラグ
  }

  // DBから取得する瞳孔径データ
  public class DiaData {
    public string? ID { get; set; } = default!;                      //測定データID
    public double? Pupil { get; set; }                               //瞳孔径
    public EyeType EyeId { get; set; }                              //左右眼情報
    public int? DeviceID { get; set; }                               //測定装置ID
    public bool? IsExamData { get; set; }                            //測定データ有無
    public DateTime? ExamDateTime { get; set; }                     //測定日時
  }

  //視力(データ表示/書込に使用するもの)
  public class  SightList{
    public string? PatientID { get; set; } = default!;   //患者ID
    public string? RExamID { get; set; } = default!;     //右測定データID
    public string? LExamID { get; set; } = default!;     //左測定データID
    public double? RSight { get; set; }                  //右眼視力
    public double? LSight { get; set; }                  //左眼視力
    public DateTime? ExamDateTime { get; set; }         //測定日
  }

  // DBから取得する視力データ
  public class SightData {
    public string? ID { get; set; } = default!;                      //測定データID
    public double? Sight { get; set; }                               //視力
    public EyeType EyeId { get; set; }                              //左右眼情報
    public int? DeviceID { get; set; }                               //測定装置ID
    public bool? IsExamData { get; set; }                            //測定データ有無
    public DateTime? ExamDateTime { get; set; }                     //測定日時
  }

  // ↑ ここまで追加項目

  // todo: 古い定義を使用しているため、修正が必要
  public enum EyeType {
    none,
    right,
    left,
    both,
  }

  // todo: Patientではないかも
  //治療方法設定
  public class TreatmentMethodSetting {
      public int ID { get; set; }
      public string TreatName { get; set; } = default!;     //治療方法の名前
      public RGBAColor? RGBAColor { get; set; }             //治療方法に割り当てられた色
      public int SuppresionRate { get; set; }               //抑制率
  }

  public class RGBAColor {
      public int R { get; set; } = 0;                       //色のR値
      public int G { get; set; } = 0;                       //色のG値
      public int B { get; set; } = 0;                       //色のB値
      public int A { get; set; } = 0;                       //色のA値
  }

  public class TreatmentData {
      public int ID { get; set; } = 0;                     //治療ID
      public int TreatID { get; set; } = 0;                //治療方法ID
      public DateTime? StartDateTime { get; set; }         //治療開始日時
      public DateTime? EndDateTime { get; set; }           //治療終了日時
  }

  public class TreatmentDataRequest {
    public string PatientID { get; set; } = default!;       //患者ID
    public TreatmentData TreatmentData { get; set; } = default!;
  }

  public class AxmComment {
      public int ID { get; set; } = default!;                 //AXM用コメントデータID
      public AxmCommentType CommentType { get; set; } = 0;    //コメントタイプ
      public string Description { get; set; } = default!;     //コメント
      public DateTime? ExamDateTime { get; set; }             //測定日時
  }

  public class AxmCommentRequest {
    public string PatientID { get; set; } = default!;       //患者ID
    public AxmComment AxmComment { get; set; } = default!;
  }

    //コメントタイプ
    public enum AxmCommentType {
      none,
      Patient,
      ExamDate
  }
}

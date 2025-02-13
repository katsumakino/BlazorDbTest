﻿namespace AxialManagerS.Shared.Common {
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
    public string PatientID { get; set; } = default!;   //患者ID
    public string RExamID { get; set; } = default!;     //右測定データID
    public string LExamID { get; set; } = default!;     //左測定データID
    public double RAxial { get; set; }                  //右眼軸長
    public double LAxial { get; set; }                  //左眼軸長
    public DateTime? ExamDateTime { get; set; }         //測定日
    public bool IsRManualInput { get; set; }            //右眼手入力フラグ
    public bool IsLManualInput { get; set; }            //左眼手入力フラグ
  }

  // DBから取得する眼軸長データ
  public class AxialData {
    public string ID { get; set; } = default!;                      //測定データID
    public List<double?> Axial { get; set; } = new List<double?>(); //眼軸長
    public EyeType EyeId { get; set; }                              //左右眼情報
    public int DeviceID { get; set; }                               //測定装置ID
    public DateTime? ExamDateTime { get; set; }                     //測定日時
  }

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

  public class AxmComment {
      public int ID { get; set; } = default!;                 //AXM用コメントデータID
      public AxmCommentType CommentType { get; set; } = 0;    //コメントタイプ
      public string Description { get; set; } = default!;     //コメント
      public DateTime? ExamDateTime { get; set; }             //測定日時
  }

  //コメントタイプ
  public enum AxmCommentType {
      none,
      Patient,
      ExamDate
  }
}

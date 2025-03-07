using System.Text;

namespace BlazorDbTest.Common {
  public class DBConst {

    // データタイプタグ一覧 ----------------------------------
    #region データタイプ
    public enum eDATATYPE {
      /// <summary>
      /// データヘッダ
      /// </summary>
      HEADER_TAG = 0,
      /// <summary>
      /// Ａモード診断(UD-6000)
      /// </summary>
      ADIAG,
      /// <summary>
      /// Ａモード診断(AL-4000・4050/UD-8000)
      /// </summary>
      ADIAG2,
      /// <summary>
      /// 眼軸長測定
      /// </summary>
      AXIAL,
      /// <summary>
      /// Ａモード診断(UD-6000)
      /// </summary>
      BAXIAL,
      /// <summary>
      /// Ａモード診断(UD-8000)
      /// </summary>
      BAXIAL2,
      /// <summary>
      /// Ｂモード診断（UD-6000）
      /// </summary>
      BDIAG,
      /// <summary>
      /// Ｂモード診断（UD-8000）
      /// </summary>
      BDIAG2,
      /// <summary>
      /// 瞳孔径・角膜径測定
      /// </summary>
      DIA,
      /// <summary>
      /// 電気生理
      /// </summary>
      ERG_VEP,
      /// <summary>
      /// Ｂモード診断グループ（UD-8000）
      /// </summary>
      GROUP_BDIAG2,
      BDIAG2_GROUP,
      /// <summary>
      /// ケラト
      /// </summary>
      KERATO,
      /// <summary>
      /// レンズメータ
      /// </summary>
      LENS,
      /// <summary>
      /// 眼圧測定
      /// </summary>
      NT,
      /// <summary>
      /// OKULIX
      /// </summary>
      OKULIX,
      /// <summary>
      /// 光干渉式眼軸長測定
      /// </summary>
      OPTAXIAL,
      /// <summary>
      /// 角膜厚測定
      /// </summary>
      PACHY,
      /// <summary>
      /// レフ
      /// </summary>
      REF,
      /// <summary>
      /// 断面測定
      /// </summary>
      SLIT,
      /// <summary>
      /// スペキュラー
      /// </summary>
      SPECULAR,
      /// <summary>
      /// スペキュラー
      /// </summary>
      SPECULAR2,
      /// <summary>
      /// 前眼部OCT
      /// </summary>
      SSOCT,
      /// <summary>
      /// 手術情報
      /// </summary>
      SURGERRY,
      /// <summary>
      /// サーモ
      /// </summary>
      THERMO,
      /// <summary>
      /// TMSスライドタイプ
      /// </summary>
      TMS_SLIDE,
      /// <summary>
      /// トポ
      /// </summary>
      TOPO,
      /// <summary>
      /// TMS/CASIAトポ測定
      /// </summary>
      TOPO2,
      /// <summary>
      /// TSAS
      /// </summary>
      TSAS,
      /// <summary>
      /// Aモード診断
      /// </summary>
      UD_AD,
      /// <summary>
      /// Bアキシャル
      /// </summary>
      UD_BA,
      /// <summary>
      /// Bモード診断
      /// </summary>
      UD_BD,
      /// <summary>
      /// 眼軸長測定（超音波）
      /// </summary>
      USAXIAL,
      /// <summary>
      /// 角膜厚測定（超音波）
      /// </summary>
      USPACHY,
      /// <summary>
      /// VERIS
      /// </summary>
      VERIS
    }
    public static Dictionary<eDATATYPE, string> strDataType = new()
    {
            { eDATATYPE.HEADER_TAG, "[FM_IF]" },
            { eDATATYPE.ADIAG, "ADIAG" },
            { eDATATYPE.ADIAG2, "ADIAG2" },
            { eDATATYPE.AXIAL, "AXIAL" },
            { eDATATYPE.BAXIAL, "BAXIAL" },
            { eDATATYPE.BAXIAL2, "BAXIAL2" },
            { eDATATYPE.BDIAG, "BDIAG" },
            { eDATATYPE.BDIAG2, "BDIAG2" },
            { eDATATYPE.DIA, "DIA" },
            { eDATATYPE.ERG_VEP, "ERG_VEP" },
            { eDATATYPE.GROUP_BDIAG2, "GROUP_BDIAG2" },
            { eDATATYPE.BDIAG2_GROUP, "BDIAG2-GROUP" },
            { eDATATYPE.KERATO, "KERATO" },
            { eDATATYPE.LENS, "LENS" },
            { eDATATYPE.NT, "NT" },
            { eDATATYPE.OKULIX, "OKULIX" },
            { eDATATYPE.OPTAXIAL, "OPTAXIAL" },
            { eDATATYPE.PACHY, "PACHY" },
            { eDATATYPE.REF, "REF" },
            { eDATATYPE.SLIT, "SLIT" },
            { eDATATYPE.SPECULAR, "SPECULAR" },
            { eDATATYPE.SPECULAR2, "SPECULAR2" },
            { eDATATYPE.SSOCT, "SSOCT" },
            { eDATATYPE.SURGERRY, "SURGERRY" },
            { eDATATYPE.THERMO, "THERMO" },
            { eDATATYPE.TMS_SLIDE, "TMS_SLIDE" },
            { eDATATYPE.TOPO, "TOPO" },
            { eDATATYPE.TOPO2, "TOPO2" },
            { eDATATYPE.TSAS, "TSAS" },
            { eDATATYPE.UD_AD, "UD_AD" },
            { eDATATYPE.UD_BA, "UD_BA" },
            { eDATATYPE.UD_BD, "UD_BD" },
            { eDATATYPE.USAXIAL, "USAXIAL" },
            { eDATATYPE.USPACHY, "USPACHY" },
            { eDATATYPE.VERIS, "VERIS" },
        };

    public enum eMSTDATATYPE {
      NONE,
      ADIAG,
      BMODE,
      BAXIAL,
      DIA,
      ERG,
      FUNDOSCOPY,
      KRT,
      OPTAXIAL,
      OPTACD,
      OPTLENS,
      PACHY_CCT,
      REF,
      SCI_REF,
      SPECULAR,
      TONO,
      TOPO,
      USAXIAL,
      IOL_CALC,
      TORIC_IOL_CALC,
      // 以下はAXM用
      SIGHT
    }
    public static Dictionary<eMSTDATATYPE, string> strMstDataType = new()
        {
            { eMSTDATATYPE.NONE, "none" },
            { eMSTDATATYPE.ADIAG, "ADIAG" },
            { eMSTDATATYPE.BMODE, "BMODE" },
            { eMSTDATATYPE.BAXIAL, "BAXIAL" },
            { eMSTDATATYPE.DIA, "DIA" },
            { eMSTDATATYPE.ERG, "ERG" },
            { eMSTDATATYPE.FUNDOSCOPY, "FUNDSCOPY" },
            { eMSTDATATYPE.KRT, "KRT" },
            { eMSTDATATYPE.OPTAXIAL, "OPTAXIAL" },
            { eMSTDATATYPE.OPTACD, "OPTACD" },
            { eMSTDATATYPE.OPTLENS, "OPTLENS" },
            { eMSTDATATYPE.PACHY_CCT, "PACHY_CCT" },
            { eMSTDATATYPE.REF, "REF" },
            { eMSTDATATYPE.SCI_REF, "SCI_REF" },
            { eMSTDATATYPE.SPECULAR, "SPECULAR" },
            { eMSTDATATYPE.TONO, "TONO" },
            { eMSTDATATYPE.TOPO, "TOPO" },
            { eMSTDATATYPE.USAXIAL, "USAXIAL" },
            { eMSTDATATYPE.IOL_CALC, "IOL_CALC" },
            { eMSTDATATYPE.TORIC_IOL_CALC, "TORIC_IOL_CALC" },
            { eMSTDATATYPE.SIGHT, "SIGHT" }
        };
    #endregion
    #region 右眼左眼
    public enum eEyeType {
      RIGHT = 1,
      LEFT,
      BOTH,
      NONE,
      // 以下はSPECULAR2専用
      RIGHT_LEFT,
      LEFT_RIGHT,
      DOUBLE_RIGHT,
      DOUBLE_LEFT,
    }
    public static Dictionary<eEyeType, string> strEyeType = new()
    {
            // ImageFramR,
            { eEyeType.RIGHT, "R" },
            { eEyeType.LEFT, "L" },
            { eEyeType.BOTH, "B" },
            { eEyeType.NONE, "none" },
            // 以下はSPECULAR2専用
            { eEyeType.RIGHT_LEFT, "RL" },
            { eEyeType.LEFT_RIGHT, "LR" },
            { eEyeType.DOUBLE_RIGHT, "RR" },
            { eEyeType.DOUBLE_LEFT, "LL" },
        };
    #endregion

    public static string AxmDeviceType = "AXM2";

    public static string EmptyText = Convert.ToBase64String(Encoding.UTF8.GetBytes("Noi_Npu_Tva_Lue"));   // todo: Common定義

    public static string[] FITTINGS_TYPE = ["none", "contact", "immersion", "contact2", "optlength"];

    public enum FittingsType {
      none = 0,
      contact,
      immersion,
      contact2,
      optlength
    }

    public static string[] SELECT_TYPE = ["none", "average", "median", "select"];

    public enum SelectType {
      none = 0,
      average,
      median,
      select
    }

    public static string[] TARGET_EYE_TYPE = ["none", "Phakic", "Aphakic", "IOL", "Cataract", "ShallowAnteriorChamber", "User_Setting"];

    public enum TargetEyeType {
      none = 0,
      Phakic,
      Aphakic,
      IOL,
      Cataract,
      ShallowAnteriorChamber,
      User_Setting
    }
  }
}

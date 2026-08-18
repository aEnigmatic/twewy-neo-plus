using Il2Cpp;
using Il2CppMaster;

namespace NeoPlus.Helpers;

public static class TextHelper {
    public static string GetName(this Badge? badge) {
        return TextManager.GetText(badge?.ItemName ?? "Com_Blank");
    }
    public static string GetName(this EnemyBase enemy)
        => enemy.EnemyDataMD.GetName();

    public static string GetName(this EnemyData enemyData) {
        var recordId = EnemyReport.GetEnemyRecordID(enemyData);
        var nameId   = MasterDataBase<EnemyReport>.Get(recordId)?.Name;

        return nameId == null
                   ? enemyData.BaseParam.ToString()
                   : TextManager.GetText(nameId);
    }
}
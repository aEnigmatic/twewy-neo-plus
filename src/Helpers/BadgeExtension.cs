using Il2Cpp;
using Il2CppMaster;

namespace NeoPlus.Helpers;

public static class BadgeExtension {
    public static Badge.ELabel GetBadgeId(this Badge badge)
        => (Badge.ELabel) badge.Id;

    public static bool IsMastered(this Badge badge)
        => badge.GetBadgeId().IsMastered();

    public static bool IsOwned(this Badge badge)
        => badge.GetBadgeId().IsOwned();

    public static bool IsKnown(this Badge badge)
        => badge.ItemId.IsKnown();

    public static Badge GetMaster(this Badge.ELabel badgeId)
        => MasterDataBase<Badge>.Get((uint) badgeId);

    public static bool IsMastered(this Badge.ELabel badgeId)
        => SaveLoadController.Get<SaveDataRecord>().IsMasterBadge(badgeId);

    public static bool IsOwned(this Badge.ELabel badgeId)
        => SaveLoadController.Get<SaveDataBadge>().FindFirstByBadgeID(badgeId) != -1;

    public static bool IsKnown(this Badge.ELabel badgeId)
        => badgeId.GetMaster().ItemId.IsKnown();

    public static bool IsKnown(this AllItems.ELabel itemId)
        => SaveLoadController.Get<SaveDataRecord>().IsGetItem(itemId);
}
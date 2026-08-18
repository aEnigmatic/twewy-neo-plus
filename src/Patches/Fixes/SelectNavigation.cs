using HarmonyLib;
using UnityEngine;
using Il2CppUI.Controller;
using Il2CppUI.MainMenu.Badge;
using Il2CppUI.Report;
using Il2CppUI.Shop;

namespace NeoPlus.Patches.Fixes;

[HarmonyPatch]
// ReSharper disable InconsistentNaming
public class SelectNavigation {
    private static KeyCode PrevPage  => Configuration.SelectUiPrevPage.Value;
    private static KeyCode NextPage  => Configuration.SelectUiNextPage.Value;
    private static KeyCode FirstPage => Configuration.SelectUiFirstPage.Value;
    private static KeyCode LastPage  => Configuration.SelectUiLastPage.Value;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIScrollController), nameof(UIScrollController.GetNextSelectObject))]
    public static bool UIScrollController_GetNextSelectObject(UIScrollController __instance) {
        if (__instance.GetSelectIndex() != GetLastPosition(__instance))
            return true;

        MoveToStart(__instance);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIScrollController), nameof(UIScrollController.GetPrevSelectObject))]
    public static bool UIScrollController_GetPrevSelectObject(UIScrollController __instance) {
        if (__instance.GetSelectIndex() != Vector2Int.zero)
            return true;

        MoveToEnd(__instance);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIScrollController), nameof(UIScrollController.OnUpdate))]
    public static void Update(UIScrollController __instance) {
        if (Input.GetKeyDown(FirstPage))
            MoveToStart(__instance);

        else if (Input.GetKeyDown(LastPage))
            MoveToEnd(__instance);

        else if (Input.GetKeyDown(PrevPage))
            MovePage(__instance, UIScrollController.EScroll.Up);

        else if (Input.GetKeyDown(NextPage))
            MovePage(__instance, UIScrollController.EScroll.Down);
    }

    private static Vector2Int GetLastPosition(UIScrollController instance) {
        var itemsPerRow = GetGridSize(instance).x;
        var children    = instance.UIInfoListCount - 1;


        return new Vector2Int(children % itemsPerRow, children / itemsPerRow);
    }

    private static void MoveToEnd(UIScrollController instance) {
        var lastPosition = GetLastPosition(instance);
        var currentPos   = instance.GetSelectIndex();

        if (currentPos == lastPosition)
            return;

        var head = lastPosition with { x = 0, y = Math.Max(0, (int) instance.GetScrollMax()) };

        MoveTo(instance, head, lastPosition);
    }

    private static void MoveToStart(UIScrollController instance) {
        var startPos = Vector2Int.zero;
        if (instance.GetSelectIndex() == startPos)
            return;

        MoveTo(instance, startPos, startPos);
    }

    private static void MovePage(UIScrollController instance, UIScrollController.EScroll direction) {
        var pageCount = GetGridSize(instance).y;
        var offset    = direction == UIScrollController.EScroll.Up ? -pageCount : +pageCount;
        var lastPos   = GetLastPosition(instance);
        var curHead   = instance.mHeadIndex;
        var curPos    = instance.GetSelectIndex();

        var nextPos  = curPos with { y = Math.Clamp(curPos.y + offset, 0, lastPos.y) };
        var nextHead = curHead with { y = Math.Clamp(curHead.y + offset, 0, lastPos.y) };

        if (nextPos == curPos)
            return;

        if (nextPos.y == lastPos.y && nextPos.x > lastPos.x)
            nextPos = nextPos with { x = lastPos.x };

        MoveTo(instance, nextHead, nextPos);
    }

    private static Vector2Int GetGridSize(UIScrollController instance)
        => instance.GetIl2CppType().Name switch {
               // 6x3
               nameof(EquipBadgeScrollController) => new Vector2Int(6, 3),

               // 4x3
               nameof(ShopScrollController)     => new Vector2Int(4, 3),
               nameof(ShopFoodScrollController) => new Vector2Int(4, 3),

               // 1x8
               nameof(NoiseReportScrollController) => new Vector2Int(1, 8),
               nameof(ChapterScrollController)     => new Vector2Int(1, 8),

               // unknown
               _ => new Vector2Int(1, 4),
           };

    private static void MoveTo(UIScrollController instance, Vector2Int nextHead, Vector2Int nextPos) {
        var currentHead = instance.mHeadIndex;
        if (currentHead.y == nextHead.y) {
            // only change position
            instance.SetSelectIndex(nextPos);
            return;
        }

        if (instance is not EquipBadgeScrollController) {
            instance.SetHead(nextHead);
            instance.UpdateShow();
            instance.SetSelectIndex(nextPos);
            return;
        }

        // scroll to head
        var direction = currentHead.y < nextHead.y
                            ? UIScrollController.EScroll.Down
                            : UIScrollController.EScroll.Up;

        foreach (var obj in instance.mSelectObjectList)
            obj?.mIsChangeGroupDecide = true;

        instance.mState              = UIScrollController.EState.Scroll;
        instance.mScroll             = direction;
        instance.mPrevScroll         = UIScrollController.EScroll.None;
        instance.mScrollContinueNum  = 0;
        instance.mScrollContinueTime = 0;
        instance.PreMoveScroll(direction);

        instance.SetHead(nextHead);
        instance.UpdateShow();
        instance.SetSelectIndex(nextPos);
        instance.PostMoveScroll();
    }
}
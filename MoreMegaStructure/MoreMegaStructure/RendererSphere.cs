using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace MoreMegaStructure
{
    //public static class RendererSphere
    //{
    //    //internal static List<DysonSphere> rendererSpheres;

    //    //private static void InitAll() //不能放在Start里加载，会报错
    //    //{
    //    //    rendererSpheres = new List<DysonSphere>();
    //    //    for (int i = 0; i < GameMain.galaxy.starCount; i++)
    //    //    {
    //    //        rendererSpheres.Add(new DysonSphere());
    //    //        rendererSpheres[i].Init(GameMain.data, GameMain.galaxy.stars[i]);
    //    //        rendererSpheres[i].ResetNew();
    //    //        rendererSpheres[i].swarm.bulletMaterial.SetColor("_Color0", new Color(1, 0, 0, 1)); //还有_Color1,2,3但是测试的时候没发现123有什么用
    //    //        rendererSpheres[i].layerCount = -1;
    //    //    }
    //    //}

    //    //[HarmonyPostfix]
    //    //[HarmonyPatch(typeof(GameMain), "Start")]
    //    //public static void GameStartPatch()
    //    //{
    //    //    rendererSpheres = new List<DysonSphere>();
    //    //}

    //    //[HarmonyPrefix]
    //    //[HarmonyPatch(typeof(GameData), "GameTick")]
    //    //public static bool BeforeGameTick()
    //    //{
    //    //    if (rendererSpheres.Count <= 0 && EffectRenderer.effectLevel > 0) // 第二个条件可能导致游戏中更改effectLevel时报错，后续启用时记得更改！
    //    //        InitAll();

    //    //    return true;
    //    //}

    //    //[HarmonyPostfix]
    //    //[HarmonyPatch(typeof(GameData), "GameTick")]
    //    //public static void RSphereGameTick(long time)
    //    //{
    //    //    if (EffectRenderer.effectLevel <= 0) return;
    //    //    foreach (var spheres in rendererSpheres)
    //    //    {
    //    //        spheres.swarm.GameTick(time);
    //    //    }
    //    //}

    //    //[HarmonyPostfix]
    //    //[HarmonyPatch(typeof(GameData), "OnPostDraw")]
    //    //public static void DrawPatch1(GameData __instance)
    //    //{
    //    //    if (EffectRenderer.effectLevel <= 0) return;
    //    //    if (rendererSpheres.Count <= 0) return;
    //    //    if (__instance.localStar != null && DysonSphere.renderPlace == ERenderPlace.Universe)
    //    //    {
    //    //        int index = __instance.localStar.index;
    //    //        if (rendererSpheres[index] != null)
    //    //        {
    //    //            if (rendererSpheres[index].swarm == null) rendererSpheres[index].swarm = new DysonSwarm(rendererSpheres[index]);
    //    //            rendererSpheres[index].DrawPost();
    //    //        }
    //    //    }
    //    //}

    //    //[HarmonyPostfix]
    //    //[HarmonyPatch(typeof(StarmapCamera), "OnPostRender")]
    //    //public static void DrawPatch2(StarmapCamera __instance)
    //    //{
    //    //    if (EffectRenderer.effectLevel <= 0) return;
    //    //    if (rendererSpheres.Count <= 0) return;
    //    //    if (__instance.uiStarmap.viewStarSystem != null && !UIStarmap.isChangingToMilkyWay)
    //    //    {
    //    //        DysonSphere dysonSphere = rendererSpheres[__instance.uiStarmap.viewStarSystem.index];
    //    //        if (dysonSphere != null && DysonSphere.renderPlace == ERenderPlace.Starmap)
    //    //        {
    //    //            dysonSphere.DrawPost();
    //    //        }
    //    //    }
    //    //}

    //    //[HarmonyPostfix]
    //    //[HarmonyPatch(typeof(UIDysonEditor), "DrawDysonSphereMapPost")]
    //    //public static void DrawPatch3(UIDysonEditor __instance)
    //    //{
    //    //    if (EffectRenderer.effectLevel <= 0) return;
    //    //    if (rendererSpheres.Count <= 0) return;
    //    //    if (__instance.selection?.viewDysonSphere != null)
    //    //    {
    //    //        if (DysonSphere.renderPlace == ERenderPlace.Dysonmap)
    //    //        {
    //    //            int index = __instance.selection.viewDysonSphere.starData.index;
    //    //            if (rendererSpheres[index] != null)
    //    //            {
    //    //                rendererSpheres[index].DrawPost();
    //    //            }
    //    //        }
    //    //    }
    //    //}

    //    //[HarmonyPostfix]
    //    //[HarmonyPatch(typeof(UIDysonEditor), "DrawDysonSphereMapPost")]
    //    //public static void DrawPatch4(UIDysonEditor __instance)
    //    //{
    //    //    if (EffectRenderer.effectLevel <= 0) return;
    //    //    if (rendererSpheres.Count <= 0) return;
    //    //    if (__instance.selection?.viewDysonSphere != null)
    //    //    {
    //    //        if (DysonSphere.renderPlace == ERenderPlace.Dysonmap)
    //    //        {
    //    //            int index = __instance.selection.viewDysonSphere.starData.index;
    //    //            if (rendererSpheres[index] != null)
    //    //            {
    //    //                rendererSpheres[index].DrawPost();
    //    //            }                    
    //    //        }
    //    //    }
    //    //}
    //}
}

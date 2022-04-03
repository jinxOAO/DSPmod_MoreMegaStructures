using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using System.Collections.Concurrent;

namespace MoreMegaStructure
{
    class EffectRenderer
    {
        public static int effectLevel = 1; //012

        public static List<bool> megaHasEffect = new List<bool> { false, false, false, false, false, false, false };
        public static List<string> megaRendererSphereUsed = new List<string> {"","","","","","","" };
        public static List<ConcurrentDictionary<int, int>> eBullets = new List<ConcurrentDictionary<int, int>>();
        public static List<int> lastEffectType = new List<int>();

        static Color SNColor = new Color(1, 0, 0, 1);

        public static void InitAll()
        {
            eBullets = new List<ConcurrentDictionary<int, int>>();
            lastEffectType = new List<int>();
            for (int i = 0; i < GameMain.galaxy.starCount; i++)
            {
                eBullets.Add(new ConcurrentDictionary<int, int>());
                lastEffectType.Add(-1);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameData), "GameTick")]
        public static void EffectGameTick(long time)
        {
            
        }

        public static void CreateBullets(int specific = -1)
        {
            int begin = 0;
            int end = GameMain.galaxy.starCount;
            if(specific >= 0 && specific<end)
            {
                begin = specific;
                end = begin + 1;
            }
            for (int i = begin; i < end; i++)
            {
                if(megaHasEffect[1] && MoreMegaStructure.StarMegaStructureType[i] == 1)
                {
                    CreateScienceNexusBullet(i);
                }
                else if(megaHasEffect[2] && MoreMegaStructure.StarMegaStructureType[i] == 2)
                {

                }
            }
        }


        static void CreateScienceNexusBullet(int starIndex)
        {
            DysonSphere rendererSphere = RendererSphere.rendererSpheres[starIndex];
            eBullets[starIndex].Clear();
            RendererSphere.rendererSpheres[starIndex].swarm.bulletMaterial.SetColor("_Color0", SNColor);
            VectorLF3 starCenter = GameMain.galaxy.stars[starIndex].uPosition;
            DysonSphere oriSphere = GameMain.data.dysonSpheres[starIndex];
            float maxRadius = 0;
            for (int i = 0; i < oriSphere.layersIdBased.Length; i++)
            {
                if (oriSphere.layersIdBased[i] == null) continue;
                float radius = oriSphere.layersIdBased[i].orbitRadius;
                maxRadius = radius > maxRadius ? radius : maxRadius;
            }
            float eRadius = maxRadius * 1.2f;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(DysonSphere), "GameTick")]
        public static void DrawEffect()
        {
            if (effectLevel <= 0) return;
        }

    }
}

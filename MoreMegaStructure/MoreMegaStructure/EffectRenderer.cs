using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using System.Collections.Concurrent;

namespace MoreMegaStructure
{
    class EffectRenderer
    {
        public static int effectLevel = 0; //012

        public static List<bool> megaHasEffect = new List<bool> { false, true, true, false, false, false, false };
        //public static List<string> megaRendererSphereUsed = new List<string> {"","","","","","","" };
        public static List<ConcurrentDictionary<int, int>> eBullets = new List<ConcurrentDictionary<int, int>>();
        public static List<int> lastEffectType = new List<int>();

        static Color MDColor = new Color(0, 1, 1, 1);
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
            if (effectLevel <= 0) return;
            int starCount = GameMain.galaxy.starCount;
            for (int i = 0; i < starCount; i++)
            {
                int type = MoreMegaStructure.StarMegaStructureType[i];
                if(megaHasEffect[type])
                {
                    if (lastEffectType[i] != type && type!=0)
                    {
                        eBullets[i].Clear();
                        CreateBullets(i);
                    }
                    else
                    {
                        DysonSwarm swarm = RendererSphere.rendererSpheres[i]?.swarm;
                        if(type == 1)
                        {
                            CreateBullets(i);
                            foreach (var item in eBullets[i])
                            {
                                try
                                {
                                    if (swarm.bulletPool[item.Key].maxt - swarm.bulletPool[item.Key].t <= 0f)
                                    {
                                        swarm.RemoveBullet(item.Key);
                                        int v;
                                        eBullets[i].TryRemove(item.Key, out v);
                                    }
                                }
                                catch (Exception)
                                { }
                            }
                        }
                        else if (type == 2 && eBullets[i].Count == 3)
                        {
                            VectorLF3[] pos = GetNexusBulletPositions(i, time);
                            foreach (var item in eBullets[i])
                            {
                                int bulletId = item.Key;
                                int bulletDelta = item.Value;
                                swarm.bulletPool[bulletId].uBegin = pos[bulletDelta];
                                swarm.bulletPool[bulletId].uEnd = pos[bulletDelta+3];
                                swarm.bulletPool[bulletId].t = 0.017f;
                            }
                        }
                        else if(type == 3)
                        {

                        }
                        else
                        {
                            lastEffectType[i] = -1;
                        }
                    }
                    
                }
            }
        }

        public static void CreateBullets(int specific = -1)
        {
            long time = GameMain.instance.timei;
            int begin = 0;
            int end = GameMain.galaxy.starCount;
            if(specific >= 0 && specific<end)
            {
                begin = specific;
                end = begin + 1;
            }
            for (int i = begin; i < end; i++)
            {
                int type = MoreMegaStructure.StarMegaStructureType[i];
                if (megaHasEffect[1] && type == 1)
                {
                    CreateMatterDecompressorBullet(i);
                }
                else if(megaHasEffect[2] && type == 2)
                {
                    CreateScienceNexusBullet(i, time);
                }
                lastEffectType[i] = type;
            }

        }


        static void CreateMatterDecompressorBullet(int starIndex)
        {
            DysonSwarm swarm = RendererSphere.rendererSpheres[starIndex].swarm;
            swarm.bulletMaterial.SetColor("_Color0", MDColor);
            DysonSphereLayer layer = null;
            DysonSphere oriSphere = GameMain.data.dysonSpheres[starIndex];
            for (int i = 0; i < oriSphere.layersIdBased.Length; i++)
            {
                if(oriSphere.layersIdBased[i]!=null)
                {
                    layer = oriSphere.layersIdBased[i];
                    break;
                }
            }
            if (layer == null) return;
            VectorLF3 starCenter = GameMain.galaxy.stars[starIndex].uPosition;
            int laserLeft = effectLevel * 20;
            for (int j = 0; j < layer.nodeCursor && laserLeft >0; j++)
            {
                if (layer.nodePool[j] == null)
                    continue;
                VectorLF3 endPByNode = layer.NodeUPos(layer.nodePool[j]);
                int bulletIndex = swarm.AddBullet(new SailBullet
                {
                    maxt = 0.01f,
                    lBegin = new Vector3(0, 0, 1),
                    uEndVel = new Vector3(0, 0, 1),
                    uBegin = endPByNode,
                    uEnd = endPByNode + (starCenter-endPByNode).normalized * ((starCenter - endPByNode).magnitude - 2000)
                }, 0);
                swarm.bulletPool[bulletIndex].state = 0;
                eBullets[starIndex].AddOrUpdate(bulletIndex, 0, (x, y) => 0);
                laserLeft -= 1;
            }
        }

        static void CreateScienceNexusBullet(int starIndex, long time)
        {
            DysonSwarm swarm = RendererSphere.rendererSpheres[starIndex].swarm;
            eBullets[starIndex].Clear();
            swarm.bulletMaterial.SetColor("_Color0", SNColor);
            VectorLF3[] pos = GetNexusBulletPositions(starIndex, time);
            for (int i = 0; i < 3; i++)
            {
                int bulletIndex = swarm.AddBullet(new SailBullet
                {
                    t=0.017f,
                    maxt = 0.1f,
                    lBegin = new Vector3(0, 0, 0),
                    uEndVel = new Vector3(0, 0, 0),
                    uBegin = pos[i],
                    uEnd = pos[i+3]
                }, 1);
                swarm.bulletPool[bulletIndex].state = 0;
                eBullets[starIndex].AddOrUpdate(bulletIndex, i, (x, y) => i);
            }
        }


        static VectorLF3[] GetNexusBulletPositions(int starIndex, long time)
        {
            DysonSphere oriSphere = GameMain.data.dysonSpheres[starIndex];
            VectorLF3 starCenter = GameMain.galaxy.stars[starIndex].uPosition;
            float maxRadius = 0;
            Quaternion rot = Quaternion.LookRotation(new Vector3(0, 0, 1));
            for (int i = 0; i < oriSphere.layersIdBased.Length; i++)
            {
                if (oriSphere.layersIdBased[i] == null) continue;
                float radius = oriSphere.layersIdBased[i].orbitRadius;
                if (radius > maxRadius)
                {
                    maxRadius = radius;
                    rot = oriSphere.layersIdBased[i].orbitRotation;
                }
            }
            float eRadius = maxRadius * 1.2f;
            VectorLF3 front = Maths.QRotateLF(rot, new VectorLF3(0, 0, 1));
            VectorLF3 vert1 = Maths.QRotateLF(rot, new VectorLF3(0, 1, 0));
            VectorLF3 vert2 = Maths.QRotateLF(rot, new VectorLF3(0.866, -0.5, 0));
            VectorLF3 vert3 = Maths.QRotateLF(rot, new VectorLF3(-0.866, -0.5, 0));

            VectorLF3 pos1 = Quaternion.AngleAxis(time % 30 * 360, vert1) * front * eRadius;
            VectorLF3 pos2 = Quaternion.AngleAxis((time + 10) % 30 * 360, vert2) * front * eRadius;
            VectorLF3 pos3 = Quaternion.AngleAxis((time + 20) % 30 * 360, vert3) * front * eRadius;
            VectorLF3 pos1Next = Quaternion.AngleAxis(((time+1) % 30) * 360, vert1) * front * eRadius;
            VectorLF3 pos2Next = Quaternion.AngleAxis(((time + 11) % 30) * 360, vert2) * front * eRadius;
            VectorLF3 pos3Next = Quaternion.AngleAxis(((time + 21) % 30) * 360, vert3) * front * eRadius;
            pos1 += starCenter;
            pos2 += starCenter;
            pos3 += starCenter;
            pos1Next += starCenter;
            pos2Next += starCenter;
            pos3Next += starCenter;
            return new VectorLF3[] { pos1, pos2, pos3, pos1Next, pos2Next, pos3Next };
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(DysonSphere), "GameTick")]
        public static void DrawEffect()
        {
            if (effectLevel <= 0) return;
        }

    }
}

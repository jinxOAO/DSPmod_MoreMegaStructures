using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MoreMegaStructure
{
    /// <summary>
    /// 模型层的**入口**：谁来调 Builder、什么时候调、当前该用哪一层级表（E14 / R34）。
    ///
    /// 三个未定点里"如何进入这个类 / 如何开始初始化"的答案都在这一个文件：
    /// · 初始化时机：<see cref="InitWhenFirstLoad"/> 由主类在 Import / IntoOtherSave 末尾各调一次，
    ///   渲染器第一帧再兜一次 <see cref="EnsureInit"/>（幂等，所以三重接线只会真跑一次）；
    ///   为什么要有兜底：新开局是否必定走 Import 我**没有取证**（文档 P30 第 1 条的未证实项）。
    /// · 当前该画哪一档：<see cref="CurrentLayers"/>，按 DysonSphere 的实际发电量选级。
    /// · 渲染器需不需要某种部件：<see cref="AnyLayerUses"/>，让"层级里到底用没用这东西"反过来决定资产装载。
    /// </summary>
    public static class ModelHandler
    {
        public static bool inited = false;

        /// <summary>
        /// 放在主类的Import末尾即可
        /// （实际还加了 IntoOtherSave 末尾 + 渲染第一帧的 EnsureInit 兜底，理由见类注释里那条未证实项。）
        /// </summary>
        public static void InitWhenFirstLoad()
        {
            // 幂等：Builder 的产物是"相当于字典或者常量的东西"，首次读档建一次之后就一直复用；
            // 反复读档也不重建 —— 重建本身不贵（纯数据），但会让正在被渲染的层对象被换掉，没必要冒这个风险。
            if (inited && WarpArrayModelBuilder.layersByLevel != null) return;
            WarpArrayModelBuilder.InitModelLayers();

            inited = true;
        }

        /// <summary>渲染器每帧第一行的保险：万一两条读档回调都没来过（例如新开局不走 Import），这里也能把层建出来。</summary>
        public static void EnsureInit()
        {
            if (!inited || WarpArrayModelBuilder.layersByLevel == null) InitWhenFirstLoad();
        }

        /// <summary>
        /// 这一帧该画的那一层集合。取不到就返回 null，渲染器据此什么都不画（不崩、不猜）。
        /// </summary>
        /// <param name="sphere">当前恒星的戴森球数据；null（这颗星还没有戴森球）时按最低档画。</param>
        public static List<ModelLayer> CurrentLayers(DysonSphere sphere)
        {
            List<List<ModelLayer>> all = WarpArrayModelBuilder.layersByLevel;
            if (all == null || all.Count == 0) return null;
            int lvl = CurrentLevel(sphere);
            if (lvl < 0) lvl = 0;
            if (lvl >= all.Count) lvl = all.Count - 1;
            return all[lvl];
        }

        /// <summary>本巨构当前用的层级表**序号**（不是层数）。抽出来公开是为了日志能自证"选级这条路真的在跑"。</summary>
        public static int CurrentLevel(DysonSphere sphere)
        {
            List<long> th = WarpArrayModelBuilder.energyThreshold;
            List<List<ModelLayer>> all = WarpArrayModelBuilder.layersByLevel;
            if (th == null || th.Count == 0 || all == null || all.Count == 0) return 0;

            // 原文事实（文档 P30 第 2 条）：发电量是 DysonSphere 上的 public long **字段** energyGenCurrentTick，
            // 每逻辑 tick 在 DysonSphere.cs:1452-1493 重算（帆 + 各层节点求和后还乘了虫群削弱系数）。
            // 官方自己拿它当阈值判据的先例：ADV_DysonSphereEnergy.cs:37。
            long energy = sphere != null ? sphere.energyGenCurrentTick : 0L;

            // 阈值表按"从低到高"命中最后一个满足项 ⇒ 能量越高档位越高。
            // 一档都没达到时**仍然画最低档**（返回 0）：需求 R8 要求远景必须看得见这座巨构，
            // "能量不够就整个不出现"会让验收现场看起来像渲染坏了。
            int lvl = 0;
            for (int i = 0; i < th.Count; i++)
            {
                if (energy >= th[i]) lvl = i;
                else break;
            }
            if (lvl >= all.Count) lvl = all.Count - 1;

            LogLevelOnce(lvl, energy, all.Count);
            return lvl;
        }

        private static int loggedLevel = int.MinValue;
        /// <summary>只在档位变化（含第一次）时说一行：选级链路一旦跑通，画面看不出差别是常态，日志是唯一判据。</summary>
        private static void LogLevelOnce(int lvl, long energy, int levels)
        {
            if (lvl == loggedLevel) return;
            bool first = loggedLevel == int.MinValue;
            loggedLevel = lvl;
            List<ModelLayer> ls = WarpArrayModelBuilder.layersByLevel != null && lvl < levels ? WarpArrayModelBuilder.layersByLevel[lvl] : null;
            int layerCount = ls != null ? ls.Count : 0;
            int entryCount = 0;
            if (ls != null) for (int i = 0; i < ls.Count; i++) if (ls[i] != null) entryCount += ls[i].entries.Count;
            Utils.Log("[MMS model] " + (first ? "选级" : "换级") + " level=" + lvl + "/" + levels +
                      " energyGenCurrentTick=" + energy + " 阈值=[" + string.Join(",", WarpArrayModelBuilder.energyThreshold.ConvertAll<string>(x => x.ToString()).ToArray()) + "]" +
                      " 层=" + layerCount + " 条目=" + entryCount);
        }

        /// <summary>
        /// 所有档位的所有层里，**有没有**任何一条条目用到这个 modelId。
        /// 渲染器用它决定要不要去装载那只 prefab（例：needReceiver = AnyLayerUses(RayReceiver)）——
        /// "画不画某类部件"从此是 Builder 的事，渲染器不再有对应的开关（旧的 showSubStructure 就是这么退役的，见文档 P30 第 6 条）。
        /// </summary>
        public static bool AnyLayerUses(int modelId)
        {
            EnsureInit();
            List<List<ModelLayer>> all = WarpArrayModelBuilder.layersByLevel;
            if (all == null) return false;
            for (int lvl = 0; lvl < all.Count; lvl++)
            {
                List<ModelLayer> layers = all[lvl];
                if (layers == null) continue;
                for (int i = 0; i < layers.Count; i++)
                {
                    ModelLayer layer = layers[i];
                    if (layer == null) continue;
                    List<ModelEntry> es = layer.entries;
                    for (int k = 0; k < es.Count; k++)
                    {
                        if (es[k].kind == EModelEntryKind.Model && es[k].modelId == modelId) return true;
                    }
                }
            }
            return false;
        }
    }
}

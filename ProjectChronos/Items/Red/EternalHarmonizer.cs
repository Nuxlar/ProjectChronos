using System;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using UnityEngine;

namespace ProjectChronos.Items.Red
{
    public class EternalHarmonizer : ItemBase<EternalHarmonizer>
    {
        public ConfigEntry<bool> enable;
        public override string ItemName => "Eternal Harmonizer";

        public override string ItemLangTokenName => "ETERNALHARMONIZER";

        public override string ItemPickupDesc => "Increases move speed by +25% and decreases slow debuffs by -50%";

        public override string ItemFullDescription => $"Increases move speed by <style=cIsUtility>25%</style><style=cStack> (+25% per stack)</style> and decreases applied slows by <style=cIsUtility>50%</style><style=cStack> (+25% per stack)</style>. Slow reduction caps out at <style=cIsUtility>3 stacks</style>.";

        public override string ItemLore =>
        """
        
        <style=cMono>Mission Procurement File B-14a</style>

        During an excursion to a long abandoned planetary base, we managed to retrieve a strange crystal that seemingly accelerates time around itself. The cadet who made the discovery was found almost transparent, vibrating in-place at an incomprensible speed. This power could be invaluable to our elite forces if we can harness it properly.
       
        I recommend assigning the crystal to the research and development team at Project 'Chronos'.
        """;

        public override ItemTier Tier => ItemTier.Tier3;

        public override GameObject ItemModel => ProjectChronos.assetBundle.LoadAsset<GameObject>("assets/harmonizeritemnux.prefab");

        public override Sprite ItemIcon => ProjectChronos.assetBundle.LoadAsset<Sprite>("assets/texharmonizericon.png");

        public override void Init(ConfigFile config)
        {
            CreateConfig(config);
            CreateLang();
            CreateItem();
            Hooks();
        }

        public override void CreateConfig(ConfigFile config)
        {
            enable = config.Bind<bool>("Items", ItemName, true, "Should this item be enabled.");
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return new ItemDisplayRuleDict(Array.Empty<ItemDisplayRule>());
        }

        public override void Hooks()
        {
            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
            IL.RoR2.CharacterBody.RecalculateStats += ReducesSlows;
        }

        private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (sender && sender.inventory)
            {
                int itemCount = GetCount(sender);
                if (itemCount > 0)
                {
                    args.moveSpeedMultAdd += 0.25f * itemCount;
                }
            }
        }

        private void ReducesSlows(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            int totalSlowIdx = -1;
            int idx1 = -1;
            bool ilFound = c.TryGotoNext(
                x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.baseMoveSpeed))
            ) && c.TryGotoNext(
                x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.levelMoveSpeed))
            ) && c.TryGotoNext(
                 x => x.MatchStloc(out idx1)
             ) && c.TryGotoNext(MoveType.Before,
                 x => x.MatchLdloc(idx1),
                 x => x.MatchLdloc(out _),
                 x => x.MatchLdloc(out totalSlowIdx),
                 x => x.MatchDiv(),
                 x => x.MatchMul(),
                 x => x.MatchStloc(idx1));

            if (ilFound)
            {
                c.TryGotoPrev(x => x.MatchLdloc(totalSlowIdx));
                c.Index += 1;
                c.Emit(OpCodes.Ldarg, 0);
                c.EmitDelegate<Func<float, CharacterBody, float>>((totalSlow, body) =>
                {
                    int itemCount = GetCount(body);
                    float extraSlow = totalSlow - 1f;
                    if (itemCount > 0 && extraSlow > 0f)
                    {
                        float slowReduction = Mathf.Clamp01(0.5f + (0.25f * (itemCount - 1)));
                        return totalSlow - (extraSlow * slowReduction);
                    }
                    else
                        return totalSlow;
                });
            }
        }
    }
}

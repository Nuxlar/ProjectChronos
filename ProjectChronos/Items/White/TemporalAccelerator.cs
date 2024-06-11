using System;
using BepInEx.Configuration;
using R2API;
using RoR2;
using UnityEngine;

namespace ProjectChronos.Items.White
{
    public class TemporalAccelerator : ItemBase<TemporalAccelerator>
    {
        public ConfigEntry<bool> enable;
        public override string ItemName => "Temporal Accelerator";

        public override string ItemLangTokenName => "TEMPORALACCELERATOR";

        public override string ItemPickupDesc => "Increase the duration of damage over time effects.";

        public override string ItemFullDescription => $"Increase the duration of damage over time effects by <style=cIsUtility>25%</style> <style=cStack>(+15% per stack)</style>.";

        public override string ItemLore =>
        """
        <style=cMono>========================================
        ====   Timekeeper's Cipher   ====
        ====    [Version 2.12.05]    ====
        ========================================
        Translating… <100000000 cycles>
        Translating… <523260 cycles>
        Translation Complete
        Displaying Results
        ========================================</style>
        
        A design of ingenious cruelty created out of necessity. This device uses temporal waves localized around inflicted wounds, accelerating the rate of decay but also the rate of healing. This creates an agonizing loop, prolonging the suffering of the victim far beyond resistance. 
        
        Early results are promising but the ethical implications are... concerning. Should we as protectors even have such a device? This is beyond me to decide or even question, but I feel like I must do something...
        """;

        public override ItemTier Tier => ItemTier.Tier1;

        public override GameObject ItemModel => ProjectChronos.assetBundle.LoadAsset<GameObject>("assets/acceleratoritemnux.prefab");

        public override Sprite ItemIcon => ProjectChronos.assetBundle.LoadAsset<Sprite>("assets/texacceleratoricon.png");

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
            On.RoR2.DotController.AddDot += IncreaseDotDuration2;
        }

        private void IncreaseDotDuration2(On.RoR2.DotController.orig_AddDot orig, DotController self, GameObject attackerObject, float duration, DotController.DotIndex dotIndex, float damageMultiplier, uint? maxStacksFromAttacker, float? totalDamage, DotController.DotIndex? preUpgradeDotIndex)
        {
            if (attackerObject && attackerObject.GetComponent<CharacterBody>())
            {
                int itemCount = GetCount(attackerObject.GetComponent<CharacterBody>());
                if (itemCount > 0)
                {
                    duration *= 1.25f + (0.15f * (itemCount - 1));
                }
            }
            orig(self, attackerObject, duration, dotIndex, damageMultiplier, maxStacksFromAttacker, totalDamage, preUpgradeDotIndex);
            if (dotIndex == DotController.DotIndex.Burn || dotIndex == DotController.DotIndex.StrongerBurn)
            {
                if (attackerObject && attackerObject.GetComponent<CharacterBody>())
                {
                    int itemCount = GetCount(attackerObject.GetComponent<CharacterBody>());
                    if (itemCount > 0)
                    {
                        foreach (DotController.DotStack stack in self.dotStackList)
                        {
                            if (stack.dotIndex == dotIndex && stack.attackerObject == attackerObject)
                            {
                                stack.timer *= 1.25f + (0.15f * (itemCount - 1));
                            }
                        }
                    }

                }
            }
        }
    }
}

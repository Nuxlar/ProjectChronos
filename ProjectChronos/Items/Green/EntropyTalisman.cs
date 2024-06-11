/*
using System;
using BepInEx.Configuration;
using R2API;
using RoR2;
using UnityEngine;

namespace ChronoItems.Items.Green
{
    public class EntropyTalisman : ItemBase<EntropyTalisman>
    {
        public ConfigEntry<bool> enable;
        public override string ItemName => "Entropy Talisman";

        public override string ItemLangTokenName => "ENTROPYTALISMAN";

        public override string ItemPickupDesc => "Chance to Decay an enemy on hit.";

        public override string ItemFullDescription => $"<style=cIsUtility>15%</style> <style=cStack>(+15% damage per stack)</style> chance to <style=cIsDamage>Decay</style> an enemy. Decay is a stacking DoT that deals <style=cIsDamage>600% base damage</style> over 5 seconds. Each stack increases the 5 second duration by 25%.";

        public override string ItemLore => "The Entropy Talisman is a relic from a forgotten civilization, said to hold the power to unravel time itself. Wielding it comes at the cost of watching the world around you slowly fall into chaos.";

        public override ItemTier Tier => ItemTier.Tier2;

        public override GameObject ItemModel => ChronoItems.assetBundle.LoadAsset<GameObject>("assets/coinitemnux.prefab");

        public override Sprite ItemIcon => ChronoItems.assetBundle.LoadAsset<Sprite>("assets/textalismanicon.png");

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

        }

    }
}
*/
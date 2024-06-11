// Util.CheckRoll(this.crit, this.master)
using System;
using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace ProjectChronos.Items.White
{
    public class IncendiaryRounds : ItemBase<IncendiaryRounds>
    {
        public ConfigEntry<bool> enable;
        public override string ItemName => "Incendiary Rounds";

        public override string ItemLangTokenName => "INCENDIARYROUNDS";

        public override string ItemPickupDesc => "10% chance to ignite enemies on hit.";

        public override string ItemFullDescription => $"<style=cIsUtility>10%</style> <style=cStack>(+10% per stack)</style> chance to <style=cIsDamage>ignite</style> enemies on hit.";

        public override string ItemLore => """
        <style=cMono>Welcome to DataScraper (v3.1.53)
        $ Scraping memory...
        $ Resolving...
        Complete</style>

        Field Report: P-14 'Cinder'.
        Location: [REDACTED].
        Objective: Test efficacy of new prototype rounds.
        Casualties: 9.
        Status: FAILURE.
        Due unforseen circumstances, an uncontrollable chemical fire erupted on the test site, leading to catastrophic damage and loss of life. This experimental munition will be collected and stored on [REDACTED] indefinitely.

        Project 'Cinder' is to be shut down immediately with no further funding.
        """;

        public override ItemTier Tier => ItemTier.Tier1;

        public override GameObject ItemModel => ProjectChronos.assetBundle.LoadAsset<GameObject>("assets/incinrounds.prefab");

        public override Sprite ItemIcon => ProjectChronos.assetBundle.LoadAsset<Sprite>("assets/texincendiaryicon.png");

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
            On.RoR2.GlobalEventManager.OnHitEnemy += ApplyIncin;
        }

        private void ApplyIncin(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            orig(self, damageInfo, victim);
            if (damageInfo.procCoefficient <= 0.0 || damageInfo.rejected || !NetworkServer.active || !(bool)damageInfo.attacker)
                return;
            CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            CharacterBody victimBody = victim.GetComponent<CharacterBody>();
            if (attackerBody && victimBody)
            {
                int itemCount = GetCount(attackerBody);
                if (itemCount > 0)
                {
                    CharacterMaster master = attackerBody.master;
                    Inventory inventory = master.inventory;
                    if (Util.CheckRoll(10f * itemCount, attackerBody.master))
                    {
                        uint? maxStacksFromAttacker = new uint?();
                        if ((bool)damageInfo?.inflictor)
                        {
                            ProjectileDamage component = damageInfo.inflictor.GetComponent<ProjectileDamage>();
                            if ((bool)component && component.useDotMaxStacksFromAttacker)
                                maxStacksFromAttacker = new uint?(component.dotMaxStacksFromAttacker);
                        }
                        float num = 0.5f;
                        InflictDotInfo inflictDotInfo = new InflictDotInfo()
                        {
                            attackerObject = damageInfo.attacker,
                            victimObject = victim,
                            totalDamage = new float?(damageInfo.damage * num),
                            damageMultiplier = 1f,
                            dotIndex = DotController.DotIndex.Burn,
                            maxStacksFromAttacker = maxStacksFromAttacker
                        };
                        StrengthenBurnUtils.CheckDotForUpgrade(inventory, ref inflictDotInfo);
                        DotController.InflictDot(ref inflictDotInfo);
                    }
                }
            }
        }

    }
}

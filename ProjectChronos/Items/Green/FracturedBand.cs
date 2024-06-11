using System;
using BepInEx.Configuration;
using ProjectChronos.EntityStates;
using EntityStates;
using R2API;
using RoR2;
using RoR2.Items;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace ProjectChronos.Items.Green
{
    public class FracturedBand : ItemBase<FracturedBand>
    {
        public ConfigEntry<bool> enable;
        public override string ItemName => "Fractured Band";

        public override string ItemLangTokenName => "FRACTUREDBAND";

        public override string ItemPickupDesc => "Hits that deal <style=cIsDamage>more than 400% damage</style> also create a <style=cIsUtility>Chronosphere</style> that <style=cIsUtility>freezes enemies and projectiles in time</style> for <style=cIsUtility>5</style> seconds. Recharges every <style=cIsUtility>20</style> seconds.";

        public override string ItemFullDescription => $"Hits that deal <style=cIsDamage>more than 400% damage</style> also create a <style=cIsUtility>Chronosphere</style> that <style=cIsUtility>freezes enemies and projectiles in time</style> for <style=cIsUtility>5</style> seconds. Recharges every <style=cIsUtility>20</style> <style=cStack>(-2 per stack)</style> seconds. Caps at <style=cIsUtility>10</style> seconds.";

        public override string ItemLore => """
        <style=cMono>Welcome to DataScraper (v3.1.53)
        $ Scraping memory...
        $ Resolving...
        Complete</style>
        Ain't nothing here yet.
        """;

        public override ItemTier Tier => ItemTier.Tier2;

        public override GameObject ItemModel => ProjectChronos.assetBundle.LoadAsset<GameObject>("assets/someband.prefab");

        public override Sprite ItemIcon => ProjectChronos.assetBundle.LoadAsset<Sprite>("assets/texbandicon.png");

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
            On.RoR2.CharacterBody.AddTimedBuff_BuffIndex_float += EnterStasis;
            On.RoR2.GlobalEventManager.OnHitEnemy += CreateChronosphere;

            CharacterBody.onBodyInventoryChangedGlobal += new Action<CharacterBody>(this.AddItemBehavior);
        }

        private void AddItemBehavior(CharacterBody body)
        {
            body.AddItemBehavior<FracturedBandItemBehavior>(body.inventory.GetItemCount(ItemBase<FracturedBand>.instance.ItemDef));
        }

        private void EnterStasis(On.RoR2.CharacterBody.orig_AddTimedBuff_BuffIndex_float orig, CharacterBody self, BuffIndex buffIndex, float duration)
        {
            if (buffIndex == ProjectChronos.timeFrozenBuff.buffIndex)
            {
                EntityStateMachine[] machines = self.GetComponents<EntityStateMachine>();
                foreach (EntityStateMachine machine in machines)
                {
                    if (machine.customName == "Body" && machine.state is not TimeFreeze)
                    {
                        machine.SetInterruptState(new TimeFreeze(), InterruptPriority.Frozen);
                    }
                }
            }
            orig(self, buffIndex, duration);
        }

        private void CreateChronosphere(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            orig(self, damageInfo, victim);
            if (damageInfo.procCoefficient == 0.0 || damageInfo.rejected || !NetworkServer.active || !(bool)damageInfo.attacker || damageInfo.procCoefficient <= 0.0)
                return;
            CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            if (attackerBody)
            {
                int itemCount = GetCount(attackerBody);
                if (itemCount > 0)
                {
                    if ((double)damageInfo.damage / attackerBody.damage >= 4.0)
                    {
                        if (attackerBody.HasBuff(ProjectChronos.timeBandOnBuff))
                        {
                            int cdDuration = Mathf.Clamp(20 - ((itemCount * 2) - 2), 10, 20);
                            attackerBody.RemoveBuff(ProjectChronos.timeBandOnBuff);
                            for (int duration = 1; duration <= cdDuration; ++duration)
                                attackerBody.AddTimedBuff(ProjectChronos.timeBandOffBuff, duration);

                            // Create a Chronosphere
                            ProjectileManager.instance.FireProjectile(ProjectChronos.chronosphereEffect, victim.transform.position, Quaternion.identity, attackerBody.gameObject, 0.0f, 0.0f, false);
                        }
                    }
                }
            }
        }

        public sealed class FracturedBandItemBehavior : CharacterBody.ItemBehavior
        {
            private void OnDisable()
            {
                // ProjectChronos.timeBandOnBuff
                // ProjectChronos.timeBandOffBuff
                if (!(bool)this.body)
                    return;
                if (this.body.HasBuff(ProjectChronos.timeBandOnBuff))
                    this.body.RemoveBuff(ProjectChronos.timeBandOnBuff);
                if (!this.body.HasBuff(ProjectChronos.timeBandOffBuff))
                    return;
                this.body.RemoveBuff(ProjectChronos.timeBandOffBuff);
            }

            private void FixedUpdate()
            {
                bool flag1 = this.body.HasBuff(ProjectChronos.timeBandOffBuff);
                bool flag2 = this.body.HasBuff(ProjectChronos.timeBandOnBuff);
                if (!flag1 && !flag2)
                    this.body.AddBuff(ProjectChronos.timeBandOnBuff);
                if (!(flag2 & flag1))
                    return;
                this.body.RemoveBuff(ProjectChronos.timeBandOnBuff);
            }
        }

    }
}
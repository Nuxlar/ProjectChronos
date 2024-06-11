using System;
using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace ProjectChronos.Items.White
{
    public class TimeShardDagger : ItemBase<TimeShardDagger>
    {
        public ConfigEntry<bool> enable;
        public override string ItemName => "Time-Shard Dagger";

        public override string ItemLangTokenName => "TIMESHARDDAGGER";

        public override string ItemPickupDesc => "10% chance to decay enemies on hit.";
        // interval = 0.25f, damageCoefficient = 0.3f for 5 seconds 30% per tick 4 ticks per second 120% per second 600% over 5 seconds
        public override string ItemFullDescription => $"<style=cIsUtility>10%</style> <style=cStack>(+10% damage per stack)</style> chance to <style=cIsDamage>Decay</style> an enemy.";

        public override string ItemLore =>
        """
        <style=cMono>//-- AUTO-TRANSCRIPTION FROM UES [REDACTED] --//</style>
        
        I couldn't wield it for long. 
        The blade was cold to the touch, the edges seemed to shimmer with a light that wasn't even there. 
        I could feel my mind slipping, my thoughts becoming disjointed and erratic. 
        My psyche being torn apart into directions I couldn't even comprehend.
        I had to put it down.
        I had to get away from it.
        I... I'm still holding it...
        How... how long has it been?
        I... i..... ..........
        """;

        public override ItemTier Tier => ItemTier.Tier1;

        public override GameObject ItemModel => ProjectChronos.assetBundle.LoadAsset<GameObject>("assets/sharddagger.prefab");

        public override Sprite ItemIcon => ProjectChronos.assetBundle.LoadAsset<Sprite>("assets/texdaggericon.png");

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
            On.RoR2.GlobalEventManager.OnHitEnemy += ApplyDecay;
        }
        private void ApplyDecay(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
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
                    if (Util.CheckRoll(10f * itemCount, attackerBody.master))
                    {
                        InflictDotInfo inflictDotInfo = new InflictDotInfo()
                        {
                            attackerObject = damageInfo.attacker,
                            victimObject = victim,
                            damageMultiplier = 0f,
                            duration = 5f,
                            dotIndex = ProjectChronos.decayDot,
                        };
                        DotController.InflictDot(ref inflictDotInfo);
                    }
                }
            }
        }
    }
}
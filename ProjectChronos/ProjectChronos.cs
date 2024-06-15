using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using R2API;
using ProjectChronos.Items;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using RoR2.Projectile;

namespace ProjectChronos
{
  [BepInPlugin("com.Nuxlar.ProjectChronos", "ProjectChronos", "0.6.3")]

  public class ProjectChronos : BaseUnityPlugin
  {
    public static AssetBundle assetBundle;
    public static GameObject acceleratorItem;
    public static GameObject coinItem;
    public List<ItemBase> itemList = new List<ItemBase>();
    public static BuffDef timeFrozenBuff;
    public static BuffDef timeBandOnBuff;
    public static BuffDef timeBandOffBuff;
    public static BuffDef decayBuff;
    public static DotController.DotIndex decayDot;
    public static DotAPI.CustomDotBehaviour decayDotBehaviour;
    public static DotAPI.CustomDotVisual decayDotVisual;
    public GameObject decayEffectPrefab = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/CrippleEffect.prefab").WaitForCompletion(), "DecayEffect", false);
    public GameObject timeFrozenEffect = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/CrippleEffect.prefab").WaitForCompletion(), "TimeFrozenEffect", false);
    public static GameObject chronosphereEffect = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Railgunner/RailgunnerMineAltDetonated.prefab").WaitForCompletion(), "Chronosphere", false);
    private Material timeFrozenMaterial = Addressables.LoadAssetAsync<Material>("RoR2/Base/mysteryspace/matSuspendedInTime.mat").WaitForCompletion();
    private BuffDef jailerBuff = Addressables.LoadAssetAsync<BuffDef>("RoR2/DLC1/VoidJailer/bdJailerSlow.asset").WaitForCompletion();

    public void Awake()
    {
      SetupVFX();

      ProjectChronos.assetBundle = AssetBundle.LoadFromFile(Assembly.GetExecutingAssembly().Location.Replace("ProjectChronos.dll", "chronositems.bundle"));
      GameObject why = ProjectChronos.assetBundle.LoadAsset<GameObject>("assets/incinrounds.prefab");
      why.transform.GetChild(0).localPosition = Vector3.zero;

      CreateBuffs();
      CreateDoTs();

      foreach (Transform child in chronosphereEffect.transform)
      {
        Destroy(child.gameObject);
      }

      chronosphereEffect.GetComponent<SlowDownProjectiles>().slowDownCoefficient = 0f;
      chronosphereEffect.AddComponent<DestroyOnTimer>().duration = 5f;

      GameObject chronosphere = PrefabAPI.InstantiateClone(ProjectChronos.assetBundle.LoadAsset<GameObject>("assets/chronosphere.prefab"), "ChronosphereIndicator", false);
      chronosphere.transform.parent = chronosphereEffect.transform;
      chronosphere.transform.localPosition = Vector3.zero;
      chronosphere.transform.localRotation = Quaternion.identity;
      chronosphere.transform.localScale = new Vector3(15f, 15f, 15f);

      BuffWard ward = chronosphereEffect.GetComponent<BuffWard>();
      ward.buffDef = timeFrozenBuff;
      // ward.interval = 1f;
      ward.expireDuration = 5f;
      ward.rangeIndicator = chronosphere.transform;

      ContentAddition.AddProjectile(chronosphereEffect);

      this.ReplaceMatShaders("RoR2/Base/Shaders/HGStandard.shader", "assets/matcylindetrimnux.mat", "assets/matsteelnux.mat", "assets/matcylindernux.mat", "assets/matringnux.mat", "assets/polygon arsenal/materials/main/polysolidglow.mat", "assets/polygon arsenal/materials/gradients/polyspriteglow_add.mat", "assets/polygon arsenal/materials/rimlight/solid/blackhole/polyblackholeblue.mat");
      this.ReplaceMatShaders("Calm Water/CalmWater - SingleSided.shader", "assets/matglassnux.mat");

      //This section automatically scans the project for all items
      IEnumerable<Type> ItemTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(ItemBase)));

      List<ItemDef.Pair> newVoidPairs = new List<ItemDef.Pair>();

      foreach (Type itemType in ItemTypes)
      {

        ItemBase item = (ItemBase)System.Activator.CreateInstance(itemType);
        if (ValidateItem(item, itemList))
        {
          item.Init(Config);

          ItemTag[] tags = item.ItemTags;
          bool aiValid = true;
          bool aiBlacklist = false;
          if (item.ItemDef.deprecatedTier == ItemTier.NoTier)
          {
            aiBlacklist = true;
            aiValid = false;
          }
          string name = item.ItemName;
          name = name.Replace("'", "");

          foreach (ItemTag tag in tags)
          {
            if (tag == ItemTag.AIBlacklist)
            {
              aiBlacklist = true;
              aiValid = false;
              break;
            }
          }
          if (aiValid)
          {
            aiBlacklist = Config.Bind<bool>("Item: " + name, "Blacklist Item from AI Use?", false, "Should the AI not be able to obtain this item?").Value;
          }
          else
          {
            aiBlacklist = true;
          }

          if (aiBlacklist)
          {
            item.AIBlacklisted = true;
          }
        }
      }
    }

    public bool ValidateItem(ItemBase item, List<ItemBase> itemList)
    {
      string name = item.ItemName.Replace("'", string.Empty);
      bool enabled = false;

      if (item.Tier == ItemTier.NoTier)
      {
        enabled = true;
        item.AIBlacklisted = true;
      }
      else
      {
        enabled = Config.Bind<bool>("Items", name, true, "Should this item be enabled.").Value;
      }

      if (enabled)
      {
        itemList.Add(item);
      }
      return enabled;
    }

    private void CreateDoTs()
    {
      decayDotBehaviour = DecayBehavior;
      decayDot = DotAPI.RegisterDotDef(new DotController.DotDef()
      {
        interval = 0.5f,
        damageCoefficient = 0.6f,
        damageColorIndex = DamageColorIndex.Fragile,
        associatedBuff = decayBuff,
        resetTimerOnAdd = false
      }, decayDotBehaviour);
    }

    public static void DecayBehavior(DotController self, DotController.DotStack dotStack)
    {
      if (dotStack.dotIndex != decayDot)
        return;

      dotStack.damageType = DamageType.DoT | DamageType.BypassArmor;

      if (self.victimBody && self.victimHealthComponent)
      {
        float maxHP = self.victimHealthComponent.fullCombinedHealth;
        float hpDamage = maxHP / 100f * 0.5f * 0.5f;

        dotStack.damage = Mathf.Min(Mathf.Max(hpDamage, dotStack.damage), dotStack.damage * 25f);
      }
    }

    private void CreateBuffs()
    {
      timeFrozenBuff = ScriptableObject.CreateInstance<BuffDef>();
      timeFrozenBuff.name = "bdTimeFrozen";
      timeFrozenBuff.canStack = false;
      timeFrozenBuff.isCooldown = false;
      timeFrozenBuff.isDebuff = true;
      timeFrozenBuff.buffColor = Color.cyan;
      timeFrozenBuff.iconSprite = jailerBuff.iconSprite;
      (timeFrozenBuff as UnityEngine.Object).name = timeFrozenBuff.name;
      ContentAddition.AddBuffDef(timeFrozenBuff);

      timeBandOnBuff = ScriptableObject.CreateInstance<BuffDef>();
      timeBandOnBuff.name = "bdTimeBandOn";
      timeBandOnBuff.canStack = false;
      timeBandOnBuff.isCooldown = false;
      timeBandOnBuff.isDebuff = false;
      timeBandOnBuff.buffColor = Color.cyan;
      timeBandOnBuff.iconSprite = ProjectChronos.assetBundle.LoadAsset<Sprite>("assets/bdfracturedbandicon.png");
      (timeBandOnBuff as UnityEngine.Object).name = timeBandOnBuff.name;
      ContentAddition.AddBuffDef(timeBandOnBuff);

      timeBandOffBuff = ScriptableObject.CreateInstance<BuffDef>();
      timeBandOffBuff.name = "bdTimeBandOff";
      timeBandOffBuff.canStack = false;
      timeBandOffBuff.isCooldown = false;
      timeBandOffBuff.isDebuff = false;
      timeBandOffBuff.buffColor = Color.gray;
      timeBandOffBuff.iconSprite = ProjectChronos.assetBundle.LoadAsset<Sprite>("assets/bdfracturedbandicon.png");
      (timeBandOffBuff as UnityEngine.Object).name = timeBandOffBuff.name;
      ContentAddition.AddBuffDef(timeBandOffBuff);

      decayBuff = ScriptableObject.CreateInstance<BuffDef>();
      decayBuff.name = "bdDecay";
      decayBuff.canStack = false;
      decayBuff.isCooldown = false;
      decayBuff.isDebuff = true;
      decayBuff.canStack = false;
      decayBuff.buffColor = Color.cyan;
      decayBuff.iconSprite = ProjectChronos.assetBundle.LoadAsset<Sprite>("assets/bddecayicon.png");
      (decayBuff as UnityEngine.Object).name = decayBuff.name;
      ContentAddition.AddBuffDef(decayBuff);
    }

    private void SetupVFX()
    {
      Transform visuals = timeFrozenEffect.transform.GetChild(0);
      visuals.GetChild(2).gameObject.SetActive(false);
      visuals.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = timeFrozenMaterial;
      visuals.GetChild(1).GetComponent<MeshRenderer>().sharedMaterial = timeFrozenMaterial;

      Transform visuals2 = decayEffectPrefab.transform.GetChild(0);
      visuals2.GetChild(0).gameObject.SetActive(false);
      visuals2.GetChild(1).gameObject.SetActive(false);
      visuals2.GetChild(2).gameObject.SetActive(false);
      float startSizeMultiplier = 2.0f;
      float startSpeedMultiplier = 1.5f;
      float startLifetimeMultiplier = 1.5f;
      float emissionRateMultiplier = 1.5f;
      ParticleSystem drip = visuals2.GetChild(3).GetComponent<ParticleSystem>();
      ParticleSystem.MainModule mainModule = drip.main;
      mainModule.startSizeMultiplier *= startSizeMultiplier;
      mainModule.startSpeedMultiplier *= startSpeedMultiplier;
      mainModule.startLifetimeMultiplier *= startLifetimeMultiplier;
      mainModule.scalingMode = ParticleSystemScalingMode.Hierarchy;
      ParticleSystem.EmissionModule emissionModule = drip.emission;
      ParticleSystem.MinMaxCurve rateOverTime = emissionModule.rateOverTime;
      rateOverTime.constantMin *= emissionRateMultiplier;
      rateOverTime.constantMax *= emissionRateMultiplier;
      emissionModule.rateOverTime = rateOverTime;

      visuals2.GetChild(5).gameObject.SetActive(true);
      ParticleSystem fog = visuals2.GetChild(5).GetComponent<ParticleSystem>();
      ParticleSystem.MainModule mainModule2 = fog.main;
      mainModule2.scalingMode = ParticleSystemScalingMode.Hierarchy;
      mainModule2.startSizeMultiplier *= startSizeMultiplier;
      mainModule2.startSpeedMultiplier *= startSpeedMultiplier;
      mainModule2.startLifetimeMultiplier *= startLifetimeMultiplier;
      ParticleSystem.EmissionModule emissionModule2 = fog.emission;
      ParticleSystem.MinMaxCurve rateOverTime2 = emissionModule2.rateOverTime;
      rateOverTime2.constantMin *= emissionRateMultiplier;
      rateOverTime2.constantMax *= emissionRateMultiplier;
      emissionModule2.rateOverTime = rateOverTime;

      visuals2.GetChild(4).gameObject.SetActive(true);
      ParticleSystem souls = visuals2.GetChild(4).GetComponent<ParticleSystem>();
      ParticleSystem.MainModule mainModule3 = souls.main;
      mainModule3.simulationSpace = ParticleSystemSimulationSpace.Local;
      mainModule3.startSizeMultiplier /= startSizeMultiplier;
      mainModule3.startSpeedMultiplier /= startSpeedMultiplier;
      mainModule3.startLifetimeMultiplier /= startLifetimeMultiplier;
      ParticleSystem.EmissionModule emissionModule3 = souls.emission;
      ParticleSystem.MinMaxCurve rateOverTime3 = emissionModule3.rateOverTime;
      rateOverTime3.constantMin /= emissionRateMultiplier;
      rateOverTime3.constantMax /= emissionRateMultiplier;
      emissionModule3.rateOverTime = rateOverTime;

      TempVisualEffectAPI.EffectCondition timeFrozenCondition = (CharacterBody body) => body.HasBuff(timeFrozenBuff);
      TempVisualEffectAPI.AddTemporaryVisualEffect(timeFrozenEffect, timeFrozenCondition);
      TempVisualEffectAPI.EffectCondition decayCondition = (CharacterBody body) => body.HasBuff(decayBuff);
      TempVisualEffectAPI.AddTemporaryVisualEffect(decayEffectPrefab, decayCondition);
    }

    private void ReplaceMatShaders(
       string shaderPath,
       params string[] materialPaths)
    {
      Shader shader = Addressables.LoadAssetAsync<Shader>(shaderPath).WaitForCompletion();
      foreach (string materialPath in materialPaths)
      {
        Material material = ProjectChronos.assetBundle.LoadAsset<Material>(materialPath);
        Debug.LogWarning(material);
        material.shader = shader;
      }
    }

  }
}

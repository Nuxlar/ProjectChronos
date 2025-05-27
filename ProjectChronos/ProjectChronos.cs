using System.Reflection;
using BepInEx;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ProjectChronos
{
    [BepInPlugin("com.Nuxlar.ProjectChronos", "ProjectChronos", "0.6.3")]

    public class ProjectChronos : BaseUnityPlugin
    {
        public static AssetBundle assetBundle;

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
            this.ReplaceMatShaders("Calm Water/CalmWater - SingleSided.shader", "assets/matglassnux.mat")
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

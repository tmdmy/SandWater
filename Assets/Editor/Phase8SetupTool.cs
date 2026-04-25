#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SandWater.Core;
using SandWater.UI;

namespace SandWater.EditorTools
{
    [InitializeOnLoad]
    public static class Phase8SetupTool
    {
        private const string SetupFlagKey = "SandWater.Phase8SetupDone";
        private const string MainScenePath = "Assets/Scenes/Main.unity";

        static Phase8SetupTool()
        {
            EditorApplication.delayCall += TryAutoSetup;
        }

        private static void TryAutoSetup()
        {
            if (EditorPrefs.GetBool(SetupFlagKey, false)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Phase7SetupTool.SetupUI();
            SetupPolish();
            EditorPrefs.SetBool(SetupFlagKey, true);
        }

        [MenuItem("SandWater/Setup Phase 8 Polish")]
        public static void SetupPolish()
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            TuneDirectionalLight(scene);
            TuneFog();
            EnsureAudioManager(scene);
            EnsureFpsCounter(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[SandWater] Phase 8 polish applied.");
        }

        private static void TuneDirectionalLight(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != "Directional Light") continue;
                var light = root.GetComponent<Light>();
                if (light == null) continue;
                light.color = new Color(1f, 0.93f, 0.78f, 1f);
                light.intensity = 1.35f;
                light.colorTemperature = 5500f;
                root.transform.rotation = Quaternion.Euler(48f, 42f, 0f);
                return;
            }
        }

        private static void TuneFog()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.88f, 0.82f, 0.68f, 1f);
            RenderSettings.fogStartDistance = 60f;
            RenderSettings.fogEndDistance = 220f;
        }

        private static void EnsureFpsCounter(Scene scene)
        {
            if (Object.FindAnyObjectByType<FpsCounter>() != null) return;

            GameObject hud = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "[GameHUD]") { hud = root; break; }
            }
            if (hud == null) return;

            var fpsGo = new GameObject("FpsText", typeof(RectTransform), typeof(Text), typeof(FpsCounter));
            fpsGo.transform.SetParent(hud.transform, false);
            var rt = fpsGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(240f, 40f);
            rt.anchoredPosition = new Vector2(30f, -30f);
            var text = fpsGo.GetComponent<Text>();
            text.text = "FPS 0";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.color = new Color(1f, 1f, 1f, 0.7f);
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        private static void EnsureAudioManager(Scene scene)
        {
            if (Object.FindAnyObjectByType<AudioManager>() != null) return;

            var go = new GameObject("[AudioManager]");
            SceneManager.MoveGameObjectToScene(go, scene);
            var manager = go.AddComponent<AudioManager>();

            var sfxGo = new GameObject("SFX", typeof(AudioSource));
            sfxGo.transform.SetParent(go.transform, false);
            var sfx = sfxGo.GetComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 0f;

            var bgmGo = new GameObject("BGM", typeof(AudioSource));
            bgmGo.transform.SetParent(go.transform, false);
            var bgm = bgmGo.GetComponent<AudioSource>();
            bgm.playOnAwake = false;
            bgm.loop = true;
            bgm.spatialBlend = 0f;
            bgm.volume = 0.5f;

            var so = new SerializedObject(manager);
            so.FindProperty("sfxSource").objectReferenceValue = sfx;
            so.FindProperty("bgmSource").objectReferenceValue = bgm;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif

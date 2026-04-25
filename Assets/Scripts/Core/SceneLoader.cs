using UnityEngine.SceneManagement;

namespace SandWater.Core
{
    public static class SceneLoader
    {
        public const string BootstrapScene = "Bootstrap";
        public const string TitleScene = "Title";
        public const string MainScene = "Main";

        public static void LoadTitle() => SceneManager.LoadSceneAsync(TitleScene);
        public static void LoadMain() => SceneManager.LoadSceneAsync(MainScene);
        public static void LoadBootstrap() => SceneManager.LoadSceneAsync(BootstrapScene);
    }
}

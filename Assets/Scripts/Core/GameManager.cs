using UnityEngine;
using UnityEngine.SceneManagement;

namespace SandWater.Core
{
    [DefaultExecutionOrder(-1000)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; }

        private int _lastHandledSceneHandle = int.MinValue;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoBootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[GameManager]");
            go.AddComponent<GameManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            State = new GameState();
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.activeSceneChanged -= OnActiveSceneChanged;
                Instance = null;
            }
        }

        private void Start()
        {
            HandleScene(SceneManager.GetActiveScene());
        }

        private void Update()
        {
            if (State == null || State.Phase != GamePhase.Playing) return;
            State.ElapsedTime += Time.unscaledDeltaTime;
            if (State.TimeLimit > 0f && State.ElapsedTime >= State.TimeLimit && !State.IsCleared)
            {
                NotifyFailed();
            }
        }

        private void OnActiveSceneChanged(Scene previous, Scene next)
        {
            HandleScene(next);
        }

        private void HandleScene(Scene scene)
        {
            if (!scene.IsValid() || string.IsNullOrEmpty(scene.name)) return;
            if (_lastHandledSceneHandle == scene.handle) return;
            _lastHandledSceneHandle = scene.handle;

            switch (scene.name)
            {
                case SceneLoader.BootstrapScene:
                    State.Phase = GamePhase.Boot;
                    Time.timeScale = 1f;
                    SceneLoader.LoadTitle();
                    break;
                case SceneLoader.TitleScene:
                    State.Phase = GamePhase.Title;
                    Time.timeScale = 1f;
                    break;
                case SceneLoader.MainScene:
                    State.ResetForNewPlay();
                    Time.timeScale = 1f;
                    break;
            }
        }

        public void Pause()
        {
            if (State == null || State.Phase != GamePhase.Playing) return;
            State.Phase = GamePhase.Paused;
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            if (State == null || State.Phase != GamePhase.Paused) return;
            State.Phase = GamePhase.Playing;
            Time.timeScale = 1f;
        }

        public void RetryMain()
        {
            Time.timeScale = 1f;
            SceneLoader.LoadMain();
        }

        public void NotifyWaterReached(int amount = 1)
        {
            if (State == null) return;
            State.WaterReachedCount += amount;
        }

        public void NotifyCleared()
        {
            if (State == null || State.IsCleared) return;
            State.IsCleared = true;
            State.Phase = GamePhase.Cleared;
            AudioManager.Instance?.PlayClear();
            Debug.Log("[SandWater] Cleared!");
        }

        public void NotifyFailed()
        {
            if (State == null || State.IsFailed || State.IsCleared) return;
            State.IsFailed = true;
            State.Phase = GamePhase.Failed;
            Debug.Log("[SandWater] Time up!");
        }
    }
}

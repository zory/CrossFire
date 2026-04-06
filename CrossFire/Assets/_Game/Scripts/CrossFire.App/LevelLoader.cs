using System;
using System.Collections;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrossFire.App
{
    // Handles scene transitions and waits for ECS subscenes to finish streaming
    // before signalling that the level is ready to use.
    //
    // Usage: LevelLoader.Instance.LoadLevel("Gameplay")
    // Listen: LevelLoader.OnLevelReady += MyCallback;
    //
    // Place on a persistent GameObject (DontDestroyOnLoad) in your bootstrap scene.
    public class LevelLoader : MonoBehaviour
    {
        public static LevelLoader Instance { get; private set; }

        // Fired once the scene and all its ECS subscenes are fully loaded.
        public static event Action OnLevelReady;

        public bool IsLoading { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void LoadLevel(string sceneName)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"[LevelLoader] Load already in progress, ignoring request for '{sceneName}'.");
                return;
            }

            StartCoroutine(LoadLevelRoutine(sceneName));
        }

        private IEnumerator LoadLevelRoutine(string sceneName)
        {
            IsLoading = true;

            yield return SceneManager.LoadSceneAsync(sceneName);

            yield return WaitForSubScenes();

            IsLoading = false;
            OnLevelReady?.Invoke();
        }

        private IEnumerator WaitForSubScenes()
        {
            World world = World.DefaultGameObjectInjectionWorld;

            if (world == null || !world.IsCreated)
            {
                yield break;
            }

            // One frame so ECS can register the SubScene entities that arrived
            // with the freshly loaded scene before we start polling them.
            yield return null;

            SubScene[] subScenes = FindObjectsByType<SubScene>(FindObjectsSortMode.None);

            if (subScenes.Length == 0)
            {
                yield break;
            }

            bool allLoaded = false;
            while (!allLoaded)
            {
                allLoaded = true;

                foreach (SubScene subScene in subScenes)
                {
                    Entity sceneEntity = SceneSystem.GetSceneEntity(world.Unmanaged, subScene.SceneGUID);

                    if (sceneEntity == Entity.Null || !SceneSystem.IsSceneLoaded(world.Unmanaged, sceneEntity))
                    {
                        allLoaded = false;
                        break;
                    }
                }

                if (!allLoaded)
                {
                    yield return null;
                }
            }
        }
    }
}

using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Core.UI
{
    // Attach to the UI overlay camera. On each scene load this script finds Camera.main
    // and adds itself to its URP camera stack, so the main camera never needs to know
    // about the UI camera. Safe to call multiple times — duplicate stack entries are prevented.
    //
    // Uses Start (not Awake) for the initial link so all scene cameras have finished their
    // own Awake before we try to resolve Camera.main.
    public class UICameraLinker : MonoBehaviour
    {
        private Camera _overlayCamera;

        private void Awake()
        {
            _overlayCamera = GetComponent<Camera>();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            LinkToBaseCamera();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            LinkToBaseCamera();
        }

        private void LinkToBaseCamera()
        {
            Camera baseCamera = Camera.main;

            if (baseCamera == null)
            {
                return;
            }

            UniversalAdditionalCameraData cameraData = baseCamera.GetUniversalAdditionalCameraData();

            if (!cameraData.cameraStack.Contains(_overlayCamera))
            {
                cameraData.cameraStack.Add(_overlayCamera);
            }
        }
    }
}

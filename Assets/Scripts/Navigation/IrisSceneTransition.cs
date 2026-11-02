using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RhythmRehab
{
    // The overlay is an authored prefab in each scene. Only its material is instanced at runtime.
    public sealed class IrisSceneTransition : MonoBehaviour
    {
        [SerializeField] private RawImage irisImage;
        [SerializeField, Min(.05f)] private float closeDuration = .22f;
        [SerializeField, Min(.05f)] private float openDuration = .28f;
        private static IrisSceneTransition instance;
        private Material runtimeMaterial;
        public static bool IsBusy => instance != null && instance.Phase != "Idle";
        public string Phase { get; private set; } = "Idle";
        public float Radius => runtimeMaterial == null ? 0 : runtimeMaterial.GetFloat("_Radius");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() => instance = null;

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            if (irisImage == null || irisImage.material == null)
            { Debug.LogError("Iris transition needs its saved overlay image and material.", this); enabled = false; return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            runtimeMaterial = new Material(irisImage.material);
            irisImage.material = runtimeMaterial;
            irisImage.raycastTarget = false;
            SetRadius(OpenRadius);
            irisImage.enabled = false;
        }

        private float Aspect => (float)Mathf.Max(1, Screen.width) / Mathf.Max(1, Screen.height);
        private float OpenRadius => Mathf.Sqrt(Aspect * Aspect + 1) * .5f + .04f;
        private void SetRadius(float radius)
        {
            runtimeMaterial.SetFloat("_Radius", radius);
            runtimeMaterial.SetFloat("_Aspect", Aspect);
        }

        public static bool Load(string scene)
        {
            if (IsBusy) return false;
            if (!Application.CanStreamedLevelBeLoaded(scene))
            { Debug.LogError("Scene is unavailable in Build Settings: " + scene); return false; }
            if (instance == null) { SceneManager.LoadScene(scene); return true; }
            instance.StartCoroutine(instance.ChangeScene(scene));
            return true;
        }

        private IEnumerator ChangeScene(string scene)
        {
            Phase = "Closing";
            irisImage.enabled = true;
            irisImage.raycastTarget = true;
            yield return Animate(true, closeDuration);
            Phase = "Loading";
            AsyncOperation operation = null;
            try { operation = SceneManager.LoadSceneAsync(scene); }
            catch (System.Exception error) { Debug.LogError("Could not change scene: " + error.Message); }
            if (operation != null) yield return operation;
            // Give the new scene a frame to initialize while the screen is covered.
            yield return null;
            Phase = "Opening";
            yield return Animate(false, openDuration);
            irisImage.raycastTarget = false;
            irisImage.enabled = false;
            Phase = "Idle";
        }

        private IEnumerator Animate(bool closing, float duration)
        {
            float elapsed = 0;
            duration = Mathf.Max(.05f, duration);
            while (elapsed < duration)
            {
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                SetRadius(closing ? Mathf.Lerp(OpenRadius, -.02f, t) : Mathf.Lerp(-.02f, OpenRadius, t));
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }
            SetRadius(closing ? -.02f : OpenRadius);
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
            if (runtimeMaterial != null) Destroy(runtimeMaterial);
        }
    }
}

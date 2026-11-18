using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RhythmRehab
{
    // Prototype shared-account gate. Firestore rules must enforce backend access separately.
    public sealed class FirebaseLoginSession : MonoBehaviour
    {
        public static FirebaseLoginSession Instance { get; private set; }
        public bool IsSignedIn => !string.IsNullOrEmpty(idToken);
        public bool IsSigningIn { get; private set; }
        public event Action Changed;
        private string apiKey;
        private string idToken, refreshToken;
        private DateTime expires;
        private int generation;
        private bool refreshing;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() => Instance = null;
        private void Awake()
        {
            if (Instance && Instance != this) { Destroy(this); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
            var config = Resources.Load<FirebaseClientConfig>("FirebaseClientConfig");
            apiKey = config ? config.ApiKey : null;
        }
        public void SignIn(string email, string password, Action<string> completed)
        {
            if (IsSigningIn) return;
            if (string.IsNullOrEmpty(apiKey))
            {
                completed?.Invoke("Sign-in is unavailable on this installation. You can still play as Guest.");
                return;
            }
            StartCoroutine(Login(email.Trim(), password, completed));
        }
        private IEnumerator Login(string email, string password, Action<string> completed)
        {
            int attempt = ++generation;
            IsSigningIn = true; Changed?.Invoke();
            var body = new JObject { ["email"] = email, ["password"] = password, ["returnSecureToken"] = true };
            using (var request = new UnityWebRequest("https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=" + Uri.EscapeDataString(apiKey), "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body.ToString()));
                body.RemoveAll(); password = null;
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json"); request.timeout = 20;
                yield return request.SendWebRequest();
                if (attempt != generation) yield break;
                string error = request.result == UnityWebRequest.Result.Success ? null : "Unable to sign in. Check your email, password and connection.";
                if (error == null)
                {
                    try { Accept(JObject.Parse(request.downloadHandler.text), false); }
                    catch (Exception) { error = "Unable to read the login response. Please try again."; }
                }
                IsSigningIn = false; Changed?.Invoke(); completed?.Invoke(error);
            }
        }
        private void Accept(JObject data, bool refreshed)
        {
            string token = (string)data[refreshed ? "id_token" : "idToken"];
            string refresh = (string)data[refreshed ? "refresh_token" : "refreshToken"];
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(refresh)) throw new FormatException();
            idToken = token; refreshToken = refresh;
            double.TryParse((string)data[refreshed ? "expires_in" : "expiresIn"], out var seconds);
            expires = DateTime.UtcNow.AddSeconds(Math.Max(60, seconds) - 30);
        }
        public IEnumerator GetToken(Action<string> completed)
        {
            int attempt = generation;
            while (refreshing && attempt == generation) yield return null;
            if (!IsSignedIn || attempt != generation) { completed(null); yield break; }
            if (DateTime.UtcNow >= expires)
            {
                refreshing = true;
                var form = new WWWForm(); form.AddField("grant_type", "refresh_token"); form.AddField("refresh_token", refreshToken);
                using (var request = UnityWebRequest.Post("https://securetoken.googleapis.com/v1/token?key=" + Uri.EscapeDataString(apiKey), form))
                {
                    request.timeout = 20; yield return request.SendWebRequest();
                    if (attempt != generation) { completed(null); yield break; }
                    bool valid = request.result == UnityWebRequest.Result.Success;
                    if (valid) { try { Accept(JObject.Parse(request.downloadHandler.text), true); } catch (Exception) { valid = false; } }
                    refreshing = false;
                    if (!valid) SignOut();
                }
            }
            completed(attempt == generation ? idToken : null);
        }
        public void SignOut()
        {
            generation++; idToken = refreshToken = null; IsSigningIn = refreshing = false;
            PatientContext.Select(PatientProfile.Guest); Changed?.Invoke();
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }
    }
}

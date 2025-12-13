using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;

public class FunticoManager : MonoBehaviour
{
    public static FunticoManager Instance { get; private set; }
    [DllImport("__Internal")] private static extern void InitializeSDK(string authClientId, string env);
    [DllImport("__Internal")] private static extern void SignIn(string gameObjectName, int promiseId);
    [DllImport("__Internal")] private static extern void GetUserInfo(string gameObjectName, int promiseId);
    [DllImport("__Internal")] private static extern void SaveScore(int score, string gameObjectName, int promiseId);
    [DllImport("__Internal")] private static extern void SignOut();
    [DllImport("__Internal")] public static extern void ShowAlert(string text);

    private int _nextPromiseId = 0;
    private readonly Dictionary<int, object> _pendingPromises = new Dictionary<int, object>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Init(string authClientId, string env)
    {
#if !UNITY_EDITOR && UNITY_WEBGL
        InitializeSDK(authClientId, env);
#endif
    }

    public UniTask SignInAsync()
    {
#if UNITY_EDITOR
        Debug.LogWarning("Funtico SDK >> Mock SignInAsync call in Editor.");
        return UniTask.CompletedTask;
#else
        var utcs = new UniTaskCompletionSource<bool>();
        int promiseId = _nextPromiseId++;
        _pendingPromises[promiseId] = utcs;
        SignIn(gameObject.name, promiseId);
        return utcs.Task;
#endif
    }

    public async UniTask<FunticoUser?> GetUserInfoAsync()
    {
#if UNITY_EDITOR
        Debug.LogWarning("Funtico SDK >> Mock GetUserInfoAsync call in Editor.");
        return await UniTask.FromResult(new FunticoUser{UserName = "Editor", UserId = "123"});
#else
        var utcs = new UniTaskCompletionSource<string>();
        int promiseId = _nextPromiseId++;
        _pendingPromises[promiseId] = utcs;
        GetUserInfo(gameObject.name, promiseId);

        // 1. Asynchronously wait for the JSON string result
        string userJson = await utcs.Task;

        // 2. Check for an empty result to avoid errors
        if (string.IsNullOrEmpty(userJson))
        {
            Debug.LogError("Funtico SDK >> Received an empty user string.");
            return await UniTask.FromResult<FunticoUser?>(null);
        }

        // 3. Deserialize the JSON using Newtonsoft.Json
        FunticoUser user = JsonConvert.DeserializeObject<FunticoUser>(userJson);

        // 4. Return the parsed object
        return user;
#endif
    }

    public UniTask<string> SaveScoreAsync(int score)
    {
#if UNITY_EDITOR
        Debug.LogWarning($"Funtico SDK >> Mock SaveScoreAsync({score}) call in Editor.");
        return UniTask.FromResult("{\"status\":\"success_editor\"}");
#else
        var utcs = new UniTaskCompletionSource<string>();
        int promiseId = _nextPromiseId++;
        _pendingPromises[promiseId] = utcs;
        SaveScore(score, gameObject.name, promiseId);
        return utcs.Task;
#endif
    }

    public void DoSignOut()
    {
#if !UNITY_EDITOR && UNITY_WEBGL
        SignOut();
#endif
    }


    public void ResolvePromise(string result)
    {
        var parts = result.Split(new[] { ':' }, 2);
        if (parts.Length < 2 || !int.TryParse(parts[0], out int promiseId)) return;
        if (!_pendingPromises.TryGetValue(promiseId, out object utcsObj)) return;

        string payload = parts[1];
        if (utcsObj is UniTaskCompletionSource<string> stringUtcs) {
            stringUtcs.TrySetResult(payload);
        } else if (utcsObj is UniTaskCompletionSource<bool> boolUtcs) {
            boolUtcs.TrySetResult(true);
        }
        _pendingPromises.Remove(promiseId);
    }

    public void RejectPromise(string error)
    {
        var parts = error.Split(new[] { ':' }, 2);
        if (parts.Length < 2 || !int.TryParse(parts[0], out int promiseId)) return;
        if (!_pendingPromises.TryGetValue(promiseId, out object utcsObj)) return;

        string payload = parts[1];
        var exception = new Exception(payload);
        if (utcsObj is UniTaskCompletionSource<string> stringUtcs) {
            stringUtcs.TrySetException(exception);
        } else if (utcsObj is UniTaskCompletionSource<bool> boolUtcs) {
            boolUtcs.TrySetException(exception);
        }
        _pendingPromises.Remove(promiseId);
    }

    public class FunticoUser
    {
        [JsonProperty("username")]
        public string UserName { get; set; }
        [JsonProperty("User_id")]
        public string UserId { get; set; }
    }
}

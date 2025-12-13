using System;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Exception = System.Exception;

public class FunticoSDKExample : MonoBehaviour
{
    
    [SerializeField] private string authClientId;
    [SerializeField] private string env;
    [SerializeField] private string secondScene;

    // Changed to TextMeshPro types
    [SerializeField] private TMP_Text userNameText;
    [SerializeField] private TMP_Text userIDText;
    [SerializeField] private TMP_InputField scoreInput;

    [CanBeNull] private static FunticoManager.FunticoUser userName = null;

    public string username;

  
    private void Start()
    {
        if (userName == null)
        {
            FunticoManager.Instance.Init(authClientId, env);
            ChangeSceneIfSingedIn().Forget();
            return;
        }

        if (userNameText != null)
        {
            userNameText.text = userName.UserName;
            username = userName.UserName;
        }

        
        if (userIDText != null) userIDText.text = userName.UserId;
    }

    #region  SIGN_IN
    public void SingIn()
    {
        SignInAsync().Forget();
    }

    public async UniTask SignInAsync()
    {
        try
        {
            Debug.LogError("SignIn called.");
            await FunticoManager.Instance.SignInAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Couldnt signIn: {ex.Message}");
        }
    }

    private async UniTask ChangeSceneIfSingedIn()
    {
        try
        {
            Debug.LogError("GetUserInfoAsync called.");
            userName = await FunticoManager.Instance.GetUserInfoAsync();
            SceneManager.LoadScene(secondScene);
        }
        catch (Exception)
        {
            Debug.Log("User is not logged in");
        }
    }
    #endregion

    #region SEND_SCORE
    public void SendScore(int scoreData)
    {
        Debug.LogError("SaveScore called.");

        //if (scoreData == null)
        //{
        //    Debug.LogError("Score input (TMP_InputField) is not assigned.");
        //    return;
        //}

       
        SendScoreAsync(scoreData).Forget();
        
        //else
        //{
        //    Debug.LogError("Invalid score entered. Please enter a number.");
        //}
    }

    private async UniTask SendScoreAsync(int score)
    {
        await FunticoManager.Instance.SaveScoreAsync(score);
       // FunticoManager.ShowAlert("Score saved successfully!");
    }
    #endregion

    #region SIGN_OUT
    public void SignOut()
    {
        Debug.LogError("SignOut called.");
        FunticoManager.Instance.DoSignOut();
    }
    #endregion
}

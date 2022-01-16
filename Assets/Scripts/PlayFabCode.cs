using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using PlayFab;
using PlayFab.ClientModels;

public class PlayFabCode : MonoBehaviour
{
    public InputField Username, Email, Password;
    public Text ErrorMessage;
    public string SceneName = "";

    public void RegisterClick()
    {
        var register = new RegisterPlayFabUserRequest { Username = Username.text, Email = Email.text, Password = Password.text };
        PlayFabClientAPI.RegisterPlayFabUser(register, OnRegisterSuccess, OnRegisterFailure);
    }

    private void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        ErrorMessage.text = "";
        SceneManager.LoadScene(SceneName); // Load Main Scene
    }

    private void OnRegisterFailure(PlayFabError error)
    {
        if (error.ErrorDetails != null && error.ErrorDetails.Count > 0)
        {
            using (var iter = error.ErrorDetails.Keys.GetEnumerator())
            {
                iter.MoveNext();
                string key = iter.Current;
                ErrorMessage.text = error.ErrorDetails[key][0];
            }
        }
        else
        {
            ErrorMessage.text = error.ErrorMessage;
        }
    }

    public void LoginClick()
    {
        var login = new LoginWithPlayFabRequest { Username = Username.text, Password = Password.text };
        PlayFabClientAPI.LoginWithPlayFab(login, OnLoginSuccess, OnLoginFailure);
    }

    private void OnLoginSuccess(LoginResult result)
    {
        ErrorMessage.text = "";
        SceneManager.LoadScene(SceneName); // Load Main Scene
    }

    private void OnLoginFailure(PlayFabError error)
    {
        if (error.ErrorDetails != null && error.ErrorDetails.Count > 0)
        {
            using (var iter = error.ErrorDetails.Keys.GetEnumerator())
            {
                iter.MoveNext();
                string key = iter.Current;
                ErrorMessage.text = error.ErrorDetails[key][0];
            }
        }
        else
        {
            ErrorMessage.text = error.ErrorMessage;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        ErrorMessage.text = "";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

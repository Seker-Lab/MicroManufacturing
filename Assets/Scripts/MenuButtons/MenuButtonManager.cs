using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuButtonManager : MonoBehaviour
{
    private bool mobileUI = true;

    public void OnStartButtonPress()
    {
        GameObject.Find("Global Scene Manager").GetComponent<globalSceneManager>().continueFromMenu();
    }

    public void OnLevelSelectButtonPressed()
    {
        SceneManager.LoadScene("LevelSelect");
    }

    public void OnOptionsButtonPress()
    {
        SceneManager.LoadScene("Options");
    }

    public void OnCreditsButtonPress()
    {
        SceneManager.LoadScene("Credits");
    }

    public void OnQuitButtonPressed()
    {
        Application.Quit();
    }
    public void OnFreePlayButton()
    {
        SceneManager.LoadScene("FreePlayLevel");
    }


    public void OnBackButtonPress()
    {
        if(GameObject.Find("Global Scene Manager").GetComponent<globalSceneManager>().optionsMenuFlag)
        {
            GameObject.Find("Global Scene Manager").GetComponent<globalSceneManager>().optionsMenuFlag = false;
            GameObject.Find("Global Scene Manager").GetComponent<globalSceneManager>().continueFromMenu();
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    public void OnFullScreenToggle()
    {
        if (Screen.fullScreen)
        {
            Screen.fullScreen = false;
            GameObject.Find("FullScreenButton").GetComponent<TextMeshProUGUI>().text = "Full Screen";
        }
        else
        {
            Screen.fullScreen = true;
            GameObject.Find("FullScreenButton").GetComponent<TextMeshProUGUI>().text = "Windowed Mode";
        }
    }

    public void OnMobileToggle()
    {
        if (mobileUI)
        {
            mobileUI = false;
            GameObject.Find("Mobile UI").GetComponent<TextMeshProUGUI>().text = "Mobile off";
        }
        else
        {
            mobileUI = true;
            GameObject.Find("Mobile UI").GetComponent<TextMeshProUGUI>().text = "Mobile on";
        }
    }

    public void lv1Button()
    {
        GameObject.Find("Global Scene Manager").GetComponent<globalSceneManager>().gotoFromMenu("Level1");
    }

    public void lv2Button()
    {
        GameObject.Find("Global Scene Manager").GetComponent<globalSceneManager>().gotoFromMenu("Level2");
    }

    public void lv3Button()
    {
        GameObject.Find("Global Scene Manager").GetComponent<globalSceneManager>().gotoFromMenu("Level3");
    }

    public void lv4Button()
    {
        GameObject.Find("Global Scene Manager").GetComponent<globalSceneManager>().gotoFromMenu("Level4");
    }


    public void onContinueButton()
    {
        GameObject.Find("Global Scene Manager").GetComponent<globalSceneManager>().gotoFromMenu();
    }
}

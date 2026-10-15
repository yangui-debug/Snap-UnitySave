using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DemoMenu : MonoBehaviour
{
    private int index;
    private int total;
    public Button nextBtn;
    public Button prevBtn;

    void Start()
    {
        index = SceneManager.GetActiveScene().buildIndex;
        total = SceneManager.sceneCountInBuildSettings;
        prevBtn.onClick.AddListener(OnClickPrev);
        nextBtn.onClick.AddListener(OnClickNext);
    }

    private void OnClickNext()
    {
        SceneManager.LoadScene(index < total - 1 ? index + 1 : 0);
    }

    private void OnClickPrev()
    {
        SceneManager.LoadScene(index > 0 ? index - 1 : total - 1);
    }

}

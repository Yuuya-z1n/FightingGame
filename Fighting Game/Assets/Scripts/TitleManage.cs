using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "StandbyScene";


    // ボタンのOnClickから呼ぶ用
    public void OnStartButton()
    {
        SceneManager.LoadScene(nextSceneName);
    }

    // 「Press Any Key」方式にしたい場合
    void Update()
    {
        if (Input.anyKeyDown)
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
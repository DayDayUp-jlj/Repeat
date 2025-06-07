using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class QuitGame : MonoBehaviour
{
    private Button quitBtn;
    // Start is called before the first frame update
    void Start()
    {
        quitBtn = GetComponent<Button>();
    }

    // Update is called once per frame
    void Update()
    {
        quitBtn.onClick.AddListener(ExitGame);
    }

    void ExitGame()
    {
        Application.Quit();
    }
}

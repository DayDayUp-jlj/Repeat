using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class StartGame : MonoBehaviour
{
    private Button startGameBtn;
    // Start is called before the first frame update
    void Start()
    {
        startGameBtn = GetComponent<Button>();
        startGameBtn.onClick.AddListener(ChangeStartSence);
    }


    void ChangeStartSence()
    {
        SceneManager.LoadScene("GameSence");
    }
}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class LobbyManager : MonoBehaviour
{

    public Button PlayWithBot;
    public Button PlayWithPlayer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PlayWithBot.onClick.AddListener(OnPlayWithBotClicked);

    }
    void OnPlayWithBotClicked()
    {
        SceneManager.LoadScene("PlayWithBot");
    }
    // Update is called once per frame
    void Update()
    {
    }
}

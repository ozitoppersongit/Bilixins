using Unity.VisualScripting;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] GameObject MainScreen;
    [SerializeField] GameObject OptionsScreen;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartGame()
    {

    }

    public void Options()
    {
        MainScreen.SetActive(false);
        OptionsScreen.SetActive(true);
    }

    public void GoBack()
    {
        OptionsScreen.SetActive(false);
        MainScreen.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}

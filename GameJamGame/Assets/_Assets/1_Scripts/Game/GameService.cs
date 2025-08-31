// GameService.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameService : MonoBehaviour, IGameService
{
    public string startScene = "01_MainMenu";
    void Start()
    {
        Service.Register<IGameService>();
        Service.Register<IPaintSurface2D>();
        Service.Register<IAudioService>();
        Service.Register<IPlayerService>();
        Service.Register<INpcService>();
        SceneManager.LoadScene(startScene, LoadSceneMode.Additive);
    }

    public void StartGame()
    {
        StartCoroutine(CoStartGame());
    }
    IEnumerator CoStartGame()
    {
        yield return SceneManager.UnloadSceneAsync("01_MainMenu");
        yield return SceneManager.LoadSceneAsync("02_Game", LoadSceneMode.Additive);
    }
}

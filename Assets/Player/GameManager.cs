using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Playing,
        SkinMenu
    }

    public GameState CurrentState { get; private set; } = GameState.Playing;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        //フレームレートを60に固定
        Application.targetFrameRate = 60;
    }

    public void SetState(GameState newState)
    {
        CurrentState = newState;
    }

    public bool IsPlayerInputAllowed()
    {
        return CurrentState == GameState.Playing;
    }
}

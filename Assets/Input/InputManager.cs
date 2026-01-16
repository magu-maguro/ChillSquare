using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private PlayerInputActions inputActions;

    private void Awake()
    {
        // シングルトンパターン
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // PlayerInputActionsのインスタンスを作成
        inputActions = new PlayerInputActions();
        inputActions.Player.Enable();
    }

    /// <summary>
    /// PlayerInputActionsのインスタンスを取得
    /// </summary>
    public PlayerInputActions GetInputActions()
    {
        return inputActions;
    }

    private void OnDestroy()
    {
        inputActions?.Dispose();
    }
}

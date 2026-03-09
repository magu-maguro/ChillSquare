using UnityEngine;
using UniRx;
using Photon.Pun;

public class GameManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private NetworkManager networkManager;
    public static GameManager Instance { get; private set; }

    [Header("Player")]
    [SerializeField] private Vector2 playerInitPos = Vector2.zero;

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

    private void Start()
    {
        networkManager.OnJoinedRoomObservable.Subscribe(_ =>
        {
            OnJoinedRoom();
        }).AddTo(this);
    }

    public void SetState(GameState newState)
    {
        CurrentState = newState;
    }

    private void OnJoinedRoom()
    {
        // ルームに参加したときの処理
        Debug.Log("ルームに参加しました");
        InitializePlayer();
    }

    //------------Player------------

    public bool IsPlayerInputAllowed()
    {
        return CurrentState == GameState.Playing;
    }

    private void InitializePlayer()
    {
        Vector3 position = playerInitPos;
        PhotonNetwork.Instantiate("Avatar", position, Quaternion.identity);
    }
}

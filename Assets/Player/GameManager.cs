using UnityEngine;
using UniRx;
using Photon.Pun;
using Unity.Cinemachine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private SkinChangeManager skinChangeManager;
    public static GameManager Instance { get; private set; }

    [Header("Player")]
    [SerializeField] private Vector2 playerInitPos = Vector2.zero;
    [Header("Camera")]
    [SerializeField] private CinemachineCamera vcam;

    public enum GameState
    {
        Playing,
        SkinMenu
    }

    public GameState CurrentState { get; private set; } = GameState.Playing;

    private readonly List<PlayerController> players = new();
    public IReadOnlyList<PlayerController> Players => players;

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
        var instance = PhotonNetwork.Instantiate("Avatar", position, Quaternion.identity);
        instance.GetComponent<PlayerSkinController>().Initialize(skinChangeManager);
        instance.GetComponent<PlayerCameraController>().Initialize(vcam);
    }

    public void RegisterPlayer(PlayerController player)
    {
        if (!players.Contains(player))
        {
            players.Add(player);
        }
    }

    public void UnregisterPlayer(PlayerController player)
    {
        if (players.Contains(player))
        {
            players.Remove(player);
        }
    }
}

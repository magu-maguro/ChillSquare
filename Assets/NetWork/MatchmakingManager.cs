using System;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using Sirenix.OdinInspector;

public class MatchmakingManager : MonoBehaviourPunCallbacks
{
    //[SerializeField] private NetworkManager networkManager; //シングルトンにした

    public enum MatchmakingState
    {
        Idle, Connecting, SerchingRoom, CreatingRoom, JoinedRoom, Failed
    }

    [ShowInInspector] public MatchmakingState CurrentState { get; private set; } = MatchmakingState.Idle;

    public string CurrentMessage { get; private set; } = "";
    public event Action<string> MessageChanged;

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public void StartMatchmaking()
    {
        ChangeState(MatchmakingState.Connecting);

        ShowMessage("サーバーに接続しています…");
        NetworkManager.Instance.Connect();
    }

    public override void OnConnectedToMaster()
    {
        ChangeState(MatchmakingState.SerchingRoom);
        //取り敢えずランダム
        ShowMessage("マッチングルームを検索しています…");
        PhotonNetwork.JoinRandomRoom();
        //成功したらOnJoinedRoom
        //失敗したらOnJoinRandomFailed
    }

    public override void OnJoinedRoom()
    {
        ChangeState(MatchmakingState.JoinedRoom);
        Debug.Log("マッチングルームに参加しました");
        ShowMessage("マッチングルームに参加しました。");
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        ChangeState(MatchmakingState.CreatingRoom);
        ShowMessage("マッチングルームが見つかりませんでした。\n新しいルームを作成します…");
        PhotonNetwork.CreateRoom(null, new RoomOptions());
        //成功したらOnCreatedRoom
        //失敗したらOnCreateRoomFailed
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        ChangeState(MatchmakingState.Failed);
        Debug.LogWarning("ルームの作成に失敗しました。" + message);
        ShowMessage("ルームの作成に失敗しました。\nもう一度お試しください。");
    }

    public override void OnCreatedRoom()
    {
        ChangeState(MatchmakingState.JoinedRoom);
        Debug.Log("ルームを作成しました。" + PhotonNetwork.CurrentRoom.Name);
        ShowMessage("ルームを作成しました。\nゲームを開始します。");
        PhotonNetwork.LoadLevel("SampleScene");
    }

    private void ShowMessage(string message)
    {
        CurrentMessage = message;
        MessageChanged?.Invoke(message);
    }

    private void ChangeState(MatchmakingState newState)
    {
        CurrentState = newState;
        //イベント発行?

    }
}

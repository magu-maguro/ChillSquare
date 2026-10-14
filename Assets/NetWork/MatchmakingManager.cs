using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class MatchmakingManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private NetworkManager networkManager;

    public void StartMatchmaking()
    {
        networkManager.Connect();
    }

    public override void OnConnectedToMaster()
    {
        //取り敢えずランダム
        PhotonNetwork.JoinRandomRoom();
        //成功したらOnJoinedRoom
        //失敗したらOnJoinRandomFailed
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("マッチングルームに参加しました");
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        PhotonNetwork.CreateRoom(null, new RoomOptions());
        //成功したらOnCreatedRoom
        //失敗したらOnCreateRoomFailed
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning("ルームの作成に失敗しました。" + message);
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("ルームを作成しました。" + PhotonNetwork.CurrentRoom.Name);
    }
}

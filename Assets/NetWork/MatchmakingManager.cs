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

    public void OnConnected()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        PhotonNetwork.CreateRoom(null, new RoomOptions());
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("マッチングルームに参加しました");
    }
}

using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UniRx;
using System;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    private Subject<Unit> onJoinedRoom = new Subject<Unit>();

    public IObservable<Unit> OnJoinedRoomObservable => onJoinedRoom;

    //Awakeで即接続していたのを廃止

    public void Connect()
    {
        if (PhotonNetwork.IsConnected)
            return;

        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        // 接続完了を通知するだけ
    }

    public void LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
    }
}

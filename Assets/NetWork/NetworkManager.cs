using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UniRx;
using System;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance { get; private set; }
    private Subject<Unit> onJoinedRoom = new Subject<Unit>();

    public IObservable<Unit> OnJoinedRoomObservable => onJoinedRoom;

    //Awakeで即接続していたのを廃止
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Connect()
    {
        if (PhotonNetwork.IsConnected)
            return;

        PhotonNetwork.ConnectUsingSettings();
        //成功したらOnConnectedToMaster
        //失敗したらOnDisconnected
    }

    public override void OnConnectedToMaster()
    {
        // 接続完了を通知するだけ
        Debug.Log("Photonのマスターサーバに接続しました。");
        //MatchmakingManagerでも同じコールバックを受けてマッチメイキングする
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("Photonから切断されました。" + cause.ToString());
    }

    public void LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
    }
}

using UnityEngine;
using Photon.Pun;

public class PlayerNetworkSync : MonoBehaviourPunCallbacks, IPunObservable
{
    private Rigidbody2D rb;
    private Vector3 networkPosition;

    [SerializeField] float networkLerpSpeed = 10f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        networkPosition = transform.position;
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                networkPosition,
                Time.fixedDeltaTime * networkLerpSpeed
            );
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
        }
    }
}

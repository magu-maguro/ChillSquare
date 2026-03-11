using Photon.Pun;
using UnityEngine;
using Unity.Cinemachine;

public class PlayerCameraController : MonoBehaviourPun
{
    private CinemachineCamera vcam;
    public void Initialize(CinemachineCamera camera)
    {
        vcam = camera;
    }
    void Start()
    {
        if (!photonView.IsMine) return;

        //var vcam = FindAnyObjectByType<CinemachineCamera>();

        if (vcam != null)
        {
            vcam.Follow = transform;
        }
    }
}

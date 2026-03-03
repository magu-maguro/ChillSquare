using System.Collections;
using UnityEngine;
using Photon.Pun;

public class CPUSpawner : MonoBehaviourPunCallbacks
{
    [Header("Prefab")]
    [Tooltip("Prefab name under Resources/ used for PhotonNetwork.Instantiate")] 
    public string cpuPrefabName = "CPUPlayer";

    [Header("Spawn Settings")]
    [Range(1, 5)] public int maxCPUs = 3;
    public float minSpawnDelay = 0.5f;
    public float maxSpawnDelay = 3f;

    private PhotonView pv;

    [SerializeField] private bool allowSpawn;

    void Start()
    {
        pv = GetComponent<PhotonView>();
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        // マスタークライアントだけが CPU を生成
        if (PhotonNetwork.IsMasterClient && allowSpawn)
        {
            StartCoroutine(SpawnCPUsCoroutine());
        }
    }

    private IEnumerator SpawnCPUsCoroutine()
    {
        //int count = Random.Range(1, maxCPUs + 1);
        for (int i = 0; i < maxCPUs; i++)
        {
            float delay = Random.Range(minSpawnDelay, maxSpawnDelay);
            yield return new WaitForSeconds(delay);
            Vector3 spawnPos = GetSpawnPosition();
            PhotonNetwork.InstantiateRoomObject(cpuPrefabName, spawnPos, Quaternion.identity);
        }
    }

    private Vector3 GetSpawnPosition()
    {
        // 簡易的にランダムスポーン位置を返す（必要に応じてマップに合わせて調整）
        return new Vector3(Random.Range(-9f, 9f), Random.Range(1f, 2f), 0f);
    }
}

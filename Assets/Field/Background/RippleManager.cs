using System.Collections.Generic;
using UnityEngine;

public class RippleManager : MonoBehaviour
{
    // singleton
    public static RippleManager Instance { get; private set; }

    [Header("Pool")]
    [SerializeField] private RippleEffect ripplePrefab;
    [SerializeField] private int poolSize = 10;

    private RippleEffect[] ripplePool;

    [Header("Spawn Settings")]
    [SerializeField] private float speedThreshold = 0.5f;
    [SerializeField] private float minInterval = 0.15f;
    [SerializeField] private float maxInterval = 0.35f;

    [Header("Ripple Settings")]
    [SerializeField] private Color rippleColor = Color.white;
    [SerializeField] private int rippleShape = 0;
    [SerializeField] private float rippleRotation = 0f;
    [SerializeField] private float rippleDuration = 1.2f;
    [SerializeField] private float rippleStartSize = 15f;
    [SerializeField] private float rippleEndSize = 20f;

    [Header("Screen")]
    [SerializeField] private float screenMargin = 0.2f;

    private Camera mainCamera;

    private readonly Dictionary<PlayerController, PlayerRippleState> playerStates = new();

    private class PlayerRippleState
    {
        // 速度計算用
        public Vector3 previousPosition;
        public float velocityTimer;
        public Vector2 averageVelocity;

        // Ripple生成用
        public float spawnTimer;
        public float nextSpawnTime;
    }
    [SerializeField] private float velocitySampleInterval = 0.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        mainCamera = Camera.main;

        CreatePool();
    }

    private void Update()
    {
        UpdatePlayerRipples();
    }

    private void CreatePool()
    {
        ripplePool = new RippleEffect[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            ripplePool[i] = Instantiate(ripplePrefab, transform);
            ripplePool[i].gameObject.SetActive(false);
        }
    }

    private void UpdatePlayerRipples()
    {
        if (GameManager.Instance == null)
            return;

        foreach (PlayerController player in GameManager.Instance.Players)
        {
            if (player == null)
                continue;

            UpdatePlayerRipple(player);
        }

        if(Input.GetKeyDown(KeyCode.L))
        {
            for (int i = 0; i < GameManager.Instance.Players.Count; i++)
            {
                PlayerRippleState state = playerStates[GameManager.Instance.Players[i]];
                Debug.Log($"Player: {i}, Average Velocity: {state.averageVelocity}, IsOnScreen: {IsOnScreen(state.previousPosition)}");
            }
        }
    }

    private void UpdatePlayerRipple(PlayerController player)
    {
        if (!playerStates.TryGetValue(player, out PlayerRippleState state))
        {
            state = new PlayerRippleState
            {
                previousPosition = player.transform.position,
                velocityTimer = 0f,
                averageVelocity = Vector2.zero,
                spawnTimer = 0f,
                nextSpawnTime = GetRandomInterval()
            };

            playerStates.Add(player, state);

            return;
        }

        Vector3 currentPosition = player.transform.position;

        Vector2 velocity;

        //自分だったら
        if (player.photonView != null && player.photonView.IsMine)
        {
            velocity = CalculateVelocity(player, state, currentPosition);
        }
        else//他人だったら
        {
            velocity = CalculateAverageVelocity(player, state);
        }
        

        //state.previousPosition = currentPosition;

        // 速度が閾値未満ならRippleを出さない
        if (velocity.magnitude < speedThreshold)
        {
            state.spawnTimer = 0f;
            return;
        }

        // 画面外ならRippleを出さない
        if (!IsOnScreen(currentPosition))
        {
            state.spawnTimer = 0f;
            return;
        }

        state.spawnTimer += Time.deltaTime;

        if (state.spawnTimer < state.nextSpawnTime)
            return;

        SpawnRipple(currentPosition);

        state.spawnTimer = 0f;
        state.nextSpawnTime = GetRandomInterval();
    }

    private Vector2 CalculateVelocity(
        PlayerController player,
        PlayerRippleState state,
        Vector3 currentPosition)
    {
            return player.GetComponent<Rigidbody2D>().linearVelocity;
    }

    private Vector2 CalculateAverageVelocity(
        PlayerController player,
        PlayerRippleState state)
    {
        state.velocityTimer += Time.deltaTime;

        // まだ0.2秒経過していない
        if (state.velocityTimer < velocitySampleInterval)
        {
            return state.averageVelocity;
        }

        Vector3 currentPosition = player.transform.position;

        Vector2 velocity = (currentPosition - state.previousPosition) / state.velocityTimer;

        state.averageVelocity = velocity;

        state.previousPosition = currentPosition;
        state.velocityTimer = 0f;

        return state.averageVelocity;
    }

    private float GetRandomInterval()
    {
        return Random.Range(minInterval, maxInterval);
    }

    private bool IsOnScreen(Vector3 worldPosition)
    {
        if (mainCamera == null)
            return false;

        Vector3 viewportPosition =
            mainCamera.WorldToViewportPoint(worldPosition);

        return viewportPosition.z > 0f
            && viewportPosition.x >= -screenMargin
            && viewportPosition.x <= 1f + screenMargin
            && viewportPosition.y >= -screenMargin
            && viewportPosition.y <= 1f + screenMargin;
    }

    private void SpawnRipple(Vector3 position)
    {
        foreach (RippleEffect ripple in ripplePool)
        {
            if (!ripple.gameObject.activeInHierarchy)
            {
                ripple.Play(
                    position,
                    rippleColor,
                    rippleShape,
                    rippleRotation,
                    rippleDuration,
                    rippleStartSize,
                    rippleEndSize,
                    null
                );

                return;
            }
        }
    }
}
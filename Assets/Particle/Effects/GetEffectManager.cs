using System.Collections.Generic;
using UnityEngine;

public class GetEffectManager : MonoBehaviour
{
    public static GetEffectManager Instance { get; private set; }

    // object pool
    private Stack<GameObject> effectPool = new Stack<GameObject>();
    private int poolSize = 20;
    [SerializeField] private GameObject effectPrefab;
    void Start()
    {
        // singleton
        if (Instance == null)
        {
            Instance = this;
            // プールの初期化
            for (int i = 0; i < poolSize; i++)
            {
                GameObject effect = Instantiate(effectPrefab);
                effect.SetActive(false);
                effectPool.Push(effect);
                effect.GetComponent<GetEffectController>().getEffectManager = this;
                effect.transform.SetParent(this.transform);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayGetEffect(Vector3 position)
    {
        var effect = GetEffectFromPool();
        if (effect != null)
        {
            effect.GetComponent<GetEffectController>().PlayEffect(position);
        }
    }

    private GameObject GetEffectFromPool()
    {
        if (effectPool.Count > 0)
        {
            return effectPool.Pop();
        }
        else
        {
            // プールが空の場合はnull
            return null;
        }
    }

    public void ReturnEffectToPool(GameObject effect)
    {
        effect.SetActive(false);
        effectPool.Push(effect);
    }
}

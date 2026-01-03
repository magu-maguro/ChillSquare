using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Timeline;

/// <summary>
/// Object Poolを使用してParticleControllerを管理
/// </summary>
public class ParticleManager : MonoBehaviour
{
    [SerializeField] private List<ParticleController> prefabs = new List<ParticleController>();

    //pool
    [SerializeField] private uint initPoolSize = 10;
    private Stack<ParticleController> stack;

    void Start()
    {
        SetupPool();
    }

    private void SetupPool()
    {
        stack = new Stack<ParticleController>((int)initPoolSize);
        ParticleController instance = null;
        for (int i = 0; i < initPoolSize; i++)
        {
            int index = 0;
            instance = Instantiate(prefabs[index]);
            instance.ParticleManager = this;
            instance.gameObject.SetActive(true);
            instance.transform.position = DecideRandomPos();
            stack.Push(instance);
        }
    }

    public ParticleController Get()
    {
        ParticleController instance = null;
        if (stack.Count > 0)
        {
            instance = stack.Pop();
        }
        else
        {
            int index = 0;
            instance = Instantiate(prefabs[index]);
            instance.ParticleManager = this;
        }
        instance.gameObject.SetActive(true);
        instance.transform.position = DecideRandomPos();
        return instance;
    }

    public void ReturnToPool(ParticleController instance)
    {
        instance.gameObject.SetActive(false);
        stack.Push(instance);
    }

    private Vector2 DecideRandomPos()
    {
        Vector2 pos;
        float x = Random.Range(-38f, 38f);
        float y = Random.Range(-15f, 10f);
        pos = new Vector2(x, y);
        return pos;
    }
}

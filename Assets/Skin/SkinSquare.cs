using UnityEngine;
using UnityEngine.UI;
using UniRx;

public class SkinSquare : MonoBehaviour
{
    private int index;
    [SerializeField] private SkinChangeManager manager;
    void Awake()
    {
        //自分が親オブジェクトの何番目に位置しているか
        index = transform.GetSiblingIndex();
    }

    void Update()
    {

    }

    public void OnClicked()
    {
        manager.OnSquareSelected.OnNext(index);
    }
}

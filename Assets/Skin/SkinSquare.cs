using UnityEngine;

public class SkinSquare : MonoBehaviour
{
    private int index;
    void Start()
    {
        //自分が親オブジェクトの何番目に位置しているか
        index = transform.GetSiblingIndex();
    }

    void Update()
    {

    }

    public void OnClicked()
    {
        //SkinChangeManagerにindexを教えつつなんか頑張ってもらう(UniRxとか使う？)
        Debug.Log(index + " clicked");
    }
}

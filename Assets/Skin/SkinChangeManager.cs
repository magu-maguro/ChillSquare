using UnityEngine;
using UnityEngine.UI;

public class SkinChangeManager : MonoBehaviour
{
    [SerializeField] private Transform SquaresParent;
    private Image[] squares = new Image[16];

    void Start()
    {
        for (int i = 0; i < 16; i++)
        {
            squares[i] = SquaresParent.GetChild(i).GetComponent<Image>();
        }
    }

    /*
    SkinSquareからの通知をもらったら
    ・これ選んでますよ的な枠を合わせる
    ・選択中の四角の番号を抑えておいて、スライダーの操作を受け付けて四角の色を随時変更する
    ・完了ボタンを押されたらスプライトを作成、プレイヤーに反映、PlayerPrefsで保存など

    */

    // Update is called once per frame
    void Update()
    {
        
    }
}

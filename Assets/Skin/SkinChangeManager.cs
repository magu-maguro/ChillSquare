using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System;

public class SkinChangeManager : MonoBehaviour
{
    [SerializeField] private Transform SquaresParent;
    [SerializeField] private SpriteRenderer playerRenderer;
    private Image[] squares = new Image[16];
    private int currentIndex = 0;
    //private IDisposable colorSubscription;

    //外部から購読
    public Subject<int> OnSquareSelected = new Subject<int>();
    public Subject<Color> OnColorChanged = new Subject<Color>();

    void Start()
    {
        for (int i = 0; i < 16; i++)
        {
            squares[i] = SquaresParent.GetChild(i).GetComponent<Image>();
        }

        //Squareクリックされたとき
        OnSquareSelected.Subscribe(index =>
        {
            currentIndex = index;
            Debug.Log("Selected Square : " + index);

            //枠線表示したりとか
        }).AddTo(this);

        //スライダーが変わった時
        OnColorChanged.Subscribe(color =>
        {
            squares[currentIndex].color = color;
        }).AddTo(this);
    }


    void Update()
    {

    }

    public void SaveSkin()
    {
        //テクスチャ自動生成
        int texSize = 64;
        int cellCount = 4;
        int cellSize = texSize / cellCount;

        Texture2D tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);

        for (int yCell = 0; yCell < cellCount; yCell++)
        {
            for (int xCell = 0; xCell < cellCount; xCell++)
            {
                int index = yCell * cellCount + xCell;
                Color col = squares[index].color;

                for (int y = 0; y < cellSize; y++)
                {
                    for (int x = 0; x < cellSize; x++)
                    {
                        int px = xCell * cellSize + x;
                        int py = yCell * cellSize + y;
                        tex.SetPixel(px, py, col);
                    }
                }
            }
        }

        tex.Apply();

        //スプライトにしてプレイヤーに反映
        Sprite newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        playerRenderer.sprite = newSprite;

        
    }
}

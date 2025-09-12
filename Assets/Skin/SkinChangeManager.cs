using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System;

public class SkinChangeManager : MonoBehaviour
{
    [SerializeField] private Transform SquaresParent;
    [SerializeField] private SkinSliderController sliderController;
    [SerializeField] private SpriteRenderer playerRenderer;
    private Image[] squares = new Image[16];
    private int currentIndex = 0;
    //private IDisposable colorSubscription;

    private int texSize = 256;
    private int ppu = 256;    

    //外部から購読
    public Subject<int> OnSquareSelected = new Subject<int>();
    public Subject<Color> OnColorChanged = new Subject<Color>();

    void Awake()
    {
        for (int i = 0; i < 16; i++)
        {
            squares[i] = SquaresParent.GetChild(i).GetComponent<Image>();
        }
        //Squareクリックされたとき
        OnSquareSelected.Subscribe(index =>
        {
            currentIndex = index;
            sliderController.SetRGBWithoutNotify(squares[currentIndex].color);
            Debug.Log("Selected Square : " + index);

            //枠線表示したりとか
        }).AddTo(this);

        //スライダーが変わった時
        OnColorChanged.Subscribe(color =>
        {
            squares[currentIndex].color = color;
            Debug.Log("squares[" + currentIndex + "].color -> " + color);
        }).AddTo(this);
    }

    void Start()
    {
        if (PlayerPrefs.HasKey("SkinData"))
        {
            LoadSkin();
        }
    }


    void Update()
    {

    }

    public void SaveSkin()
    {
        //SkinDataクラスに色情報まとめる
        SkinData data = new SkinData();
        for (int i = 0; i < 16; i++)
        {
            data.colors[i] = squares[i].color;
        }

        //JSONで保存
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("SkinData", json);
        PlayerPrefs.Save();
        Debug.Log("SkinData Saved!");
        //Debug.Log(PlayerPrefs.GetString("SkinData"));

        ApplySkin(data);
    }

    private void LoadSkin()
    {
        string json = PlayerPrefs.GetString("SkinData");
        SkinData data = JsonUtility.FromJson<SkinData>(json);

        // squaresのUIにも反映
        for (int i = 0; i < 16; i++)
        {
            squares[i].color = data.colors[i];
        }

        sliderController.SetRGBWithoutNotify(squares[currentIndex].color);

        ApplySkin(data);
    }

    private void ApplySkin(SkinData data)
    {
        //テクスチャ自動生成
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
                        tex.SetPixel(xCell * cellSize + x, yCell * cellSize + y, col);
                    }
                }
            }
        }

        tex.Apply();

        //スプライトにしてプレイヤーに反映
        Sprite newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu);
        playerRenderer.sprite = newSprite;


    }
}

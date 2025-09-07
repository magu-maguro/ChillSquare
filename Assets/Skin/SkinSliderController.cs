using UnityEngine;
using UnityEngine.UI;
using UniRx;

public class SkinSliderController : MonoBehaviour
{
    [SerializeField] private Slider rSlider;
    [SerializeField] private Slider gSlider;
    [SerializeField] private Slider bSlider;

    [SerializeField] private SkinChangeManager manager;
    void Start()
    {
        Observable.Merge(
            rSlider.OnValueChangedAsObservable(),
            gSlider.OnValueChangedAsObservable(),
            bSlider.OnValueChangedAsObservable()
        ).Subscribe(_ =>
        {
            var color = new Color(rSlider.value, gSlider.value, bSlider.value, 1f);
            manager.OnColorChanged.OnNext(color);
        }).AddTo(this);
    }

    void Update()
    {

    }
}

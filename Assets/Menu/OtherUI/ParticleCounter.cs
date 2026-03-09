using UnityEngine;
using UniRx;
using TMPro;

/// <summary>
/// パーティクル個数の値変化監視して表示
/// </summary>
public class ParticleCounter : MonoBehaviour
{
    [SerializeField] private ParticleManager particleManager;
    [SerializeField] private TMPro.TextMeshProUGUI totalCountText;

    void Start()
    {
        // totalCollected の変化を監視してテキスト更新
        particleManager.totalCollected.Subscribe
        (
            _ => UpdateUI()
        ).AddTo(this);
        particleManager.myCollected.Subscribe
        (
            _ => UpdateUI()
        ).AddTo(this);
    }

    private void UpdateUI()
    {
        totalCountText.text = $"Total: {particleManager.totalCollected.Value} / My: {particleManager.myCollected.Value}";
    }
}

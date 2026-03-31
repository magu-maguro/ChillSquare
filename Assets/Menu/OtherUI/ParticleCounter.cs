using UnityEngine;
using UniRx;
using TMPro;
using DG.Tweening;

/// <summary>
/// パーティクル個数の値変化監視して表示
/// </summary>
public class ParticleCounter : MonoBehaviour
{
    [SerializeField] private ParticleManager particleManager;
    [SerializeField] private ParticleEventBridge particleEventBridge;
    [SerializeField] private EventManager eventManager;
    [SerializeField] private TextMeshProUGUI totalCountText;
    [SerializeField] private TextMeshProUGUI eventThresholdText;
    [SerializeField] private TextMeshProUGUI currentEventText;
    private Vector2 initialEventTextPos;


    void Start()
    {
        // totalCollected の変化を監視してテキスト更新
        particleManager.totalCollected.Subscribe
        (
            _ => UpdateTotalCount()
        ).AddTo(this);
        particleManager.myCollected.Subscribe
        (
            _ => UpdateMyCount()
        ).AddTo(this);

        particleEventBridge.nextThreshold.Subscribe
        (
            threshold => UpdateThreshold(threshold)
        ).AddTo(this);

        eventManager.OnEventStart.Subscribe
        (
            eventData => DisplayCurrentEvent(eventData)
        ).AddTo(this);
        eventManager.OnEventEnd.Subscribe
        (
            _ => HideCurrentEvent()
        ).AddTo(this);

        initialEventTextPos = currentEventText.transform.localPosition;
    }

    private void UpdateTotalCount()
    {
        totalCountText.text = $"Total: {particleManager.totalCollected.Value} / My: {particleManager.myCollected.Value}";
        //InstantScaling(totalCountText);
    }

    private void UpdateMyCount()
    {
        totalCountText.text = $"Total: {particleManager.totalCollected.Value} / My: {particleManager.myCollected.Value}";
        InstantScaling(totalCountText);
    }

    private void UpdateThreshold(long threshold)
    {
        eventThresholdText.text = $"Next Event Threshold: {threshold}";
        InstantScaling(eventThresholdText);
    }

    Tween tween;
    private void InstantScaling(TextMeshProUGUI text)
    {
        tween?.Kill();
        text.transform.localScale = Vector3.one * 1.1f;
        tween = text.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).SetAutoKill(true);
    }

    private void DisplayCurrentEvent(EventData eventData)
    {
        currentEventText.text = $"Current Event: {eventData.eventName}";
        //画面上部から登場
        currentEventText.transform.DOLocalMoveY(initialEventTextPos.y - 200, 0.5f).SetEase(Ease.OutBack).SetAutoKill(true);
    }

    private void HideCurrentEvent()
    {
        //currentEventText.text = "";
        //画面上部に退場
        currentEventText.transform.DOLocalMoveY(initialEventTextPos.y, 0.5f).SetEase(Ease.InBack).SetAutoKill(true)
            .OnComplete(() => currentEventText.text = "");
    }
}

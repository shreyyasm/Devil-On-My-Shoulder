using UnityEngine;
using UnityEngine.EventSystems;

public class CardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private bool isFlipped = false;
    private RectTransform rect;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isFlipped)
        {
            rect.localRotation = Quaternion.Euler(0, 180, 0);
            isFlipped = true;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isFlipped)
        {
            rect.localRotation = Quaternion.identity;
            isFlipped = false;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        int cardIndex = transform.GetSiblingIndex(); // 0 or 1
        ScoreManager.Instance.PickCard(cardIndex);
    }
}

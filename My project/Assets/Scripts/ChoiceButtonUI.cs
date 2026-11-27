using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChoiceButtonUI : MonoBehaviour
{
    [Header("Komponen UI")]
    public Button button;
    public Image backgroundImage;
    public Image numberBadgeBackground;
    public TMP_Text numberBadgeText;
    public TMP_Text japaneseText;
    public TMP_Text romajiText;

    public void Setup(int number, string jpText, string romaji, System.Action onClickAction, Sprite cardBg = null, Color? badgeColor = null)
    {
        if (numberBadgeText != null)
            numberBadgeText.text = number.ToString();

        if (japaneseText != null)
            japaneseText.text = jpText;

        if (romajiText != null)
            romajiText.text = romaji;

        // Hanya timpa sprite jika di prefab belum ada sprite sama sekali, atau jika cardBg memang disediakan
        // Jika user sudah pasang sprite sendiri di prefab, hormati sprite tersebut!
        if (cardBg != null && backgroundImage != null && backgroundImage.sprite == null)
            backgroundImage.sprite = cardBg;

        if (badgeColor.HasValue && numberBadgeBackground != null && numberBadgeBackground.color == Color.white)
            numberBadgeBackground.color = badgeColor.Value;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClickAction?.Invoke());
        }
    }
}


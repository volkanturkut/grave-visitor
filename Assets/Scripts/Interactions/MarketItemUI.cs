using UnityEngine;
using TMPro;

public class MarketItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text countText; // Drag your text here
    private int _quantity = 0;

    public void AddItem()
    {
        if (_quantity < 99)
        {
            _quantity++;
            UpdateUI();
        }
    }

    public void RemoveItem()
    {
        if (_quantity > 0)
        {
            _quantity--;
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (countText != null)
            countText.text = _quantity.ToString();
    }
}
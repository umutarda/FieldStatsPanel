using UnityEngine;
using TMPro;

public class BottomPanelItem : MonoBehaviour
{
    // Holds the int value
    public int Value;

    // Cached reference to the TMP_Text component
    private TMP_Text tmpText;

    void Awake()
    {
        // Try to get the TMP_Text component attached to this GameObject,
        // if not found, try to get one from its children.
        tmpText = GetComponent<TMP_Text>();
        if (tmpText == null)
        {
            tmpText = GetComponentInChildren<TMP_Text>();
        }
    }

    /// <summary>
    /// Sets the value and updates the TMP_Text (if available).
    /// </summary>
    /// <param name="newValue">The int value to assign.</param>
    public void SetValue(int newValue)
    {
        Value = newValue;
        if (tmpText != null)
        {
            tmpText.text = Value.ToString();
        }
        else
        {
            Debug.Log("Value set to " + Value);
        }
    }
}

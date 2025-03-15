using UnityEngine;
using UnityEngine.UI;

public class UISpriteAnimator : MonoBehaviour
{
    // Array of sprites for the animation.
    public Sprite[] sprites;
    // Animation speed in frames per second.
    public float frameRate = 10f;

    private Image imageComponent;
    private int currentIndex = 0;
    private float timer = 0f;

    private void Awake()
    {
        // Try to get the Image component attached to this GameObject.
        imageComponent = GetComponent<Image>();
        if (imageComponent == null)
        {
            Debug.LogError("UISpriteAnimator: No Image component found on this GameObject.");
        }
    }

    private void Update()
    {
        if (sprites == null || sprites.Length == 0 || imageComponent == null)
            return;

        timer += Time.deltaTime;
        if (timer >= 1f / frameRate)
        {
            timer -= 1f / frameRate;
            // Update sprite and loop back if at end.
            currentIndex = (currentIndex + 1) % sprites.Length;
            imageComponent.sprite = sprites[currentIndex];
        }
    }
}

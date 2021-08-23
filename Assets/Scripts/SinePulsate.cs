using UnityEngine;

//simple class that can be added to any element if you want it to pulsate with a siney motion
public class SinePulsate : MonoBehaviour
{
    //current position in the sine wave, goes up and around a circle in radians. different from the value that Mathf.Sin method actually spits out, which goes between -1 and 1.
    private float sinePosition = 0f;
    //sine multiplier affects how big the pulsation motion is
    private float sineMultiplier = 0.1f;
    //speedMultiplier affects how fast the pulsation motion happens
    private float speedMultiplier = 4f;
    private Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        sinePosition += Time.deltaTime * speedMultiplier;
        if(sinePosition > Mathf.PI * 2f) sinePosition = 0f;
        transform.localScale = originalScale * (1f - (Mathf.Sin(sinePosition) * sineMultiplier));
    }
}

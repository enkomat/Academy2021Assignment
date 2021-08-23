using UnityEngine;

//this controller can be used to control any obstacle from the editor
//should make it easier to create new obstacles without having to leave the editor
public class ObstacleController : MonoBehaviour
{
    [SerializeField]
    private bool randomStartRotationEnabled = true;
    [SerializeField]
    private float spinAmount = 1f;
    [SerializeField]
    private float moveAmount = 0f;
    [SerializeField]
    private float moveSpeed = 0f;
    private float sinePosition = 0f;
    private Vector3 originalPosition;
    
    void Start()
    {
        if(randomStartRotationEnabled) SetRandomRotation();
        originalPosition = transform.localPosition;
    }

    void Update()
    {
        if(spinAmount != 0f) SpinObstacle();
        if(moveAmount != 0f) MoveObstacle();
    }

    private void SpinObstacle()
    {
        this.transform.Rotate(0, 0, spinAmount * Time.deltaTime, Space.Self);
    }

    private void SetRandomRotation()
    {
        this.transform.Rotate(0, 0, Random.Range(-360, 360), Space.Self);
    }

    //the obstacle is moved with a sine motion
    //could be replaced with a lerp coroutine
    private void MoveObstacle()
    {
        sinePosition += Time.deltaTime * moveSpeed;
        if(sinePosition > Mathf.PI * 2f) sinePosition = 0f;
        transform.localPosition = new Vector3(originalPosition.x + (Mathf.Sin(sinePosition) * moveAmount), originalPosition.y, originalPosition.z);
    }
}

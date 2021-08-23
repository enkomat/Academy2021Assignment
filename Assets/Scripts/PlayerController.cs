using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer playerSprite;
    [SerializeField]
    private ParticleSystem loseGameParticles;
    [SerializeField]
    private Rigidbody2D playerRigidbody;
    [SerializeField]
    private float jumpForce;
    [SerializeField]
    private float cameraSmoothTime = 0.3F;
    [SerializeField]
    private AudioClip jumpSound;
    private Vector3 cameraVelocity = Vector3.zero;
    private Vector3 cameraOffset;
    private Camera mainCamera;
    private GameController gameController;
    private bool playerActive = false;
    private float lastSpawnPosition = 0f;

    void Start()
    {
        AssignVariables();
    }

    private void AssignVariables()
    {
        mainCamera = Camera.main;
        gameController = GameObject.Find("GameController").GetComponent<GameController>();
        SwitchColor();
        DeactivatePlayer();
        cameraOffset = mainCamera.transform.position;
        lastSpawnPosition = mainCamera.transform.position.y;
    }
    
    private void Update()
    {
        //check if the player has clicked on their left mouse button and make the player jump if so
        CheckForInput();
        //make the camera follow the player if they have moved up on the y axis
        CameraFollowUpwards();
    }

    private void CheckForInput()
    {
        //check if the player pressed left mouse button down
        if((Input.GetMouseButtonDown(0)))
        {
            //activate player's rigidbody and gravity only after first jump
            if(!playerActive) 
            {
                ActivatePlayer();
            }
            //make the player jump up
            JumpUp();
        }
    }

    private void DeactivatePlayer()
    {
        //eliminate all velocity affecting the rigidbody before making it kinematic
        playerRigidbody.velocity = Vector2.zero;
        playerRigidbody.isKinematic = true;
    }

    private void ActivatePlayer()
    {
        playerRigidbody.isKinematic = false;
        playerActive = true;
    }

    private void JumpUp()
    {
        //downward velocity must be reset each time player jumps to make it snappy, which is done by overwriting velocity
        //no need to use AddForce for a one click jump, overwriting with a vector that points up will do the trick
        playerRigidbody.velocity = Vector2.up * jumpForce;
        gameController.PlaySound(jumpSound);
    }

    private void CameraFollowUpwards()
    {
        Vector3 cameraTargetPosition = this.transform.position + cameraOffset;
        //only follow the ball upwards. if the target position the camera should move to is lower than the position before, do not move camera.
        if(cameraTargetPosition.y > mainCamera.transform.position.y)
        {
            mainCamera.transform.position = Vector3.SmoothDamp(mainCamera.transform.position, cameraTargetPosition, ref cameraVelocity, cameraSmoothTime);
        }  
    }

    private void SwitchColor()
    {
        //switch the player ball's color through its SpriteRenderer
        Color originalColor = playerSprite.color;
        //make sure the color does not pick the same color as the ball already was
        while(playerSprite.color == originalColor) playerSprite.color = gameController.GetRandomColor();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        //if the player hits an obstacle item and the obstacle is of different color than the ball, you lose
        if(other.tag == "Obstacle" && playerSprite.color != other.GetComponent<SpriteRenderer>().color)
        {
            LoseGame();
        }
        //if you hit a star, the star is collected and score is incremented in GameController
        else if(other.tag == "Star")
        {
            HitStar();
        }
        //if you hit a color switcher your ball's color is changed to something else than it currently is
        else if(other.tag == "ColorSwitcher")
        {
            HitColorSwitcher();
        }
    }

    private void LoseGame()
    {
        Instantiate(loseGameParticles, transform.position, Quaternion.identity);
        gameController.LoseGame();
        Destroy(this.gameObject);
    } 

    private void HitStar()
    {
        gameController.CollectStar();
    }

    private void HitColorSwitcher()
    {
        SwitchColor();
        gameController.CollectColorSwitcher();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameController : MonoBehaviour
{
    [SerializeField]
    private List<Color> colors = new List<Color>();
    [SerializeField]
    private List<Color> backgroundColors = new List<Color>();
    [SerializeField]
    private List<GameObject> obstaclePrefabs = new List<GameObject>();
    [SerializeField]
    private Camera mainCamera;
    [SerializeField]
    private GameObject playerPrefab;
    [SerializeField]
    private GameObject handPointer;
    [SerializeField]
    private GameObject highScoreLine;
    [SerializeField]
    private GameObject gameOverMenu;
    [SerializeField]
    private GameObject starPrefab;
    [SerializeField]
    private GameObject colorSwitcherPrefab;
    [SerializeField]
    private ParticleSystem starCollectParticles;
    [SerializeField]
    private TextMeshPro scoreText;
    [SerializeField]
    private TextMeshPro scoreMenuText;
    [SerializeField]
    private TextMeshPro highScoreMenuText;
    [SerializeField]
    private TextMeshPro totalStarsMenuText;
    [SerializeField]
    private GameObject resetIcon;
    [SerializeField]
    private AudioSource audioSource;
    [SerializeField]
    private AudioClip starSound;
    [SerializeField]
    private AudioClip colorSwitcherSound;
    [SerializeField]
    private AudioClip loseGameSound;
    [SerializeField] [Range(0f, 30f)]
    private float spaceBetweenObstacles = 17.5f;
    private Vector3 cameraStartPosition;
    private GameObject currentPlayer;
    private List<GameObject> currentObstacles = new List<GameObject>();
    private List<GameObject> currentStars = new List<GameObject>();
    private List<GameObject> currentColorSwitchers = new List<GameObject>();
    private Vector3 resetIconOriginalScale;
    private float spawnHeight = 0f;
    private int maximumObstacleAmount = 7;
    private int totalStarsCollected;
    private int highScore;
    private int currentScore;
    private int backgroundColorIndex = 0;
    private bool mouseDown = false;

    private enum GameState
    {
        PLAYING,
        GAME_OVER_MENU
    }

    private GameState state = GameState.PLAYING;

    private void Awake()
    {
        AssignCameraVariables();
        GetSavedVariables();
        StartNewGame();
    }

    private void AssignCameraVariables()
    {
        //set main camera variable and save its original starting position
        mainCamera = Camera.main;
        cameraStartPosition = mainCamera.transform.position;
    }

    //get high score and amount of total collected stars from player preferences
    //default to 0 on both values if there is nothing saved yet
    private void GetSavedVariables()
    {
        highScore = PlayerPrefs.GetInt("HighScore", 0);
        totalStarsCollected = PlayerPrefs.GetInt("TotalStars", 0);
    }

    private void Update()
    {
        //when playing, check for the first mouse click to disable wanted UI elements
        //currently only the hand graphic needs to be disabled
        if(state == GameState.PLAYING)
        {
            HandlePlayingInput();
        }
        //when in game over menu, keep track of whether the player clicks their mouse to move back to playing
        else if(state == GameState.GAME_OVER_MENU)
        {
            HandleMenuInput();
        }
    }

    private void HandleMenuInput()
    {
        //only switch back from game over menu after mouse button is up, feels more like pressing on a button
        //mouseDown variable exists to track if the mouse has been pressed down in the game over menu specifically,
        //otherwise we would not be aware whether the mouse was clicked down before the switch to game lost menu
        if(Input.GetMouseButtonDown(0))
        {
            mouseDown = true;
            resetIconOriginalScale = resetIcon.transform.localScale;
            resetIcon.transform.localScale *= 1.1f;
        }
        else if(Input.GetMouseButtonUp(0) && mouseDown)
        {
            mouseDown = false;
            resetIcon.transform.localScale = resetIconOriginalScale;
            DestroyCurrentLevel();
            StartNewGame();  
        }
    }

    private void HandlePlayingInput()
    {
        if(handPointer.activeSelf && Input.GetMouseButtonDown(0))
        {
            DisableHandPointer();
        }
    }

    private void StartNewGame()
    {
        //hide game over menu, if it was visible to begin with
        gameOverMenu.SetActive(false);
        //change state back to playing
        state = GameState.PLAYING;
        //set main camera back to its starting position
        mainCamera.transform.position = cameraStartPosition;
        //set background color to the first default backfround color
        ResetBackgroundColor();
        //reset current score back to 0
        ResetScore();
        //spawn a new player ball, with a new PlayerController within it
        SpawnPlayer();
        //spawn the first few obstacles so the level is not empty
        SpawnInitialObstacles();
        //put high score line as high as the high scoring star is
        SetHighScoreLinePosition();
        //activate the hand pointer which prompts player to start clicking
        EnableHandPointer();
    }

    private void SpawnPlayer()
    {
        currentPlayer = Instantiate(playerPrefab, new Vector3(0f, -10f, 0f), Quaternion.identity);
    }

    private void SpawnInitialObstacles()
    {
        spawnHeight = 0f;
        for(int i = 0; i < 4; i++) SpawnObstacle();
    }

    public Color GetRandomColor()
    {
        //get a random color from the list of colors that you've picked in editor
        return colors[Random.Range(0, colors.Count)];
    }

    //spawnHeight parameter is used to control where on the y axis an obstacle is spawned
    public void SpawnObstacle()
    {
        //spawn obstacle prefab and add it to list that keeps count of spawned obstacles
        GameObject spawnableObstacle = GetObstacleBasedOnScore();
        Vector3 spawnablePos = spawnableObstacle.transform.localPosition;
        GameObject newObstacle = Instantiate(spawnableObstacle, new Vector3(spawnablePos.x, spawnablePos.y + spawnHeight, spawnablePos.z), spawnableObstacle.transform.rotation);
        currentObstacles.Add(newObstacle);
        //spawn star prefab and add it to list that keeps count of spawned stars, every obstacle contains a star in the middle
        GameObject newStar = Instantiate(starPrefab, new Vector3(0f, spawnHeight, 0f), Quaternion.identity);
        //add the new star into a list so it can be referred back to when it needs to be removed
        currentStars.Add(newStar);
        //spawn new color switcher after about every other obstacle
        if(Random.value > 0.5f)
        {
            GameObject newColorSwitcher = Instantiate(colorSwitcherPrefab, new Vector3(0f, spawnHeight + (spaceBetweenObstacles / 2f), 0f), Quaternion.identity);
            //add the new color switcher into a list so it can be referred back to when it needs to be removed
            currentColorSwitchers.Add(newColorSwitcher);
        }
        //destroy old obstacles so the scene does not get cluttered
        DestroyOldObstacles();
        //spawn height is the height where an obstacle is spawned at on the y axis
        //the height difference between every spawn is currently always 15 units
        spawnHeight += spaceBetweenObstacles;
    }

    private GameObject GetObstacleBasedOnScore()
    {
        //if the players current score is under five, spawn only the first part of the obstacle list, which is supposed to contain the easiest obstacles
        if(currentScore < 5)
        {
            return obstaclePrefabs[Random.Range(0, 5)];
        }
        //when the score is under fifteen, it's possible for any kind of obstacle to spawn. easier and harder ones.
        else if(currentScore < 15)
        {
            return obstaclePrefabs[Random.Range(0, obstaclePrefabs.Count)];
        }
        //if the score is fifteen or over, only harder obstacles spawn
        else
        {
            return obstaclePrefabs[Random.Range(4, obstaclePrefabs.Count)];
        }
    }

    private void DestroyOldObstacles()
    {
        //if there are more than the maaximum allowed number of spawned obstacles, take the oldest one and get rid of it, because it's already off screen
        if(currentObstacles.Count > maximumObstacleAmount)
        {
            //we can refer to oldest spawned obstacle, remove it from our list and destroy it
            GameObject destroyableObstacle = currentObstacles[0];
            currentObstacles.RemoveAt(0);
            Destroy(destroyableObstacle);
        }
    }

    private void DestroyCurrentLevel()
    {
        //iterate through all the objects the level is made of and destroy them
        for(int i = 0; i < currentObstacles.Count; i++) Destroy(currentObstacles[i]);
        for(int i = 0; i < currentStars.Count; i++) Destroy(currentStars[i]);
        for(int i = 0; i < currentColorSwitchers.Count; i++) Destroy(currentColorSwitchers[i]);
        //clear the lists so no null references are left in them
        currentObstacles.Clear();
        currentStars.Clear();
        currentColorSwitchers.Clear();
    }

    public void LoseGame()
    {
        //invoke the move to game over menu only after one second, so that the player has some time to react to what happened
        Invoke("MoveToGameOverMenu", 1f);
        StartCoroutine(ShakeCamera());
        PlaySound(loseGameSound);
    }

    //this method is invoked from LoseGame
    private void MoveToGameOverMenu()
    {
        state = GameState.GAME_OVER_MENU;
        highScoreMenuText.text = highScore + "";
        scoreMenuText.text = currentScore + "";
        totalStarsMenuText.text = totalStarsCollected + "";
        gameOverMenu.SetActive(true);
    }

    //the hand pointer prefab enabled here is set from the editor
    private void EnableHandPointer()
    {
        handPointer.SetActive(true);
    }

    private void DisableHandPointer()
    {
        handPointer.SetActive(false);
    }

    public void CollectStar()
    {
        //the collision can not happen with any other star than the oldest one
        GameObject destroyableStar = currentStars[0];
        //spawn new obstacle after every collected star
        SpawnObstacle();
        currentStars.RemoveAt(0);
        Instantiate(starCollectParticles, destroyableStar.transform.position, Quaternion.identity);
        //finally actually destroy the star object
        Destroy(destroyableStar);
        PlaySound(starSound);
        //increment the score every time the player collects a star
        IncrementScore();
        
    }

    public void CollectColorSwitcher()
    {
        //the collision can not happen with any other color switcher than the oldest one
        GameObject destroyableColorSwitcher = currentColorSwitchers[0];
        currentColorSwitchers.RemoveAt(0);
        Destroy(destroyableColorSwitcher);
        PlaySound(colorSwitcherSound);
    }

    private void IncrementScore()
    {
        //increment score counter by one
        currentScore++;
        //increment amount of total stars collected
        totalStarsCollected++;
        //save the amount of total stars collected right after
        PlayerPrefs.SetInt("TotalStars", totalStarsCollected);
        if(currentScore > highScore)
        {
            //if the current score is higher than the previous high score, save current score as the highest score
            highScore = currentScore;
            PlayerPrefs.SetInt("HighScore", highScore);
        }
        scoreText.text = currentScore + "";
        //change the background color if the score is high enough
        if(currentScore % 5 == 0) ChangeBackgroundColor();
    }

    private void ResetScore()
    {
        currentScore = 0;
        scoreText.text = currentScore + "";
    }

    private void SetHighScoreLinePosition()
    {
        //set the high score line to the position where the star for your next high score will be, if you get there
        //the highScore variable is multiplied by 15, because the current gap between every obstacle/star is always 15 units
        //this multiplier could possibly be a variable, especially if the gap between obstacles/stars could vary
        highScoreLine.transform.position = new Vector3(highScoreLine.transform.position.x, highScore * spaceBetweenObstacles, highScoreLine.transform.position.z);
    }

    public void PlaySound(AudioClip sound)
    {
        audioSource.PlayOneShot(sound);
    }

    private void ResetBackgroundColor()
    {
        backgroundColorIndex = 0;
        mainCamera.backgroundColor = backgroundColors[backgroundColorIndex];
    }

    //this method changes the background color every time the player has collected five more stars
    //the colors can be modified from the editor
    private void ChangeBackgroundColor()
    {
        if(backgroundColorIndex < backgroundColors.Count)
        {
           backgroundColorIndex++;
           StartCoroutine(LerpBackgroundColor());
        }
    }

    //background color lerped within a method that can be called from a coroutine, so no need to plug it to main update loop
    private IEnumerator LerpBackgroundColor()
    {
        float time = 0f;
        float duration = 1f;
        Color startColor = mainCamera.backgroundColor;

        while(time <= duration)
        {
            time += Time.deltaTime;
            mainCamera.backgroundColor = Color.Lerp(startColor, backgroundColors[backgroundColorIndex], time / duration);
            yield return null;
        }
    }

    //camera is shaken within a method that can be called from a coroutine, so no need to plug it to main update loop
    private IEnumerator ShakeCamera()
    {
        float time = 0.5f;
        Vector3 cameraStartPosition = mainCamera.transform.position;

        while(time >= 0)
        {
            time -= Time.deltaTime;
            //simplest way to shake a camera is to simply change its position inside a random unit sphere on every frame
            //shake amount is currently tied to the amount of time left
            mainCamera.transform.position = cameraStartPosition + (Random.insideUnitSphere * time);
            yield return null;
        }

        mainCamera.transform.position = cameraStartPosition;
    }
}

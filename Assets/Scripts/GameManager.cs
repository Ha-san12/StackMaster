using System.Collections;
using UnityEngine;
// WAJIB tambahkan ini di paling atas agar bisa restart scene lewat tombol Retry
using UnityEngine.SceneManagement; 

public class GameManager : MonoBehaviour {
    [Header("Block Settings")]
    [SerializeField] private Transform blockPrefab; 
    [SerializeField] private Transform blockHolder;

    [Header("UI Health (Hearts) Settings")]
    // Masukkan daftar gambar hati + teks kata "HEALTH" ke sini
    [SerializeField] private GameObject[] heartImages; 

    [Header("UI Score Components")]
    [SerializeField] private TMPro.TextMeshProUGUI hudScoreText; 
    [SerializeField] private TMPro.TextMeshProUGUI gameOverScoreText;

    [Header("UI Panels Management")]
    [SerializeField] private GameObject panelLobby;
    [SerializeField] private GameObject panelGameOver;
    [SerializeField] private GameObject panelCredits; // Slot untuk panel credit

    private Transform currentBlock = null;
    private Rigidbody2D currentRigidbody;

    private Vector2 blockStartPosition = new Vector2(0f, 4f);

    private float blockSpeed = 8f;
    private float blockSpeedIncrement = 0.5f;
    private int blockDirection = 1;
    private float xLimit = 5;

    private float timeBetweenRounds = 1f;

    private int startingLives = 3;
    private int livesRemaining;
    private int currentScore = 0;

    private bool playing = false; 

    private float currentCameraTargetY = 0f;
    private static bool isRestartingToGame = false;
    private float cameraYThreshold = 1f; 

    private readonly Vector2[][] tetrisShapes = new Vector2[][] {
        new Vector2[] { new Vector2(-1.5f, 0f), new Vector2(-0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1.5f, 0f) }, // I
        new Vector2[] { new Vector2(-0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-0.5f, -0.5f), new Vector2(0.5f, -0.5f) }, // O
        new Vector2[] { new Vector2(-1f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f) }, // T
        new Vector2[] { new Vector2(-1f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f) }, // L
        new Vector2[] { new Vector2(-1f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-1f, 1f) }, // J
        new Vector2[] { new Vector2(-1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) }, // S
        new Vector2[] { new Vector2(-1f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f) }  // Z
    };

    private readonly Color[] tetrisColors = new Color[] {
        new Color(0f, 0.9f, 0.9f, 1f),   // Cyan I
        new Color(0.9f, 0.9f, 0f, 1f),   // Kuning O
        new Color(0.6f, 0f, 0.9f, 1f),   // Ungu T
        new Color(0.9f, 0.5f, 0f, 1f),   // Orange L
        new Color(0.1f, 0.3f, 1f, 1f),   // Biru J
        new Color(0f, 0.9f, 0f, 1f),     // Hijau S
        new Color(0.9f, 0f, 0.1f, 1f)    // Merah Z
    };

    void Start() {
        livesRemaining = startingLives;
        currentScore = 0;

        if (hudScoreText != null) {
            hudScoreText.text = "0";
        }
        
        ResetHeartUI();
        
        if (isRestartingToGame) {
            isRestartingToGame = false;
            
            if (panelGameOver != null) panelGameOver.SetActive(false);
            if (panelLobby != null) panelLobby.SetActive(false); 
            if (panelCredits != null) panelCredits.SetActive(false); 
            
            if (hudScoreText != null) hudScoreText.gameObject.SetActive(true);
            SetHeartsVisibility(true);
            
            StartGame();
        } 
        else {
            if (panelLobby != null) panelLobby.SetActive(true);
            if (panelGameOver != null) panelGameOver.SetActive(false);
            if (panelCredits != null) panelCredits.SetActive(false);
            
            if (hudScoreText != null) hudScoreText.gameObject.SetActive(false);
            SetHeartsVisibility(false);
        }
    }

    public void StartGame() {
        if (panelLobby != null) panelLobby.SetActive(false);
        playing = true;
        
        if (hudScoreText != null) hudScoreText.gameObject.SetActive(true);
        SetHeartsVisibility(true);

        SpawnNewBlock();
    }

    public void RestartGame() {
        isRestartingToGame = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
    }

    public void GoToMainMenu() {
        isRestartingToGame = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
    }

    private void SpawnNewBlock() {
        GameObject[] blocks = GameObject.FindGameObjectsWithTag("GeneratedBlock");
        float highestY = currentCameraTargetY; 

        foreach (GameObject block in blocks) {
            if (block != null) {
                Rigidbody2D blockRb = block.GetComponent<Rigidbody2D>();
                if (blockRb != null && blockRb.simulated) {
                    if (block.transform.position.y > highestY) {
                        highestY = block.transform.position.y;
                    }
                }
            }
        }

        if (highestY > currentCameraTargetY + cameraYThreshold) {
            currentCameraTargetY = highestY - cameraYThreshold;
            CameraFollow camFollow = Camera.main.GetComponent<CameraFollow>();
            if (camFollow != null) {
                camFollow.SetTargetHeight(currentCameraTargetY);
            }
        }

        GameObject tetrisParent = new GameObject("TetrisBlock_Generated");
        tetrisParent.tag = "GeneratedBlock"; 

        Vector3 dynamicSpawnPosition = new Vector3(blockStartPosition.x, blockStartPosition.y + currentCameraTargetY, 0f);
        tetrisParent.transform.position = dynamicSpawnPosition;
        tetrisParent.transform.SetParent(blockHolder, true);

        Rigidbody2D rb = tetrisParent.AddComponent<Rigidbody2D>();
        rb.simulated = false; 
        
        CompositeCollider2D composite = tetrisParent.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        
        int randomShapeIndex = Random.Range(0, tetrisShapes.Length);
        Vector2[] chosenShape = tetrisShapes[randomShapeIndex];
        Color shapeColor = tetrisColors[randomShapeIndex];

        foreach (Vector2 pos in chosenShape) {
            Transform childBlock = Instantiate(blockPrefab, tetrisParent.transform);
            childBlock.localPosition = pos; 
            
            if (childBlock.TryGetComponent<SpriteRenderer>(out var sr)) {
                sr.color = shapeColor;
            }

            if (childBlock.TryGetComponent<BoxCollider2D>(out var boxCollider)) {
                boxCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            }
        }

        currentBlock = tetrisParent.transform;
        currentRigidbody = rb;

        blockSpeed += blockSpeedIncrement;
    }

    private IEnumerator DelayedSpawn() {
        yield return new WaitForSeconds(timeBetweenRounds);
        
        if (playing) {
            SpawnNewBlock();
        }
    }

    void Update() {
        if (currentBlock && playing) {
            float moveAmount = Time.deltaTime * blockSpeed * blockDirection;
            currentBlock.position += new Vector3(moveAmount, 0, 0);

            if (Mathf.Abs(currentBlock.position.x) > xLimit) {
                currentBlock.position = new Vector3(blockDirection * xLimit, currentBlock.position.y, 0);
                blockDirection = -blockDirection;
            }

            if (Input.GetKeyDown(KeyCode.Space)) { 
                currentBlock = null; 
                currentRigidbody.simulated = true; 
                
                currentScore += 10;
                if (hudScoreText != null) {
                    hudScoreText.text = $"{currentScore}";
                }

                StartCoroutine(DelayedSpawn()); 
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape)) {
            SceneManager.LoadScene(0);
        }
    }

    public void RemoveLife() {
        livesRemaining = Mathf.Max(livesRemaining - 1, 0); 
        
        if (heartImages != null && livesRemaining < heartImages.Length) {
            int heartToTurnOffIndex = livesRemaining; 
            if (heartImages[heartToTurnOffIndex] != null) {
                heartImages[heartToTurnOffIndex].SetActive(false);
            }
        }
        
        if (livesRemaining == 0) { 
            playing = false; 
            
            if (gameOverScoreText != null) {
                gameOverScoreText.text = $"{currentScore}";
            }
            
            // === DISAPU BERSIH PAS GAME OVER ===
            CleanUpGameComponents();

            if (panelGameOver != null) panelGameOver.SetActive(true); 
        }
    }

    public void OpenCredits() {
        if (panelLobby != null) panelLobby.SetActive(false);   
        if (panelCredits != null) panelCredits.SetActive(true);  
    }

    public void CloseCredits() {
        if (panelCredits != null) panelCredits.SetActive(false); 
        if (panelLobby != null) panelLobby.SetActive(true);    
    }

    private void ResetHeartUI() {
        if (heartImages != null) {
            foreach (GameObject heart in heartImages) {
                if (heart != null) heart.SetActive(true);
            }
        }
    }

    private void SetHeartsVisibility(bool visible) {
        if (heartImages != null) {
            foreach (GameObject heart in heartImages) {
                if (heart != null) heart.SetActive(visible);
            }
        }
    }

    // --- FUNGSI BARU: BERSIH-BERSIH TOTAL PAS KALAH ---
    private void CleanUpGameComponents() {
        // 1. Sembunyikan Skor In-Game (Biar gak tabrakan sama skor Game Over)
        if (hudScoreText != null) {
            hudScoreText.gameObject.SetActive(false);
        }

        // 2. Sembunyikan UI Health (Hati & Teks kata HEALTH)
        SetHeartsVisibility(false);

        // 3. Hancurkan Balok berjalan yang belum sempat dilepas (kalau ada)
        if (currentBlock != null) {
            Destroy(currentBlock.gameObject);
            currentBlock = null;
        }

        // 4. Hancurkan semua balok tumpukan yang ada di bawah langit-langit
        GameObject[] activeBlocks = GameObject.FindGameObjectsWithTag("GeneratedBlock");
        foreach (GameObject block in activeBlocks) {
            if (block != null) {
                Destroy(block);
            }
        }
    }
}
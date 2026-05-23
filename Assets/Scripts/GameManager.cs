using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour {
    [SerializeField] private Transform blockPrefab; 
    [SerializeField] private Transform blockHolder;
    [SerializeField] private TMPro.TextMeshProUGUI livesText;

    private Transform currentBlock = null;
    private Rigidbody2D currentRigidbody;

    // Jarak murni tempat spawn awal (4 unit di atas kamera)
    private Vector2 blockStartPosition = new Vector2(0f, 4f);

    private float blockSpeed = 8f;
    private float blockSpeedIncrement = 0.5f;
    private int blockDirection = 1;
    private float xLimit = 5;

    private float timeBetweenRounds = 1f;

    private int startingLives = 3;
    private int livesRemaining;
    private bool playing = true;

    // Nilai acuan untuk menggeser kamera (Stacktris Style)
    private float currentCameraTargetY = 0f;
    private float cameraYThreshold = 1f; // Batas toleransi tinggi tumpukan sebelum kamera disuruh naik

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
        livesText.text = $"{livesRemaining}";
        SpawnNewBlock();
    }

    private void SpawnNewBlock() {
        // --- LOGIKA ADJUSTMENT KAMERA & SPAWN POINT (STACKTRIS STYLE) ---
        // Cari tahu balok diam mana yang posisinya paling tinggi di dalam game saat ini
        GameObject[] blocks = GameObject.FindGameObjectsWithTag("GeneratedBlock");
        float highestY = currentCameraTargetY; 

        foreach (GameObject block in blocks) {
            if (block != null) {
                Rigidbody2D blockRb = block.GetComponent<Rigidbody2D>();
                // Pastikan hanya mengecek balok yang SUDAH dijatuhkan player
                if (blockRb != null && blockRb.simulated) {
                    if (block.transform.position.y > highestY) {
                        highestY = block.transform.position.y;
                    }
                }
            }
        }

        // Jika balok tertinggi sudah mulai mendekati batas tengah layar kamera saat ini
        if (highestY > currentCameraTargetY + cameraYThreshold) {
            // Hitung target posisi baru kamera. Kita naikkan kamera setinggi tumpukan tersebut melampaui threshold
            currentCameraTargetY = highestY - cameraYThreshold;
            
            // Perintahkan script CameraFollow untuk bergeser naik ke target baru
            CameraFollow camFollow = Camera.main.GetComponent<CameraFollow>();
            if (camFollow != null) {
                camFollow.SetTargetHeight(currentCameraTargetY);
            }
        }

        // Bikin balok baru di atas posisi target kamera saat ini agar jarak kosongnya selalu konsisten
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
        SpawnNewBlock();
    }

    void Update() {
        if (currentBlock && playing) {
            // Pergerakan kanan-kiri sekarang adaptif mengikuti koordinat kamera saat ini
            float moveAmount = Time.deltaTime * blockSpeed * blockDirection;
            currentBlock.position += new Vector3(moveAmount, 0, 0);

            if (Mathf.Abs(currentBlock.position.x) > xLimit) {
                currentBlock.position = new Vector3(blockDirection * xLimit, currentBlock.position.y, 0);
                blockDirection = -blockDirection;
            }

            if (Input.GetKeyDown(KeyCode.Space)) {
                currentBlock = null;
                currentRigidbody.simulated = true;
                StartCoroutine(DelayedSpawn());
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape)) {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }

    public void RemoveLife() {
        livesRemaining = Mathf.Max(livesRemaining - 1, 0);
        livesText.text = $"{livesRemaining}";
        if (livesRemaining == 0) {
            playing = false;
        }
    }
}
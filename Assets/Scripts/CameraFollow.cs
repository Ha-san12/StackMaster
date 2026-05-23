using UnityEngine;

public class CameraFollow : MonoBehaviour {
    [SerializeField] private float smoothSpeed = 5f; // Kecepatan kamera naik ke posisi baru
    private float targetY = 0f;

    void Start() {
        // Posisi awal kamera saat game mulai
        targetY = transform.position.y;
    }

    void LateUpdate() {
        // Kamera bergerak secara smooth hanya ke posisi targetY yang diminta GameManager
        Vector3 targetPosition = new Vector3(transform.position.x, targetY, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
    }

    // Fungsi ini bakal dipanggil oleh GameManager tiap kali tumpukan meninggi
    public void SetTargetHeight(float newHeight) {
        // Pastikan kamera cuma bisa naik, gak bisa turun
        if (newHeight > targetY) {
            targetY = newHeight;
        }
    }
}
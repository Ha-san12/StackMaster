using UnityEngine;

public class LoseLife : MonoBehaviour {
  [SerializeField] private GameManager gameManager;

  private void OnTriggerEnter2D(Collider2D collision) {
    gameManager.RemoveLife();
  }
}
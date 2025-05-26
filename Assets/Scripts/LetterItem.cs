using UnityEngine;

public class LetterItem : MonoBehaviour
{
    public char letter; // 该道具代表的字母

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                player.CollectLetter(letter);
            }
            Destroy(gameObject);
        }
    }
}
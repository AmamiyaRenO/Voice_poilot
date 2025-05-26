using UnityEngine;

public class Spawner : MonoBehaviour
{
    public Pipes prefab;
    public float spawnRate = 1f;
    public float minHeight = -1f;
    public float maxHeight = 2f;
    public float verticalGap = 3f;

    public GameObject[] letterPrefabs; 

    private char[] rushOrder = new char[] { 'R', 'U', 'S', 'H' };
    private int nextLetterIndex = 0;

    public void SetGap(float gap)
    {
        verticalGap = gap;
    }

    private void OnEnable()
    {
        InvokeRepeating(nameof(Spawn), spawnRate, spawnRate);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(Spawn));
    }

    private void Spawn()
    {
        Pipes pipes = Instantiate(prefab, transform.position, Quaternion.identity);
        pipes.transform.position += Vector3.up * Random.Range(minHeight, maxHeight);
        pipes.gap = verticalGap;

        // 只要没收集完rush，就有概率生成当前目标字母
        float rushLetterProbability = 0.3f; // 30%概率
        if (letterPrefabs != null && letterPrefabs.Length == 4 && nextLetterIndex < rushOrder.Length && Random.value < rushLetterProbability)
        {
            char letterToSpawn = rushOrder[nextLetterIndex];
            GameObject prefabToSpawn = null;
            foreach (var go in letterPrefabs)
            {
                var letterItem = go.GetComponent<LetterItem>();
                if (letterItem != null && letterItem.letter == letterToSpawn)
                {
                    prefabToSpawn = go;
                    break;
                }
            }
            if (prefabToSpawn != null)
            {
                Vector3 letterPos = pipes.transform.position + Vector3.right * 1.5f;
                GameObject letterObj = Instantiate(prefabToSpawn, letterPos, Quaternion.identity, pipes.transform);
            }
            // 注意：这里不再递增 nextLetterIndex
        }
    }

    public void ResetRushLetter()
    {
        nextLetterIndex = 0;
    }

    // 新增方法，供Player调用
    public void NextRushLetter()
    {
        nextLetterIndex++;
    }
}

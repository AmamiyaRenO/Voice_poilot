using UnityEngine;

public class Parallax : MonoBehaviour
{
    public float animationSpeed = 1f;
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    private void Update()
    {
        float speed = animationSpeed * Player.RushWorldSpeedMultiplier;
        meshRenderer.material.mainTextureOffset += new Vector2(speed * Time.deltaTime, 0);
    }

}

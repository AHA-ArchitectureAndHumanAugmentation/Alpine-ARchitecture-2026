using UnityEngine;

public class SphereReaction : MonoBehaviour
{
    public AudioClip hitSound;
    public Color hitColor = Color.red;

    private AudioSource audioSource;
    private Renderer sphereRenderer;

    private void Start()
    {
        // Another way is to add a component by code like so:
        audioSource = gameObject.AddComponent<AudioSource>();
        sphereRenderer = GetComponent<Renderer>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Ouch! I hit " + collision.gameObject.name);

        sphereRenderer.material.color = hitColor;
        audioSource.PlayOneShot(hitSound);
    }
}

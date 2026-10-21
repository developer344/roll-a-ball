using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerController : MonoBehaviour
{
    // Rigidbody of the player.
    private Rigidbody rb;
    public GameObject winTextObject;

    // Movement along X and Y axes.
    private float movementX;
    private float movementY;

    // Speed at which the player moves.
    public float speed = 10;

    public TextMeshProUGUI countText;

    private int count;

    // Sound played when a pickup is collected.
    public AudioClip pickupSound;
    private AudioSource audioSource;

    // Music looped during gameplay. It stops on win, and on lose when the player is destroyed.
    public AudioClip gameplayMusic;
    public float musicVolume = 0.5f;
    private AudioSource musicSource;

    // Start is called before the first frame update.
    void Start()
    {
        // Get and store the Rigidbody component attached to the player.
        rb = GetComponent<Rigidbody>();
        // Never let the Rigidbody sleep, so the enemy can still hit a player standing still.
        rb.sleepThreshold = 0.0f;
        audioSource = GetComponent<AudioSource>();
        // Light blue arrow floating above the player.
        MarkerArrow.Create(transform, new Color(0.1f, 0.45f, 1.0f));
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        // Fall back to Assets/Sounds/Resources/pickup.mp3 when nothing is set in the Inspector.
        if (pickupSound == null) pickupSound = Resources.Load<AudioClip>("pickup");
        if (pickupSound == null)
        {
            Debug.LogWarning("PlayerController: no Pickup Sound assigned, so collecting moons will be silent.");
        }
        else
        {
            // The clip doesn't preload, so load it now rather than on the first pickup.
            pickupSound.LoadAudioData();
        }

        // Fall back to Assets/Sounds/Resources/gameplay_soundtrack.mp3 when nothing is set in the Inspector.
        if (gameplayMusic == null) gameplayMusic = Resources.Load<AudioClip>("gameplay_soundtrack");
        if (gameplayMusic != null)
        {
            // A separate source so the music can be stopped without cutting off pickup sounds.
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.clip = gameplayMusic;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }

        count = 0;
        SetCountText();
        winTextObject.SetActive(false);
    }

    // This function is called when a move input is detected.
    void OnMove(InputValue movementValue)
    {
        // Convert the input value into a Vector2 for movement.
        Vector2 movementVector = movementValue.Get<Vector2>();

        // Store the X and Y components of the movement.
        movementX = movementVector.x;
        movementY = movementVector.y;

    }

    void SetCountText()
    {
        countText.text = "Moons Collected\n" + count.ToString() + "/12";
        if (count >= 12)
        {
            if (musicSource != null) musicSource.Stop();
            // Go to the win screen scene.
            EndScreen.ShowWin();
        }
    }

    // FixedUpdate is called once per fixed frame-rate frame.
    void FixedUpdate()
    {
        // Create a 3D movement vector using the X and Y inputs.
        Vector3 movement = new Vector3(movementX, 0.0f, movementY);

        // Apply force to the Rigidbody to move the player.
        rb.AddForce(movement * speed);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("PickUp"))
        {
            other.gameObject.SetActive(false);
            if (pickupSound != null) audioSource.PlayOneShot(pickupSound);
            count = count + 1;
            SetCountText();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // Destroy the current object
            Destroy(gameObject);
            // Go to the bloody "You Lose!" screen scene.
            EndScreen.ShowLose();
        }
    }
}
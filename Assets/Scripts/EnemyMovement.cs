using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    public Transform player;
    private NavMeshAgent navMeshAgent;
    private PlayerController playerController;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        // Faster or slower depending on the difficulty chosen in the start menu.
        navMeshAgent.speed *= GameSettings.EnemySpeedMultiplier;
        navMeshAgent.acceleration *= GameSettings.EnemySpeedMultiplier;
        // Red arrow floating above the enemy.
        MarkerArrow.Create(transform, new Color(1.0f, 0.2f, 0.2f));
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (player != null)
        {
            navMeshAgent.SetDestination(player.position);
        }
    }
}

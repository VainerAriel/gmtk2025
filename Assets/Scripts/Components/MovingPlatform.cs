using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Vector2 pointA;
    public Vector2 pointB;
    public float speed = 2f;

    private Vector2 target;

    private PlayerController player;

    void Start()
    {
        player = FindObjectOfType<PlayerController>();
        transform.position = pointA;
        target = pointB;
    }

    void Update()
    {
        transform.position = Vector2.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (Vector2.Distance(transform.position, target) < 0.01f)
        {
            target = (target == pointA) ? pointB : pointA;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(pointA, 0.1f);
        Gizmos.DrawSphere(pointB, 0.1f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(pointA, pointB);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && player.CheckGrounded())
        {
            Debug.Log("on platform");
            collision.gameObject.transform.parent = transform;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !player.CheckGrounded())
        {
            collision.gameObject.transform.parent = null;
        }
    }
}

using System.Collections;
using UnityEngine;

public class PushableBlock : MonoBehaviour
{
    public LayerMask obstacleLayer; // Layer for tilemap with obstacles
    [SerializeField] private float moveSpeed = 5f;

    private bool isMoving = false;
    private Vector3 originalPosition; // New variable to store the initial position

    public float fallSpeed = 2f;
    public float checkDistance = 0.1f;

    private bool isFalling = false;
    private Vector3 targetPosition;

    private float fallStartX;

    void Start()
    {
        // Store the block's initial position
        originalPosition = transform.position;
    }

    void Update()
    {
        if (!isFalling)
        {
            if (!IsGrounded())
            {
                StartFalling();
            }
        }
        else
        {
            FallStep();
        }
    }

    bool IsGrounded()
    {
        Vector2 boxCenter = (Vector2)transform.position + Vector2.down * 0.51f; // just below
        Vector2 boxSize = new Vector2(0.8f, 0.1f); // wide and shallow box
        Collider2D hit = Physics2D.OverlapBox(boxCenter, boxSize, 0f, obstacleLayer);

        Debug.DrawLine(boxCenter + Vector2.left * 0.4f, boxCenter + Vector2.right * 0.4f, Color.red, 0.1f);
        // Debug.Log($"IsGrounded: {hit != null} at position {transform.position}");

        return hit != null;
    }

    void StartFalling()
    {
        isFalling = true;
        fallStartX = transform.position.x; // lock in x-position
        targetPosition = transform.position + Vector3.down; // one grid unit down
    }

    void FallStep()
    {
        // Move down while keeping X constant
        Vector3 nextPos = new Vector3(fallStartX, transform.position.y - fallSpeed * Time.deltaTime, 0);
        transform.position = Vector3.MoveTowards(transform.position, nextPos, fallSpeed * Time.deltaTime);

        // If reached target cell
        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            if (!IsGrounded())
            {
                targetPosition += Vector3.down; // queue another step
            }
            else
            {
                // Snap to grid to avoid floating point imprecision
                Vector3 pos = transform.position;
                pos.x = Mathf.Round(pos.x - 0.5f) + 0.5f;
                pos.y = Mathf.Round(pos.y - 0.5f) + 0.5f;
                transform.position = pos;

                isFalling = false;
            }
        }
    }

    public bool TryMove(Vector2 dir)
    {
        if (isMoving || isFalling || !CanMove(dir)) return false;

        Vector3 targetPos = transform.position + (Vector3)dir;

        // Snap to grid (centered)
        targetPos.x = Mathf.Round(targetPos.x - 0.5f) + 0.5f;
        targetPos.y = Mathf.Round(targetPos.y - 0.5f) + 0.5f;

        // Check if the destination is blocked
        Collider2D hit = Physics2D.OverlapBox(targetPos, new Vector2(0.8f, 0.8f), 0f, obstacleLayer);
        if (hit == null)
        {
            StartCoroutine(MoveToPosition(targetPos));
            return true;
        }

        return false;
    }

    private IEnumerator MoveToPosition(Vector3 targetPos)
    {
        isMoving = true;

        while ((targetPos - transform.position).sqrMagnitude > 0.001f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPos; // Final snap to avoid tiny offsets
        Debug.Log($"Block moved to {targetPos}");
        isMoving = false;
    }

    private bool CanMove(Vector2 direction)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, 1f, obstacleLayer);
        return hit.collider == null;
    }

    // Public method to reset the block's position
    public void ResetPosition()
    {
        transform.position = originalPosition;
    }
}

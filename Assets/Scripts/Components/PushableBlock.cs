using System.Collections;
using UnityEngine;

public class PushableBlock : MonoBehaviour
{
    public LayerMask obstacleLayer; // Layer for tilemap with obstacles
    [SerializeField] private float moveSpeed = 5f;

    private bool isMoving = false;
    private Vector3 originalPosition; // New variable to store the initial position

    void Start()
    {
        // Store the block's initial position
        originalPosition = transform.position;
    }

    public bool TryMove(Vector2 dir)
    {
        if (isMoving || !CanMove(dir)) return false;

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

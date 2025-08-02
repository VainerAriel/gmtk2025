using UnityEngine;

public class PushableBlock : MonoBehaviour
{
    public LayerMask obstacleLayer;   // Layer for tilemap with obstacles

    public bool TryMove(Vector2 dir)
    {
        Vector3 targetPos = transform.position + (Vector3)dir;

        targetPos.x = Mathf.Round(targetPos.x - 0.5f) + 0.5f;
        targetPos.y = Mathf.Round(targetPos.y - 0.5f) + 0.5f;

        Collider2D hit = Physics2D.OverlapBox(targetPos, new Vector2(0.8f, 0.8f), 0f, obstacleLayer);
        if (hit == null)
        {
            transform.position = targetPos;
            Debug.Log($"Block moved to {targetPos}");
            return true;
        }
        return false;
    }
}

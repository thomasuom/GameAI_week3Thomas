using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HunterMovement : MonoBehaviour
{
    private const float WallClearance = 0f;

    [SerializeField] private Transform player;
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private float slowingRadius = 2f;

    private Vector2 velocity;
    private Vector2 wanderDirection;
    private float nextWanderChangeTime;
    private Collider2D hunterCollider;
    private ContactFilter2D obstacleFilter;
    private readonly RaycastHit2D[] obstacleHits = new RaycastHit2D[8];

    private void Awake()
    {
        hunterCollider = GetComponent<Collider2D>();
        obstacleFilter = new ContactFilter2D { useTriggers = false };
        obstacleFilter.SetLayerMask(Physics2D.DefaultRaycastLayers);
        SetWanderDirection();
    }

    private Vector2 Arrive(Transform target)
    {
        if (target == null)
            return Vector2.zero;

        Vector2 toTarget = new Vector2(target.position.x, target.position.y) - new Vector2(transform.position.x, transform.position.y);
        float distance = toTarget.magnitude;
        if (distance <= Mathf.Epsilon)
            return -velocity;

        float speed = maxSpeed * Mathf.Min(distance / Mathf.Max(slowingRadius, Mathf.Epsilon), 1f);
        Vector2 desired = toTarget.normalized * speed;
        return desired - velocity;
    }

    private void HunterUpdate()
    {
        Vector2 steering = player != null && Vector2.Distance(new Vector2(transform.position.x, transform.position.y), new Vector2(player.position.x, player.position.y)) < detectionRadius
            ? Arrive(player)
            : Wander();

        ApplySteering(steering);
    }

    private Vector2 Wander()
    {
        if (Time.time >= nextWanderChangeTime)
            SetWanderDirection();

        return wanderDirection * maxSpeed - velocity;
    }

    private void SetWanderDirection()
    {
        Vector2 direction = Random.insideUnitCircle.normalized;
        wanderDirection = new Vector2(direction.x, direction.y);
        nextWanderChangeTime = Time.time + 2f;
    }

    private void ApplySteering(Vector2 steering)
    {
        velocity = Vector2.ClampMagnitude(velocity + steering * Time.deltaTime, maxSpeed);
        Vector2 displacement = MoveWithoutCrossingWalls(velocity * Time.deltaTime);
        transform.position += (Vector3)displacement;

        if (Time.deltaTime > 0.0001f)
            velocity = displacement / Time.deltaTime;
    }

    private Vector2 MoveWithoutCrossingWalls(Vector2 displacement)
    {
        Vector2 position = transform.position;
        Vector2 remaining = displacement;
        float radius = Mathf.Max(hunterCollider.bounds.extents.x, hunterCollider.bounds.extents.y) + WallClearance;

        for (int i = 0; i < 2 && remaining.sqrMagnitude > Mathf.Epsilon; i++)
        {
            float distance = remaining.magnitude;
            Vector2 direction = remaining / distance;
            int hitCount = Physics2D.CircleCast(position, radius, direction, obstacleFilter, obstacleHits, distance);

            if (hitCount == 0)
            {
                position += remaining;
                break;
            }

            RaycastHit2D closestHit = obstacleHits[0];
            for (int j = 1; j < hitCount; j++)
            {
                if (obstacleHits[j].distance < closestHit.distance)
                    closestHit = obstacleHits[j];
            }

            float travelDistance = Mathf.Max(closestHit.distance - WallClearance, 0f);
            Vector2 travelled = direction * travelDistance;
            position += travelled;
            remaining -= travelled;
            remaining -= Vector2.Dot(remaining, closestHit.normal) * closestHit.normal;
        }

        return position - (Vector2)transform.position;
    }

    private void Update()
    {
        HunterUpdate();
    }
}

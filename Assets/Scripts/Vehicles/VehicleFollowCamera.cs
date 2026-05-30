using UnityEngine;

public sealed class VehicleFollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 3.2f, -7f);
    [SerializeField] private float followSharpness = 12f;
    [SerializeField] private float lookAhead = 2.2f;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.TransformPoint(localOffset);
        float blend = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, blend);

        Vector3 focus = target.position + target.forward * lookAhead;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(focus - transform.position, Vector3.up),
            blend);
    }
}

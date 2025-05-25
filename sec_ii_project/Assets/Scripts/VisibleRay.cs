using UnityEngine;
public class VisibleRay : MonoBehaviour
{
    public Vector3 origin;
    public Vector3 direction;
    public float length = 10f;
    private LineRenderer lineRenderer;
    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }
    private void Update()
    {
        // Set the line renderer's positions
        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, origin + direction.normalized * length);
    }
}
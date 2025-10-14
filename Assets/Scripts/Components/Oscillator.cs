using UnityEngine;

public class Oscillator : MonoBehaviour
{
    [SerializeField] private Vector3 direction = Vector3.up; // Movement direction
    [SerializeField] private float amplitude = 1f;           // How far it moves
    [SerializeField] private float frequency = 1f;           // How fast it oscillates
    [SerializeField] private bool useLocalSpace = false;     // Toggle between world/local

    private Vector3 startPos;

    private void Start()
    {
        startPos = useLocalSpace ? transform.localPosition : transform.position;
    }

    private void Update()
    {
        Oscillate();
    }

    private void Oscillate()
    {
        float offset = Mathf.Sin(Time.time * frequency) * amplitude;
        Vector3 targetPos = startPos + direction.normalized * offset;

        if (useLocalSpace)
            transform.localPosition = targetPos;
        else
            transform.position = targetPos;
    }
}

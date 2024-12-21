using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class VideoTest : MonoBehaviour
{
    private Vector3 m_target = Vector3.forward;

    [FormerlySerializedAs("secondsInterval")]
    [SerializeField]
    private float m_secondsInterval = 3f;

    private void Start()
    {
        StartCoroutine(SetTarget());
    }

    private void Update()
    {
        transform.rotation = Quaternion.LerpUnclamped(transform.rotation, Quaternion.LookRotation(m_target), 0.1f);
    }

    private IEnumerator SetTarget()
    {
        while (true){
            yield return new WaitForSeconds(m_secondsInterval);
            m_target = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f)).normalized;
        }
    }
}
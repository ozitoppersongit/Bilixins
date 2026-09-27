using UnityEngine;
using UnityEngine.SceneManagement;

public class EncounterZone : MonoBehaviour
{
    [Range(0f, 1f)]
    [SerializeField] float chanceDeEncontro = 0.1f;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (Random.value <= chanceDeEncontro)
            SceneManager.LoadScene("Battle");
    }
}

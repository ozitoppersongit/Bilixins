using UnityEngine;

public class MissionCompleteScene : MonoBehaviour
{
    public void Close()
    {
        Destroy(transform.root.gameObject);
    }
}

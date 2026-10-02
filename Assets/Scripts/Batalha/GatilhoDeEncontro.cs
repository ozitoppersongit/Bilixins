using UnityEngine;
using UnityEngine.Events;
using Unity.Cinemachine;

public class GatilhoDeEncontro : MonoBehaviour
{
    public UnityEvent aoEncontrarPlayer;

    public BilixinsData bilixinInimigo;

    [Header("Batalha")]
    public GameObject battlePrefab;

    private Collider gatilho;

    void Awake()
    {
        gatilho = GetComponent<Collider>();
    }

    void OnTriggerEnter(Collider other)
    {

        if (!other.CompareTag("Player"))
            return;

        Debug.Log($"GatilhoDeEncontro: player encontrou '{gameObject.name}'.");

        if (gatilho != null)
            gatilho.enabled = false;


        // Inicia a batalha
        IniciarBatalha();
    }

    void IniciarBatalha()
    {
        if (battlePrefab == null)
        {
            Debug.LogError("BattlePrefab não foi configurado no GatilhoDeEncontro!");
            return;
        }

        if (bilixinInimigo == null)
        {
            Debug.LogError("Nenhum BilixinsData foi atribuído a este inimigo!");
            return;
        }

        Transform battleParent = GameObject.Find("Batalha").transform;

        GameObject batalha = Instantiate(
            battlePrefab,
            battleParent
        );

        BattleScript battleScript =
            batalha.GetComponent<BattleScript>();

        if (battleScript == null)
        {
            Debug.LogError(
                "BattlePrefab não possui um BattleScript!"
            );
            return;
        }

        battleScript.IniciarBatalha(
            bilixinInimigo,
            this
        );
        
    }

    public void Vencer()
    {
        Destroy(gameObject);
    }

    public void Reativar()
    {
        if (gatilho != null) {gatilho.enabled = true;}

    }
}
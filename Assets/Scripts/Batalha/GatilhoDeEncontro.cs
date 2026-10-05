using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using Unity.Cinemachine;

public class GatilhoDeEncontro : MonoBehaviour
{
    public UnityEvent aoEncontrarPlayer;

    public BilixinsData bilixinInimigo;

    [Header("Batalha")]
    public GameObject battlePrefab;
    private GerenciadorBatalha gB;
    private bool batalhaIniciada = false;

    private Collider gatilho;

    void Awake()
    {
        gatilho = GetComponent<Collider>();

        gB = Object.FindAnyObjectByType<GerenciadorBatalha>();
    }

    private void OnTriggerEnter(Collider other)
{
    if (!other.CompareTag("Player"))
        return;

    if (batalhaIniciada)
        return;

    batalhaIniciada = true;

    Debug.Log($"GatilhoDeEncontro: player encontrou '{gameObject.name}'.");

    if (gatilho != null)
        gatilho.enabled = false;

    StartCoroutine(IniciarBatalhaComTransicao());
}

    private IEnumerator IniciarBatalhaComTransicao()
{
    // A bolinha cresce e cobre a tela
    yield return StartCoroutine(
        TransicaoBatalha.instancia.Abrir()
    );

    // Instancia a batalha
    GameObject batalha = Instantiate(battlePrefab);

    BattleScript battleScript =
        batalha.GetComponent<BattleScript>();

    battleScript.IniciarBatalha(
        bilixinInimigo,
        this
    );

    // A bolinha diminui revelando a batalha
    yield return StartCoroutine(
        TransicaoBatalha.instancia.Fechar()
    );
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
        gB.BilixinDerrotado();
        Destroy(gameObject);
    }

    public void Reativar()
    {
        if (gatilho != null) {gatilho.enabled = true;}

    }
}
using UnityEngine;

public class BattleTrigger : MonoBehaviour
{
    [Header("Batalha")]
    public GameObject battlePrefab;

    [Header("Adversário")]
    public BilixinsData bilixinInimigo;

    private bool ativado = false;

    private void OnTriggerEnter(Collider other)
    {
        if (ativado)
            return;

        if (other.CompareTag("Player"))
        {
            ativado = true;

            IniciarBatalha();
        }
    }

    void IniciarBatalha()
    {
        GameObject batalha = Instantiate(battlePrefab);

        BattleScript battleScript = batalha.GetComponent<BattleScript>();

        battleScript.bilixinInimigo = bilixinInimigo;
    }
}
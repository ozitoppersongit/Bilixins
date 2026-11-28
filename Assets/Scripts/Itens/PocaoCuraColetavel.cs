using UnityEngine;

[DefaultExecutionOrder(1000)]
public sealed class PocaoCuraColetavel : MonoBehaviour
{
    [SerializeField] private ItemData pocao;
    [SerializeField] private SpriteRenderer icone;
    [SerializeField] private Material brilho;

    private bool coletada;

    private void Start()
    {
        icone.sprite = pocao.icone;
        ItemInfinito.CriarBrilho(transform, brilho, new Color(1f, 1f, 1f, 0.85f));
    }

    private void LateUpdate()
    {
        Camera camera = Camera.main;
        if (camera != null)
            icone.transform.rotation = camera.transform.rotation;
    }

    private void OnTriggerEnter(Collider outro)
    {
        if (coletada || !outro.GetComponentInParent<PlayerMovement>())
            return;

        InventoryManager inventario = InventoryManager.instancia;
        if (inventario == null || !inventario.TentarAdicionarItem(pocao))
            return;

        coletada = true;
        Missoes.MostrarAvisoColeta("Você encontrou 1x Poção de Cura!");
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}

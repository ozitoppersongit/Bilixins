using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemStack
{
    public ItemData item;
    public int quantidade;
}

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager instancia;

    [Header("Inventário")]
    public List<ItemStack> itens = new List<ItemStack>();

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AdicionarItem(ItemData item, int quantidade = 1)
    {
        if (item == null || quantidade <= 0)
            return;

        ItemStack existente = itens.Find(x => x.item == item);

        if (existente != null)
        {
            existente.quantidade += quantidade;
        }
        else
        {
            itens.Add(new ItemStack
            {
                item = item,
                quantidade = quantidade
            });
        }
    }

    public bool RemoverItem(ItemData item, int quantidade = 1)
    {
        if (item == null || quantidade <= 0)
            return false;

        ItemStack existente = itens.Find(x => x.item == item);

        if (existente == null || existente.quantidade < quantidade)
            return false;

        existente.quantidade -= quantidade;

        if (existente.quantidade <= 0)
            itens.Remove(existente);

        return true;
    }

    public int ConsultarQuantidade(ItemData item)
    {
        if (item == null)
            return 0;

        ItemStack existente = itens.Find(x => x.item == item);

        return existente != null ? existente.quantidade : 0;
    }

    public List<ItemStack> ObterItens()
    {
        return itens;
    }
}
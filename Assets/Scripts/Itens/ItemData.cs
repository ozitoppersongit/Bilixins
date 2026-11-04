using UnityEngine;

[CreateAssetMenu(fileName = "Novo Item", menuName = "Bilixin/Item")]
public class ItemData : ScriptableObject
{
    [Header("Informações")]
    public string nome;
    [TextArea] public string descricao;
    public Sprite icone;

    [Header("Configurações")]
    public bool consumivel = true;
    public ItemEffect efeito;
}
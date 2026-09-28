using UnityEngine;

[CreateAssetMenu(fileName = "Bilixin", menuName = "Bilixin/Bilixin Data")]
public class BilixinsData : ScriptableObject
{
    public string nome;
    public int nivel;
    public int ataque;
    public int defesa;
    public TipoLixo tipo;
    public Sprite spr;
}

public enum TipoLixo
{
    Organico,
    Metal,
    Vidro,
    Plastico,
    Papel
}

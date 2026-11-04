
using UnityEngine;

[System.Serializable]
public class AtaqueBilixin
{
    public string nome;
    public float poder = 40f;
}

[CreateAssetMenu(fileName = "Bilixin", menuName = "Bilixin/Bilixin Data")]
public class BilixinsData : ScriptableObject
{
    [Header("Informações")]
    public string nome;
    public int hp;
    public int nivel;
    public int ataque;
    public int defesa;
    public TipoLixo tipo;
    public Sprite spr;

    [Header("Ataques")]
    public AtaqueBilixin ataque1 = new AtaqueBilixin();
    public AtaqueBilixin ataque2 = new AtaqueBilixin();
}

public enum TipoLixo
{
    Organico,
    Metal,
    Vidro,
    Plastico,
    Papel
}

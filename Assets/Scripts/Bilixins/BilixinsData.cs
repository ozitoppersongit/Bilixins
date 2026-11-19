
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
    public int HpNoNivel(int level) => Mathf.Max(1, hp + 5 * (Mathf.Max(1, level) - 1));
    public int AtaqueNoNivel(int level) => Mathf.Max(1, ataque + 2 * (Mathf.Max(1, level) - 1));
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

using UnityEngine;

public static class RetornoSeguro
{
    public static Pose ObterPose(Transform pontoConfigurado, Pose spawnInicial)
    {
        return pontoConfigurado != null
            ? new Pose(pontoConfigurado.position, pontoConfigurado.rotation)
            : spawnInicial;
    }
}
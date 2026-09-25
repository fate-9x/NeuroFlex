using UnityEngine;

[CreateAssetMenu(fileName = "LanConfig", menuName = "NeuroFlex/LAN Config")]
public class LanConfig : ScriptableObject
{
    [Tooltip("Puerto UDP donde el dashboard escucha discovery")]
    public int discoveryPort = 48100;

    [Tooltip("Timeout en ms para esperar respuesta UDP")]
    public int discoveryTimeoutMs = 3000;

    [Tooltip("Payload enviado por broadcast")]
    public string discoveryMagic = "NEUROFLEX_DISCOVER";

    [Tooltip("Prefijo esperado en la respuesta del dashboard")]
    public string expectedResponsePrefix = "NEUROFLEX_HERE";

    [Tooltip("Intervalo inicial de reintento de discovery en segundos")]
    public float discoveryRetryInterval = 3f;

    [Tooltip("Intervalo de polling para pair/session status")]
    public float pollInterval = 2f;

    [Tooltip("Tiempo máximo de espera para confirmación de emparejamiento")]
    public float pairingTimeout = 180f;
}

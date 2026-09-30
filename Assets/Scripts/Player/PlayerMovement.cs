using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float speed = 5f;

    void Update()
    {
        Keyboard teclado = Keyboard.current;
        if (teclado == null) return;

        float h = 0f, v = 0f;
        if (teclado.aKey.isPressed || teclado.leftArrowKey.isPressed)  h -= 1f;
        if (teclado.dKey.isPressed || teclado.rightArrowKey.isPressed) h += 1f;
        if (teclado.sKey.isPressed || teclado.downArrowKey.isPressed)  v -= 1f;
        if (teclado.wKey.isPressed || teclado.upArrowKey.isPressed)    v += 1f;

        Vector3 dir = DirecaoDaCamera(h, v);
        transform.Translate(dir * speed * Time.deltaTime, Space.World);
    }

    Vector3 DirecaoDaCamera(float h, float v)
    {
        Camera cam = Camera.main;
        if (cam == null) return new Vector3(h, 0f, v).normalized;

        Vector3 frente = cam.transform.forward;
        Vector3 direita = cam.transform.right;
        frente.y = 0f;
        direita.y = 0f;
        frente.Normalize();
        direita.Normalize();

        return (direita * h + frente * v).normalized;
    }
}

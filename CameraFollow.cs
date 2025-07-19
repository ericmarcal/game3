using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour
{
    [Header("Configuração do Alvo")]
    public Transform target;

    [Header("Configurações de Movimento")]
    [Range(0.01f, 1.0f)]
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(0, 0, -10);

    private Camera mainCamera;
    private float originalSize;
    private Coroutine zoomCoroutine;
    private Coroutine panCoroutine;
    private Bounds currentBounds;
    private bool hasBounds = false;
    private bool isPanning = false;

    private void Awake()
    {
        mainCamera = GetComponent<Camera>();
    }

    private void Start()
    {
        if (mainCamera != null)
        {
            originalSize = mainCamera.orthographicSize;
        }
    }

    void LateUpdate()
    {
        if (target != null && !isPanning)
        {
            // Começa com a posição ideal do alvo.
            Vector3 targetPosition = target.position + offset;

            // Se tivermos limites, calcula a posição final, caso contrário, usa a posição do alvo.
            Vector3 finalPosition = hasBounds ? CalculateClampedPosition(targetPosition) : targetPosition;

            // Suaviza o movimento até à posição final.
            transform.position = Vector3.Lerp(transform.position, finalPosition, smoothSpeed);
        }
    }

    // << LÓGICA DE LIMITES CENTRALIZADA E CORRIGIDA >>
    private Vector3 CalculateClampedPosition(Vector3 targetPosition)
    {
        if (!hasBounds) return targetPosition;

        float camHeight = mainCamera.orthographicSize;
        float camWidth = camHeight * mainCamera.aspect;

        // Calcula os limites para o *centro* da câmara.
        float minX = currentBounds.min.x + camWidth;
        float maxX = currentBounds.max.x - camWidth;
        float minY = currentBounds.min.y + camHeight;
        float maxY = currentBounds.max.y - camHeight;

        // Começa com a posição da câmara no centro da sala.
        Vector3 newPos = currentBounds.center;

        // Se a sala for mais larga que a câmara, permite o movimento horizontal.
        if (currentBounds.size.x > camWidth * 2f)
        {
            newPos.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        }

        // Se a sala for mais alta que a câmara, permite o movimento vertical.
        if (currentBounds.size.y > camHeight * 2f)
        {
            newPos.y = Mathf.Clamp(targetPosition.y, minY, maxY);
        }

        newPos.z = transform.position.z; // Mantém a posição Z original.
        return newPos;
    }

    public void SnapToTarget()
    {
        if (target == null) return;

        // Teleporta o jogador, depois calcula a posição final da câmara e "salta" para lá.
        Vector3 targetPosition = target.position + offset;
        transform.position = hasBounds ? CalculateClampedPosition(targetPosition) : targetPosition;
    }

    // O resto dos métodos permanece o mesmo.
    public void PanToPosition(Vector3 targetPosition, float duration) { if (panCoroutine != null) StopCoroutine(panCoroutine); panCoroutine = StartCoroutine(PanRoutine(targetPosition, duration)); }
    private IEnumerator PanRoutine(Vector3 targetPosition, float duration) { isPanning = true; float timer = 0f; Vector3 startPosition = transform.position; Vector3 finalTarget = new Vector3(targetPosition.x, targetPosition.y, transform.position.z); while (timer < duration) { timer += Time.deltaTime; float progress = Mathf.SmoothStep(0.0f, 1.0f, timer / duration); transform.position = Vector3.Lerp(startPosition, finalTarget, progress); yield return null; } transform.position = finalTarget; isPanning = false; }
    public void SetBounds(Bounds newBounds) { currentBounds = newBounds; hasBounds = true; }
    public void ClearBounds() { hasBounds = false; }
    public void SetTarget(Transform newTarget) { this.target = newTarget; }
    public void SetZoom(float newSize, float duration) { if (mainCamera == null) return; if (zoomCoroutine != null) StopCoroutine(zoomCoroutine); zoomCoroutine = StartCoroutine(ZoomRoutine(newSize, duration)); }
    private IEnumerator ZoomRoutine(float targetSize, float duration) { float timer = 0f; float startSize = mainCamera.orthographicSize; while (timer < duration) { timer += Time.deltaTime; mainCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, timer / duration); yield return null; } mainCamera.orthographicSize = targetSize; }
    public void ResetZoom(float duration) { SetZoom(originalSize, duration); }
}
// Renombra este script a "TopoControlador.cs" o algo similar
using System.Collections;
using UnityEngine;

public class movimientos : MonoBehaviour 
{   
    [Header("Imagenes")]
    [SerializeField] private Sprite FENNEC;
    [SerializeField] private Sprite FENNECCGOLPEADO;
    
    [Header("Posiciones")]
    [SerializeField] private Vector2 posInicial = new Vector2(0.5f, -5.2f);
    [SerializeField] private Vector2 posFinal = new Vector2 (0.5f, 0.5f);
    
    [Header("Tiempos")]
    [Tooltip("Tiempo que tarda en subir O en bajar")]
    [SerializeField] private float tiempoDeMovimiento = 0.5f; // Renombré 'duracionFennecVisible'
    
    [Tooltip("Tiempo que se queda visible arriba (si no lo golpean)")]
    [SerializeField] private float tiempoVisible = 1f; // Renombré 'duracion'

    // --- Referencias ---
    private SpriteRenderer spriteVisible;
    private GameManager gameManager; // Referencia al Jefe
    private bool golpeable = false; // Empieza NO golpeable

    private void Awake()
    {
        spriteVisible = GetComponent<SpriteRenderer>();
        // Asegurarse de que empieza oculto y con el sprite normal
        transform.localPosition = posInicial;
        spriteVisible.sprite = FENNEC;
    }

    // 1. ELIMINAMOS LA FUNCIÓN START()
    
    // 2. CREAMOS UNA FUNCIÓN PÚBLICA que el GameManager llamará
    public void Salir(GameManager manager)
    {
        this.gameManager = manager;
        
        // Resetea el estado
        spriteVisible.sprite = FENNEC;
        golpeable = true; // Ahora sí es golpeable
        
        // Inicia la rutina de salir
        StartCoroutine(MuestraEsconde(posInicial, posFinal));
    }

    private IEnumerator MuestraEsconde(Vector2 inicio, Vector2 final) 
    {
        // --- Subida ---
        transform.localPosition = inicio;
        float tiempoTranscurrido = 0f;
        while (tiempoTranscurrido < tiempoDeMovimiento)
        {
            transform.localPosition = Vector2.Lerp(inicio, final, tiempoTranscurrido / tiempoDeMovimiento);
            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = final; // Asegura la posición final

        // --- Espera Arriba ---
        yield return new WaitForSeconds(tiempoVisible);

        // --- Bajada (si no fue golpeado) ---
        tiempoTranscurrido = 0f;
        while (tiempoTranscurrido < tiempoDeMovimiento)
        {
            transform.localPosition = Vector2.Lerp(final, inicio, tiempoTranscurrido / tiempoDeMovimiento);
            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = inicio; // Asegura la posición inicial
        
        golpeable = false; // Ya no se puede golpear
    }

    private void OnMouseDown()
    {
        // 3. SOLO REACCIONA SI ES GOLPEABLE
        if(golpeable)
        {
            golpeable = false; // Ya no se puede volver a golpear
            
            // 4. AVISA AL GAMEMANAGER QUE SUME PUNTOS
            gameManager.AddScore(1); // (O los puntos que quieras)

            // Cambia el sprite
            spriteVisible.sprite = FENNECCGOLPEADO;
            
            // Detiene la corutina MuestraEsconde (para que no se baje sola)
            StopAllCoroutines(); 
            
            // Inicia la corutina de esconderse por golpe
            StartCoroutine(postGolpe());
        }
    }

    private IEnumerator postGolpe()
    {
        // Espera un momento con la cara de golpeado
        yield return new WaitForSeconds(0.5f); 
        
        // Rutina de bajada rápida
        float tiempoTranscurrido = 0f;
        Vector2 posActual = transform.localPosition; // Baja desde donde esté
        
        while (tiempoTranscurrido < (tiempoDeMovimiento / 2)) // Baja más rápido
        {
            transform.localPosition = Vector2.Lerp(posActual, posInicial, tiempoTranscurrido / (tiempoDeMovimiento / 2));
            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }
        
        transform.localPosition = posInicial; // Asegura la posición inicial
        
        // 5. IMPORTANTE: Resetea el sprite para la próxima vez
        spriteVisible.sprite = FENNEC; 
    }
}
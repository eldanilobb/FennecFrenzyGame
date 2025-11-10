using System.Collections;
using UnityEngine;

public class movimientosNiveles : MonoBehaviour 
{   
    // --- (Tus enums y sprites se quedan igual) ---
    public enum TipoDeTopo { Normal, Especial }
    private GameManagerNiveles.TipoDeTopo tipoActual = GameManagerNiveles.TipoDeTopo.Normal; 
    [Header("Sprites Normales")]
    [SerializeField] private Sprite FENNEC_NORMAL; 
    [SerializeField] private Sprite FENNEC_NORMAL_GOLPEADO; 
    [Header("Sprites Especiales")]
    [SerializeField] private Sprite FENNEC_ESPECIAL;
    [SerializeField] private Sprite FENNEC_ESPECIAL_GOLPEADO;
    
    // <-- CAMBIO: Estas son las alturas DENTRO del hoyo
    [Header("Alturas de Movimiento (Local)")]
    [SerializeField] private float yOculto = 0f; // Posición Y cuando está oculto (ej: 0)
    [SerializeField] private float yVisible = 1.5f; // Posición Y cuando está visible (ej: 1.5)

    [Header("Tiempos de Movimiento")] 
    [SerializeField] private float duracionMostrarEsconder = 0.5f;
    [SerializeField] private float duracionVisibleArriba = 1f; 

    // --- (Variables de componentes) ---
    private SpriteRenderer spriteVisible;
    private BoxCollider2D hitbox;
    // <-- CAMBIO: Ya no necesitamos las variables de hitbox dinámico, 
    // puedes simplemente activar/desactivar el collider. Es más simple.
    // private Vector2 offsetMostrado;
    // private Vector2 tamanoMostrado;
    // private Vector2 offsetEscondido;
    // private Vector2 tamanoEscondido;
    
    private int indiceFennec; 
    private bool golpeable = false; 
    private GameManagerNiveles gameManagerQueMeActivo;

    private void Awake() {
        spriteVisible = GetComponent<SpriteRenderer>();
        hitbox = GetComponent<BoxCollider2D>();
        
        // <-- CAMBIO: El topo empieza oculto en su 'yOculto'
        transform.localPosition = new Vector2(0, yOculto); 
        hitbox.enabled = false; // El collider empieza desactivado
        
        // Ya no necesitamos guardar la X ni calcular offsets
    }

    public void ActivarFennec(GameManagerNiveles manager, int indice, GameManagerNiveles.TipoDeTopo tipo) 
    {
        this.gameManagerQueMeActivo = manager; 
        this.tipoActual = tipo; 
        this.indiceFennec = indice; 
        
        if (tipoActual == GameManagerNiveles.TipoDeTopo.Normal)
        {
            spriteVisible.sprite = FENNEC_NORMAL;
        }
        else
        {
            spriteVisible.sprite = FENNEC_ESPECIAL;
        }
        
        golpeable = true;
        hitbox.enabled = true; // Activa el collider
        StopAllCoroutines(); 
        StartCoroutine(MostrarEsconder());
    }

    public bool EstaOcupado()
    {
        // Está ocupado si no está en la posición inicial (yOculto)
        return transform.localPosition.y > yOculto;
    }

    private IEnumerator MostrarEsconder() 
    {
        // Define las posiciones INICIO y FINAL basadas en las alturas
        Vector2 posDeInicio = new Vector2(0, yOculto);
        Vector2 posDeFinal = new Vector2(0, yVisible);

        // --- Mostrar ---
        transform.localPosition = posDeInicio;
        float tiempoTranscurrido = 0f;
        while (tiempoTranscurrido < duracionMostrarEsconder) {
            transform.localPosition = Vector2.Lerp(posDeInicio, posDeFinal, tiempoTranscurrido / duracionMostrarEsconder);
            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = posDeFinal;

        yield return new WaitForSeconds(duracionVisibleArriba); 

        // --- Esconder (si no fue golpeado) ---
        if (golpeable) 
        {
            tiempoTranscurrido = 0f;
            while (tiempoTranscurrido < duracionMostrarEsconder) {
                transform.localPosition = Vector2.Lerp(posDeFinal, posDeInicio, tiempoTranscurrido / duracionMostrarEsconder);
                tiempoTranscurrido += Time.deltaTime;
                yield return null;
            }
            
            esconder(); // Llama a la función de esconder para limpiar

            if(gameManagerQueMeActivo != null) {
                gameManagerQueMeActivo.Perdido(indiceFennec, tipoActual);
            }
        }
    }

    private void OnMouseDown() {
        if(GameManagerNiveles.juegopausado){return;}
        if(golpeable) {
            golpeable = false; 
            hitbox.enabled = false; // Desactiva el collider al ser golpeado
            
            if (tipoActual == GameManagerNiveles.TipoDeTopo.Normal)
            {
                spriteVisible.sprite = FENNEC_NORMAL_GOLPEADO;
            }
            else
            {
                spriteVisible.sprite = FENNEC_ESPECIAL_GOLPEADO;
            }

            StopAllCoroutines(); 
            StartCoroutine(EsconderRapido());
            
            if(gameManagerQueMeActivo != null) {
                gameManagerQueMeActivo.TopoGolpeado(indiceFennec, tipoActual); 
            }
        }
    }

    private IEnumerator EsconderRapido() {
        yield return new WaitForSeconds(0.25f);
        
        // Rutina de bajada rápida
        float tiempoTranscurrido = 0f;
        Vector2 posActual = transform.localPosition;
        Vector2 posDeInicio = new Vector2(0, yOculto);
        float duracionBajada = duracionMostrarEsconder / 2f; // Baja más rápido
        
        while (tiempoTranscurrido < duracionBajada)
        {
            transform.localPosition = Vector2.Lerp(posActual, posDeInicio, tiempoTranscurrido / duracionBajada);
            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }
        
        esconder();
    }

    public void esconder() {
        transform.localPosition = new Vector2(0, yOculto);
        hitbox.enabled = false; 
        golpeable = false; 
        spriteVisible.sprite = FENNEC_NORMAL; 
    }

    public void DetenerJuego() {
        StopAllCoroutines();
        golpeable = false;
        esconder(); 
    }
}
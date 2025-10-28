using System.Collections;
using UnityEngine;

public class movimientos : MonoBehaviour 
{   
    [Header("Imagenes")]
    [SerializeField] private Sprite FENNEC;
    [SerializeField] private Sprite FENNECCGOLPEADO;

    [Header("GameManager")]
    [SerializeField] private GameManager gameManager;
    
    private Vector2 posInicial = new Vector2(0.5f, -5.2f);
    private Vector2 posFinal = new Vector2(0.5f, 0.5f);
    
    private float duracionMostrarEsconder = 0.5f;
    private float duracion = 1f;

    private SpriteRenderer spriteVisible;
    private BoxCollider2D hitbox;
    private Vector2 offsetMostrado;
    private Vector2 tamanoMostrado;
    private Vector2 offsetEscondido;
    private Vector2 tamanoEscondido;
    private int indiceFennec = 0;
    private bool golpeable = true;

    private void Awake() {
        spriteVisible = GetComponent<SpriteRenderer>();
        hitbox = GetComponent<BoxCollider2D>();
        
        offsetMostrado = hitbox.offset;
        tamanoMostrado = hitbox.size;
        offsetEscondido = new Vector2(offsetMostrado.x, -posInicial.y / 2f);
        tamanoEscondido = new Vector2(tamanoMostrado.x, 0f);
    }

    private IEnumerator MostrarEsconder(Vector2 inicio, Vector2 final) 
    {
        // Mostrar
        transform.localPosition = inicio;
        float tiempoTranscurrido = 0f;
        
        while (tiempoTranscurrido < duracionMostrarEsconder) {
            transform.localPosition = Vector2.Lerp(inicio, final, tiempoTranscurrido / duracionMostrarEsconder);
            hitbox.offset = Vector2.Lerp(offsetEscondido, offsetMostrado, tiempoTranscurrido / duracionMostrarEsconder);
            hitbox.size = Vector2.Lerp(tamanoEscondido, tamanoMostrado, tiempoTranscurrido / duracionMostrarEsconder);
            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = final;
        hitbox.offset = offsetMostrado;
        hitbox.size = tamanoMostrado;

        yield return new WaitForSeconds(duracion);

        // Esconder
        tiempoTranscurrido = 0f;
        
        while (tiempoTranscurrido < duracionMostrarEsconder) {
            transform.localPosition = Vector2.Lerp(final, inicio, tiempoTranscurrido / duracionMostrarEsconder);
            hitbox.offset = Vector2.Lerp(offsetMostrado, offsetEscondido, tiempoTranscurrido / duracionMostrarEsconder);
            hitbox.size = Vector2.Lerp(tamanoMostrado, tamanoEscondido, tiempoTranscurrido / duracionMostrarEsconder);       
            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = inicio;
        hitbox.offset = offsetEscondido;
        hitbox.size = tamanoEscondido;

        // Notificar al GameManager que este fennec desapareció
        if(gameManager != null) {
            gameManager.Perdido(indiceFennec);
        }
    }

    private void OnMouseDown() {
        if(golpeable) {
            spriteVisible.sprite = FENNECCGOLPEADO;
            StopAllCoroutines();
            StartCoroutine(EsconderRapido());
            golpeable = false;
            gameManager.aumentarPuntaje(indiceFennec);
            
            if(gameManager != null) {
                gameManager.aumentarPuntaje(indiceFennec);
            }
        }
    }

    private IEnumerator EsconderRapido() {
        yield return new WaitForSeconds(0.25f);

        if(!golpeable) {
            esconder();
            
            // Notificar al GameManager que este fennec desapareció
            if(gameManager != null) {
                gameManager.Perdido(indiceFennec);
            }
        }
    }

    public void esconder() {
        transform.localPosition = posInicial;
        hitbox.offset = offsetEscondido;
        hitbox.size = tamanoEscondido;
    }

    public void Activate(int nivel) {
        spriteVisible.sprite = FENNEC;
        golpeable = true;
        StartCoroutine(MostrarEsconder(posInicial, posFinal));
    }

    public void DetenerJuego() {
        StopAllCoroutines();
        golpeable = false;
    }

    public void actualizarIndice(int indice) {
        indiceFennec = indice;
    }
}

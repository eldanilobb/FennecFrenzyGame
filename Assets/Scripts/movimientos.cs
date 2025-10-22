using System.Collections;
using UnityEngine;

public class movimientos : MonoBehaviour 
{   
    //referencias a los sprites del fennec
    [Header("Imagenes")]
    [SerializeField] private Sprite FENNEC;
    [SerializeField] private Sprite FENNECCGOLPEADO;
    
    //Vector2(posicion en x, posicion en Y)
    private Vector2 posInicial = new Vector2(0.5f, -5.2f);
    private Vector2 posFinal = new Vector2 (0.5f, 0.5f);
    
    private float duracionFennecVisible = 1f; //Modificar si neceita aumentar o disminuir la velocidad de movimiento
    private float duracion = 1f; //Modificar si necesita mantenerse mas tiempo arriba (modificar numero, mantener f)

    private SpriteRenderer spriteVisible;
    private bool golpeable = true; //Si es que es posible pegarle al fennec en este momento

    //de aca mejor no tocar nada :)
    private IEnumerator MuestraEsconde(Vector2 inicio, Vector2 final) 
    {
        transform.localPosition = inicio;

        float tiempoTranscurrido = 0f;
        while (tiempoTranscurrido < duracionFennecVisible)
        {
            transform.localPosition = Vector2.Lerp(inicio, final, tiempoTranscurrido / duracionFennecVisible);
            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = final;

        yield return new WaitForSeconds(duracion);

        tiempoTranscurrido = 0f;
        while (tiempoTranscurrido < duracionFennecVisible)
        {
            transform.localPosition = Vector2.Lerp(final, inicio, tiempoTranscurrido / duracionFennecVisible);
            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = inicio;
    }

    private void OnMouseDown(){
        if(golpeable){
        spriteVisible.sprite = FENNECCGOLPEADO;
        StopAllCoroutines();
        StartCoroutine(postGolpe());
        golpeable = false; //Hace que no se pueda golpear al fennec
        }
    }

    private IEnumerator postGolpe(){
        yield return new WaitForSeconds(0.8f);

        if(!golpeable) {esconder();}
    }

    private void esconder(){
    transform.localPosition = posInicial;
    }


    private void Awake(){
        spriteVisible = GetComponent<SpriteRenderer>();
    }

    private void Start() 
    {
        StartCoroutine(MuestraEsconde(posInicial, posFinal));
    }
}

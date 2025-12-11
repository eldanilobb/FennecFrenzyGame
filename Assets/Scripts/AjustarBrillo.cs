using UnityEngine;
using UnityEngine.UI;
public class AjustarBrillo : MonoBehaviour
{
    public Slider slider;
    public float silderValue;
    public Image panelBrillo;

    void Start()
    {
        slider.value = PlayerPrefs.GetFloat("brillo", 0.5f);
        panelBrillo.color = new Color(panelBrillo.color.r, panelBrillo.color.g, panelBrillo.color.b, slider.value);
        
    }

    void Update()
    {
        
    }
    public void ChangeSlider(float valor)
    {
        silderValue = valor;
        PlayerPrefs.SetFloat("brillo", silderValue);
        panelBrillo.color = new Color(panelBrillo.color.r, panelBrillo.color.g, panelBrillo.color.b, slider.value);
    }
}
